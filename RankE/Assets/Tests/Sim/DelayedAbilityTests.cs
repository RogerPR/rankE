using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class DelayedAbilityTests
    {
        [Test]
        public void FallingStar_LandsAfterDelay()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.FallingStar()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, DefaultContent.FallingStarId);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.DelayedScheduled).Count);
            Assert.AreEqual(1, b.Pending.Count);
            Assert.AreEqual(7, b.Fighters[0].SpellGems); // gem spent on use
            Assert.AreEqual(500, b.Fighters[1].Hp);

            TestKit.StepUntil(b, 61); // delay 60t
            var dmg = TestKit.EventsOf(b, SimEventType.Damaged, DefaultContent.FallingStarId);
            Assert.AreEqual(1, dmg.Count);
            Assert.AreEqual(60, dmg[0].Tick);
            Assert.AreEqual(470, b.Fighters[1].Hp);
            Assert.AreEqual(0, b.Pending.Count);
        }

        [Test]
        public void Delayed_FiresEvenIfCasterStunnedMeanwhile()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.FallingStar()),
                TestKit.Config("B", TestKit.Stunner()));

            TestKit.UseAt(b, 0, 0, DefaultContent.FallingStarId);
            TestKit.UseAt(b, 30, 1, "test_stunner"); // the star is already falling

            TestKit.StepUntil(b, 61);
            Assert.AreEqual(470, b.Fighters[1].Hp);
        }
    }
}
