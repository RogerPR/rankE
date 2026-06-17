using System.Collections.Generic;
using RankE.Game;
using RankE.Sim;
using RankE.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RankE.Editor
{
    /// <summary>
    /// Author each ability's animation + VFX from dropdowns instead of hand-editing the
    /// registry .asset files or re-running the keyword heuristics in <c>ArtSetupBuilder</c>.
    /// Edits the same assets the reactive view reads:
    ///   • <see cref="FighterVisualRegistry"/> — a chosen visual's per-ability Animator state
    ///     (<c>FighterVisualDef.AbilityStates</c>, consumed by <c>FighterAnimator</c>).
    ///   • <see cref="AbilityVfxRegistry"/> — per-ability cast/muzzle/projectile/impact prefabs,
    ///     the reaction cues, and the global feel knobs (consumed by <c>FighterVfx</c>).
    ///   • (optional) <see cref="UiSkin"/> — the ability bar icon map.
    /// Additive: only touches the entries you edit. Save writes the assets to disk.
    /// </summary>
    public sealed class AbilityAuthoringWindow : EditorWindow
    {
        const string VfxPath = "Assets/Resources/RankE/AbilityVfxRegistry.asset";
        const string VisualsPath = "Assets/Resources/RankE/FighterVisualRegistry.asset";
        const string SkinPath = "Assets/Resources/RankE/UiSkin.asset";

        AbilityVfxRegistry vfx;
        FighterVisualRegistry visuals;
        UiSkin skin;

        // (id, display name) of every ability, in content order.
        readonly List<(string id, string name)> abilityList = new List<(string, string)>();

        Vector2 scroll;
        int targetDefIndex;
        bool showCues = true;
        bool showKnobs;
        bool showIcons;

        [MenuItem("Tools/RANK E/Ability Authoring")]
        public static void Open()
        {
            var w = GetWindow<AbilityAuthoringWindow>("Ability Authoring");
            w.minSize = new Vector2(420f, 500f);
            w.Reload();
            w.Show();
        }

        void OnEnable() => Reload();

        void Reload()
        {
            vfx = AssetDatabase.LoadAssetAtPath<AbilityVfxRegistry>(VfxPath);
            visuals = AssetDatabase.LoadAssetAtPath<FighterVisualRegistry>(VisualsPath);
            skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);

            abilityList.Clear();
            foreach (var kv in PocContent.CreateContent().Abilities)
                abilityList.Add((kv.Key, kv.Value.Name));
        }

        void OnGUI()
        {
            if (GUILayout.Button("Reload assets", EditorStyles.miniButton)) Reload();
            if (vfx == null && visuals == null)
            {
                EditorGUILayout.HelpBox("Registries not built. Run Tools ▸ RANK E ▸ Build Art Setup first.",
                    MessageType.Warning);
                return;
            }

            var defs = CollectDefs();
            DrawTargetSelector(defs);
            FighterVisualDef targetDef = defs.Count > 0 ? defs[Mathf.Clamp(targetDefIndex, 0, defs.Count - 1)].def : null;
            var states = StatesFor(targetDef);

            EditorGUILayout.Space();
            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var (id, name) in abilityList)
                DrawAbilityRow(id, name, targetDef, states);

            EditorGUILayout.Space();
            DrawCues();
            DrawKnobs();
            DrawIcons();

            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        // --- target visual (whose AbilityStates we edit) ---
        struct DefEntry { public string label; public FighterVisualDef def; }

        List<DefEntry> CollectDefs()
        {
            var list = new List<DefEntry>();
            if (visuals == null) return list;
            foreach (var d in visuals.Players)
                if (d != null) list.Add(new DefEntry { label = "Player: " + (d.DisplayName ?? d.Id), def = d });
            foreach (var d in visuals.Monsters)
                if (d != null) list.Add(new DefEntry { label = "Monster: " + (d.DisplayName ?? d.Id), def = d });
            return list;
        }

        void DrawTargetSelector(List<DefEntry> defs)
        {
            if (defs.Count == 0)
            {
                EditorGUILayout.LabelField("Anim target", "(no fighter visuals built)");
                return;
            }
            var labels = new string[defs.Count];
            for (int i = 0; i < defs.Count; i++) labels[i] = defs[i].label;
            targetDefIndex = EditorGUILayout.Popup(
                new GUIContent("Anim target", "Whose per-ability Animator states these rows edit."),
                Mathf.Clamp(targetDefIndex, 0, defs.Count - 1), labels);
        }

        static string[] StatesFor(FighterVisualDef def)
        {
            if (def == null) return new string[0];
            RuntimeAnimatorController rac = def.Controller;
            if (rac == null && def.Prefab != null)
            {
                var anim = def.Prefab.GetComponentInChildren<Animator>();
                if (anim != null) rac = anim.runtimeAnimatorController;
            }
            var ac = rac as AnimatorController;
            if (ac == null && rac is AnimatorOverrideController ovr) ac = ovr.runtimeAnimatorController as AnimatorController;
            if (ac == null) return new string[0];

            var names = new List<string>();
            foreach (var layer in ac.layers)
                foreach (var st in layer.stateMachine.states)
                    names.Add(st.state.name);
            return names.ToArray();
        }

        // --- one ability: anim state + VFX slots ---
        void DrawAbilityRow(string id, string name, FighterVisualDef targetDef, string[] states)
        {
            EditorGUILayout.LabelField($"{name}  ({id})", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // Animation state.
            if (targetDef != null)
                DrawAnimPicker(id, targetDef, states);

            // VFX slots.
            if (vfx != null)
            {
                var def = GetOrCreateVfxDef(id);
                EditorGUI.BeginChangeCheck();
                def.CastAura = (GameObject)EditorGUILayout.ObjectField("Cast aura", def.CastAura, typeof(GameObject), false);
                def.Muzzle = (GameObject)EditorGUILayout.ObjectField("Muzzle", def.Muzzle, typeof(GameObject), false);
                def.Projectile = (GameObject)EditorGUILayout.ObjectField("Projectile", def.Projectile, typeof(GameObject), false);
                def.Impact = (GameObject)EditorGUILayout.ObjectField("Impact", def.Impact, typeof(GameObject), false);
                def.Mode = (ProjectileMode)EditorGUILayout.EnumPopup("Mode", def.Mode);
                def.TravelSeconds = EditorGUILayout.FloatField(
                    new GUIContent("Travel seconds", "0 = derive from the ability's cast/delay ticks."),
                    def.TravelSeconds);
                if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(vfx);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2f);
        }

        void DrawAnimPicker(string id, FighterVisualDef def, string[] states)
        {
            string current = def.StateForAbility(id);
            if (states.Length == 0)
            {
                // No readable controller — fall back to a free text field.
                EditorGUI.BeginChangeCheck();
                string typed = EditorGUILayout.TextField("Anim state", current ?? "");
                if (EditorGUI.EndChangeCheck())
                {
                    UpsertAnim(def, id, typed);
                    EditorUtility.SetDirty(visuals);
                }
                return;
            }

            var options = new string[states.Length + 1];
            options[0] = "(none)";
            for (int i = 0; i < states.Length; i++) options[i + 1] = states[i];
            int idx = 0;
            if (!string.IsNullOrEmpty(current))
            {
                int hit = System.Array.IndexOf(states, current);
                idx = hit >= 0 ? hit + 1 : 0;
            }
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup("Anim state", idx, options);
            if (EditorGUI.EndChangeCheck())
            {
                UpsertAnim(def, id, next == 0 ? null : options[next]);
                EditorUtility.SetDirty(visuals);
            }
        }

        static void UpsertAnim(FighterVisualDef def, string id, string state)
        {
            for (int i = 0; i < def.AbilityStates.Count; i++)
            {
                if (def.AbilityStates[i].AbilityId == id)
                {
                    if (string.IsNullOrEmpty(state)) { def.AbilityStates.RemoveAt(i); return; }
                    def.AbilityStates[i] = new AbilityAnim { AbilityId = id, State = state };
                    return;
                }
            }
            if (!string.IsNullOrEmpty(state))
                def.AbilityStates.Add(new AbilityAnim { AbilityId = id, State = state });
        }

        AbilityVfxDef GetOrCreateVfxDef(string id)
        {
            var def = vfx.DefFor(id);
            if (def == null)
            {
                def = new AbilityVfxDef { AbilityId = id };
                vfx.Abilities.Add(def);
                EditorUtility.SetDirty(vfx);
            }
            return def;
        }

        // --- reaction cues, feel knobs, icons ---
        void DrawCues()
        {
            if (vfx == null) return;
            showCues = EditorGUILayout.Foldout(showCues, "Reaction cues", true);
            if (!showCues) return;
            EditorGUI.indentLevel++;
            foreach (VfxCue cue in System.Enum.GetValues(typeof(VfxCue)))
            {
                int idx = vfx.Cues.FindIndex(c => c.Cue == cue);
                var prefab = idx >= 0 ? vfx.Cues[idx].Prefab : null;
                EditorGUI.BeginChangeCheck();
                var next = (GameObject)EditorGUILayout.ObjectField(cue.ToString(), prefab, typeof(GameObject), false);
                if (EditorGUI.EndChangeCheck())
                {
                    var entry = new CueVfx { Cue = cue, Prefab = next };
                    if (idx >= 0) vfx.Cues[idx] = entry; else vfx.Cues.Add(entry);
                    EditorUtility.SetDirty(vfx);
                }
            }
            EditorGUI.indentLevel--;
        }

        void DrawKnobs()
        {
            if (vfx == null) return;
            showKnobs = EditorGUILayout.Foldout(showKnobs, "VFX feel knobs", true);
            if (!showKnobs) return;
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            vfx.VfxScale = EditorGUILayout.FloatField("VFX scale", vfx.VfxScale);
            vfx.DefaultTravelSeconds = EditorGUILayout.FloatField("Default travel s", vfx.DefaultTravelSeconds);
            vfx.FallHeight = EditorGUILayout.FloatField("Fall height", vfx.FallHeight);
            vfx.ChestHeight = EditorGUILayout.FloatField("Chest height", vfx.ChestHeight);
            vfx.CueLifetime = EditorGUILayout.FloatField("Cue lifetime", vfx.CueLifetime);
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(vfx);
            EditorGUI.indentLevel--;
        }

        void DrawIcons()
        {
            if (skin == null) return;
            showIcons = EditorGUILayout.Foldout(showIcons, "Ability bar icons", true);
            if (!showIcons) return;
            EditorGUI.indentLevel++;
            foreach (var (id, name) in abilityList)
            {
                int idx = skin.Icons.FindIndex(e => e.AbilityId == id);
                var sprite = idx >= 0 ? skin.Icons[idx].Icon : null;
                EditorGUI.BeginChangeCheck();
                var next = (Sprite)EditorGUILayout.ObjectField(name, sprite, typeof(Sprite), false);
                if (EditorGUI.EndChangeCheck())
                {
                    var entry = new AbilityIcon { AbilityId = id, Icon = next };
                    if (idx >= 0) skin.Icons[idx] = entry; else skin.Icons.Add(entry);
                    EditorUtility.SetDirty(skin);
                }
            }
            EditorGUI.indentLevel--;
        }

        void DrawFooter()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Save", GUILayout.Height(26f)))
            {
                if (vfx != null) EditorUtility.SetDirty(vfx);
                if (visuals != null) EditorUtility.SetDirty(visuals);
                if (skin != null) EditorUtility.SetDirty(skin);
                AssetDatabase.SaveAssets();
                Debug.Log("[AbilityAuthoring] Saved registries.");
            }
        }
    }
}
