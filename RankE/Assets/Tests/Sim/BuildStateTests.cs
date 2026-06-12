using NUnit.Framework;

namespace RankE.Sim.Tests
{
    public class BuildStateTests
    {
        static FighterConfig DefaultWithGear(params GearDef[] gear)
        {
            var cfg = PocContent.DefaultConfig("A");
            cfg.Build.Gear.AddRange(gear);
            return cfg;
        }

        static Fighter Build(FighterConfig cfg) => new Fighter(0, cfg, PocContent.CreateContent());

        [Test]
        public void RockStance_SetsAutoAttackDamage()
        {
            var f = Build(DefaultWithGear(PocContent.StanceRock()));
            Assert.AreEqual(8, f.AutoAttack.Def.Effects[0].Amount);
        }

        [Test]
        public void WindStance_TradesHpForParryCooldown()
        {
            var f = Build(DefaultWithGear(PocContent.StanceWind()));
            Assert.AreEqual(450, f.MaxHp);
            Assert.AreEqual(60, f.GetAbility(PocContent.ParryId).EffCooldownTicks); // 120 × 0.5
        }

        [Test]
        public void WaterStance_TradesHpForGems()
        {
            var f = Build(DefaultWithGear(PocContent.StanceWater()));
            Assert.AreEqual(400, f.MaxHp);
            Assert.AreEqual(7, f.SpellGems);
        }

        [Test]
        public void Dagger_FasterWeakerAutoAttack()
        {
            var f = Build(DefaultWithGear(PocContent.WeaponDagger()));
            Assert.AreEqual(32, f.AutoAttackInterval);                // 40 × 0.8
            Assert.AreEqual(4, f.AutoAttack.Def.Effects[0].Amount);   // int(5 × 0.9)
        }

        [Test]
        public void Wand_ShortensCastTimes()
        {
            var f = Build(DefaultWithGear(PocContent.WeaponWand()));
            Assert.AreEqual(32, f.GetAbility(PocContent.FireballId).EffCastTicks); // 40 × 0.8
            Assert.AreEqual(48, f.GetAbility(PocContent.VampiroId).EffCastTicks);  // 60 × 0.8
        }

        [Test]
        public void RockPlusDagger_AppliesSequentially()
        {
            var f = Build(DefaultWithGear(PocContent.StanceRock(), PocContent.WeaponDagger()));
            Assert.AreEqual(7, f.AutoAttack.Def.Effects[0].Amount); // int(8 × 0.9)
        }

        [Test]
        public void HeavyArmor_MoreDamageTaken_SlowerCooldowns()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash()),
                DefaultWithGear(PocContent.ArmorHeavy()));
            b.Fighters[1].AutoAttackRemaining = int.MaxValue; // keep B's auto-attack out of the way

            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            Assert.AreEqual(489, b.Fighters[1].Hp); // int(10 × 1.1) = 11

            var c = TestKit.Duel(DefaultWithGear(PocContent.ArmorHeavy()), TestKit.Config("B"));
            c.Fighters[0].AutoAttackRemaining = int.MaxValue;
            TestKit.UseAt(c, 0, 0, PocContent.SlashId);
            Assert.AreEqual(88, c.Fighters[0].GetAbility(PocContent.SlashId).CooldownRemaining); // int(80 × 1.1)
        }

        [Test]
        public void LightArmor_LessDamageTaken_FasterCooldowns()
        {
            var b = TestKit.Duel(
                TestKit.Config("A", PocContent.Slash()),
                DefaultWithGear(PocContent.ArmorLight()));
            b.Fighters[1].AutoAttackRemaining = int.MaxValue;

            TestKit.UseAt(b, 0, 0, PocContent.SlashId);
            Assert.AreEqual(491, b.Fighters[1].Hp); // int(10 × 0.9) = 9

            var c = TestKit.Duel(DefaultWithGear(PocContent.ArmorLight()), TestKit.Config("B"));
            c.Fighters[0].AutoAttackRemaining = int.MaxValue;
            TestKit.UseAt(c, 0, 0, PocContent.SlashId);
            Assert.AreEqual(72, c.Fighters[0].GetAbility(PocContent.SlashId).CooldownRemaining); // int(80 × 0.9)
        }
    }
}
