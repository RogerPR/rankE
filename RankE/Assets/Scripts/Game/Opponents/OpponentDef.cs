using System.Collections.Generic;
using RankE.Sim;

namespace RankE.Game
{
    /// <summary>
    /// A first-class, reusable opponent: its build (stats + abilities + passives + gear) <i>and</i>
    /// its AI logic (a telegraphed, weighted rotation). Stored one-per-file as JSON under
    /// <c>RankE/Opponents/</c> (see <see cref="OpponentStore"/>) so opponents are authored/edited
    /// without recompiling, and a tuning scenario (<see cref="TuningPreset"/>) just references one
    /// by <see cref="id"/>. Reuses <see cref="FighterBuildDto"/> for the build so a build resolves
    /// the same way as the player's. JsonUtility-friendly (lists of named entries, no dictionaries).
    /// </summary>
    [System.Serializable]
    public sealed class OpponentDef
    {
        public string id;
        public string displayName;
        public string visualName;
        public FighterBuildDto build = new FighterBuildDto();
        public BehaviorSpecDto behavior = new BehaviorSpecDto();

        /// <summary>Resolve the build DTO into an editable <see cref="FighterBuild"/> (the same
        /// shape the player's build uses), named after the opponent.</summary>
        public FighterBuild ToFighterBuild()
        {
            var b = new FighterBuild();
            TuningPreset.ApplyBuild(b, build);
            if (!string.IsNullOrEmpty(displayName)) b.Name = displayName;
            return b;
        }

        /// <summary>The rotation as weighted steps for <see cref="WeightedRotationBehavior"/>.</summary>
        public List<List<WeightedChoice>> ToRotationSteps()
        {
            var result = new List<List<WeightedChoice>>();
            if (behavior?.steps == null) return result;
            foreach (var s in behavior.steps)
            {
                var opts = new List<WeightedChoice>();
                if (s?.options != null)
                    foreach (var o in s.options)
                        if (o != null && !string.IsNullOrEmpty(o.id))
                            opts.Add(new WeightedChoice(o.id, o.weight));
                result.Add(opts);
            }
            return result;
        }

        public bool HasRotation => behavior != null && behavior.steps != null && behavior.steps.Count > 0;
    }

    /// <summary>Telegraphed cadence + the weighted rotation steps.</summary>
    [System.Serializable]
    public sealed class BehaviorSpecDto
    {
        public int intervalTicks = 60;   // one telegraphed beat every 3s
        public int telegraphTicks = 10;  // ~0.5s wind-up
        public List<RotationStepDto> steps = new List<RotationStepDto>();
    }

    /// <summary>One beat: a weighted set of candidate abilities (one option = a fixed beat).</summary>
    [System.Serializable]
    public sealed class RotationStepDto
    {
        public List<WeightedAbilityDto> options = new List<WeightedAbilityDto>();
    }

    [System.Serializable]
    public sealed class WeightedAbilityDto
    {
        public string id;
        public float weight = 1f;
    }
}
