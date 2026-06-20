using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class CooldownTests
    {
        [Test]
        public void NormalAbility_SetsGcd_BlocksOtherNormals()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.Slash(), DefaultContent.Bash()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, DefaultContent.SlashId);
            TestKit.UseAt(b, 10, 0, DefaultContent.BashId); // GCD still running → ignored
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityUsed, DefaultContent.BashId));

            TestKit.UseAt(b, 20, 0, DefaultContent.BashId); // GCD (20t) expired
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, DefaultContent.BashId).Count);
        }

        [Test]
        public void QuickAction_IgnoresGcd()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.Slash(), DefaultContent.Parry()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, DefaultContent.SlashId);
            TestKit.UseAt(b, 1, 0, DefaultContent.ParryId); // GCD active, quick still fires
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, DefaultContent.ParryId).Count);
        }

        [Test]
        public void QuickAction_SetsShortGcd()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.Slash(), DefaultContent.Parry()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, DefaultContent.ParryId);
            TestKit.UseAt(b, 3, 0, DefaultContent.SlashId); // quick GCD (6t) running
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityUsed, DefaultContent.SlashId));

            TestKit.UseAt(b, 6, 0, DefaultContent.SlashId);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, DefaultContent.SlashId).Count);
        }

        [Test]
        public void QuickAction_DoesNotShortenRunningGcd()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.Slash(), DefaultContent.Bash(), DefaultContent.Parry()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, DefaultContent.SlashId);  // GCD until tick 20
            TestKit.UseAt(b, 2, 0, DefaultContent.ParryId);  // must not reset GCD to 6
            TestKit.UseAt(b, 10, 0, DefaultContent.BashId);
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityUsed, DefaultContent.BashId));

            TestKit.UseAt(b, 20, 0, DefaultContent.BashId);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, DefaultContent.BashId).Count);
        }

        [Test]
        public void AbilityCooldown_BlocksUntilExpired()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.Slash()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, DefaultContent.SlashId);
            TestKit.UseAt(b, 40, 0, DefaultContent.SlashId); // cooldown is 80t
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, DefaultContent.SlashId).Count);

            TestKit.UseAt(b, 80, 0, DefaultContent.SlashId);
            var uses = TestKit.EventsOf(b, SimEventType.AbilityUsed, DefaultContent.SlashId);
            Assert.AreEqual(2, uses.Count);
            Assert.AreEqual(80, uses[1].Tick);
        }
    }
}
