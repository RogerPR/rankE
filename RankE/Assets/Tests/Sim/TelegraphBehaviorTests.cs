using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class TelegraphBehaviorTests
    {
        /// <summary>Returns a fixed sequence of decisions, then null forever.</summary>
        sealed class Scripted : IBehavior
        {
            readonly Queue<string> decisions;
            public Scripted(params string[] decisions) => this.decisions = new Queue<string>(decisions);
            public string Decide(Battle battle, int selfIndex)
                => decisions.Count > 0 ? decisions.Dequeue() : null;
        }

        static Battle Drive(Battle b, IBehavior a0, IBehavior a1, int ticks)
        {
            for (int t = 0; t < ticks && !b.IsOver; t++)
            {
                b.SubmitIntent(0, a0?.Decide(b, 0));
                b.SubmitIntent(1, a1?.Decide(b, 1));
                b.Step();
            }
            return b;
        }

        [Test]
        public void NormalAbility_HeldForTelegraphTicks_ThenCommitted()
        {
            var b = TestKit.Duel(TestKit.Config("A", PocContent.Slash()), TestKit.Config("B"));
            var tele = new TelegraphBehavior(new Scripted(PocContent.SlashId), 10);

            Drive(b, tele, null, 11);

            var used = TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId);
            Assert.AreEqual(1, used.Count);
            Assert.AreEqual(10, used[0].Tick); // decided at tick 0, committed 10 ticks later
        }

        [Test]
        public void PendingIntent_VisibleDuringHold_ClearedOnCommit()
        {
            var b = TestKit.Duel(TestKit.Config("A", PocContent.Slash()), TestKit.Config("B"));
            var tele = new TelegraphBehavior(new Scripted(PocContent.SlashId), 10);

            // Decision tick: intent captured, nothing submitted.
            b.SubmitIntent(0, tele.Decide(b, 0));
            b.Step();
            Assert.AreEqual(PocContent.SlashId, tele.PendingIntent);
            Assert.AreEqual(10, tele.TicksUntilCommit);

            Drive(b, tele, null, 9);
            Assert.AreEqual(PocContent.SlashId, tele.PendingIntent);
            Assert.AreEqual(1, tele.TicksUntilCommit);

            Drive(b, tele, null, 1);
            Assert.IsNull(tele.PendingIntent);
            Assert.AreEqual(0, tele.TicksUntilCommit);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId).Count);
        }

        [Test]
        public void QuickAbility_PassesThroughUntelegraphed()
        {
            var b = TestKit.Duel(TestKit.Config("A", PocContent.Parry()), TestKit.Config("B"));
            var tele = new TelegraphBehavior(new Scripted(PocContent.ParryId), 10);

            Drive(b, tele, null, 1);

            Assert.IsNull(tele.PendingIntent);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.ParryId).Count);
            Assert.AreEqual(0, b.Events.First(e => e.Type == SimEventType.AbilityUsed).Tick);
        }

        [Test]
        public void ZeroTelegraphTicks_BehavesLikeInner()
        {
            var b = TestKit.Duel(TestKit.Config("A", PocContent.Slash()), TestKit.Config("B"));
            var tele = new TelegraphBehavior(new Scripted(PocContent.SlashId), 0);

            Drive(b, tele, null, 1);

            Assert.AreEqual(0, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.SlashId)[0].Tick);
        }

        [Test]
        public void WrappedPocProfiles_StayDeterministic_AndFightsTerminate()
        {
            string RunLog(int seed)
            {
                var b = new Battle(
                    PocContent.DefaultConfig("A"), PocContent.DefaultConfig("B"),
                    PocContent.CreateContent(), PocContent.CreateTuning(), seed);
                BattleRunner.RunSingle(b,
                    new TelegraphBehavior(new PocBehaviorProfile(), 10),
                    new TelegraphBehavior(new PocBehaviorProfile(), 10));
                Assert.IsTrue(b.IsOver, "wrapped AI fight must still end");
                return TestKit.Log(b);
            }

            Assert.AreEqual(RunLog(42), RunLog(42));
            Assert.AreNotEqual(RunLog(42), RunLog(43));
        }
    }
}
