using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class SimSmokeTests
    {
        [Test]
        public void TickRate_Is20TicksPerSecond()
        {
            Assert.AreEqual(20, SimConstants.TicksPerSecond);
            Assert.AreEqual(0.05f, SimConstants.TickDuration, 1e-6f);
        }

        [Test]
        public void SeededRandom_IsReproducible()
        {
            var a = new System.Random(12345);
            var b = new System.Random(12345);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(a.Next(), b.Next());
            }
        }
    }
}
