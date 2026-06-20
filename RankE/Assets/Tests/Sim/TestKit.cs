using System.Collections.Generic;
using System.Linq;

namespace RankE.Sim.Tests
{
    /// <summary>Shared helpers + tiny custom abilities for exercising single mechanics.</summary>
    public static class TestKit
    {
        /// <summary>
        /// Reference GCD the mechanic tests pin (1.0s). Tests own their combat numbers so they
        /// exercise the rules, not the shipped balance in <see cref="DefaultContent"/> — retuning the
        /// game's GCD never breaks a timing assertion here.
        /// </summary>
        public const int RefGcdTicks = 20;

        /// <summary>The shipped tuning with the GCD pinned to the test reference.</summary>
        public static CombatTuning RefTuning()
        {
            var t = DefaultContent.CreateTuning();
            t.GcdTicks = RefGcdTicks;
            return t;
        }

        public static Battle Duel(FighterConfig a, FighterConfig b, int seed = 1)
            => new Battle(a, b, DefaultContent.CreateContent(), RefTuning(), seed);

        /// <summary>500 HP, 8 gems, no auto-attack (tests opt in explicitly).</summary>
        public static FighterConfig Config(string name, params AbilityDef[] abilities)
            => new FighterConfig { Name = name, MaxHp = 500, SpellGems = 8, Abilities = abilities.ToList() };

        public static void StepUntil(Battle b, int tick)
        {
            while (b.CurrentTick < tick) b.Step();
        }

        /// <summary>Submit an intent and run one tick (acts at the current tick).</summary>
        public static void Use(Battle b, int fighter, string ability)
        {
            b.SubmitIntent(fighter, ability);
            b.Step();
        }

        public static void UseAt(Battle b, int tick, int fighter, string ability)
        {
            StepUntil(b, tick);
            Use(b, fighter, ability);
        }

        public static List<SimEvent> EventsOf(Battle b, SimEventType type)
            => b.Events.Where(e => e.Type == type).ToList();

        public static List<SimEvent> EventsOf(Battle b, SimEventType type, string abilityId)
            => b.Events.Where(e => e.Type == type && e.AbilityId == abilityId).ToList();

        public static string Log(Battle b) => string.Join("\n", b.Events);

        // ---- custom single-mechanic abilities ----

        /// <summary>Quick, no cooldown, 5 parriable melee damage.</summary>
        public static AbilityDef Jab() => new AbilityDef
        {
            Id = "test_jab",
            Gcd = GcdClass.Quick,
            Parriable = true,
            IsMelee = true,
            Effects = new List<EffectDef> { EffectDef.Damage(5) },
        };

        /// <summary>Quick parry with a short cooldown so riposte cycles are fast.</summary>
        public static AbilityDef FastParry() => new AbilityDef
        {
            Id = "test_fast_parry",
            Gcd = GcdClass.Quick,
            CooldownTicks = 14,
            Effects = new List<EffectDef>
            {
                EffectDef.Status(EffectTarget.Self, DefaultContent.ParryStatus, 12),
            },
        };

        public static AbilityDef Stunner(int durationTicks = 30) => new AbilityDef
        {
            Id = "test_stunner",
            Gcd = GcdClass.Quick,
            Effects = new List<EffectDef>
            {
                EffectDef.Status(EffectTarget.Opponent, DefaultContent.StunStatus, durationTicks),
            },
        };

        public static AbilityDef Smash(int breakAmount = 60) => new AbilityDef
        {
            Id = "test_smash",
            Gcd = GcdClass.Quick,
            Effects = new List<EffectDef> { EffectDef.Break(breakAmount) },
        };

        public static AbilityDef Net(int durationTicks = 100) => new AbilityDef
        {
            Id = "test_net",
            Gcd = GcdClass.Quick,
            Effects = new List<EffectDef>
            {
                EffectDef.Status(EffectTarget.Opponent, DefaultContent.DistanceStatus, durationTicks),
            },
        };

        public static AbilityDef HeavyBlow() => new AbilityDef
        {
            Id = "test_heavy_blow",
            Gcd = GcdClass.Normal,
            PreLockTicks = 10,
            PostLockTicks = 10,
            Effects = new List<EffectDef> { EffectDef.Damage(7) },
        };

        public static AbilityDef PoisonDart() => new AbilityDef
        {
            Id = "test_poison_dart",
            Gcd = GcdClass.Quick,
            Effects = new List<EffectDef>
            {
                EffectDef.Status(EffectTarget.Opponent, DefaultContent.PoisonStatus, 160),
            },
        };

        public static AbilityDef RegenPotion() => new AbilityDef
        {
            Id = "test_regen_potion",
            Gcd = GcdClass.Quick,
            Effects = new List<EffectDef>
            {
                EffectDef.Status(EffectTarget.Self, DefaultContent.RegenStatus, 120),
            },
        };

        public static AbilityDef UninterruptibleCast() => new AbilityDef
        {
            Id = "test_uncast",
            Gcd = GcdClass.Normal,
            CastTicks = 40,
            Interruptible = false,
            Effects = new List<EffectDef> { EffectDef.Damage(40) },
        };
    }
}
