using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RankE.Sim.Tests
{
    /// <summary>
    /// The weighted enemy rotation: single-option steps stay a deterministic, RNG-free rhythm;
    /// multi-option steps pick by weight off the seeded battle RNG, deterministically, and the
    /// pre-rolled choice is exactly what the plan (Upcoming) reports.
    /// </summary>
    public class WeightedRotationBehaviorTests
    {
        static AbilityDef Beat(string id) => new AbilityDef
        {
            Id = id, Name = id, Gcd = GcdClass.Normal,
            Effects = new List<EffectDef> { EffectDef.Damage(1) },
        };

        static List<IReadOnlyList<WeightedChoice>> Steps(params WeightedChoice[][] steps)
        {
            var list = new List<IReadOnlyList<WeightedChoice>>();
            foreach (var s in steps) list.Add(new List<WeightedChoice>(s));
            return list;
        }

        static WeightedChoice C(string id, double w) => new WeightedChoice(id, w);

        static List<string> DriveBeats(Battle b, IBehavior ai, int ticks)
        {
            var beats = new List<string>();
            for (int t = 0; t < ticks && !b.IsOver; t++)
            {
                int before = b.Events.Count;
                b.SubmitIntent(0, ai.Decide(b, 0));
                b.SubmitIntent(1, null);
                b.Step();
                foreach (var e in b.Events.Skip(before))
                    if (e.Type == SimEventType.AbilityUsed && (e.AbilityId == "a" || e.AbilityId == "c" || e.AbilityId == "main"))
                        beats.Add(e.AbilityId);
            }
            return beats;
        }

        [Test]
        public void SingleOption_FixedOrder_DrawsNoRng()
        {
            var ai = new WeightedRotationBehavior(Steps(new[] { C("a", 1) }, new[] { C("c", 1) }), 40);
            var b = TestKit.Duel(TestKit.Config("E", Beat("a"), Beat("c")), TestKit.Config("T"), seed: 777);

            var beats = DriveBeats(b, ai, 130); // ~3 beats at interval 40
            CollectionAssert.AreEqual(new[] { "a", "c", "a" }, beats);

            // A fixed rotation must not touch the battle RNG (keeps default fights byte-identical).
            Assert.AreEqual(new System.Random(777).Next(1_000_000), b.Rng.Next(1_000_000));
        }

        [Test]
        public void Weighted_IsDeterministicPerSeed()
        {
            List<string> Run()
            {
                var ai = new WeightedRotationBehavior(Steps(new[] { C("main", 1) }, new[] { C("a", 0.66), C("c", 0.34) }), 20);
                var b = TestKit.Duel(TestKit.Config("E", Beat("main"), Beat("a"), Beat("c")), TestKit.Config("T"), seed: 4242);
                return DriveBeats(b, ai, 220);
            }

            CollectionAssert.AreEqual(Run(), Run());
        }

        [Test]
        public void Weighted_PicksBothOptions_AcrossSeeds()
        {
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 50; seed++)
            {
                var ai = new WeightedRotationBehavior(Steps(new[] { C("main", 1) }, new[] { C("a", 0.66), C("c", 0.34) }), 20);
                var b = TestKit.Duel(TestKit.Config("E", Beat("main"), Beat("a"), Beat("c")), TestKit.Config("T"), seed: seed);
                foreach (var id in DriveBeats(b, ai, 220))
                    if (id == "a" || id == "c") seen.Add(id);
            }
            Assert.IsTrue(seen.Contains("a") && seen.Contains("c"), "both weighted options should occur across seeds");
        }

        [Test]
        public void Upcoming_MatchesWhatFires()
        {
            var ai = new WeightedRotationBehavior(Steps(new[] { C("main", 1) }, new[] { C("a", 0.66), C("c", 0.34) }), 20);
            var b = TestKit.Duel(TestKit.Config("E", Beat("main"), Beat("a"), Beat("c")), TestKit.Config("T"), seed: 99);

            var predicted = new List<string>();
            var fired = new List<string>();
            for (int t = 0; t < 220 && !b.IsOver; t++)
            {
                string pred = ai.Upcoming(1).FirstOrDefault(); // peek must not draw RNG
                int before = b.Events.Count;
                b.SubmitIntent(0, ai.Decide(b, 0));
                b.SubmitIntent(1, null);
                b.Step();
                foreach (var e in b.Events.Skip(before))
                    if (e.Type == SimEventType.AbilityUsed && (e.AbilityId == "a" || e.AbilityId == "c" || e.AbilityId == "main"))
                    {
                        fired.Add(e.AbilityId);
                        predicted.Add(pred);
                    }
            }

            CollectionAssert.AreEqual(fired, predicted);
            Assert.IsTrue(fired.Count >= 4, "expected several beats to fire");
        }

        [Test]
        public void Upcoming_MultiRow_MatchesNextFires()
        {
            // The HUD shows several rows of look-ahead; every row must be truthful, not just the
            // imminent one (the old bug showed each step's first option for rows 1+).
            var ai = new WeightedRotationBehavior(
                Steps(new[] { C("main", 1) }, new[] { C("a", 0.66), C("c", 0.34) }), 20);
            var b = TestKit.Duel(TestKit.Config("E", Beat("main"), Beat("a"), Beat("c")),
                TestKit.Config("T"), seed: 7);

            // Prime one tick so the committed look-ahead fills (no beat is due yet), then snapshot
            // the next five promised beats.
            b.SubmitIntent(0, ai.Decide(b, 0)); b.SubmitIntent(1, null); b.Step();
            var predicted = new List<string>(ai.Upcoming(5));

            var fired = new List<string>();
            for (int t = 0; t < 400 && fired.Count < predicted.Count && !b.IsOver; t++)
            {
                int before = b.Events.Count;
                b.SubmitIntent(0, ai.Decide(b, 0));
                b.SubmitIntent(1, null);
                b.Step();
                foreach (var e in b.Events.Skip(before))
                    if (e.Type == SimEventType.AbilityUsed && (e.AbilityId == "a" || e.AbilityId == "c" || e.AbilityId == "main"))
                        fired.Add(e.AbilityId);
            }

            Assert.AreEqual(5, fired.Count, "expected five beats to fire");
            CollectionAssert.AreEqual(predicted, fired);
        }
    }
}
