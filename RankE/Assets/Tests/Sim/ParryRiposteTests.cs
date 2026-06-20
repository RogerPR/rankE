using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class ParryRiposteTests
    {
        [Test]
        public void Parry_NegatesParriableDamage()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.Slash()),
                TestKit.Config("B", DefaultContent.Parry()));

            TestKit.UseAt(b, 0, 1, DefaultContent.ParryId); // window ticks 0–11
            TestKit.UseAt(b, 1, 0, DefaultContent.SlashId);

            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.Parried).Count);
            Assert.AreEqual(500, b.Fighters[1].Hp);
        }

        [Test]
        public void ParryWindow_Expires()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.Slash()),
                TestKit.Config("B", DefaultContent.Parry()));

            TestKit.UseAt(b, 0, 1, DefaultContent.ParryId);
            TestKit.UseAt(b, 12, 0, DefaultContent.SlashId); // window (12t) just closed

            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.Parried));
            Assert.AreEqual(490, b.Fighters[1].Hp);
        }

        [Test]
        public void NonParriable_IgnoresWindow()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.Kick()),
                TestKit.Config("B", DefaultContent.Parry()));

            TestKit.UseAt(b, 0, 1, DefaultContent.ParryId);
            TestKit.UseAt(b, 1, 0, DefaultContent.KickId); // kick is not parriable

            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.Parried));
            Assert.AreEqual(498, b.Fighters[1].Hp);
        }

        [Test]
        public void Parry_AddsBreakToAttacker()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.Slash()),
                TestKit.Config("B", DefaultContent.Parry()));

            TestKit.UseAt(b, 0, 1, DefaultContent.ParryId);
            TestKit.UseAt(b, 1, 0, DefaultContent.SlashId);

            Assert.AreEqual(15, b.Fighters[0].BreakBar);
            Assert.AreEqual(2, b.Fighters[1].RiposteCounter);
        }

        [Test]
        public void FourParries_TriggerAutomaticRiposte()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", TestKit.Jab()),
                TestKit.Config("B", TestKit.FastParry()));

            for (int cycle = 0; cycle < 4; cycle++)
            {
                TestKit.UseAt(b, cycle * 14, 1, "test_fast_parry");
                TestKit.UseAt(b, cycle * 14 + 1, 0, "test_jab");
            }

            Assert.AreEqual(4, TestKit.EventsOf(b, SimEventType.Parried).Count);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.RiposteTriggered).Count);
            Assert.AreEqual(0, b.Fighters[1].RiposteCounter); // reset after riposte
            Assert.AreEqual(460, b.Fighters[0].Hp); // riposte damage 40; all jabs negated
            Assert.IsTrue(b.Fighters[0].HasStatus(DefaultContent.StunStatus));
            Assert.AreEqual(500, b.Fighters[1].Hp);
            Assert.AreEqual(90, b.Fighters[0].BreakBar); // 4×15 parry break + 30 riposte break
        }
    }
}
