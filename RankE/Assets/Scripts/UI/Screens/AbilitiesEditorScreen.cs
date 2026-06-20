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
    /// The per-ability parameter editor, opened from the control panel's "Edit abilities"
    /// button. Two steps: first a PICKER listing every ability in the shared library
    /// (<see cref="TuningProfile.Active"/>); choosing one opens a focused EDITOR showing only
    /// that ability's tunable numbers (cooldown/cast/locks/gem cost + its damage/break amount),
    /// each with a one-line description. Edits the library definitions directly — the same
    /// objects each fight clones from — so changes apply on the next fight. Rows look the
    /// ability up by id every read, so a later Reset/Load is picked up safely. Pure view.
    /// </summary>
    public sealed class AbilitiesEditorScreen : MonoBehaviour
    {
        GameObject root;
        Text title;

        GameObject pickerView;
        RectTransform pickerContent;
        Button pickerBack;

        GameObject editorView;
        RectTransform editorContent;
        Button editorBack;
        TuningWidgets editorWidgets;

        Action onClose;

        public void Init(Transform parent)
        {
            var panel = UiFactory.Panel("AbilitiesEditor", parent, new Color(0f, 0f, 0f, 0.86f));
            panel.raycastTarget = true;
            UiFactory.PlaceStretch((RectTransform)panel.transform);
            root = panel.gameObject;

            var box = UiFactory.Frame("AbilitiesBox", panel.transform);
            UiFactory.PlaceFixed((RectTransform)box.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1180f, 940f));

            title = UiFactory.Label("Title", box.transform, "", 40, TuningWidgets.HeaderColor);
            UiFactory.PlaceFixed((RectTransform)title.transform, new Vector2(0.5f, 1f),
                new Vector2(0f, -18f), new Vector2(1080f, 56f));

            pickerView = BuildView(box.transform, "PickerView", out pickerContent, out pickerBack,
                "BACK", Close);
            editorView = BuildView(box.transform, "EditorView", out editorContent, out editorBack,
                "‹ ABILITIES", ShowPicker);

            try { BuildPickerList(); }
            catch (Exception e) { Debug.LogError("AbilitiesEditor BuildPickerList failed: " + e); }

            root.SetActive(false);
        }

        // A full-area view: a scroll list + a back button at the bottom. Used for both steps.
        GameObject BuildView(Transform box, string name, out RectTransform content,
            out Button back, string backLabel, UnityEngine.Events.UnityAction backAction)
        {
            var view = UiFactory.Rect(name, box);
            UiFactory.PlaceStretch(view);

            var scroll = UiFactory.ScrollView(name + "Scroll", view, out content);
            scroll.scrollSensitivity = 0.75f;
            scroll.inertia = false;
            scroll.gameObject.AddComponent<ScrollFollowSelection>().Init(scroll);
            var vp = (RectTransform)content.parent;
            vp.anchorMin = Vector2.zero;
            vp.anchorMax = Vector2.one;
            vp.offsetMin = new Vector2(22f, 96f);
            vp.offsetMax = new Vector2(-22f, -84f);

            back = UiFactory.TextButton("Back", view, backLabel, 28, backAction);
            UiFactory.PlaceFixed((RectTransform)back.transform, new Vector2(0.5f, 0f),
                new Vector2(0f, 22f), new Vector2(320f, 60f));

            return view.gameObject;
        }

        // --- picker step ---

        void BuildPickerList()
        {
            var ids = SortedAbilityIds();
            foreach (var id in ids)
            {
                var def = TuningProfile.Active.Abilities[id];
                if (def == null) continue;
                string aid = id;
                var btn = UiFactory.TextButton($"Pick_{id}", pickerContent, def.Name ?? id, 26,
                    () => SelectAbility(aid));
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 60f;
                le.preferredHeight = 60f;
            }
        }

        static List<string> SortedAbilityIds()
        {
            var ids = new List<string>(TuningProfile.Active.Abilities.Keys);
            ids.Sort();
            return ids;
        }

        void ShowPicker()
        {
            editorView.SetActive(false);
            pickerView.SetActive(true);
            title.text = "EDIT ABILITIES — PICK ONE";
            SelectFirst(pickerContent, pickerBack);
        }

        // --- editor step (rebuilt fresh per ability so refreshers never go stale) ---

        void SelectAbility(string id)
        {
            var def = Def(id);
            if (def == null) return;

            foreach (Transform child in editorContent) Destroy(child.gameObject);
            editorWidgets = new TuningWidgets();
            BuildAbilityFields(editorContent, id);

            title.text = (def.Name ?? id).ToUpperInvariant();
            pickerView.SetActive(false);
            editorView.SetActive(true);
            editorWidgets.RefreshAll();
            LayoutRebuilder.ForceRebuildLayoutImmediate(editorContent);
            SelectFirst(editorContent, editorBack);
        }

        void BuildAbilityFields(Transform parent, string id)
        {
            var def = Def(id);
            if (def == null) return;
            var w = editorWidgets;
            string aid = id;

            w.AddIntField(parent, "Cooldown", TuningDocs.Field("Cooldown"),
                () => Def(aid)?.CooldownTicks ?? 0, v => { var d = Def(aid); if (d != null) d.CooldownTicks = v; });
            w.AddIntField(parent, "Cast", TuningDocs.Field("Cast"),
                () => Def(aid)?.CastTicks ?? 0, v => { var d = Def(aid); if (d != null) d.CastTicks = v; });
            w.AddIntField(parent, "Delay", TuningDocs.Field("Delay"),
                () => Def(aid)?.DelayTicks ?? 0, v => { var d = Def(aid); if (d != null) d.DelayTicks = v; });
            w.AddIntField(parent, "Pre-lock", TuningDocs.Field("Pre-lock"),
                () => Def(aid)?.PreLockTicks ?? 0, v => { var d = Def(aid); if (d != null) d.PreLockTicks = v; });
            w.AddIntField(parent, "Post-lock", TuningDocs.Field("Post-lock"),
                () => Def(aid)?.PostLockTicks ?? 0, v => { var d = Def(aid); if (d != null) d.PostLockTicks = v; });
            w.AddIntField(parent, "Gem cost", TuningDocs.Field("Gem cost"),
                () => Def(aid)?.GemCost ?? 0, v => { var d = Def(aid); if (d != null) d.GemCost = v; });

            int ei = FirstAmountEffectIndex(def);
            if (ei >= 0)
            {
                int idx = ei;
                string lbl = def.Effects[ei].Kind == EffectKinds.BreakDamage ? "Break amount" : "Damage";
                w.AddIntField(parent, lbl, TuningDocs.Field(lbl),
                    () => AmountAt(aid, idx), v => SetAmountAt(aid, idx, v));
            }
        }

        // --- shared helpers ---

        static AbilityDef Def(string id) =>
            TuningProfile.Active.Abilities.TryGetValue(id, out var d) ? d : null;

        static int AmountAt(string id, int i)
        {
            var d = Def(id);
            return d != null && i < d.Effects.Count ? d.Effects[i].Amount : 0;
        }

        static void SetAmountAt(string id, int i, int v)
        {
            var d = Def(id);
            if (d != null && i < d.Effects.Count) d.Effects[i].Amount = v;
        }

        static int FirstAmountEffectIndex(AbilityDef def)
        {
            for (int i = 0; i < def.Effects.Count; i++)
            {
                var e = def.Effects[i];
                if (e != null && (e.Kind == EffectKinds.Damage || e.Kind == EffectKinds.BreakDamage))
                    return i;
            }
            return -1;
        }

        static void SelectFirst(Transform content, Button fallback)
        {
            if (EventSystem.current == null) return;
            var first = content != null && content.childCount > 0
                ? content.GetChild(0).GetComponentInChildren<Selectable>() : null;
            EventSystem.current.SetSelectedGameObject(
                (first != null ? first : fallback)?.gameObject);
        }

        public void Open(Action onClose)
        {
            this.onClose = onClose;
            root.SetActive(true);
            ShowPicker();
        }

        void Close()
        {
            root.SetActive(false);
            onClose?.Invoke();
        }

        public void Hide() => root.SetActive(false);
    }
}
