using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RankE.Sim.Tests
{
    /// <summary>
    /// Block's two independent physical defences: a −60% physical-damage status and a separate
    /// physical absorb pool. Both are physical-only (magic bypasses them), and a hit is reduced
    /// first, then soaked.
    /// </summary>
    public class ShieldTests
    {
        // Instant quick attacks so the assertions are tick-exact.
        static AbilityDef PhysJab() => new AbilityDef
        {
            Id = "test_phys_jab", Gcd = GcdClass.Quick, IsMelee = true,
            Effects = new List<EffectDef> { EffectDef.Damage(5) },
        };

        static AbilityDef MagicJab() => new AbilityDef
        {
            Id = "test_magic_jab", Gcd = GcdClass.Quick,
            Effects = new List<EffectDef> { EffectDef.Damage(5, Schools.Magic) },
        };

        // Self-buffs that isolate one layer (long duration so timing never interferes).
        static AbilityDef ReduceUp() => new AbilityDef
        {
            Id = "test_reduce", Gcd = GcdClass.Quick,
            Effects = new List<EffectDef> { EffectDef.Status(EffectTarget.Self, DefaultContent.BlockPhysStatus, 200) },
        };

        static AbilityDef ShieldUp(int amount) => new AbilityDef
        {
            Id = "test_shield", Gcd = GcdClass.Quick,
            Effects = new List<EffectDef> { EffectDef.Shield(EffectTarget.Self, DefaultContent.PhysShieldStatus, amount, 200) },
        };

        static int Absorb(Battle b, int fighter) =>
            b.Fighters[fighter].Statuses
                .Where(s => s.Def.Id == DefaultContent.PhysShieldStatus)
                .Select(s => s.AbsorbRemaining).FirstOrDefault();

        [Test]
        public void Reduction_CutsPhysical_LeavesMagic()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PhysJab(), MagicJab()),
                TestKit.Config("B", ReduceUp()));

            TestKit.UseAt(b, 0, 1, "test_reduce");      // B shields up first
            TestKit.UseAt(b, 1, 0, "test_phys_jab");    // 5 phys → ×0.4 = 2
            Assert.AreEqual(498, b.Fighters[1].Hp);

            TestKit.UseAt(b, 7, 0, "test_magic_jab");   // 5 magic → unaffected
            Assert.AreEqual(493, b.Fighters[1].Hp);
        }

        [Test]
        public void Shield_SoaksPhysical_ThenDepletes_IgnoresMagic()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PhysJab(), MagicJab()),
                TestKit.Config("B", ShieldUp(8)));

            TestKit.UseAt(b, 0, 1, "test_shield");
            TestKit.UseAt(b, 1, 0, "test_phys_jab");    // 5 soaked, 0 dmg, pool 8→3
            Assert.AreEqual(500, b.Fighters[1].Hp);
            Assert.AreEqual(3, Absorb(b, 1));

            TestKit.UseAt(b, 7, 0, "test_phys_jab");    // 3 soaked, 2 dmg, pool 3→0
            Assert.AreEqual(498, b.Fighters[1].Hp);
            Assert.AreEqual(0, Absorb(b, 1));

            TestKit.UseAt(b, 13, 0, "test_magic_jab");  // magic ignores the (empty) shield
            Assert.AreEqual(493, b.Fighters[1].Hp);

            var soaked = TestKit.EventsOf(b, SimEventType.ShieldAbsorbed);
            CollectionAssert.AreEqual(new[] { 5, 3 }, soaked.Select(e => e.Amount).ToList());
        }

        [Test]
        public void Block_ReducesThenAbsorbs_PhysicalOnly()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", DefaultContent.Slash(), MagicJab()),
                TestKit.Config("B", DefaultContent.Block()));

            TestKit.UseAt(b, 0, 1, DefaultContent.BlockId);  // block_phys + phys_shield(10), 2s
            TestKit.UseAt(b, 1, 0, DefaultContent.SlashId);  // 10 phys → ×0.4 = 4 → soak 4 → 0 dmg

            Assert.AreEqual(500, b.Fighters[1].Hp);
            Assert.AreEqual(6, Absorb(b, 1)); // soaked 4 (reduce-then-absorb), not 10

            TestKit.UseAt(b, 5, 0, "test_magic_jab");        // magic bypasses both layers
            Assert.AreEqual(495, b.Fighters[1].Hp);
        }
    }
}
