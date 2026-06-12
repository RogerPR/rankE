using System.Linq;
using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class AutoAttackTests
    {
        static FighterConfig WithAutoAttack(FighterConfig cfg)
        {
            cfg.AutoAttack = PocContent.AutoAttack();
            return cfg;
        }

        [Test]
        public void FiresImmediately_ThenEveryInterval()
        {
            var b = TestKit.Duel(
                WithAutoAttack(TestKit.Config("A")),
                TestKit.Config("B"));

            TestKit.StepUntil(b, 81);
            var hits = TestKit.EventsOf(b, SimEventType.Damaged, PocContent.AutoAttackId);
            CollectionAssert.AreEqual(new[] { 0, 40, 80 }, hits.Select(e => e.Tick).ToList());
            Assert.AreEqual(485, b.Fighters[1].Hp);
        }

        [Test]
        public void SuppressedWhileCasting()
        {
            var b = TestKit.Duel(
                WithAutoAttack(TestKit.Config("A", PocContent.Fireball())),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.FireballId); // AA fires at 0, cast until 40
            TestKit.StepUntil(b, 42);

            var hits = TestKit.EventsOf(b, SimEventType.Damaged, PocContent.AutoAttackId);
            CollectionAssert.AreEqual(new[] { 0, 41 }, hits.Select(e => e.Tick).ToList());
        }

        [Test]
        public void SuppressedWhileStunned()
        {
            var b = TestKit.Duel(
                WithAutoAttack(TestKit.Config("A")),
                TestKit.Config("B", TestKit.Stunner()));

            TestKit.UseAt(b, 35, 1, "test_stunner"); // stun ticks 35–64
            TestKit.StepUntil(b, 70);

            var ticks = TestKit.EventsOf(b, SimEventType.Damaged, PocContent.AutoAttackId)
                .Select(e => e.Tick).ToList();
            CollectionAssert.AreEqual(new[] { 0, 65 }, ticks); // due at 40, held until stun ends
        }

        [Test]
        public void AutoAttack_IsParriable()
        {
            var b = TestKit.Duel(
                WithAutoAttack(TestKit.Config("A")),
                TestKit.Config("B", PocContent.Parry()));

            TestKit.UseAt(b, 35, 1, PocContent.ParryId); // window 35–46 covers AA at 40
            TestKit.StepUntil(b, 50);

            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.Parried, PocContent.AutoAttackId).Count);
            Assert.AreEqual(495, b.Fighters[1].Hp); // only the tick-0 hit landed
        }
    }
}
