using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RankE.Game;
using RankE.Sim;
using RankE.UI;
using UnityEditor;
using UnityEngine;

namespace RankE.Editor
{
    /// <summary>
    /// Live combat-feel tuning while you playtest. Edits the held <see cref="TuningProfile"/>
    /// (sim numbers) and the presentation ScriptableObjects (VFX feel knobs + UiSkin), so the
    /// long Phase-3 fun-gate loop doesn't need a recompile per tweak.
    ///
    /// Sim edits apply on the NEXT fight (each fight clones the profile — see
    /// <c>BattleDriver.Begin</c>), keeping every fight a clean deterministic replay-from-seed;
    /// finish the fight / press Rematch to feel a change. Presentation knobs apply live (the
    /// view reads the assets per spawn) and persist (they're assets). The profile itself is
    /// in-memory and resets on a domain reload / play-exit — "Copy values" dumps the current
    /// numbers so good ones can be pasted back into <c>PocContent</c>.
    /// </summary>
    public sealed class CombatTuningWindow : EditorWindow
    {
        const string VfxPath = "Assets/Resources/RankE/AbilityVfxRegistry.asset";
        const string SkinPath = "Assets/Resources/RankE/UiSkin.asset";

        Vector2 scroll;
        bool showGlobals = true;
        bool showAbilities = true;
        bool showPresentation = true;
        readonly HashSet<string> expandedAbilities = new HashSet<string>();

        [MenuItem("Tools/RANK E/Combat Tuning")]
        public static void Open()
        {
            var w = GetWindow<CombatTuningWindow>("Combat Tuning");
            w.minSize = new Vector2(360f, 400f);
            w.Show();
        }

        void OnGUI()
        {
            var profile = TuningProfile.Active;

            DrawHeader();
            scroll = EditorGUILayout.BeginScrollView(scroll);

            showGlobals = EditorGUILayout.Foldout(showGlobals, "CombatTuning globals", true);
            if (showGlobals) DrawGlobals(profile.Tuning);

            EditorGUILayout.Space();
            showAbilities = EditorGUILayout.Foldout(showAbilities, "Per-ability values", true);
            if (showAbilities) DrawAbilities(profile.Abilities);

            EditorGUILayout.Space();
            showPresentation = EditorGUILayout.Foldout(showPresentation, "Presentation knobs (live)", true);
            if (showPresentation) DrawPresentation();

            EditorGUILayout.EndScrollView();
            DrawFooter(profile);
        }

        void DrawHeader()
        {
            EditorGUILayout.HelpBox(
                Application.isPlaying
                    ? "Sim edits apply on the NEXT fight (Rematch). Presentation knobs apply live."
                    : "Enter Play to tune a running fight. Values reset on domain reload — use Copy values to keep good ones.",
                Application.isPlaying ? MessageType.Info : MessageType.Warning);
        }

        // --- CombatTuning: reflect numeric fields so the list stays in sync with the type ---
        static void DrawGlobals(CombatTuning t)
        {
            EditorGUI.indentLevel++;
            foreach (var f in typeof(CombatTuning).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var label = ObjectNames.NicifyVariableName(f.Name);
                if (f.FieldType == typeof(int))
                    f.SetValue(t, EditorGUILayout.IntField(label, (int)f.GetValue(t)));
                else if (f.FieldType == typeof(double))
                    f.SetValue(t, EditorGUILayout.DoubleField(label, (double)f.GetValue(t)));
                // string ids (status/ability lookups) are not feel knobs — left out on purpose.
            }
            EditorGUI.indentLevel--;
        }

