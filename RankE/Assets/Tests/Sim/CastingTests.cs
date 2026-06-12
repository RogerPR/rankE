using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class CastingTests
    {
        [Test]
        public void Cast_FiresAfterCastTicks()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Fireball()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.FireballId);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.CastStarted).Count);
            Assert.AreEqual(500, b.Fighters[1].Hp);

            TestKit.StepUntil(b, 41); // fireball cast = 40t, lands at tick 40
            var dmg = TestKit.EventsOf(b, SimEventType.Damaged, PocContent.FireballId);
            Assert.AreEqual(1, dmg.Count);
            Assert.AreEqual(40, dmg[0].Tick);
            Assert.AreEqual(460, b.Fighters[1].Hp);
        }

        [Test]
        public void CannotActWhileCasting()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Fireball(), PocContent.Slash()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.FireballId);
            TestKit.UseAt(b, 25, 0, PocContent.SlashId); // mid-cast, GCD already over
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId));

            TestKit.UseAt(b, 41, 0, PocContent.SlashId); // cast done
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId).Count);
        }

        [Test]
        public void Kick_InterruptsInterruptibleCast_AndStuns()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Fireball()),
                TestKit.Config("B", PocContent.Kick()));

            TestKit.UseAt(b, 0, 0, PocContent.FireballId);
            TestKit.UseAt(b, 10, 1, PocContent.KickId);

            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.CastInterrupted, PocContent.KickId).Count);
            Assert.IsTrue(b.Fighters[0].HasStatus(PocContent.StunStatus));
            Assert.AreEqual(498, b.Fighters[0].Hp); // kick damage 2

            TestKit.StepUntil(b, 60);
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.Damaged, PocContent.FireballId));
            Assert.AreEqual(500, b.Fighters[1].Hp);
        }

        [Test]
        public void Kick_DoesNotInterrupt_UninterruptibleCast()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", TestKit.UninterruptibleCast()),
                TestKit.Config("B", PocContent.Kick()));

            TestKit.UseAt(b, 0, 0, "test_uncast");
            TestKit.UseAt(b, 10, 1, PocContent.KickId);

            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.CastInterrupted));
            Assert.IsFalse(b.Fighters[0].HasStatus(PocContent.StunStatus)); // stun only on success
            Assert.AreEqual(498, b.Fighters[0].Hp); // kick damage still lands

            TestKit.StepUntil(b, 41);
            Assert.AreEqual(460, b.Fighters[1].Hp); // cast completed anyway
        }

        [Test]
        public void StunCancelsCast()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Fireball()),
                TestKit.Config("B", TestKit.Stunner()));

            TestKit.UseAt(b, 0, 0, PocContent.FireballId);
            TestKit.UseAt(b, 10, 1, "test_stunner");

            TestKit.StepUntil(b, 60);
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.Damaged, PocContent.FireballId));
            Assert.IsFalse(b.Fighters[0].IsCasting);
        }

        [Test]
        public void InterruptCastSelf_CancelsOwnCast_NoCooldownSpent()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Fireball(), PocContent.InterruptCast()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.FireballId);
            TestKit.UseAt(b, 10, 0, PocContent.InterruptCastId); // usable while casting
            Assert.IsFalse(b.Fighters[0].IsCasting);
            Assert.AreEqual(0, b.Fighters[0].GetAbility(PocContent.FireballId).CooldownRemaining);

            TestKit.UseAt(b, 20, 0, PocContent.FireballId); // recast right after GCD
            Assert.AreEqual(2, TestKit.EventsOf(b, SimEventType.CastStarted, PocContent.FireballId).Count);
        }

        [Test]
        public void GemConsumedOnCompletion_NotOnStart()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Fireball()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.FireballId);
            TestKit.StepUntil(b, 20);
            Assert.AreEqual(8, b.Fighters[0].SpellGems); // mid-cast: unspent

            TestKit.StepUntil(b, 41);
            Assert.AreEqual(7, b.Fighters[0].SpellGems);
        }

        [Test]
        public void CannotCastWithZeroGems()
        {
            var a = TestKit.Config("A", PocContent.Fireball());
            a.SpellGems = 0;
            var b = TestKit.Duel(a, TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.FireballId);
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.CastStarted));
        }
    }
}
