using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class StatusTests
    {
        [Test]
        public void Poison_TicksEveryInterval_TotalDamageMatchesPoc()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", TestKit.PoisonDart()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, "test_poison_dart"); // poison 160t, -3 per 20t
            TestKit.StepUntil(b, 200);

            Assert.AreEqual(476, b.Fighters[1].Hp); // 8 applications × 3
            Assert.IsFalse(b.Fighters[1].HasStatus(PocContent.PoisonStatus));
        }

        [Test]
        public void Regen_Heals_ClampedAtMaxHp()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", TestKit.Jab()),
                TestKit.Config("B", TestKit.RegenPotion()));

            TestKit.UseAt(b, 0, 0, "test_jab");
            TestKit.UseAt(b, 1, 0, "test_jab");
            Assert.AreEqual(490, b.Fighters[1].Hp);

            TestKit.UseAt(b, 2, 1, "test_regen_potion"); // regen 120t, +2 per 20t = 12 max
            TestKit.StepUntil(b, 130);
            Assert.AreEqual(500, b.Fighters[1].Hp); // clamped at max, not 502
        }

        [Test]
        public void Stun_BlocksActions_UntilExpiry()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash()),
                TestKit.Config("B", TestKit.Stunner()));

            TestKit.UseAt(b, 0, 1, "test_stunner"); // stun 30t on A
            TestKit.UseAt(b, 5, 0, PocContent.SlashId);
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId));

            TestKit.UseAt(b, 30, 0, PocContent.SlashId); // stun expired this tick
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId).Count);
        }

        [Test]
        public void Reapply_RefreshesDuration()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash()),
                TestKit.Config("B", TestKit.Stunner()));

            TestKit.UseAt(b, 0, 1, "test_stunner");
            TestKit.UseAt(b, 10, 1, "test_stunner"); // refresh: now runs to tick 40
            TestKit.UseAt(b, 35, 0, PocContent.SlashId);
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId));

            TestKit.UseAt(b, 40, 0, PocContent.SlashId);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId).Count);
        }

        [Test]
        public void Distance_MeleeWhiffs()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash()),
                TestKit.Config("B", TestKit.Net()));

            TestKit.UseAt(b, 0, 1, "test_net"); // Distance on A
            TestKit.UseAt(b, 1, 0, PocContent.SlashId);

            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityWhiffed, PocContent.SlashId).Count);
            Assert.AreEqual(500, b.Fighters[1].Hp);
        }

        [Test]
        public void Distance_RangedStillLands()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Fireball()),
                TestKit.Config("B", TestKit.Net()));

            TestKit.UseAt(b, 0, 1, "test_net");
            TestKit.UseAt(b, 1, 0, PocContent.FireballId);
            TestKit.StepUntil(b, 45);

            Assert.AreEqual(460, b.Fighters[1].Hp);
        }

        [Test]
        public void Distance_GapCloserClears()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash(), PocContent.Lunge()),
                TestKit.Config("B", TestKit.Net()));

            TestKit.UseAt(b, 0, 1, "test_net");
            TestKit.UseAt(b, 1, 0, PocContent.LungeId);
            Assert.IsFalse(b.Fighters[0].HasStatus(PocContent.DistanceStatus));

            TestKit.UseAt(b, 21, 0, PocContent.SlashId); // after GCD
            Assert.AreEqual(490, b.Fighters[1].Hp);
        }

        [Test]
        public void Distance_EndsOnTimer()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash()),
                TestKit.Config("B", TestKit.Net(30)));

            TestKit.UseAt(b, 0, 1, "test_net");
            TestKit.UseAt(b, 35, 0, PocContent.SlashId); // distance expired at tick 30

            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityWhiffed));
            Assert.AreEqual(490, b.Fighters[1].Hp);
        }
    }
}
