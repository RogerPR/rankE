using System;
using System.Collections.Generic;
using RankE.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Modular character creator: a left-side slot list (base body + every discovered
    /// accessory category) with a live, slowly-turning 3D preview assembled in the clear
    /// area to the right. Reached from the loadout picker's "Customize" button; on Done it
    /// hands control back. Pure presentation — it edits <c>DebugLoadout.Appearance</c> and
    /// the runtime assembles the same look at fight start.
    /// </summary>
    public sealed class CharacterCreatorScreen : MonoBehaviour
    {
        // Preview placement (orthographic side-on camera at (0,1.5,-10), size 3). x≈2
        // centres the model in the area not covered by the left slot panel; yaw 180 faces
        // it toward the camera.
        const float PreviewX = 2f;
        const float PreviewZ = 0f;
        const float PreviewYaw = 180f;
        const float SpinSpeed = 22f; // deg/s turntable

        const float PanelWidth = 760f;
        const float RowHeight = 56f;

        MatchController match;
        CharacterPartCatalogue cat;
        Action onClose;
        readonly System.Random rng = new System.Random();

        GameObject root;
        RectTransform content;
        readonly List<string> rowCategoryIds = new List<string>(); // null marks the Base row
        readonly List<Text> rowValues = new List<Text>();
        Button firstButton;

        GameObject preview;
        Transform previewSpin;

        public void Init(MatchController match, Transform parent)
        {
            this.match = match;
            cat = CharacterPartCatalogue.Load();

            var panel = UiFactory.Panel("CharacterCreator", parent, new Color(0.06f, 0.06f, 0.1f, 0.0f));
            UiFactory.PlaceStretch((RectTransform)panel.transform);
            root = panel.gameObject;

            // Left slot panel (semi-opaque so the preview shows through on the right).
            var side = UiFactory.Panel("SidePanel", panel.transform, new Color(0.06f, 0.06f, 0.1f, 0.92f));
            UiFactory.PlaceFixed((RectTransform)side.transform, new Vector2(0f, 0.5f),
                new Vector2(PanelWidth * 0.5f, 0f), new Vector2(PanelWidth, 1080f));

            var title = UiFactory.Label("Title", side.transform, "CHARACTER CREATOR", 40, Color.white);
            UiFactory.PlaceFixed((RectTransform)title.transform, new Vector2(0.5f, 1f),
                new Vector2(0f, -50f), new Vector2(PanelWidth, 50f));

            BuildScroll(side.transform);
            BuildRows();
            BuildButtons(side.transform);

            root.SetActive(false);
        }

        // ---- public flow ----

        /// <summary>Enter the creator. <paramref name="onClose"/> fires when the user is done.</summary>
        public void Open(Action onClose)
        {
            this.onClose = onClose;
            match.Loadout.UseCustomAppearance = true;
            root.SetActive(true);
            Rebuild();
            RefreshAll();
            if (EventSystem.current != null && firstButton != null)
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }

        /// <summary>Hide and tear down the preview (safe to call any time).</summary>
        public void Hide()
        {
            if (root != null) root.SetActive(false);
            DestroyPreview();
        }

        void Done()
        {
            var cb = onClose;
            onClose = null;
            Hide();
            cb?.Invoke();
        }

        // ---- UI construction ----

        void BuildScroll(Transform parent)
        {
            var scrollRt = UiFactory.Rect("Scroll", parent);
            UiFactory.PlaceFixed(scrollRt, new Vector2(0.5f, 1f),
                new Vector2(0f, -90f), new Vector2(PanelWidth - 36f, 820f));
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var viewport = UiFactory.Rect("Viewport", scrollRt);
            UiFactory.PlaceStretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            // A transparent raycast target so drag/wheel over the list reaches the ScrollRect.
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;
            scroll.viewport = viewport;

            content = UiFactory.Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            scroll.content = content;
        }

        void BuildRows()
        {
            rowCategoryIds.Clear();
            rowValues.Clear();

            AddRow("Base", null);
            if (cat != null)
                foreach (var c in cat.Categories)
                    AddRow(c.DisplayName, c.Id);

            content.sizeDelta = new Vector2(0f, rowCategoryIds.Count * RowHeight);
        }

        void AddRow(string label, string categoryId)
        {
            int i = rowCategoryIds.Count;
            rowCategoryIds.Add(categoryId);

            var row = UiFactory.Rect($"Row_{i}", content);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -i * RowHeight);
            row.sizeDelta = new Vector2(0f, RowHeight);

            var name = UiFactory.Label("Name", row, label, 24,
                new Color(0.8f, 0.8f, 0.9f), TextAnchor.MiddleLeft);
            UiFactory.PlaceFixed((RectTransform)name.transform, new Vector2(0f, 0.5f),
                new Vector2(16f, 0f), new Vector2(220f, 44f));

            var prev = UiFactory.TextButton("Prev", row, "<", 26, () => Cycle(i, -1));
            UiFactory.PlaceFixed((RectTransform)prev.transform, new Vector2(0f, 0.5f),
                new Vector2(248f, 0f), new Vector2(48f, 48f));
            if (firstButton == null) firstButton = prev;

            var value = UiFactory.Label("Value", row, "", 24, Color.white, TextAnchor.MiddleCenter);
            UiFactory.PlaceFixed((RectTransform)value.transform, new Vector2(0f, 0.5f),
                new Vector2(308f, 0f), new Vector2(330f, 44f));
            rowValues.Add(value);

            var next = UiFactory.TextButton("Next", row, ">", 26, () => Cycle(i, +1));
            UiFactory.PlaceFixed((RectTransform)next.transform, new Vector2(0f, 0.5f),
                new Vector2(648f, 0f), new Vector2(48f, 48f));
        }

        void BuildButtons(Transform parent)
        {
            var randomize = UiFactory.TextButton("Randomize", parent, "RANDOMIZE", 24, Randomize);
            UiFactory.PlaceFixed((RectTransform)randomize.transform, new Vector2(0.5f, 0f),
                new Vector2(-200f, 60f), new Vector2(220f, 56f));

            var reset = UiFactory.TextButton("Reset", parent, "RESET", 24, ResetLook);
            UiFactory.PlaceFixed((RectTransform)reset.transform, new Vector2(0.5f, 0f),
                new Vector2(40f, 60f), new Vector2(160f, 56f));

            var done = UiFactory.TextButton("Done", parent, "DONE", 26, Done);
            UiFactory.PlaceFixed((RectTransform)done.transform, new Vector2(0.5f, 0f),
                new Vector2(240f, 60f), new Vector2(160f, 56f));
        }

        // ---- editing ----

        CharacterAppearance Appr => match.Loadout.Appearance;

        void Cycle(int row, int dir)
        {
            string categoryId = rowCategoryIds[row];
            if (categoryId == null) CycleBase(dir);
            else CycleCategory(categoryId, dir);
            Rebuild();
            RefreshRow(row);
        }

        void CycleBase(int dir)
        {
            if (cat == null || cat.Bases.Count == 0) return;
            int idx = IndexOfBase(Appr.BaseId);
            idx = Wrap(idx + dir, cat.Bases.Count);
            Appr.BaseId = cat.Bases[idx].Id;
        }

        void CycleCategory(string categoryId, int dir)
        {
            var c = cat?.CategoryById(categoryId);
            if (c == null || c.Parts.Count == 0) return;

            // Option space is parts + a "(none)" slot at index 0 for optional categories.
            bool optional = c.Optional;
            int span = c.Parts.Count + (optional ? 1 : 0);
            int cur = OptionIndex(c, Appr.Get(categoryId), optional);
            int next = Wrap(cur + dir, span);
            if (optional && next == 0) Appr.Set(categoryId, null);
            else Appr.Set(categoryId, c.Parts[next - (optional ? 1 : 0)].Id);
        }

        void Randomize()
        {
            var made = CharacterAppearance.Random(cat, rng);
            Appr.BaseId = made.BaseId;
            Appr.Choices.Clear();
            foreach (var kv in made.Choices) Appr.Choices[kv.Key] = kv.Value;
            Rebuild();
            RefreshAll();
        }

        void ResetLook()
        {
            Appr.BaseId = (cat != null && cat.Bases.Count > 0) ? cat.Bases[0].Id : null;
            Appr.Choices.Clear();
            Rebuild();
            RefreshAll();
        }

        // ---- refresh ----

        void RefreshAll()
        {
            for (int i = 0; i < rowValues.Count; i++) RefreshRow(i);
        }

        void RefreshRow(int row)
        {
            string categoryId = rowCategoryIds[row];
            if (categoryId == null)
            {
                var b = cat?.BaseById(Appr.BaseId);
                rowValues[row].text = b != null ? b.DisplayName : "(none)";
                return;
            }
            var c = cat?.CategoryById(categoryId);
            string partId = Appr.Get(categoryId);
            var part = c?.PartById(partId);
            rowValues[row].text = part != null ? part.DisplayName : "(none)";
        }

        // ---- live preview ----

        void Rebuild()
        {
            DestroyPreview();
            if (cat == null) return;

            preview = CharacterAssembler.Assemble(cat, Appr, out var def);
            if (preview == null) return;
            preview.name = "CreatorPreview";

            var animator = preview.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                if (def.Controller != null) animator.runtimeAnimatorController = def.Controller;
            }

            previewSpin = preview.transform;
            previewSpin.localScale = Vector3.one * (def.ModelScale > 0f ? def.ModelScale : 1f);
            previewSpin.position = new Vector3(PreviewX, def.ModelYOffset, PreviewZ);
            previewSpin.rotation = Quaternion.Euler(0f, PreviewYaw, 0f);
        }

        void DestroyPreview()
        {
            if (preview != null) Destroy(preview);
            preview = null;
            previewSpin = null;
        }

        void Update()
        {
            if (previewSpin != null)
                previewSpin.Rotate(0f, SpinSpeed * Time.deltaTime, 0f, Space.World);
        }

        void OnDestroy() => DestroyPreview();

        // ---- helpers ----

        int IndexOfBase(string baseId)
        {
            for (int i = 0; i < cat.Bases.Count; i++)
                if (cat.Bases[i].Id == baseId) return i;
            return 0;
        }

        static int OptionIndex(PartCategory c, string partId, bool optional)
        {
            if (string.IsNullOrEmpty(partId)) return 0; // "(none)" is index 0 when optional
            for (int i = 0; i < c.Parts.Count; i++)
                if (c.Parts[i].Id == partId) return i + (optional ? 1 : 0);
            return 0;
        }

        static int Wrap(int v, int n) => n <= 0 ? 0 : ((v % n) + n) % n;
    }
}
