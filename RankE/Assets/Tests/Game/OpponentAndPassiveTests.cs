using System.Collections.Generic;
using NUnit.Framework;
using RankE.Sim;

namespace RankE.Game.Tests
{
    /// <summary>
    /// Builds resolve passives (auto-attack is opt-in now), and the data-driven tutorial opponent
    /// + scenario load from disk and wire the adversary's build, AI logic and visual.
    /// </summary>
    public class OpponentAndPassiveTests
    {
        static FighterBuild BuildWith(List<string> passiveIds) => new FighterBuild
        {
            AbilityIds = new List<string> { DefaultContent.SlashId },
            PassiveIds = passiveIds,
        };

        [Test]
        public void NoPassives_MeansNoAutoAttack()
        {
            var profile = TuningProfile.FromDefaults();
            var cfg = BuildWith(new List<string>()).ToConfig(profile);

            Assert.IsNull(cfg.AutoAttack);
            Assert.IsEmpty(cfg.Passives);
        }

        [Test]
        public void AutoAttackPassive_PopulatesTheSwing()
        {
            var profile = TuningProfile.FromDefaults();
            var cfg = BuildWith(new List<string> { DefaultContent.AutoAttackPassiveId }).ToConfig(profile);

            Assert.IsNotNull(cfg.AutoAttack);
            Assert.AreEqual(DefaultContent.AutoAttackId, cfg.AutoAttack.Id);
            Assert.AreEqual(1, cfg.Passives.Count);
            Assert.AreEqual(PassiveKinds.AutoAttack, cfg.Passives[0].Kind);
        }

        [Test]
        public void TutorialOpponent_LoadsWithRotationAndNoPassive()
        {
            var def = OpponentStore.Load("tutorial");
            Assert.IsNotNull(def, "Opponents/tutorial.json should exist and parse");
            Assert.IsTrue(def.HasRotation);
            CollectionAssert.IsEmpty(def.build.passiveIds);

            var steps = def.ToRotationSteps();
            Assert.AreEqual(2, steps.Count);
            Assert.AreEqual(1, steps[0].Count);                 // Slash always
            Assert.AreEqual(2, steps[1].Count);                 // Slash / Fireball
            Assert.AreEqual(DefaultContent.SlashId, steps[0][0].Id);
        }

        [Test]
        public void TutorialScenario_ResolvesAdversaryFromOpponent()
        {
            var preset = TuningPresetStore.Load("Tutorial");
            Assert.IsNotNull(preset, "TuningPresets/Tutorial.json should exist");
            Assert.AreEqual("tutorial", preset.opponentId);

            var profile = TuningProfile.FromDefaults();
            preset.Apply(profile, null);

            Assert.IsNotNull(profile.Opponent);
            Assert.IsTrue(profile.Opponent.HasRotation);
            CollectionAssert.Contains(profile.Adversary.AbilityIds, DefaultContent.FireballId);

            // Player: tutorial loadout, no auto-attack, Block in a quick slot.
            CollectionAssert.IsEmpty(profile.Player.PassiveIds);
            CollectionAssert.Contains(profile.Player.AbilityIds, DefaultContent.BlockId);
        }
    }
}
