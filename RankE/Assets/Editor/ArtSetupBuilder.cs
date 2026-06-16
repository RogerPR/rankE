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
        const string CataloguePath = OutDir + "/CharacterPartCatalogue.asset";
        const string VfxRegistryPath = OutDir + "/AbilityVfxRegistry.asset";

        // Cartoon FX Remaster Free (Jean Moreno / "JMO Assets"). Adjust if the pack lands
        // in a different folder; the VFX registry build is skipped if it isn't present.
        const string CfxRoot = "Assets/JMO Assets/Cartoon FX Remaster";

        const string CharSamples = CharRoot + "/Prefabs/00 Character Samples";
        const string CharBases = CharRoot + "/Prefabs/01 Select Bases";
        const string CharAccessories = CharRoot + "/Prefabs/02 Add Accessories";

        // Normalize every fighter to a sane on-screen height so a tiny slime and a
        // huge dragon read at comparable scale in a 1v1 duel.
        const float PlayerTargetHeight = 1.8f;
        const float MonsterTargetHeight = 2.1f;

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

            var catalogue = BuildPartCatalogue(controller);
            if (AssetDatabase.LoadAssetAtPath<CharacterPartCatalogue>(CataloguePath) != null)
                AssetDatabase.DeleteAsset(CataloguePath);
            AssetDatabase.CreateAsset(catalogue, CataloguePath);

            WriteVfxRegistry(); // skipped cleanly if the VFX pack isn't imported yet

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ArtSetup] Built {registry.Players.Count} player characters + " +
                      $"{registry.Monsters.Count} monsters → {RegistryPath}");
            Debug.Log($"[ArtSetup] Built character creator catalogue: {catalogue.Bases.Count} bases + " +
                      $"{catalogue.Categories.Count} slots → {CataloguePath}");
        }

        /// <summary>Build only the skill-VFX registry (run after importing/updating the pack).</summary>
        [MenuItem("Tools/RANK E/Build VFX Registry")]
        public static void BuildVfx()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(OutDir);
            if (!WriteVfxRegistry()) return;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static bool WriteVfxRegistry()
        {
            var vfx = BuildVfxRegistry();
            if (vfx == null) return false;
            if (AssetDatabase.LoadAssetAtPath<AbilityVfxRegistry>(VfxRegistryPath) != null)
                AssetDatabase.DeleteAsset(VfxRegistryPath);
            AssetDatabase.CreateAsset(vfx, VfxRegistryPath);
            Debug.Log($"[ArtSetup] Built VFX registry: {vfx.Abilities.Count} ability bindings + " +
                      $"{vfx.Cues.Count} cues → {VfxRegistryPath}");
            return true;
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
            if (!AssetDatabase.IsValidFolder(CharSamples))
            {
                Debug.LogWarning($"[ArtSetup] Player samples folder not found: {CharSamples}");
                return list;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { CharSamples }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                ComputeFit(prefab, PlayerTargetHeight, out float scale, out float yOff);

                var def = HumanoidTemplate(controller);
                def.Id = "char_" + name.ToLowerInvariant();
                def.DisplayName = name;
                def.Prefab = prefab;
                def.ModelScale = scale;
                def.ModelYOffset = yOff;
                list.Add(def);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            return list;
        }

        /// <summary>
        /// The canonical humanoid visual: shared controller + the action/ability anim
        /// maps every player shares (all 8 samples + any custom-assembled character).
        /// <c>Prefab</c>, scale and offset are filled in per use.
        /// </summary>
        static FighterVisualDef HumanoidTemplate(RuntimeAnimatorController controller) =>
            new FighterVisualDef
            {
                Id = "char_humanoid",
                DisplayName = "Humanoid",
                Prefab = null,
                Controller = controller,
                IsHumanoid = true,
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
            };

        // ---- Character creator: scan modular bases + accessories into a catalogue ----

        static CharacterPartCatalogue BuildPartCatalogue(RuntimeAnimatorController controller)
        {
            var cat = ScriptableObject.CreateInstance<CharacterPartCatalogue>();
            cat.HumanoidTemplate = HumanoidTemplate(controller);
            cat.Bases = BuildBases();
            cat.Categories = BuildCategories();
            return cat;
        }

        static List<PartEntry> BuildBases()
        {
            var list = new List<PartEntry>();
            if (!AssetDatabase.IsValidFolder(CharBases))
            {
                Debug.LogWarning($"[ArtSetup] Base bodies folder not found: {CharBases}");
                return list;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { CharBases }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                list.Add(new PartEntry { Id = "base_" + Slug(name), DisplayName = name, Prefab = prefab });
            }
            list.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            return list;
        }

        static List<PartCategory> BuildCategories()
        {
            var list = new List<PartCategory>();
            string accAbs = ToAbsolute(CharAccessories);
            if (!Directory.Exists(accAbs))
            {
                Debug.LogWarning($"[ArtSetup] Accessories folder not found: {CharAccessories}");
                return list;
            }

            foreach (var groupAbs in Directory.GetDirectories(accAbs))
            {
                string group = Path.GetFileName(groupAbs); // e.g. "+Head", "+R Hand and +L Hand"
                foreach (var subAbs in Directory.GetDirectories(groupAbs))
                {
                    string sub = Path.GetFileName(subAbs); // e.g. "Helmet", "Sword"
                    var prefabFiles = Directory.GetFiles(subAbs, "*.prefab", SearchOption.AllDirectories);
                    if (prefabFiles.Length == 0) continue;

                    var parts = new List<PartEntry>();
                    foreach (var f in prefabFiles)
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ToAssetPath(f));
                        if (prefab == null) continue;
                        string pname = Path.GetFileNameWithoutExtension(f);
                        parts.Add(new PartEntry { Id = Slug(pname), DisplayName = pname, Prefab = prefab });
                    }
                    if (parts.Count == 0) continue;
                    parts.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));

                    list.Add(new PartCategory
                    {
                        Id = Slug(group) + "_" + Slug(sub),
                        DisplayName = sub,
                        AttachGroup = group,
                        AttachBones = ResolveAttachBones(group, sub),
                        Optional = true,
                        Parts = parts,
                    });
                }
            }
            list.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            return list;
        }

        /// <summary>
        /// Resolve which bone(s) a slot mounts to. Single-bone groups ("+Head") map to
        /// themselves. Two-sided groups ("+R Hand and +L Hand") route by subcategory:
        /// shields go left-hand, other hand items right-hand, and forearm/upperarm
        /// pieces mirror onto both sides.
        /// </summary>
        static string[] ResolveAttachBones(string group, string sub)
        {
            int andIdx = group.IndexOf(" and ", StringComparison.OrdinalIgnoreCase);
            if (andIdx < 0) return new[] { group };

            string a = group.Substring(0, andIdx).Trim();
            string b = group.Substring(andIdx + 5).Trim();
            bool isHand = group.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isHand) return new[] { a, b }; // bracers / shoulders mirror both sides

            string right = a.IndexOf("+R", StringComparison.OrdinalIgnoreCase) >= 0 ? a : b;
            string left = a.IndexOf("+L", StringComparison.OrdinalIgnoreCase) >= 0 ? a : b;
            bool isShield = sub.IndexOf("Shield", StringComparison.OrdinalIgnoreCase) >= 0;
            return new[] { isShield ? left : right };
        }

        static string Slug(string s) =>
            s.Replace("+", "").Trim().Replace(' ', '_').Replace("(", "").Replace(")", "").ToLowerInvariant();

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
                ComputeFit(prefab, MonsterTargetHeight, out float scale, out float yOff);

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
                    ModelScale = scale,
                    ModelYOffset = yOff,
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

        // ---- Skill VFX: scan the pack, map prefabs to abilities/cues by keyword ----

        /// <summary>
        /// Build the <see cref="AbilityVfxRegistry"/> by scanning the imported VFX pack and
        /// matching prefab names to abilities/cues with keyword heuristics (same spirit as
        /// the monster controller scanner). Slots with no match stay null and are skipped
        /// gracefully at runtime — the asset is meant to be fine-tuned in the Inspector
        /// afterward, this just gives a sensible starting mapping. Returns null (no asset
        /// written) if the pack isn't imported.
        /// </summary>
        static AbilityVfxRegistry BuildVfxRegistry()
        {
            if (!AssetDatabase.IsValidFolder(CfxRoot))
            {
                Debug.LogWarning($"[ArtSetup] VFX pack not found at {CfxRoot} — skipping VFX " +
                    "registry. Import Cartoon FX Remaster Free, then re-run Build Art Setup.");
                return null;
            }

            var prefabs = new List<(string name, GameObject go)>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { CfxRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null) prefabs.Add((Path.GetFileNameWithoutExtension(path), go));
            }
            prefabs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (prefabs.Count == 0)
            {
                Debug.LogWarning($"[ArtSetup] No prefabs under {CfxRoot} — skipping VFX registry.");
                return null;
            }

            var reg = ScriptableObject.CreateInstance<AbilityVfxRegistry>();

            // Fireball — travels hand→enemy with a cast aura + impact.
            var fireProjectile = Find(prefabs, prefer: new[] { "fireball", "fire trail", "flame", "fire" },
                exclude: new[] { "explosion", "impact", "muzzle", "aura", "ground", "hit", "breath" });
            var fireImpact = Find(prefabs, prefer: new[] { "explosion", "impact", "burst" }, must: new[] { "fire" })
                ?? Find(prefabs, prefer: new[] { "explosion", "burst" });
            var fireMuzzle = Find(prefabs, prefer: new[] { "muzzle", "cast", "charge" }, must: new[] { "fire" })
                ?? Find(prefabs, prefer: new[] { "muzzle", "magic charge", "cast", "flash" });
            var fireAura = Find(prefabs, prefer: new[] { "aura", "loop" }, must: new[] { "fire" })
                ?? Find(prefabs, prefer: new[] { "magic aura", "aura" });

            // Falling Star — descends onto the enemy on the delayed hit.
            var starProjectile = Find(prefabs, prefer: new[] { "falling star", "meteor", "comet", "star", "sparkle", "magic" });
            var starImpact = Find(prefabs, prefer: new[] { "explosion", "sparkle", "burst" }, must: new[] { "star" })
                ?? Find(prefabs, prefer: new[] { "firework", "sparkle explosion", "light explosion", "explosion", "burst" });

            // Shared melee swing flash (neutral white swoosh; magic/fire variants left for tuning).
            var slashFx = Find(prefabs, prefer: new[] { "sword trail plain", "sword trail", "slash", "sword", "cut", "swing" });

            // Vampiro — channelled poison/dark aura + a release poof.
            var vampAura = Find(prefabs, prefer: new[] { "poison", "dark", "green", "nature", "magic aura", "aura" });
            var vampMuzzle = Find(prefabs, prefer: new[] { "dark magic", "green magic", "magic poof", "magic" });

            reg.Abilities.Add(Vfx(PocContent.FireballId, ProjectileMode.Travel, fireAura, fireMuzzle, fireProjectile, fireImpact));
            reg.Abilities.Add(Vfx(PocContent.FallingStarId, ProjectileMode.Fall, null, null, starProjectile, starImpact));
            reg.Abilities.Add(Vfx(PocContent.VampiroId, ProjectileMode.None, vampAura, vampMuzzle, null, null));
            foreach (var id in new[] { PocContent.SlashId, PocContent.BashId, PocContent.AutoAttackId,
                PocContent.LungeId, PocContent.KickId, PocContent.InterruptCastId })
                reg.Abilities.Add(Vfx(id, ProjectileMode.None, null, slashFx, null, null));

            // Reaction cues (always listed so empty ones are easy to fill in the Inspector).
            reg.Cues.Add(Cue(VfxCue.Hit, Find(prefabs, prefer: new[] { "hit", "impact", "blood" }, exclude: new[] { "explosion", "fire", "electric", "ice", "leaves" })));
            reg.Cues.Add(Cue(VfxCue.Heal, Find(prefabs, prefer: new[] { "heal", "cure", "lightglow", "glow", "leaves", "shiny" }, exclude: new[] { "ambient" })));
            reg.Cues.Add(Cue(VfxCue.Parry, Find(prefabs, prefer: new[] { "spark", "clash", "metal", "shield", "block", "flash" })));
            reg.Cues.Add(Cue(VfxCue.Break, Find(prefabs, prefer: new[] { "shockwave", "wham", "boom", "explosion", "burst" })));
            reg.Cues.Add(Cue(VfxCue.Riposte, Find(prefabs, prefer: new[] { "critical", "sword hit plain", "sword hit", "slash", "flash" })));
            reg.Cues.Add(Cue(VfxCue.Death, Find(prefabs, prefer: new[] { "souls", "poof", "skull", "death", "smoke source" })));

            return reg;
        }

        /// <summary>
        /// First prefab whose lowercased name contains one of <paramref name="prefer"/>
        /// (keyword priority order), optionally also containing every <paramref name="must"/>
        /// term and none of the <paramref name="exclude"/> terms.
        /// </summary>
        static GameObject Find(List<(string name, GameObject go)> prefabs,
            string[] prefer, string[] must = null, string[] exclude = null)
        {
            foreach (var key in prefer)
                foreach (var (name, go) in prefabs)
                {
                    string n = name.ToLowerInvariant();
                    if (!n.Contains(key)) continue;
                    if (must != null && !must.All(m => n.Contains(m))) continue;
                    if (exclude != null && exclude.Any(x => n.Contains(x))) continue;
                    return go;
                }
            return null;
        }

        static AbilityVfxDef Vfx(string id, ProjectileMode mode, GameObject aura,
            GameObject muzzle, GameObject projectile, GameObject impact) =>
            new AbilityVfxDef
            {
                AbilityId = id,
                Mode = mode,
                CastAura = aura,
                Muzzle = muzzle,
                Projectile = projectile,
                Impact = impact,
            };

        static CueVfx Cue(VfxCue cue, GameObject prefab) => new CueVfx { Cue = cue, Prefab = prefab };

        // ---- small helpers ----

        /// <summary>
        /// Measure a prefab's renderer bounds and derive a uniform scale that makes it
        /// <paramref name="targetHeight"/> tall, plus a Y offset that seats its lowest
        /// point on the ground (y=0). Instantiates briefly in the editor to read bounds.
        /// </summary>
        static void ComputeFit(GameObject prefab, float targetHeight, out float scale, out float yOffset)
        {
            var inst = (GameObject)UnityEngine.Object.Instantiate(prefab);
            inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            inst.transform.localScale = Vector3.one;
            ModelFit.Measure(inst, targetHeight, out scale, out yOffset);
            UnityEngine.Object.DestroyImmediate(inst);
        }

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
