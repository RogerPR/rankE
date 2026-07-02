using System;
using RankE.Sim;

namespace RankE.Game
{
    /// <summary>
    /// A resolved, hermetic fight setup for headless balance sweeps: player build vs adversary
    /// (or an authored opponent) under a specific tuning. Built explicitly from disk data —
    /// never from <see cref="TuningProfile.Active"/> implicitly — so sweep results depend only
    /// on the named inputs, and resolves content/builds/brains exactly the way a played fight
    /// does (<see cref="TuningProfile.CreateContentDb"/>; brain wiring mirrors
    /// <c>MatchController.BeginFight</c> + the <c>BattleDriver</c> telegraph wrap).
    ///
    /// Caveat: the "player" side is driven by <see cref="PocBehaviorProfile"/>, not a human —
    /// results are for RELATIVE comparisons between tunings, not absolute win-rate truth.
    /// </summary>
    public sealed class SweepScenario
    {
        public string Label;
        public TuningProfile Profile;

        /// <summary>
        /// Resolve from disk data: overlay the named preset (null/empty = code defaults) and
        /// optionally force a specific opponent from <c>Opponents/&lt;id&gt;.json</c>.
        /// Throws on a missing preset/opponent — a sweep against the wrong data must fail
        /// loudly, not silently fall back.
        /// </summary>
        public static SweepScenario FromPreset(string presetName = null, string opponentId = null)
        {
            var profile = TuningProfile.FromDefaults();
            string label = "defaults";
            if (!string.IsNullOrEmpty(presetName))
            {
                var preset = TuningPresetStore.Load(presetName);
                if (preset == null)
                    throw new ArgumentException($"No tuning preset \"{presetName}\" under {TuningPresetStore.Dir}");
                preset.Apply(profile, null);
                label = presetName;
            }
            if (!string.IsNullOrEmpty(opponentId))
            {
                var opponent = OpponentStore.Load(opponentId);
                if (opponent == null)
                    throw new ArgumentException($"No opponent \"{opponentId}\" under {OpponentStore.Dir}");
                profile.SetOpponent(opponent);
            }
            if (profile.Opponent != null) label += " vs " + profile.Opponent.id;
            return new SweepScenario { Label = label, Profile = profile };
        }

        /// <summary>Wrap an existing profile (e.g. the live one from the tuning window). Running
        /// never mutates the profile — configs, content and tuning are cloned per fight/run.</summary>
        public static SweepScenario FromProfile(TuningProfile profile, string label = "current profile")
            => new SweepScenario { Label = label, Profile = profile };

        public BattleStats Run(int fights, int seed)
        {
            var profile = Profile;
            return BattleRunner.Run(
                fights,
                seed,
                () =>
                {
                    var player = profile.Player.ToConfig(profile);
                    player.UsesComboSystem = true; // parity with MatchController.BeginFight
                    return player;
                },
                () => profile.Adversary.ToConfig(profile),
                () => new PocBehaviorProfile(),
                MakeEnemyBrain,
                profile.CreateContentDb(),
                profile.Tuning.Clone());
        }

        /// <summary>Same brain a played fight gets: the opponent's telegraphed weighted rotation,
        /// or the default sparring rhythm (BehaviorSpecDto's default cadence, as in
        /// MatchController), wrapped in the telegraph the BattleDriver would add.</summary>
        IBehavior MakeEnemyBrain()
        {
            var opponent = Profile.Opponent;
            if (opponent != null && opponent.HasRotation)
                return new TelegraphBehavior(
                    new WeightedRotationBehavior(opponent.ToRotationSteps(), opponent.behavior.intervalTicks),
                    opponent.behavior.telegraphTicks);

            var cadence = new BehaviorSpecDto(); // default interval/telegraph, one source of truth
            return new TelegraphBehavior(
                new ScriptedRhythmBehavior(new[] { DefaultContent.SlashId }, cadence.intervalTicks),
                cadence.telegraphTicks);
        }
    }
}
