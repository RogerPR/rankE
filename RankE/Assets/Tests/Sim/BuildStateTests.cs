using NUnit.Framework;

namespace RankE.Sim.Tests
{
    /// <summary>
    /// Gear → stat-sheet deltas (Appendix A). Most assertions read the resolved
    /// <see cref="Fighter.Stats"/> sheet (deterministic, no RNG); damage is asserted via
    /// a fight only where no crit chance is involved.
    /// </summary>
    public class BuildStateTests
    {
        static FighterConfig DefaultWithGear(params GearDef[] gear)
        {
            var cfg = PocContent.DefaultConfig("A");
            cfg.Build.Gear.AddRange(gear);
            return cfg;
        }

        static Fighter Build(FighterConfig cfg) => new Fighter(0, cfg, PocContent.CreateContent());

        // ---- stances ----

        [Test]
        public void RockStance_BoostsAttack_AutoHitsForEight()
        {
            var f = Build(DefaultWithGear(PocContent.StanceRock()));
            Assert.AreEqual(60, f.Stats.Attack);
            Assert.AreEqual(5, f.AutoAttack.Def.Effects[0].Amount); // base unchanged; Attack scales at hit

            // Rock auto-attack lands for round(5 × 1.6) = 8 (no crit on this stance).
            var battle = TestKit.Duel(DefaultWithGear(PocContent.StanceRock()), TestKit.Config("B"));
            battle.Step(); // A's first auto fires immediately
            Assert.AreEqual(492, battle.Fighters[1].Hp);
        }

        [Test]
        public void WindStance_TradesHpForParryCooldown()
        {
            var f = Build(DefaultWithGear(PocContent.StanceWind()));
            Assert.AreEqual(450, f.MaxHp);
            Assert.AreEqual(60, f.GetAbility(PocContent.ParryId).EffCooldownTicks); // 120 × 0.5
        }

        [Test]
        public void WaterStance_TradesHpForGemsAndMagic()
        {
            var f = Build(DefaultWithGear(PocContent.StanceWater()));
            Assert.AreEqual(400, f.MaxHp);
            Assert.AreEqual(7, f.SpellGems);
            Assert.AreEqual(20, f.Stats.Magic);
        }

        // ---- weapons ----

        [Test]
        public void Sword_AddsAttack()
        {
            var f = Build(DefaultWithGear(PocContent.WeaponSword()));
            Assert.AreEqual(20, f.Stats.Attack);
        }

        [Test]
        public void Dagger_FasterWeakerAutoAttack_MoreCrit()
        {
            var f = Build(DefaultWithGear(PocContent.WeaponDagger()));
            Assert.AreEqual(32, f.AutoAttackInterval);              // 40 × 0.8
            Assert.AreEqual(4, f.AutoAttack.Def.Effects[0].Amount); // int(5 × 0.9) baked base
            Assert.AreEqual(10, f.Stats.Attack);
            Assert.AreEqual(15, f.Stats.CritChance);
        }

        [Test]
        public void Wand_ShortensCastTimes_AddsMagic()
        {
            var f = Build(DefaultWithGear(PocContent.WeaponWand()));
            Assert.AreEqual(32, f.GetAbility(PocContent.FireballId).EffCastTicks); // 40 × 0.8
            Assert.AreEqual(48, f.GetAbility(PocContent.VampiroId).EffCastTicks);  // 60 × 0.8
            Assert.AreEqual(30, f.Stats.Magic);
        }

        [Test]
        public void Greataxe_AddsAttackBreakPower_SlowsActions()
        {
            var f = Build(DefaultWithGear(PocContent.WeaponGreataxe()));
            Assert.AreEqual(40, f.Stats.Attack);
            Assert.AreEqual(1.5, f.Stats.BreakPower, 1e-9);
            Assert.AreEqual(-20, f.Stats.Haste);
            Assert.AreEqual(96, f.GetAbility(PocContent.SlashId).EffCooldownTicks); // int(80 × 1.2)
        }

        [Test]
        public void RockPlusDagger_StackAttack_DaggerShapesAuto()
        {
            var f = Build(DefaultWithGear(PocContent.StanceRock(), PocContent.WeaponDagger()));
            Assert.AreEqual(70, f.Stats.Attack);                   // 60 + 10
            Assert.AreEqual(15, f.Stats.CritChance);
            Assert.AreEqual(4, f.AutoAttack.Def.Effects[0].Amount); // int(5 × 0.9) baked base
        }

        // ---- armor ----

        [Test]
        public void HeavyArmor_RaisesDefenseAndHp_SlowsCooldowns()
        {
            var f = Build(DefaultWithGear(PocContent.ArmorHeavy()));
            Assert.AreEqual(600, f.MaxHp);            // 500 × 1.2
            Assert.AreEqual(30, f.Stats.Defense);
            Assert.AreEqual(-15, f.Stats.Haste);
            Assert.AreEqual(92, f.GetAbility(PocContent.SlashId).EffCooldownTicks); // int(80 × 1.15)

            // Incoming Slash (10 phys) reduced by defMult 100/130: int(10 × 0.769) = 7.
            var b = TestKit.Duel(TestKit.Config("A", PocContent.Slash()), DefaultWithGear(PocContent.ArmorHeavy()));
            b.Fighters[1].AutoAttackRemaining = int.MaxValue;
            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            Assert.AreEqual(593, b.Fighters[1].Hp); // 600 - 7
        }

        [Test]
        public void LightArmor_DropsDefense_AddsHasteAndCrit()
        {
            var f = Build(DefaultWithGear(PocContent.ArmorLight()));
            Assert.AreEqual(-20, f.Stats.Defense);
            Assert.AreEqual(15, f.Stats.Haste);
            Assert.AreEqual(10, f.Stats.CritChance);
            Assert.AreEqual(68, f.GetAbility(PocContent.SlashId).EffCooldownTicks); // int(80 × 0.85)
        }
    }
}
