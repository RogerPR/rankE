using System;
using NUnit.Framework;
using RankE.Sim;

namespace RankE.Game.Tests
{
    /// <summary>
    /// The headless-sweep resolution path: a <see cref="SweepScenario"/> built from repo data
    /// (TuningPresets/Tutorial.json + Opponents/tutorial.json) resolves the same way a played
    /// fight would, fails loudly on unknown names, and is deterministic per (fights, seed) —
    /// so A/B comparisons diff tunings, never run-to-run noise.
    /// </summary>
    public class SweepScenarioTests
    {
        [Test]
        public void FromPreset_Tutorial_ResolvesOpponentAndBuilds()
        {
            var scenario = SweepScenario.FromPreset("Tutorial");

            Assert.NotNull(scenario.Profile.Opponent, "Tutorial preset references the tutorial opponent");
            Assert.AreEqual("tutorial", scenario.Profile.Opponent.id);
            Assert.IsTrue(scenario.Profile.Opponent.HasRotation);
            Assert.AreEqual("Sparring Mage", scenario.Profile.Adversary.Name, "opponent build overlaid onto the adversary");
            Assert.IsTrue(scenario.Label.Contains("Tutorial"));
            Assert.IsTrue(scenario.Profile.Abilities.ContainsKey(DefaultContent.SlashId), "library seeded");
        }

        [Test]
        public void FromPreset_OpponentOverride_WinsOverPresetAdversary()
        {
            var scenario = SweepScenario.FromPreset(null, "tutorial");
            Assert.NotNull(scenario.Profile.Opponent);
            Assert.AreEqual("tutorial", scenario.Profile.Opponent.id);
        }

        [Test]
        public void FromPreset_UnknownPreset_Throws()
            => Assert.Throws<ArgumentException>(() => SweepScenario.FromPreset("NoSuchPreset__"));

        [Test]
        public void FromPreset_UnknownOpponent_Throws()
            => Assert.Throws<ArgumentException>(() => SweepScenario.FromPreset(null, "no_such_opponent__"));

        [Test]
        public void Run_SameSeed_IsDeterministic()
        {
            var a = SweepScenario.FromPreset("Tutorial").Run(20, 7);
            var b = SweepScenario.FromPreset("Tutorial").Run(20, 7);
            Assert.AreEqual(20, a.Fights);
            Assert.AreEqual(a.Summary(), b.Summary());
        }

        [Test]
        public void Run_Defaults_CompletesFights()
        {
            var stats = SweepScenario.FromPreset().Run(10, 3);
            Assert.AreEqual(10, stats.Fights);
            Assert.AreEqual(10, stats.WinsA + stats.WinsB + stats.Draws);
        }

        [Test]
        public void Run_DoesNotMutateTheProfile()
        {
            var scenario = SweepScenario.FromPreset("Tutorial");
            int gcdBefore = scenario.Profile.Tuning.GcdTicks;
            int slashCdBefore = scenario.Profile.Abilities[DefaultContent.SlashId].CooldownTicks;
            scenario.Run(5, 1);
            Assert.AreEqual(gcdBefore, scenario.Profile.Tuning.GcdTicks);
            Assert.AreEqual(slashCdBefore, scenario.Profile.Abilities[DefaultContent.SlashId].CooldownTicks);
        }
    }
}
