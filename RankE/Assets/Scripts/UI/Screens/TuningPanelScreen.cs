using System;
using System.Collections.Generic;
using System.Reflection;
using RankE.Game;
using RankE.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// In-game combat-feel tuning, the controller-friendly twin of the Combat Tuning editor
    /// window. Pokes the very same runtime data the window does — <see cref="TuningProfile.Active"/>
    /// (sim numbers each fight clones from) and the <see cref="AbilityVfxRegistry"/> feel knobs —
    /// so you can tweak without alt-tabbing to Unity. Opened from the pause menu.
    ///
    /// Sim edits apply on the NEXT fight: "Apply &amp; Restart" calls <see cref="MatchController.RestartFight"/>,
    /// which rebuilds the battle off a fresh clone of the profile (running fights stay a clean
    /// deterministic replay). VFX-knob edits hit the loaded registry instance and so apply live.
    /// Edits are in-memory only — they reset on play-exit; use the editor window's "Copy values"
    /// to keep good numbers. Pure view: reads/writes presentation+tuning data, no gameplay logic.
    /// </summary>
    public sealed class TuningPanelScreen : MonoBehaviour
    {
        MatchController match;
        GameObject root;
        Button applyButton;

        /// <summary>Raised when the user dismisses the panel via Close (not a state-change hide),
        /// so the host can hand controller selection back to the pause menu.</summary>
        public System.Action Closed;

        // Each stepper registers a closure that re-reads its value into the label, so "Reset"
        // and re-opening refresh the whole list at once.
        readonly List<Action> refreshers = new List<Action>();

        static readonly Color HeaderColor = new Color(1f, 0.86f, 0.5f);
        static readonly Color LabelColor = new Color(0.86f, 0.86f, 0.9f);

        public void Init(MatchController match, Transform parent)
        {
            this.match = match;

            var panel = UiFactory.Panel("TuningPanel", parent, new Color(0f, 0f, 0f, 0.78f));
            panel.raycastTarget = true; // covers the pause menu behind it while open
            UiFactory.PlaceStretch((RectTransform)panel.transform);
            root = panel.gameObject;

            var box = UiFactory.Frame("TuningBox", panel.transform);
            UiFactory.PlaceFixed((RectTransform)box.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1120f, 900f));

            var title = UiFactory.Label("Title", box.transform, "COMBAT TUNING", 40, HeaderColor);
            UiFactory.PlaceFixed((RectTransform)title.transform, new Vector2(0.5f, 1f),
                new Vector2(0f, -18f), new Vector2(1000f, 56f));

            // Scroll list fills the box between the title and the footer buttons.
            UiFactory.ScrollView("TuningScroll", box.transform, out var content);
            var vp = (RectTransform)content.parent;
            vp.anchorMin = Vector2.zero;
            vp.anchorMax = Vector2.one;
            vp.offsetMin = new Vector2(22f, 96f);
            vp.offsetMax = new Vector2(-22f, -84f);

            BuildRows(content);
            BuildFooter(box.transform);

            root.SetActive(false);
        }

        // --- content ---

        void BuildRows(Transform content)
        {
            var profile = TuningProfile.Active;

            AddHeader(content, "Combat globals");
            foreach (var f in typeof(CombatTuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(int))
                    AddStepper(content, ObjectNames(f.Name),
                        () => (int)f.GetValue(profile.Tuning),
                        v => f.SetValue(profile.Tuning, (int)Math.Round(v)), 1, true);
                else if (f.FieldType == typeof(double))
                    AddStepper(content, ObjectNames(f.Name),
                        () => (double)f.GetValue(profile.Tuning),
                        v => f.SetValue(profile.Tuning, v), 0.05, false);
                // string ids (status/ability lookups) aren't feel knobs — skipped, like the window.
            }

            AddHeader(content, "Per-ability (ticks · 20/s)");
            var ids = new List<string>(profile.Abilities.Keys);
            ids.Sort();
            foreach (var id in ids)
            {
                var def = profile.Abilities[id];
                if (def == null) continue;
                AddSubHeader(content, def.Name ?? id);
                AddIntField(content, "Cooldown", () => def.CooldownTicks, v => def.CooldownTicks = v);
                AddIntField(content, "Cast", () => def.CastTicks, v => def.CastTicks = v);
                AddIntField(content, "Delay", () => def.DelayTicks, v => def.DelayTicks = v);
                AddIntField(content, "Pre-lock", () => def.PreLockTicks, v => def.PreLockTicks = v);
                AddIntField(content, "Post-lock", () => def.PostLockTicks, v => def.PostLockTicks = v);

                var dmg = FirstAmountEffect(def);
                if (dmg != null)
                    AddIntField(content, dmg.Kind == EffectKinds.BreakDamage ? "Break amount" : "Damage",
                        () => dmg.Amount, v => dmg.Amount = v);
            }

            var vfx = AbilityVfxRegistry.Load();
            if (vfx != null)
            {
                AddHeader(content, "Presentation — VFX feel (live)");
                AddFloatField(content, "VFX scale", () => vfx.VfxScale, v => vfx.VfxScale = v, 0.05f);
                AddFloatField(content, "Travel seconds", () => vfx.DefaultTravelSeconds, v => vfx.DefaultTravelSeconds = v, 0.02f);
                AddFloatField(content, "Fall height", () => vfx.FallHeight, v => vfx.FallHeight = v, 0.25f);
                AddFloatField(content, "Chest height", () => vfx.ChestHeight, v => vfx.ChestHeight = v, 0.05f);
                AddFloatField(content, "Cue lifetime", () => vfx.CueLifetime, v => vfx.CueLifetime = v, 0.25f);
            }
        }

        static EffectDef FirstAmountEffect(AbilityDef def)
        {
            for (int i = 0; i < def.Effects.Count; i++)
            {
                var e = def.Effects[i];
                if (e != null && (e.Kind == EffectKinds.Damage || e.Kind == EffectKinds.BreakDamage))
                    return e;
            }
            return null;
        }

        void BuildFooter(Transform box)
        {
            applyButton = UiFactory.TextButton("Apply", box, "APPLY & RESTART", 28, () => match.RestartFight());
            UiFactory.PlaceFixed((RectTransform)applyButton.transform, new Vector2(0.5f, 0f),
                new Vector2(-330f, 22f), new Vector2(380f, 60f));

            var reset = UiFactory.TextButton("Reset", box, "RESET", 28, ResetAndRefresh);
            UiFactory.PlaceFixed((RectTransform)reset.transform, new Vector2(0.5f, 0f),
                new Vector2(50f, 22f), new Vector2(220f, 60f));

            var close = UiFactory.TextButton("Close", box, "CLOSE", 28, () => { Show(false); Closed?.Invoke(); });
            UiFactory.PlaceFixed((RectTransform)close.transform, new Vector2(0.5f, 0f),
                new Vector2(330f, 22f), new Vector2(220f, 60f));
        }

        void ResetAndRefresh()
        {
            TuningProfile.Active.ResetToDefaults();
            RefreshAll();
        }

        void RefreshAll()
        {
            for (int i = 0; i < refreshers.Count; i++) refreshers[i]();
        }

        public void Show(bool on)
        {
            root.SetActive(on);
            if (!on) return;
            RefreshAll();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(applyButton.gameObject);
        }

        public bool IsOpen => root != null && root.activeSelf;

        // --- row builders ---

        void AddIntField(Transform parent, string label, Func<int> get, Action<int> set) =>
            AddStepper(parent, label, () => get(), v => set((int)Math.Round(v)), 1, true);

        void AddFloatField(Transform parent, string label, Func<float> get, Action<float> set, float step) =>
            AddStepper(parent, label, () => get(), v => set((float)v), step, false);

        void AddStepper(Transform parent, string label, Func<double> get, Action<double> set,
            double step, bool isInt)
        {
            var row = AddRow(parent, 46f);
            var lbl = UiFactory.Label("L", row, label, 24, LabelColor, TextAnchor.MiddleLeft);
            Flexible(lbl.rectTransform);

            var dec = StepButton(row, "−"); // minus sign
            var valT = UiFactory.Label("V", row, "", 26, Color.white);
            FixedWidth(valT.rectTransform, 150f);
            var inc = StepButton(row, "+");

            Action refresh = () => valT.text = isInt
                ? ((int)Math.Round(get())).ToString()
                : get().ToString("0.###");
            Action down = () => { set(get() - step); refresh(); };
            Action up = () => { set(get() + step); refresh(); };

            dec.onClick.AddListener(() => down());
            inc.onClick.AddListener(() => up());
            dec.gameObject.AddComponent<HoldRepeat>().OnRepeat = down;
            inc.gameObject.AddComponent<HoldRepeat>().OnRepeat = up;

            refresh();
            refreshers.Add(refresh);
        }

        Button StepButton(Transform parent, string glyph)
        {
            var b = UiFactory.TextButton("Step", parent, glyph, 30, null);
            FixedWidth((RectTransform)b.transform, 64f);
            return b;
        }

        void AddHeader(Transform parent, string text)
        {
            var row = AddRow(parent, 56f);
            var lbl = UiFactory.Label("H", row, text.ToUpperInvariant(), 30, HeaderColor, TextAnchor.MiddleLeft);
            Flexible(lbl.rectTransform);
        }

        void AddSubHeader(Transform parent, string text)
        {
            var row = AddRow(parent, 40f);
            var lbl = UiFactory.Label("S", row, text, 26, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleLeft);
            Flexible(lbl.rectTransform);
        }

        static RectTransform AddRow(Transform parent, float height)
        {
            var row = UiFactory.Rect("Row", parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleLeft;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return row;
        }

        static void Flexible(RectTransform rt)
        {
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
        }

        static void FixedWidth(RectTransform rt, float w)
        {
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.minWidth = w;
            le.flexibleWidth = 0f;
        }

        // Tiny local copy of UnityEditor.ObjectNames.NicifyVariableName (editor-only API),
        // so field labels read "Gcd Ticks" not "GcdTicks" at runtime.
        static string ObjectNames(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var sb = new System.Text.StringBuilder(name.Length + 4);
            sb.Append(char.ToUpperInvariant(name[0]));
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c) && !char.IsUpper(name[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
