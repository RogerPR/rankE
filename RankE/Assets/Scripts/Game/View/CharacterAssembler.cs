using System.Collections.Generic;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Builds a custom humanoid model at runtime from a <see cref="CharacterAppearance"/>:
    /// instantiate the chosen base body, then parent each chosen accessory prefab onto
    /// its named attach-point bone (the Meshtint skeleton exposes <c>+Head</c>,
    /// <c>+R Hand</c>, etc.). Returns the assembled GameObject plus a
    /// <see cref="FighterVisualDef"/> (cloned from the catalogue's humanoid template, so
    /// it carries the shared controller + anim maps). The caller positions/scales it —
    /// see <c>FighterStage.Configure</c> and <c>CharacterCreatorScreen</c>.
    /// Pure presentation; never touches the sim.
    /// </summary>
    public static class CharacterAssembler
    {
        // Matches ArtSetupBuilder.PlayerTargetHeight so a custom build reads at the same
        // on-screen height as the ready-made samples.
        const float PlayerTargetHeight = 1.8f;

        /// <summary>
        /// Assemble the model for <paramref name="appr"/>. Returns null (and a null
        /// <paramref name="def"/>) if the catalogue has no usable base body.
        /// </summary>
        public static GameObject Assemble(CharacterPartCatalogue cat, CharacterAppearance appr,
            out FighterVisualDef def)
        {
            def = null;
            if (cat == null) return null;

            var baseEntry = appr != null ? cat.BaseById(appr.BaseId) : (cat.Bases.Count > 0 ? cat.Bases[0] : null);
            if (baseEntry == null || baseEntry.Prefab == null) return null;

            var root = Object.Instantiate(baseEntry.Prefab);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var bones = MapBones(root.transform);

            if (appr != null)
            {
                foreach (var category in cat.Categories)
                {
                    string partId = appr.Get(category.Id);
                    if (string.IsNullOrEmpty(partId)) continue;
                    var part = category.PartById(partId);
                    if (part == null || part.Prefab == null) continue;

                    foreach (var boneName in category.AttachBones)
                    {
                        if (!bones.TryGetValue(boneName, out var bone))
                        {
                            Debug.LogWarning($"[CharacterAssembler] attach bone '{boneName}' " +
                                $"not found for '{part.Id}' on base '{baseEntry.Id}'; skipping.");
                            continue;
                        }
                        var piece = Object.Instantiate(part.Prefab);
                        piece.transform.SetParent(bone, false);
                        piece.transform.localPosition = Vector3.zero;
                        piece.transform.localRotation = Quaternion.identity;
                        piece.transform.localScale = Vector3.one;
                    }
                }
            }

            ModelFit.Measure(root, PlayerTargetHeight, out float scale, out float yOffset);

            var t = cat.HumanoidTemplate;
            def = new FighterVisualDef
            {
                Id = "char_custom",
                DisplayName = "Custom",
                Prefab = null, // the assembled instance is returned directly
                Controller = t.Controller,
                IsHumanoid = true,
                ModelScale = scale,
                ModelYOffset = yOffset,
                Actions = t.Actions,
                AbilityStates = t.AbilityStates,
            };
            return root;
        }

        static Dictionary<string, Transform> MapBones(Transform root)
        {
            var map = new Dictionary<string, Transform>();
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                map[tr.name] = tr;
            return map;
        }
    }
}
