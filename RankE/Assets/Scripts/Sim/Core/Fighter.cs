using System;
using System.Collections.Generic;

namespace RankE.Sim
{
    /// <summary>Per-fighter runtime state of one ability slot.</summary>
    public sealed class AbilityState
    {
        public AbilityDef Def;

        /// <summary>Base cooldown after build mults (use-time armor mult applied separately).</summary>
        public int EffCooldownTicks;

        public int EffCastTicks;
        public int CooldownRemaining;

        public bool IsReady => CooldownRemaining <= 0;
    }

    public sealed class StatusInstance
    {
        public StatusDef Def;
        public int Remaining;
    }

    /// <summary>
    /// One fighter's runtime state. Pure state + build construction; all rules live
    /// in Battle so the resolution order stays in one place.
    /// </summary>
    public sealed class Fighter
    {
        public readonly int Index;
        public readonly string Name;

        public int MaxHp;
        public int Hp;
        public int SpellGems;
        public int MaxSpellGems;

        /// <summary>Resolved stat sheet (base config + gear deltas), fixed for the battle.</summary>
        public StatSheet Stats;

        /// <summary>Ticks until the next gem is regenerated (Stats.GemRegenIntervalTicks &gt; 0).</summary>
        public int GemRegenRemaining;

        /// <summary>PoC armor extra_cooldown_mult: applied to cooldowns at use time.</summary>
        public double CooldownUseMult = 1.0;

        /// <summary>PoC armor damage multiplier on incoming damage.</summary>
        public double DamageTakenMult = 1.0;

        public int BreakBar;
        public int TicksSinceBreakDamage = 1_000_000;

        public int GcdRemaining;

        /// <summary>Post-effect animation lock; no actions while > 0.</summary>
        public int LockRemaining;

        public AbilityState Casting;
        public int CastRemaining;

        /// <summary>Pre-effect animation lock in progress (instant ability wind-up).</summary>
        public AbilityState Windup;
        public int WindupRemaining;
        public bool WindupEmpowered;

        public AbilityState AutoAttack; // null = none
        public int AutoAttackInterval;
        public int AutoAttackRemaining;

        public int RiposteCounter;

        /// <summary>Completed steps in the current combo chain (0–2).</summary>
        public int ComboStep;

        public int ComboDeadlineTick = -1;

        public readonly List<AbilityState> Abilities = new List<AbilityState>();
        public readonly List<StatusInstance> Statuses = new List<StatusInstance>();

        public bool IsCasting => Casting != null;
        public bool IsWindingUp => Windup != null;

        public Fighter(int index, FighterConfig cfg, ContentDb content)
        {
            Index = index;
            Name = cfg.Name;

            // Apply gear sequentially with integer truncation per step (PoC parity).
            int maxHp = cfg.MaxHp;
            int gems = cfg.SpellGems;
            double castMult = 1.0;
            var cdMults = new Dictionary<string, double>();
            int aaDamage = FirstDamageAmount(cfg.AutoAttack);
            int aaInterval = cfg.AutoAttack != null ? cfg.AutoAttack.CooldownTicks : 0;

            Stats = cfg.Stats != null ? cfg.Stats.Clone() : new StatSheet();

            var gear = cfg.Build != null ? cfg.Build.Gear : null;
            if (gear != null)
            {
                foreach (var g in gear)
                {
                    maxHp = (int)(maxHp * g.MaxHpMult);
                    if (g.GemsOverride >= 0) gems = g.GemsOverride;
                    if (g.AutoAttackDamageOverride >= 0) aaDamage = g.AutoAttackDamageOverride;
                    aaDamage = (int)(aaDamage * g.AutoAttackDamageMult);
                    aaInterval = (int)(aaInterval * g.AutoAttackIntervalMult);
                    castMult *= g.CastTimeMult;
                    CooldownUseMult *= g.CooldownMult;
                    DamageTakenMult *= g.DamageTakenMult;
                    if (g.AbilityCooldownMults != null)
                    {
                        foreach (var kv in g.AbilityCooldownMults)
                            cdMults[kv.Key] = (cdMults.TryGetValue(kv.Key, out var m) ? m : 1.0) * kv.Value;
                    }
                    if (g.StatDeltas != null)
                        foreach (var kv in g.StatDeltas)
                            Stats.AddDelta(kv.Key, kv.Value);
                }
            }

            MaxHp = maxHp;
            Hp = maxHp;
            SpellGems = gems;
            MaxSpellGems = gems;

            // Haste reduces cooldowns and cast times alike (neutral at Haste 0).
            double hasteMult = (100 - Stats.Haste) / 100.0;
            foreach (var def in cfg.Abilities)
            {
                double mult = cdMults.TryGetValue(def.Id, out var m) ? m : 1.0;
                Abilities.Add(new AbilityState
                {
                    Def = def,
                    EffCooldownTicks = (int)(def.CooldownTicks * mult * hasteMult),
                    EffCastTicks = (int)(def.CastTicks * castMult * hasteMult),
                });
            }

            GemRegenRemaining = Stats.GemRegenIntervalTicks;

            if (cfg.AutoAttack != null)
            {
                AutoAttack = new AbilityState { Def = CloneWithDamage(cfg.AutoAttack, aaDamage) };
                AutoAttackInterval = aaInterval;
                AutoAttackRemaining = 0; // PoC: first auto-attack fires immediately
            }
        }

        public bool CanAct
        {
            get
            {
                foreach (var s in Statuses)
                    if (s.Def.BlocksActions) return false;
                return true;
            }
        }

        public AbilityState GetAbility(string id)
        {
            foreach (var a in Abilities)
                if (a.Def.Id == id) return a;
            return null;
        }

        public bool HasStatus(string id)
        {
            foreach (var s in Statuses)
                if (s.Def.Id == id) return true;
            return false;
        }

        public double StatusDamageTakenMult()
        {
            double mult = 1.0;
            foreach (var s in Statuses)
                mult *= s.Def.DamageTakenMult;
            return mult;
        }

        static int FirstDamageAmount(AbilityDef def)
        {
            if (def == null) return 0;
            foreach (var e in def.Effects)
                if (e.Kind == EffectKinds.Damage) return e.Amount;
            return 0;
        }

        /// <summary>Gear can change auto-attack damage per fighter, so the shared def
        /// gets cloned with adjusted damage amounts.</summary>
        static AbilityDef CloneWithDamage(AbilityDef def, int damage)
        {
            var copy = def.Clone();
            foreach (var e in copy.Effects)
                if (e.Kind == EffectKinds.Damage) e.Amount = damage;
            return copy;
        }
    }
}
