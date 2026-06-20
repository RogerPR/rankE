using NUnit.Framework;
using RankE.Sim;
using UnityEngine;

namespace RankE.Game.Tests
{
    /// <summary>
    /// The save/load contract for tuning fixtures: a captured preset, when applied onto fresh
    /// defaults, restores the exact numbers/builds/enemy — including an emptied slot and a
    /// gear swap — and survives a JsonUtility round-trip (the on-disk path).
    /// </summary>
    public class TuningPresetTests
    {
        static TuningProfile Mutated()
        {
            var p = TuningProfile.FromDefaults();
            p.Tuning.GcdTicks = 33;
            p.Tuning.FinisherEffectMult = 2.25;
            p.Abilities[PocContent.SlashId].CooldownTicks = 999;
            p.Abilities[PocContent.SlashId].Effects[0].Amount = 77; // Slash's Damage effect
            p.Player.MaxHp = 1234;
            p.Player.Stats.Attack = 42;
            p.Player.AbilityIds[0] = LoadoutPools.NoneId; // empty a main slot
            LoadoutPools.CycleGear(p.Player.Gear, LoadoutPools.Weapons, +1); // Sword -> Dagger
            p.Adversary.Name = "BossX";
            p.Adversary.SpellGems = 3;
            return p;
        }

        static void AssertRestored(TuningProfile src, TuningProfile target)
        {
            Assert.AreEqual(33, target.Tuning.GcdTicks);
            Assert.AreEqual(2.25, target.Tuning.FinisherEffectMult, 1e-9);
            Assert.AreEqual(999, target.Abilities[PocContent.SlashId].CooldownTicks);
            Assert.AreEqual(77, target.Abilities[PocContent.SlashId].Effects[0].Amount);
            Assert.AreEqual(1234, target.Player.MaxHp);
            Assert.AreEqual(42, target.Player.Stats.Attack);
            Assert.AreEqual(LoadoutPools.NoneId, target.Player.AbilityIds[0]);
            Assert.AreEqual(
                LoadoutPools.GearName(src.Player.Gear, LoadoutPools.Weapons),
                LoadoutPools.GearName(target.Player.Gear, LoadoutPools.Weapons));
            Assert.AreEqual("BossX", target.Adversary.Name);
            Assert.AreEqual(3, target.Adversary.SpellGems);
        }

        [Test]
        public void CaptureApply_RestoresEverything()
        {
            var src = Mutated();
            var preset = TuningPreset.Capture(src, null);

            var target = TuningProfile.FromDefaults();
            Assert.AreNotEqual(33, target.Tuning.GcdTicks, "precondition: defaults differ");

            preset.Apply(target, null);
            AssertRestored(src, target);
        }

        [Test]
        public void JsonRoundTrip_PreservesValues()
        {
            var src = Mutated();
            var json = JsonUtility.ToJson(TuningPreset.Capture(src, null));
            var preset = JsonUtility.FromJson<TuningPreset>(json);

            var target = TuningProfile.FromDefaults();
            preset.Apply(target, null);
            AssertRestored(src, target);
        }
    }
}
