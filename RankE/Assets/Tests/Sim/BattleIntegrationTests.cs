using System.Diagnostics;
using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class BattleIntegrationTests
    {
        static Battle RunAiFight(int seed)
        {
            var battle = new Battle(
                PocContent.DefaultConfig("A"),
                PocContent.DefaultConfig("B"),
                PocContent.CreateContent(),
                PocContent.CreateTuning(),
                seed);
            return BattleRunner.RunSingle(battle, new PocBehaviorProfile(), new PocBehaviorProfile());
        }

        [Test]
        public void SameSeed_ProducesIdenticalEventLog()
        {
            var a = RunAiFight(42);
            var b = RunAiFight(42);
            Assert.AreEqual(TestKit.Log(a), TestKit.Log(b));
        }

        [Test]
        public void DifferentSeeds_Diverge()
        {
            var a = RunAiFight(1);
            var b = RunAiFight(2);
            Assert.AreNotEqual(TestKit.Log(a), TestKit.Log(b));
        }

        [Test]
        public void AiVsAi_FightCompletes()
        {
            var battle = RunAiFight(7);
            Assert.IsTrue(battle.IsOver, "fight should end before the tick cap");
            Assert.That(battle.Winner, Is.EqualTo(0).Or.EqualTo(1));
            Assert.Less(battle.CurrentTick, battle.Tuning.MaxTicks);
        }

        [Test]
        public void BattleRunner_1000Fights_RunsInSeconds()
        {
            var sw = Stopwatch.StartNew();
            var stats = BattleRunner.RunDefault(1000, 12345);
            sw.Stop();

            Assert.AreEqual(1000, stats.Fights);
            Assert.AreEqual(1000, stats.WinsA + stats.WinsB + stats.Draws);
            Assert.Greater(stats.WinsA, 0);
            Assert.Greater(stats.WinsB, 0);
            Assert.Greater(stats.Parries, 0);
            Assert.IsTrue(stats.AbilityUses.ContainsKey(PocContent.FireballId));

            TestContext.WriteLine($"1000 fights in {sw.ElapsedMilliseconds} ms");
            TestContext.WriteLine(stats.Summary());
            Assert.Less(sw.ElapsedMilliseconds, 20000, "1000 fights must run in seconds (DoD)");
        }

        [Test]
        public void BattleRunner_IsDeterministic()
        {
            var a = BattleRunner.RunDefault(50, 99).Summary();
            var b = BattleRunner.RunDefault(50, 99).Summary();
            Assert.AreEqual(a, b);
        }
    }
}
