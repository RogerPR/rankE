using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class BreakBarTests
    {
        [Test]
        public void BreakDamage_Accumulates()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Bash()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.BashId);
            Assert.AreEqual(20, b.Fighters[1].BreakBar);
        }

        [Test]
        public void NoDecay_DuringGracePeriod()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", TestKit.Smash()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, "test_smash"); // 60 break at tick 0
            TestKit.StepUntil(b, 60);             // last tick before decay starts
            Assert.AreEqual(60, b.Fighters[1].BreakBar);
        }

        [Test]
        public void Decays_AfterGrace_TwoPerSecond()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", TestKit.Smash()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, "test_smash");
            TestKit.StepUntil(b, 61); // decay tick at 60
            Assert.AreEqual(59, b.Fighters[1].BreakBar);

            TestKit.StepUntil(b, 161); // +10 more decay ticks (every 10t)
            Assert.AreEqual(49, b.Fighters[1].BreakBar);
        }

        [Test]
        public void BrokenAt100_StunnedVulnerable_BarResets()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", TestKit.Smash(), PocContent.Slash()),
                TestKit.Config("B", PocContent.Slash()));

            TestKit.UseAt(b, 0, 0, "test_smash");
            TestKit.UseAt(b, 1, 0, "test_smash"); // 120 ≥ 100 → BROKEN

            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.Broken).Count);
            Assert.AreEqual(0, b.Fighters[1].BreakBar);
            Assert.IsTrue(b.Fighters[1].HasStatus(PocContent.BrokenStatus));

            // Broken fighter can't act…
            TestKit.UseAt(b, 5, 1, PocContent.SlashId);
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId));

            // …and takes +50% damage.
            TestKit.UseAt(b, 10, 0, PocContent.SlashId); // after A's own quick GCD

            Assert.AreEqual(485, b.Fighters[1].Hp); // 10 × 1.5

            // BROKEN lasts 2.5s (50t from tick 1).
            TestKit.StepUntil(b, 52);
            Assert.IsFalse(b.Fighters[1].HasStatus(PocContent.BrokenStatus));
        }
    }
}
