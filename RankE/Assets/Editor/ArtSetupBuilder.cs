using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RankE.Game;
using RankE.Sim;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RankE.Editor
{
    /// <summary>
    /// One-shot editor build of the 3D art wiring: generates a humanoid player
    /// AnimatorController from the Modular Fantasy Characters clips, then a
    /// <see cref="FighterVisualRegistry"/> (under Resources) listing the player
    /// sample prefabs and every monster prefab with its semantic→state animation
    /// map. Monster maps are derived by scanning each monster's own controller, so
    /// new creatures need no hand wiring. Re-runnable: rebuilds both assets.
    ///
    /// Run via menu Tools ▸ RANK E ▸ Build Art Setup, or headless:
    /// -executeMethod RankE.Editor.ArtSetupBuilder.Build
    /// </summary>
    public static class ArtSetupBuilder
    {
        const string CharRoot = "Assets/Modular Fantasy Characters Mega Toon Series";
        const string MonRoot = "Assets/Monsters Ultimate Pack 09 Cute Series";
        const string OutDir = "Assets/Resources/RankE";
        const string ControllerPath = OutDir + "/PlayerCombat.controller";
        const string RegistryPath = OutDir + "/FighterVisualRegistry.asset";

        [MenuItem("Tools/RANK E/Build Art Setup")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(OutDir);

            var controller = BuildPlayerController();
            var registry = ScriptableObject.CreateInstance<FighterVisualRegistry>();
            registry.Players = BuildPlayers(controller);
            registry.Monsters = BuildMonsters();

            if (AssetDatabase.LoadAssetAtPath<FighterVisualRegistry>(RegistryPath) != null)
                AssetDatabase.DeleteAsset(RegistryPath);
            AssetDatabase.CreateAsset(registry, RegistryPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ArtSetup] Built {registry.Players.Count} player characters + " +
                      $"{registry.Monsters.Count} monsters → {RegistryPath}");
        }

        // ---- Player humanoid controller (clean named states, driven by CrossFade) ----

        static RuntimeAnimatorController BuildPlayerController()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var sm = ctrl.layers[0].stateMachine;
            string anim = $"{CharRoot}/Animations";

            AddState(sm, "Idle", ClipFrom($"{anim}/Character@Idle Battle.FBX"), isDefault: true);
            AddState(sm, "Attack_Slash", ClipFrom($"{anim}/Character@Slash Attack.FBX"));
            AddState(sm, "Attack_Cut", ClipFrom($"{anim}/Character@Cut Attack.FBX"));
            AddState(sm, "Attack_Stab", ClipFrom($"{anim}/Character@Stab Attack.FBX"));
            AddState(sm, "Cast", ClipFrom($"{anim}/Character@Cast Spell 01.FBX"));
            AddState(sm, "Block", ClipFrom($"{anim}/Character@Block.FBX"));
            AddState(sm, "Hit", ClipFrom($"{anim}/Character@Take Damage.FBX"));
            AddState(sm, "Die", ClipFrom($"{anim}/Character@Die.FBX"));
            AddState(sm, "Spawn", ClipFrom($"{anim}/Character@Spawn.FBX"));

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static void AddState(AnimatorStateMachine sm, string name, AnimationClip clip, bool isDefault = false)
        {
            var st = sm.AddState(name);
            st.motion = clip;
            st.writeDefaultValues = true;
            if (isDefault) sm.defaultState = st;
            if (clip == null) Debug.LogWarning($"[ArtSetup] No clip found for player state '{name}'.");
        }

        static AnimationClip ClipFrom(string fbxPath)
        {
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                if (a is AnimationClip c && !c.name.StartsWith("__preview")) return c;
            return null;
        }

        // ---- Player visuals: the ready-made sample characters ----

        static List<FighterVisualDef> BuildPlayers(RuntimeAnimatorController controller)
        {
            var list = new List<FighterVisualDef>();
            string dir = $"{CharRoot}/Prefabs/00 Character Samples";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Debug.LogWarning($"[ArtSetup] Player samples folder not found: {dir}");
                return list;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                string name = Path.GetFileNameWithoutExtension(path);

                list.Add(new FighterVisualDef
                {
                    Id = "char_" + name.ToLowerInvariant(),
                    DisplayName = name,
                    Prefab = prefab,
                    Controller = controller,
                    IsHumanoid = true,
                    ModelScale = 1f,
                    Actions = new List<ActionState>
                    {
                        A(AnimAction.Idle, "Idle"),
                        A(AnimAction.Attack, "Attack_Slash"),
                        A(AnimAction.Cast, "Cast"),
                        A(AnimAction.Hit, "Hit"),
                        A(AnimAction.Block, "Block"),
                        A(AnimAction.Broken, "Hit"),
                        A(AnimAction.Riposte, "Attack_Stab"),
                        A(AnimAction.Die, "Die"),
                        A(AnimAction.Spawn, "Spawn"),
                    },
                    AbilityStates = new List<AbilityAnim>
                    {
                        Ab(PocContent.SlashId, "Attack_Slash"),
                        Ab(PocContent.BashId, "Attack_Cut"),
                        Ab(PocContent.FireballId, "Cast"),
                        Ab(PocContent.VampiroId, "Cast"),
                        Ab(PocContent.FallingStarId, "Cast"),
                        Ab(PocContent.LungeId, "Attack_Stab"),
                        Ab(PocContent.AutoAttackId, "Attack_Slash"),
                        Ab(PocContent.KickId, "Attack_Cut"),
                        Ab(PocContent.InterruptCastId, "Attack_Stab"),
                        Ab(PocContent.RiposteId, "Attack_Stab"),
                        Ab(PocContent.ParryId, "Block"),
                    },
                });
            }

            list.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            return list;
        }

        // ---- Monster visuals: auto-derive the state map from each own controller ----

        static List<FighterVisualDef> BuildMonsters()
        {
            var list = new List<FighterVisualDef>();
            string monAbs = ToAbsolute(MonRoot);
            if (!Directory.Exists(monAbs))
            {
                Debug.LogWarning($"[ArtSetup] Monster root not found: {MonRoot}");
                return list;
            }

            foreach (var folderAbs in Directory.GetDirectories(monAbs))
            {
                if (Path.GetFileName(folderAbs).ToLowerInvariant().Contains("demo")) continue;

                string prefabsAbs = Path.Combine(folderAbs, "Prefabs");
                if (!Directory.Exists(prefabsAbs)) continue;
                var prefabFiles = Directory.GetFiles(prefabsAbs, "*.prefab");
                if (prefabFiles.Length == 0) continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ToAssetPath(prefabFiles[0]));
                if (prefab == null) continue;
                string monName = Path.GetFileNameWithoutExtension(prefabFiles[0]);

                var states = ControllerStates(folderAbs);
                string idle = Pick(states, exact: "Idle", contains: new[] { "Idle" }) ?? "Idle";
                string hit = Pick(states, contains: new[] { "Take Damage", "Damage", "Hit", "Hurt" }) ?? idle;
                string die = Pick(states, contains: new[] { "Die", "Death" }) ?? hit;
                string cast = Pick(states, contains: new[] { "Cast", "Spell" });
                string spawn = Pick(states, contains: new[] { "Spawn" });
                string atk = PickAttack(states, null) ?? idle;
                string atk2 = PickAttack(states, atk) ?? atk;
                string castOrAtk = cast ?? atk;

                var def = new FighterVisualDef
                {
                    Id = "mon_" + monName.ToLowerInvariant().Replace(' ', '_'),
                    DisplayName = monName,
                    Prefab = prefab,
                    Controller = null, // keep the monster's own controller
                    IsHumanoid = false,
                    ModelScale = 1f,
                    Actions = new List<ActionState>
                    {
                        A(AnimAction.Idle, idle),
                        A(AnimAction.Attack, atk),
                        A(AnimAction.Cast, castOrAtk),
                        A(AnimAction.Hit, hit),
                        A(AnimAction.Broken, hit),
                        A(AnimAction.Riposte, atk2),
                        A(AnimAction.Die, die),
                    },
                    AbilityStates = new List<AbilityAnim>
                    {
                        Ab(PocContent.SlashId, atk),
                        Ab(PocContent.BashId, atk),
                        Ab(PocContent.KickId, atk),
                        Ab(PocContent.LungeId, atk),
                        Ab(PocContent.AutoAttackId, atk),
                        Ab(PocContent.InterruptCastId, atk),
                        Ab(PocContent.FireballId, castOrAtk),
                        Ab(PocContent.VampiroId, castOrAtk),
                        Ab(PocContent.FallingStarId, castOrAtk),
                        Ab(PocContent.RiposteId, atk2),
                    },
                };
                if (!string.IsNullOrEmpty(spawn)) def.Actions.Add(A(AnimAction.Spawn, spawn));
                list.Add(def);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            return list;
        }

        static List<string> ControllerStates(string folderAbs)
        {
            var names = new List<string>();
            var ctrlFiles = Directory.GetFiles(folderAbs, "*.controller", SearchOption.AllDirectories);
            if (ctrlFiles.Length == 0) return names;
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ToAssetPath(ctrlFiles[0]));
            if (ctrl == null) return names;
            foreach (var layer in ctrl.layers) CollectStates(layer.stateMachine, names);
            return names;
        }

        static void CollectStates(AnimatorStateMachine sm, List<string> names)
        {
            foreach (var cs in sm.states) names.Add(cs.state.name);
            foreach (var child in sm.stateMachines) CollectStates(child.stateMachine, names);
        }

        static string Pick(List<string> states, string exact = null, string[] contains = null)
        {
            if (exact != null)
            {
                var e = states.FirstOrDefault(s => string.Equals(s, exact, StringComparison.OrdinalIgnoreCase));
                if (e != null) return e;
            }
            if (contains != null)
                foreach (var key in contains)
                {
                    var m = states.FirstOrDefault(s => s.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (m != null) return m;
                }
            return null;
        }

        static string PickAttack(List<string> states, string exclude)
        {
            // Prefer a melee strike; avoid "Low" and ranged variants for a stand-and-duel.
            string[] prefer = { "Bite Attack", "Claw", "Smash", "Slash", "Melee", "Wing Attack", "Blast Attack", "Attack" };
            foreach (var key in prefer)
            {
                var m = states.FirstOrDefault(s =>
                    s.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0
                    && s.IndexOf("Low", StringComparison.OrdinalIgnoreCase) < 0
                    && !string.Equals(s, exclude, StringComparison.OrdinalIgnoreCase));
                if (m != null) return m;
            }
            return states.FirstOrDefault(s =>
                s.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0
                && !string.Equals(s, exclude, StringComparison.OrdinalIgnoreCase));
        }

        // ---- small helpers ----

        static ActionState A(AnimAction action, string state) => new ActionState { Action = action, State = state };
        static AbilityAnim Ab(string id, string state) => new AbilityAnim { AbilityId = id, State = state };

        static string ToAbsolute(string assetPath) =>
            Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);

        static string ToAssetPath(string absPath)
        {
            absPath = absPath.Replace('\\', '/');
            string data = Application.dataPath.Replace('\\', '/'); // ends with /Assets
            return absPath.StartsWith(data) ? "Assets" + absPath.Substring(data.Length) : absPath;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
