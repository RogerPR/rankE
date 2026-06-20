using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RankE.Sim.Tests
{
    /// <summary>
    /// The scripted enemy brain: a steady, predictable non-quick rotation (its plan), with
    /// reactive quick defense that never disturbs that plan.
    /// </summary>
    public class ScriptedRhythmBehaviorTests
    {
        // A clean beat ability: Normal-GCD, no cooldown/locks, so the cadence is governed by
        // the behavior's interval (not an ability cooldown).
        static AbilityDef Beat(string id) => new AbilityDef
        {
            Id = id,
            Name = id,
            Gcd = GcdClass.Normal,
            Effects = new List<EffectDef> { EffectDef.Damage(1) },
        };

        static void Drive(Battle b, IBehavior a0, int ticks)
        {
            for (int t = 0; t < ticks && !b.IsOver; t++)
            {
                b.SubmitIntent(0, a0?.Decide(b, 0));
                b.SubmitIntent(1, null);
                b.Step();
            }
        }

        [Test]
        public void Upcoming_FollowsRotationOrder_AndAdvancesOnlyOnBeats()
        {
            var ai = new ScriptedRhythmBehavior(new[] { "beat_a", "beat_b" }, 40);
            CollectionAssert.AreEqual(new[] { "beat_a", "beat_b", "beat_a" }, ai.Upcoming(3));

            var b = TestKit.Duel(TestKit.Config("E", Beat("beat_a"), Beat("beat_b")), TestKit.Config("T"));
            Drive(b, ai, 41); // long enough for exactly one beat (interval 40)

            // One beat fired, plan rotated by one.
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, "beat_a").Count);
            CollectionAssert.AreEqual(new[] { "beat_b", "beat_a", "beat_b" }, ai.Upcoming(3));
        }

        [Test]
        public void Beats_FireOnCadence_InRotationOrder()
        {
            var ai = new ScriptedRhythmBehavior(new[] { "beat_a", "beat_b" }, 40);
            var b = TestKit.Duel(TestKit.Config("E", Beat("beat_a"), Beat("beat_b")), TestKit.Config("T"));

            Drive(b, ai, 130); // ~3 beats at interval 40

            var beats = b.Events
                .Where(e => e.Type == SimEventType.AbilityUsed &&
                            (e.AbilityId == "beat_a" || e.AbilityId == "beat_b"))
                .ToList();
            Assert.AreEqual(3, beats.Count);
            CollectionAssert.AreEqual(new[] { "beat_a", "beat_b", "beat_a" },
                beats.Select(e => e.AbilityId).ToArray());

            // Roughly evenly spaced by the interval (telegraph isn't applied at this layer).
            for (int i = 1; i < beats.Count; i++)
                Assert.AreEqual(40, beats[i].Tick - beats[i - 1].Tick);
        }

        [Test]
        public void QuickDefense_StaysReactive_WithoutAdvancingThePlan()
        {
            var ai = new ScriptedRhythmBehavior(new[] { "beat_a" }, 40);
            var cfg = TestKit.Config("E", Beat("beat_a"), PocContent.Parry());
            var b = TestKit.Duel(cfg, TestKit.Config("T"));

            // Before any beat fires, a ready Parry is used reactively...
            Drive(b, ai, 5);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.AbilityUsed, PocContent.ParryId).Count);
            // ...and the plan is untouched (still pointing at the same beat).
            CollectionAssert.AreEqual(new[] { "beat_a" }, ai.Upcoming(1));
            Assert.AreEqual(0, TestKit.EventsOf(b, SimEventType.AbilityUsed, "beat_a").Count);
        }
    }
}
