using System.Collections.Generic;
using System.IO;
using RankE.UI;
using UnityEditor;
using UnityEngine;

namespace RankE.Editor
{
    /// <summary>
    /// One-shot editor build of the wooden-UI theme: sets 9-slice borders on the Wooden
    /// UI sprites we use, then populates a <see cref="UiSkin"/> asset (under Resources) the
    /// runtime <see cref="UiFactory"/> loads to skin the whole HUD + menus. Ability icons
    /// are picked up from <c>Assets/UI/Icons</c> by filename (file name = ability id).
    ///
    /// Re-runnable and <b>additive</b>: an existing UiSkin's hand-tuned slots/knobs/icons
    /// are preserved — the builder only fills empty sprite slots and adds newly-found icons,
    /// so you can re-run after dropping in more icons without losing Inspector tweaks.
    ///
    /// Run via menu Tools ▸ RANK E ▸ Build UI Skin, or headless:
    /// -executeMethod RankE.Editor.UiSkinBuilder.Build
    /// </summary>
    public static class UiSkinBuilder
    {
        const string OutDir = "Assets/Resources/RankE";
        const string SkinPath = OutDir + "/UiSkin.asset";
        const string WoodenDir = "Assets/Wooden_UI/Wooden_UI_png";
        const string IconsDir = "Assets/UI/Icons";

        // 9-slice corners as a fraction of each side; generous so ornate wooden corners
        // stay crisp while the middle stretches.
        const float BorderFraction = 0.35f;

        [MenuItem("Tools/RANK E/Build UI Skin")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(OutDir);

            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            bool created = skin == null;
            if (created) skin = ScriptableObject.CreateInstance<UiSkin>();

            // Sliced container/button/bar sprites — set borders, then fill empty slots.
            AssignIfEmpty(ref skin.PanelFrame, "frame", sliced: true);
            AssignIfEmpty(ref skin.SlotFrame, "frame_s_01", sliced: true);
            if (skin.SlotFrame == null) AssignIfEmpty(ref skin.SlotFrame, "frame", sliced: true);
            AssignIfEmpty(ref skin.Plank, "Plank_01", sliced: true);
            AssignIfEmpty(ref skin.Button, "button_01_01", sliced: true);
            AssignIfEmpty(ref skin.ButtonHover, "button_01_02", sliced: true);
            AssignIfEmpty(ref skin.ButtonPressed, "button_01_03", sliced: true);
            AssignIfEmpty(ref skin.BarBackground, "strip_bg01", sliced: true);
            AssignIfEmpty(ref skin.BarFill, "strip_bg02", sliced: true);

            int icons = MergeIcons(skin);

            if (created)
                AssetDatabase.CreateAsset(skin, SkinPath);
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[UiSkinBuilder] {(created ? "Created" : "Updated")} {SkinPath} — "
                + $"frame:{(skin.PanelFrame != null)} button:{(skin.Button != null)} "
                + $"bar:{(skin.BarBackground != null)}/{(skin.BarFill != null)} "
                + $"icons:{skin.Icons.Count} (+{icons} this run). "
                + "Tune slots/knobs in the Inspector; re-run is additive.");
        }

        /// <summary>Fill a slot from a Wooden UI file (no extension) if it's currently empty.</summary>
        static void AssignIfEmpty(ref Sprite slot, string fileName, bool sliced)
        {
            if (slot != null) return;
            var path = $"{WoodenDir}/{fileName}.png";
            if (!File.Exists(path)) return;

            EnsureSprite(path, sliced);
            slot = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Force a texture to import as a Single sprite, optionally with 9-slice borders.</summary>
        static void EnsureSprite(string path, bool sliced)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var wantBorder = Vector4.zero;
            if (sliced && tex != null)
            {
                float bx = Mathf.Round(Mathf.Min(tex.width * BorderFraction, tex.width / 2f - 2f));
                float by = Mathf.Round(Mathf.Min(tex.height * BorderFraction, tex.height / 2f - 2f));
                wantBorder = new Vector4(bx, by, bx, by);
            }
            if (importer.spriteBorder != wantBorder)
            {
                importer.spriteBorder = wantBorder;
                dirty = true;
            }

            if (dirty) importer.SaveAndReimport();
        }

        /// <summary>Add ability icons from the icons folder (file name = ability id). Returns new count.</summary>
        static int MergeIcons(UiSkin skin)
        {
            if (!AssetDatabase.IsValidFolder(IconsDir)) return 0;

            var existing = new HashSet<string>();
            foreach (var e in skin.Icons)
                if (!string.IsNullOrEmpty(e.AbilityId)) existing.Add(e.AbilityId);

            int added = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { IconsDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                EnsureSprite(path, sliced: false);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                var id = Path.GetFileNameWithoutExtension(path);
                if (existing.Contains(id)) continue;

                skin.Icons.Add(new AbilityIcon { AbilityId = id, Icon = sprite });
                existing.Add(id);
                added++;
            }
            return added;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
