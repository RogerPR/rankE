using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class AutoAttackTests
    {
        // A fixed-interval auto-attack so these tick-exact assertions test the auto-attack
        // MECHANIC, independent of the shipped interval in DefaultContent (free to retune).
        const int Interval = 40;

        static AbilityDef RefAutoAttack() => new AbilityDef
        {
            Id = DefaultContent.AutoAttackId,
            Name = "Auto-attack",
            Gcd = GcdClass.None,
            CooldownTicks = Interval,
            Parriable = true,
            IsMelee = true,
            Effects = new List<EffectDef> { EffectDef.Damage(5) },
        };

        static FighterConfig WithAutoAttack(FighterConfig cfg)
        {
            cfg.AutoAttack = RefAutoAttack();
            return cfg;
        }

        [Test]
        public void FiresImmediately_ThenEveryInterval()
        {
            var b = TestKit.Duel(
                WithAutoAttack(TestKit.Config("A")),
                TestKit.Config("B"));

            TestKit.StepUntil(b, 81);
            var hits = TestKit.EventsOf(b, SimEventType.Damaged, DefaultContent.AutoAttackId);
            CollectionAssert.AreEqual(new[] { 0, 40, 80 }, hits.Select(e => e.Tick).ToList());
            Assert.AreEqual(485, b.Fighters[1].Hp);
        }

        [Test]
        public void SuppressedWhileCasting()
        {
            var b = TestKit.Duel(
                WithAutoAttack(TestKit.Config("A", DefaultContent.Fireball())),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, DefaultContent.FireballId); // AA fires at 0, cast until 40
            TestKit.StepUntil(b, 42);

            var hits = TestKit.EventsOf(b, SimEventType.Damaged, DefaultContent.AutoAttackId);
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

            var ticks = TestKit.EventsOf(b, SimEventType.Damaged, DefaultContent.AutoAttackId)
                .Select(e => e.Tick).ToList();
            CollectionAssert.AreEqual(new[] { 0, 65 }, ticks); // due at 40, held until stun ends
        }

        [Test]
        public void AutoAttack_IsParriable()
        {
            var b = TestKit.Duel(
                WithAutoAttack(TestKit.Config("A")),
                TestKit.Config("B", DefaultContent.Parry()));

            TestKit.UseAt(b, 35, 1, DefaultContent.ParryId); // window 35–46 covers AA at 40
            TestKit.StepUntil(b, 50);

            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.Parried, DefaultContent.AutoAttackId).Count);
            Assert.AreEqual(495, b.Fighters[1].Hp); // only the tick-0 hit landed
        }
    }
}
