using NUnit.Framework;

namespace RankE.Sim.Tests
{
    /// <summary>
    /// The Combat Tuning tool edits a held source that each new fight clones from, so a
    /// clone must reproduce every field yet stay fully independent (editing the copy can't
    /// reach back into the running, deterministic fight).
    /// </summary>
    public class CloneTests
    {
        [Test]
        public void CombatTuning_Clone_CopiesFields_Independently()
        {
            var src = new CombatTuning();
            var copy = src.Clone();

            Assert.AreEqual(src.GcdTicks, copy.GcdTicks);
            Assert.AreEqual(src.ParryRiposteGain, copy.ParryRiposteGain);
            Assert.AreEqual(src.BrokenDurationTicks, copy.BrokenDurationTicks);
            Assert.AreEqual(src.EmpoweredDurationTicks, copy.EmpoweredDurationTicks);

            copy.GcdTicks = 999;
            Assert.AreNotEqual(copy.GcdTicks, src.GcdTicks, "Editing the clone must not touch the source.");
        }

        [Test]
        public void AbilityDef_Clone_DeepCopiesEffects()
        {
            var src = DefaultContent.Bash(); // has Damage + Status + Break effects
            var copy = src.Clone();

            Assert.AreEqual(src.Id, copy.Id);
            Assert.AreEqual(src.CooldownTicks, copy.CooldownTicks);
            Assert.AreEqual(src.Effects.Count, copy.Effects.Count);
            Assert.AreNotSame(src.Effects, copy.Effects);
            for (int i = 0; i < src.Effects.Count; i++)
            {
                Assert.AreNotSame(src.Effects[i], copy.Effects[i], "Effects must be cloned, not shared.");
                Assert.AreEqual(src.Effects[i].Kind, copy.Effects[i].Kind);
                Assert.AreEqual(src.Effects[i].Amount, copy.Effects[i].Amount);
            }

            copy.CooldownTicks = 12345;
            copy.Effects[0].Amount = 777;
            Assert.AreNotEqual(copy.CooldownTicks, src.CooldownTicks);
            Assert.AreNotEqual(copy.Effects[0].Amount, src.Effects[0].Amount,
                "Editing a cloned effect must not touch the source.");
        }
    }
}
