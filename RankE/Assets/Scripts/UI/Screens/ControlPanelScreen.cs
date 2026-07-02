using System;
using System.Collections.Generic;
using RankE.Game;
using RankE.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// The pre-fight gameplay control panel (replaces the old debug loadout picker). Edits the
    /// live <see cref="TuningProfile.Active"/> — global rules, both fighter builds (stats, gear,
    /// six freely editable ability slots) — and the chosen visuals (incl. the enemy monster),
    /// each row carrying a short description. A preset bar saves/loads named fixtures
    /// (early/mid/late-game fights) to disk via <see cref="TuningPresetStore"/>. The "Edit
    /// abilities" button opens the <see cref="AbilitiesEditorScreen"/> for per-ability numbers.
    ///
    /// Pure view: it pokes tuning/loadout data and starts the match, but computes no gameplay.
    /// Edits apply on the next fight (each fight clones the profile), so "Start fight" is when a
    /// change takes effect.
    /// </summary>
    public sealed class ControlPanelScreen : MonoBehaviour
    {
        readonly TuningWidgets w = new TuningWidgets();

        MatchController match;
        DebugLoadout loadout;
        AbilitiesEditorScreen abilities;
        CharacterCreatorScreen creator;

        GameObject root;
        RectTransform content;
        Button startButton;
        TextEntryField nameField;
        Text presetStatus;

        List<string> presetNames = new List<string>();
        int presetCursor = -1;

        public void SetAbilitiesScreen(AbilitiesEditorScreen screen) => abilities = screen;
        public void SetCreator(CharacterCreatorScreen creator) => this.creator = creator;

        public void Init(MatchController match, Transform parent)
        {
            this.match = match;
            this.loadout = match.Loadout;

            var panel = UiFactory.Frame("ControlPanel", parent);
            UiFactory.PlaceStretch((RectTransform)panel.transform);
            root = panel.gameObject;

            var title = UiFactory.Label("Title", panel.transform, "FIGHT SETUP", 46, Color.white);
            UiFactory.PlaceFixed((RectTransform)title.transform, new Vector2(0.5f, 1f),
                new Vector2(0f, -28f), new Vector2(700f, 56f));

            BuildPresetBar(panel.transform);

            var scroll = UiFactory.ScrollView("ControlScroll", panel.transform, out content);
            scroll.scrollSensitivity = 0.75f; // gentle wheel/trackpad steps for the long list
            scroll.inertia = false;         // no momentum overshoot while reading rows
            scroll.gameObject.AddComponent<ScrollFollowSelection>().Init(scroll);
            var vp = (RectTransform)content.parent;
            vp.anchorMin = Vector2.zero;
            vp.anchorMax = Vector2.one;
            vp.offsetMin = new Vector2(60f, 130f);
            vp.offsetMax = new Vector2(-60f, -160f);

            try { BuildContent(content); }
            catch (Exception e) { Debug.LogError("ControlPanel BuildContent failed: " + e); }

            BuildFooter(panel.transform);

            root.SetActive(false);
        }

        // --- preset bar ---

        void BuildPresetBar(Transform parent)
        {
            var bar = UiFactory.Rect("PresetBar", parent);
            UiFactory.PlaceFixed(bar, new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(1720f, 60f));
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleCenter;

            BarLabel(bar, "Preset:", 100);
            BarButton(bar, "<", 56, () => CyclePreset(-1));
            nameField = BuildNameField(bar, 320);
            BarButton(bar, ">", 56, () => CyclePreset(+1));
            BarButton(bar, "SAVE", 120, SavePreset);
            BarButton(bar, "LOAD", 120, LoadPreset);
            BarButton(bar, "DELETE", 140, DeletePreset);
            BarButton(bar, "NEW", 110, NewPreset);
            BarButton(bar, "SET STARTUP", 190, SaveAsStartup);
            presetStatus = BarLabel(bar, "", 320);
            presetStatus.alignment = TextAnchor.MiddleLeft;
            presetStatus.color = new Color(0.7f, 0.85f, 1f);
        }

        Text BarLabel(Transform parent, string text, float width)
        {
            var t = UiFactory.Label("BL", parent, text, 24, TuningWidgets.LabelColor, TextAnchor.MiddleRight);
            SetWidth(t.rectTransform, width);
            return t;
        }

        void BarButton(Transform parent, string label, float width, UnityEngine.Events.UnityAction onClick)
        {
            var b = UiFactory.TextButton("PB", parent, label, 24, onClick);
            SetWidth((RectTransform)b.transform, width);
        }

        TextEntryField BuildNameField(Transform parent, float width)
        {
            var img = UiFactory.Panel("NameField", parent, new Color(0.1f, 0.1f, 0.14f, 0.97f));
            img.raycastTarget = true;
            SetWidth((RectTransform)img.transform, width);
            var t = UiFactory.Label("Text", img.transform, "", 24, Color.white, TextAnchor.MiddleLeft);
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(12f, 0f);
            t.rectTransform.offsetMax = new Vector2(-12f, 0f);
            var field = img.gameObject.AddComponent<TextEntryField>();
            field.Init(t, "type a name…");
            return field;
        }

        static void SetWidth(RectTransform rt, float w)
        {
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.minWidth = w;
            le.flexibleWidth = 0f;
        }

        void RefreshPresetList()
        {
            presetNames = TuningPresetStore.List();
            presetCursor = nameField != null ? presetNames.IndexOf(nameField.Value) : -1;
        }

        void CyclePreset(int dir)
        {
            RefreshPresetList();
            if (presetNames.Count == 0) { SetStatus("No saved presets."); return; }
            presetCursor = LoadoutPools.Wrap(presetCursor < 0 ? (dir > 0 ? 0 : -1) : presetCursor + dir, presetNames.Count);
            nameField.Value = presetNames[presetCursor];
            SetStatus("");
        }

        void SavePreset()
        {
            var name = nameField.Value;
            if (string.IsNullOrWhiteSpace(name)) { SetStatus("Enter a name first."); return; }
            TuningPresetStore.Save(name, TuningPreset.Capture(TuningProfile.Active, loadout));
            RefreshPresetList();
            SetStatus("Saved \"" + name + "\".");
        }

        void LoadPreset()
        {
            var name = nameField.Value;
            var preset = string.IsNullOrWhiteSpace(name) ? null : TuningPresetStore.Load(name);
            if (preset == null) { SetStatus("No preset \"" + name + "\"."); return; }
            preset.Apply(TuningProfile.Active, loadout);
            RefreshAfterProfileChange();
            SetStatus("Loaded \"" + name + "\".");
        }

        void DeletePreset()
        {
            var name = nameField.Value;
            if (!TuningPresetStore.Exists(name)) { SetStatus("Nothing to delete."); return; }
            TuningPresetStore.Delete(name);
            RefreshPresetList();
            SetStatus("Deleted \"" + name + "\".");
        }

        // Saves under the well-known startup name — that preset auto-applies every boot
        // (see TuningProfile.Active), so this is the one-tap "keep these numbers" action.
        void SaveAsStartup()
        {
            TuningPresetStore.Save(TuningPresetStore.StartupName, TuningPreset.Capture(TuningProfile.Active, loadout));
            RefreshPresetList();
            SetStatus("Saved as startup — auto-loads every boot.");
        }

        void NewPreset()
        {
            TuningProfile.Active.ResetToDefaults();
            RefreshAfterProfileChange();
            SetStatus(TuningPresetStore.Exists(TuningPresetStore.StartupName)
                ? "Reset (startup preset still applies at boot)."
                : "Reset to defaults.");
        }

        void SetStatus(string s) { if (presetStatus != null) presetStatus.text = s; }

        // --- content ---

        void BuildContent(Transform parent)
        {
            w.AddHeader(parent, "Global modifiers");
            w.AddNote(parent, "Apply to both fighters. Ticks are 20 per second.");
            w.ReflectNumericFields(parent, typeof(CombatTuning), () => TuningProfile.Active.Tuning, TuningDocs.Field);

            w.AddHeader(parent, "Player");
            BuildFighterBlock(parent, () => TuningProfile.Active.Player, "Character",
                () => loadout.PlayerVisualName,
                dir => { loadout.UseCustomAppearance = false; loadout.CyclePlayerVisual(dir); });

            w.AddHeader(parent, "Opponent (enemy)");
            w.AddCycler(parent, "Opponent",
                "Authored opponent (build + AI rotation + visual) from Opponents/*.json. Inline = hand-edit the build below.",
                OpponentLabel, CycleOpponent);
            BuildFighterBlock(parent, () => TuningProfile.Active.Adversary, "Monster",
                () => loadout.EnemyVisualName,
                dir => loadout.CycleEnemyVisual(dir));
        }

        // --- opponent picker (cursor -1 = inline adversary, otherwise an Opponents/*.json id) ---

        const string InlineOpponentName = "(inline adversary)";

        string OpponentLabel()
        {
            var o = TuningProfile.Active.Opponent;
            if (o == null) return InlineOpponentName;
            return string.IsNullOrEmpty(o.displayName) ? o.id : o.displayName;
        }

        void CycleOpponent(int dir)
        {
            var profile = TuningProfile.Active;
            var ids = OpponentStore.List();
            int cur = profile.Opponent == null ? -1 : ids.IndexOf(profile.Opponent.id);
            int next = LoadoutPools.Wrap(cur + 1 + dir, ids.Count + 1) - 1;
            if (next < 0)
            {
                profile.SetOpponent(null);
            }
            else
            {
                var opponent = OpponentStore.Load(ids[next]);
                profile.SetOpponent(opponent);
                if (opponent != null && !string.IsNullOrEmpty(opponent.visualName))
                    loadout.SetEnemyVisualByName(opponent.visualName);
            }
            RefreshAfterProfileChange();
        }

        void BuildFighterBlock(Transform parent, Func<FighterBuild> get, string visualLabel,
            Func<string> visualName, Action<int> cycleVisual)
        {
            w.AddCycler(parent, visualLabel, "Which model fights in this corner.", visualName, cycleVisual);
            w.AddCycler(parent, "Stance", "Playstyle gear: shifts stats/cooldowns.",
                () => LoadoutPools.GearName(get().Gear, LoadoutPools.Stances),
                dir => LoadoutPools.CycleGear(get().Gear, LoadoutPools.Stances, dir));
            w.AddCycler(parent, "Weapon", "Sets offense and shapes the auto-attack.",
                () => LoadoutPools.GearName(get().Gear, LoadoutPools.Weapons),
                dir => LoadoutPools.CycleGear(get().Gear, LoadoutPools.Weapons, dir));
            w.AddCycler(parent, "Armor", "Trades defense for mobility.",
                () => LoadoutPools.GearName(get().Gear, LoadoutPools.Armors),
                dir => LoadoutPools.CycleGear(get().Gear, LoadoutPools.Armors, dir));

            w.AddIntField(parent, "Max HP", "Starting & maximum hit points.",
                () => get().MaxHp, v => get().MaxHp = v);
            w.AddIntField(parent, "Spell gems", "Starting spell-gem resource for casts.",
                () => get().SpellGems, v => get().SpellGems = v);
            w.ReflectNumericFields(parent, typeof(StatSheet), () => get().Stats, TuningDocs.Field);

            EnsureSlots(get());
            var build = get();
            int total = build.MainSlotCount + 2;
            for (int i = 0; i < total; i++)
            {
                int slot = i;
                bool main = slot < build.MainSlotCount;
                string label = main ? "Main ability " + (slot + 1) : "Quick ability " + (slot - build.MainSlotCount + 1);
                w.AddCycler(parent, label, TuningDocs.Field("Slot"),
                    () => get().AbilityName(TuningProfile.Active, slot),
                    dir => CycleSlotFor(get(), slot, dir));
            }
        }

        static void EnsureSlots(FighterBuild b)
        {
            if (b.AbilityIds == null) b.AbilityIds = new List<string>();
            int want = b.MainSlotCount + 2;
            while (b.AbilityIds.Count < want) b.AbilityIds.Add(LoadoutPools.NoneId);
        }

        static void CycleSlotFor(FighterBuild b, int slot, int dir)
        {
            EnsureSlots(b);
            if (slot < b.MainSlotCount)
                LoadoutPools.CycleSlot(b.AbilityIds, slot, dir, LoadoutPools.MainAbilities, 0, b.MainSlotCount);
            else
                LoadoutPools.CycleSlot(b.AbilityIds, slot, dir, LoadoutPools.QuickAbilities, b.MainSlotCount, b.AbilityIds.Count);
        }

        // --- footer ---

        void BuildFooter(Transform parent)
        {
            var footer = UiFactory.Rect("Footer", parent);
            UiFactory.PlaceFixed(footer, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1720f, 72f));
            var h = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            h.spacing = 18f;
            h.childAlignment = TextAnchor.MiddleCenter;

            FooterButton(footer, "EDIT ABILITIES", 360, OpenAbilities);
            FooterButton(footer, "CUSTOMIZE", 300, OpenCreator);
            FooterButton(footer, "RESET", 220, NewPreset);
            startButton = UiFactory.TextButton("Start", footer, "START FIGHT", 30, () => match.StartMatch());
            SetWidth((RectTransform)startButton.transform, 360);
        }

        void FooterButton(Transform parent, string label, float width, UnityEngine.Events.UnityAction onClick)
        {
            var b = UiFactory.TextButton("F", parent, label, 28, onClick);
            SetWidth((RectTransform)b.transform, width);
        }

        void OpenAbilities()
        {
            if (abilities == null) return;
            root.SetActive(false);
            abilities.Open(() => Show(true));
        }

        void OpenCreator()
        {
            if (creator == null) return;
            root.SetActive(false);
            creator.Open(() => Show(true));
        }

        // --- show / refresh ---

        void RefreshAfterProfileChange()
        {
            EnsureSlots(TuningProfile.Active.Player);
            EnsureSlots(TuningProfile.Active.Adversary);
            w.RefreshAll();
        }

        public void Show(bool on)
        {
            root.SetActive(on);
            if (!on) return;
            RefreshPresetList();
            RefreshAfterProfileChange();
            if (content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }
    }
}
