using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class CooldownTests
    {
        [Test]
        public void NormalAbility_SetsGcd_BlocksOtherNormals()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash(), PocContent.Bash()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            TestKit.UseAt(b, 10, 0, PocContent.BashId); // GCD still running → ignored
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.BashId));

            TestKit.UseAt(b, 20, 0, PocContent.BashId); // GCD (20t) expired
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.BashId).Count);
        }

        [Test]
        public void QuickAction_IgnoresGcd()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash(), PocContent.Parry()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            TestKit.UseAt(b, 1, 0, PocContent.ParryId); // GCD active, quick still fires
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.ParryId).Count);
        }

        [Test]
        public void QuickAction_SetsShortGcd()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash(), PocContent.Parry()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.ParryId);
            TestKit.UseAt(b, 3, 0, PocContent.SlashId); // quick GCD (6t) running
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId));

            TestKit.UseAt(b, 6, 0, PocContent.SlashId);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId).Count);
        }

        [Test]
        public void QuickAction_DoesNotShortenRunningGcd()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash(), PocContent.Bash(), PocContent.Parry()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.SlashId);  // GCD until tick 20
            TestKit.UseAt(b, 2, 0, PocContent.ParryId);  // must not reset GCD to 6
            TestKit.UseAt(b, 10, 0, PocContent.BashId);
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.BashId));

            TestKit.UseAt(b, 20, 0, PocContent.BashId);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.BashId).Count);
        }

        [Test]
        public void AbilityCooldown_BlocksUntilExpired()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            TestKit.UseAt(b, 40, 0, PocContent.SlashId); // cooldown is 80t
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId).Count);

            TestKit.UseAt(b, 80, 0, PocContent.SlashId);
            var uses = TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId);
            Assert.AreEqual(2, uses.Count);
            Assert.AreEqual(80, uses[1].Tick);
        }
    }
}
