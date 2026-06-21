using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RankE.Sim.Tests
{
    /// <summary>
    /// The player-only colour-sequence combo: a random target order of colours (drawn from the
    /// colours the player's abilities carry) that, pressed in order, grants the EMPOWERED status
    /// so the next damaging hit is doubled. Wrong colour → reshuffle. Enemies never combo.
    /// </summary>
    public class ComboTests
    {
        const string Red = DefaultContent.ColorRed;
        const string Yellow = DefaultContent.ColorYellow;

        /// <summary>Normal-GCD, no-cooldown, 5-damage attack tagged with a combo colour.</summary>
        static AbilityDef ColorAttack(string id, string color) => new AbilityDef
        {
            Id = id,
            Name = id,
            Gcd = GcdClass.Normal,
            CooldownTicks = 0,
            ComboColor = color,
            Effects = new List<EffectDef> { EffectDef.Damage(5) },
        };

        static FighterConfig Player(params AbilityDef[] abilities)
        {
            var cfg = TestKit.Config("Player", abilities);
            cfg.UsesComboSystem = true;
            return cfg;
        }

        // Pressing the same colour repeatedly always matches when that's the only colour owned,
        // so the chain is fully controllable without knowing the random sequence up front.
        const string RedId = "red_attack";
        const string YellowId = "yellow_attack";

        static Battle SingleColorDuel() => TestKit.Duel(
            Player(ColorAttack(RedId, Red)), TestKit.Config("B"));

        [Test]
        public void Sequence_UsesOnlyOwnedColors_AndLengthInRange()
        {
            var b = SingleColorDuel();
            TestKit.UseAt(b, 0, 0, RedId); // first coloured press generates the sequence

            var seq = b.Fighters[0].ComboSequence;
            Assert.IsTrue(seq.Count >= b.Tuning.ComboMinLen && seq.Count <= b.Tuning.ComboMaxLen,
                $"length {seq.Count} out of [{b.Tuning.ComboMinLen},{b.Tuning.ComboMaxLen}]");
            Assert.IsTrue(seq.All(c => c == Red), "only owned colour (red) may appear");
        }

        [Test]
        public void CompletingChain_EmpowersNextHit_ThenConsumes()
        {
            var b = SingleColorDuel();

            int tick = 0;
            TestKit.UseAt(b, tick, 0, RedId);
            // Same colour always matches, so pressing red on the GCD completes the chain.
            while (!TestKit.EventsOf(b, SimEventType.ComboCompleted).Any() && tick < 400)
            {
                tick += TestKit.RefGcdTicks;
                TestKit.UseAt(b, tick, 0, RedId);
            }
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.ComboCompleted).Count,
                "exactly one completion");
            Assert.IsTrue(b.Fighters[0].HasStatus(DefaultContent.EmpoweredStatus),
                "completion grants empowered");

            // The completing hit itself was not doubled (still 5).
            Assert.IsFalse(TestKit.EventsOf(b, SimEventType.Damaged, RedId).Any(e => e.Amount == 10),
                "completing hit must not be the doubled one");

            // Next hit is doubled (5 → 10), then the buff is spent.
            tick += TestKit.RefGcdTicks;
            TestKit.UseAt(b, tick, 0, RedId);
            Assert.AreEqual(1, TestKit.EventsOf(b, SimEventType.Damaged, RedId).Count(e => e.Amount == 10),
                "exactly one doubled hit");
            Assert.IsFalse(b.Fighters[0].HasStatus(DefaultContent.EmpoweredStatus),
                "empowered consumed after the doubled hit");
        }

        [Test]
        public void WrongColor_ReshufflesSequence()
        {
            var b = TestKit.Duel(
                Player(ColorAttack(RedId, Red), ColorAttack(YellowId, Yellow)),
                TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, RedId); // generate + a first match attempt
            var f = b.Fighters[0];
            string expected = f.ComboSequence[f.ComboProgress];
            string wrongId = expected == Red ? YellowId : RedId;

            int resetsBefore = TestKit.EventsOf(b, SimEventType.ComboReset).Count;
            TestKit.UseAt(b, TestKit.RefGcdTicks, 0, wrongId);
            Assert.Greater(TestKit.EventsOf(b, SimEventType.ComboReset).Count, resetsBefore,
                "a wrong colour reshuffles the sequence");
        }

        [Test]
        public void Enemy_NeverCombos()
        {
            // Enemy (fighter 1) owns a coloured ability but UsesComboSystem stays false.
            var enemy = TestKit.Config("B", ColorAttack(RedId, Red)); // no UsesComboSystem
            var b = TestKit.Duel(TestKit.Config("A"), enemy);

            TestKit.UseAt(b, 0, 1, RedId);
            TestKit.UseAt(b, TestKit.RefGcdTicks, 1, RedId);

            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.ComboAdvanced));
            Assert.IsEmpty(TestKit.EventsOf(b, SimEventType.ComboCompleted));
            Assert.IsEmpty(b.Fighters[1].ComboSequence);
        }
    }
}
