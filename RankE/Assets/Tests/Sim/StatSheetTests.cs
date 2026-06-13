using NUnit.Framework;

namespace RankE.Sim.Tests
{
    /// <summary>
    /// The derived damage formula and the new stats in isolation, driven through explicit
    /// <see cref="FighterConfig.Stats"/> (no gear). Neutral defaults are covered by the
    /// rest of the suite staying green; here we exercise non-zero stats.
    /// </summary>
    public class StatSheetTests
    {
        [Test]
        public void Attack_ScalesPhysicalDamage()
        {
            var a = TestKit.Config("A", PocContent.Slash());
            a.Stats.Attack = 50;
            var b = TestKit.Duel(a, TestKit.Config("B"));
            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            Assert.AreEqual(485, b.Fighters[1].Hp); // round(10 × 1.5) = 15
        }

        [Test]
        public void MagicSchool_ScalesWithMagic_NotAttack()
        {
            var a = TestKit.Config("A", PocContent.Fireball());
            a.Stats.Attack = 100; // ignored by a magic-school effect
            a.Stats.Magic = 50;
            var b = TestKit.Duel(a, TestKit.Config("B"));
            TestKit.UseAt(b, 0, 0, PocContent.FireballId);
            TestKit.StepUntil(b, 45);
            Assert.AreEqual(440, b.Fighters[1].Hp); // round(40 × 1.5) = 60
        }

        [Test]
        public void Defense_ReducesIncomingDamage()
        {
            var def = TestKit.Config("B");
            def.Stats.Defense = 100; // defMult = 100/200 = 0.5
            var b = TestKit.Duel(TestKit.Config("A", PocContent.Slash()), def);
            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            Assert.AreEqual(495, b.Fighters[1].Hp); // int(10 × 0.5) = 5
        }

        [Test]
        public void NegativeDefense_IncreasesIncomingDamage()
        {
            var def = TestKit.Config("B");
            def.Stats.Defense = -25; // defMult = 100/75 = 1.333…
            var b = TestKit.Duel(TestKit.Config("A", PocContent.Slash()), def);
            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            Assert.AreEqual(487, b.Fighters[1].Hp); // int(10 × 1.333) = 13
        }

        [Test]
        public void CritChance_Zero_DrawsNoRng_NeverCrits()
        {
            var a = TestKit.Config("A", PocContent.Slash());
            a.Stats.CritChance = 0;
            a.Stats.CritDamage = 3.0; // would be obvious if it ever applied
            var b = TestKit.Duel(a, TestKit.Config("B"));
            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            Assert.AreEqual(490, b.Fighters[1].Hp); // flat 10
        }

        [Test]
        public void CritChance_Hundred_AlwaysCrits()
        {
            var a = TestKit.Config("A", PocContent.Slash());
            a.Stats.CritChance = 100;
            a.Stats.CritDamage = 2.0;
            var b = TestKit.Duel(a, TestKit.Config("B"));
            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            Assert.AreEqual(480, b.Fighters[1].Hp); // round(10 × 2.0) = 20
        }

        [Test]
        public void BreakPower_ScalesBreakDealt()
        {
            var a = TestKit.Config("A", TestKit.Smash(20));
            a.Stats.BreakPower = 2.0;
            var b = TestKit.Duel(a, TestKit.Config("B"));
            TestKit.UseAt(b, 0, 0, "test_smash");
            Assert.AreEqual(40, b.Fighters[1].BreakBar); // 20 × 2.0
        }

        [Test]
        public void Haste_ShortensCooldownAndCast()
        {
            var cfg = TestKit.Config("A", PocContent.Slash(), PocContent.Fireball());
            cfg.Stats.Haste = 50;
            var f = new Fighter(0, cfg, PocContent.CreateContent());
            Assert.AreEqual(40, f.GetAbility(PocContent.SlashId).EffCooldownTicks); // int(80 × 0.5)
            Assert.AreEqual(20, f.GetAbility(PocContent.FireballId).EffCastTicks);  // int(40 × 0.5)
        }

        [Test]
        public void GemRegen_RestoresAfterSpend_CapsAtMax()
        {
            var cfg = TestKit.Config("A", PocContent.Fireball());
            cfg.Stats.GemRegenIntervalTicks = 20;
            var b = TestKit.Duel(cfg, TestKit.Config("B"));

            TestKit.UseAt(b, 0, 0, PocContent.FireballId); // cast; consumes 1 gem on completion
            TestKit.StepUntil(b, 50);
            Assert.AreEqual(7, b.Fighters[0].SpellGems);   // 8 → 7

            TestKit.StepUntil(b, 200);
            Assert.AreEqual(8, b.Fighters[0].SpellGems);   // regenerated, capped at max 8
        }
    }
}
