using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class AnimationLockTests
    {
        [Test]
        public void Effect_FiresAfterPreLock()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", TestKit.HeavyBlow()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, "test_heavy_blow"); // pre-lock 10t
            Assert.AreEqual(500, b.Fighters[1].Hp);    // nothing yet

            TestKit.StepUntil(b, 11);
            var dmg = TestKit.EventsOf(b, SimEventType.Damaged, "test_heavy_blow");
            Assert.AreEqual(1, dmg.Count);
            Assert.AreEqual(10, dmg[0].Tick);
            Assert.AreEqual(493, b.Fighters[1].Hp);
        }

        [Test]
        public void CannotAct_DuringPreOrPostLock()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", TestKit.HeavyBlow(), TestKit.Jab()),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, "test_heavy_blow");
            TestKit.UseAt(b, 5, 0, "test_jab");  // wind-up (quick would otherwise fire)
            TestKit.UseAt(b, 15, 0, "test_jab"); // post-lock (10t after effect at 10)
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.AbilityUsed, "test_jab"));

            TestKit.UseAt(b, 20, 0, "test_jab"); // lock over
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, "test_jab").Count);
        }

        [Test]
        public void Stun_DuringPreLock_CancelsEffect()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", TestKit.HeavyBlow()),
                TestKit.Config("B", TestKit.Stunner()));

            TestKit.UseAt(b, 0, 0, "test_heavy_blow");
            TestKit.UseAt(b, 5, 1, "test_stunner");

            TestKit.StepUntil(b, 30);
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.Damaged, "test_heavy_blow"));
            Assert.AreEqual(500, b.Fighters[1].Hp);
        }
    }
}