        void DrawAbilities(Dictionary<string, AbilityDef> abilities)
        {
            EditorGUI.indentLevel++;
            var ids = new List<string>(abilities.Keys);
            ids.Sort();
            foreach (var id in ids)
            {
                var def = abilities[id];
                if (def == null) continue;

                bool open = expandedAbilities.Contains(id);
                bool now = EditorGUILayout.Foldout(open, def.Name ?? id, true);
                if (now != open)
                {
                    if (now) expandedAbilities.Add(id); else expandedAbilities.Remove(id);
                }
                if (!now) continue;

                EditorGUI.indentLevel++;
                def.CooldownTicks = EditorGUILayout.IntField("Cooldown ticks", def.CooldownTicks);
                def.CastTicks = EditorGUILayout.IntField("Cast ticks", def.CastTicks);
                def.DelayTicks = EditorGUILayout.IntField("Delay ticks", def.DelayTicks);
                def.PreLockTicks = EditorGUILayout.IntField("Pre-lock ticks", def.PreLockTicks);
                def.PostLockTicks = EditorGUILayout.IntField("Post-lock ticks", def.PostLockTicks);

                for (int i = 0; i < def.Effects.Count; i++)
                {
                    var e = def.Effects[i];
                    EditorGUILayout.LabelField($"Effect {i}: {e.Kind} → {e.Target}", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    if (e.Kind == EffectKinds.Damage || e.Kind == EffectKinds.BreakDamage)
                        e.Amount = EditorGUILayout.IntField("Amount", e.Amount);
                    if (e.Kind == EffectKinds.ApplyStatus)
                        e.DurationTicks = EditorGUILayout.IntField($"{e.StatusId} duration ticks", e.DurationTicks);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        void DrawPresentation()
        {
            EditorGUI.indentLevel++;
            var vfx = AssetDatabase.LoadAssetAtPath<AbilityVfxRegistry>(VfxPath);
            if (vfx != null)
            {
                EditorGUI.BeginChangeCheck();
                vfx.VfxScale = EditorGUILayout.FloatField("VFX scale", vfx.VfxScale);
                vfx.DefaultTravelSeconds = EditorGUILayout.FloatField("Default travel s", vfx.DefaultTravelSeconds);
                vfx.FallHeight = EditorGUILayout.FloatField("Fall height", vfx.FallHeight);
                vfx.ChestHeight = EditorGUILayout.FloatField("Chest height", vfx.ChestHeight);
                vfx.CueLifetime = EditorGUILayout.FloatField("Cue lifetime", vfx.CueLifetime);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(vfx);
                    AssetDatabase.SaveAssets();
                }
            }
            else EditorGUILayout.LabelField("AbilityVfxRegistry not built.", EditorStyles.miniLabel);

            EditorGUILayout.Space(4f);
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            if (skin != null)
            {
                EditorGUI.BeginChangeCheck();
                skin.FrameTint = EditorGUILayout.ColorField("UI frame tint", skin.FrameTint);
                skin.PixelsPerUnitMultiplier = EditorGUILayout.FloatField("UI pixels-per-unit ×", skin.PixelsPerUnitMultiplier);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(skin);
                    AssetDatabase.SaveAssets();
                }
            }
            else EditorGUILayout.LabelField("UiSkin not built.", EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
        }

        void DrawFooter(TuningProfile profile)
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset to defaults"))
                    profile.ResetToDefaults();

                var match = FindFirstObjectByType<MatchController>();
                bool canRematch = match != null &&
                    (match.State == MatchState.Result || match.State == MatchState.Loadout);
                using (new EditorGUI.DisabledScope(!canRematch))
                {
                    if (GUILayout.Button(new GUIContent("Rematch",
                        "Apply sim edits now (only from the Result/Loadout screen).")) && match != null)
                        match.Rematch();
                }

                if (GUILayout.Button("Copy values"))
                {
                    EditorGUIUtility.systemCopyBuffer = DumpJson(profile);
                    Debug.Log("[CombatTuning] Current values copied to clipboard.");
                }
            }
        }

        static string DumpJson(TuningProfile profile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"tuning\": {");
            var fields = typeof(CombatTuning).GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                if (f.FieldType != typeof(int) && f.FieldType != typeof(double)) continue;
                sb.AppendLine($"    \"{f.Name}\": {f.GetValue(profile.Tuning)},");
            }
            sb.AppendLine("  },");
            sb.AppendLine("  \"abilities\": {");
            var ids = new List<string>(profile.Abilities.Keys);
            ids.Sort();
            foreach (var id in ids)
            {
                var d = profile.Abilities[id];
                sb.Append($"    \"{id}\": {{ \"cd\": {d.CooldownTicks}, \"cast\": {d.CastTicks}, ");
                sb.Append($"\"delay\": {d.DelayTicks}, \"preLock\": {d.PreLockTicks}, \"postLock\": {d.PostLockTicks}, ");
                sb.Append("\"effects\": [");
                for (int i = 0; i < d.Effects.Count; i++)
                {
                    var e = d.Effects[i];
                    sb.Append($"{{\"kind\":\"{e.Kind}\",\"amount\":{e.Amount},\"dur\":{e.DurationTicks}}}");
                    if (i < d.Effects.Count - 1) sb.Append(", ");
                }
                sb.AppendLine("] },");
            }
            sb.AppendLine("  }");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
