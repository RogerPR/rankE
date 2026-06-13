using System;

namespace RankE.Sim
{
    /// <summary>Stat ids used as keys for gear stat deltas (GAME_DESIGN §1, Appendix A).
    /// Fixed sheet, not an open category — the formula reads named stats.</summary>
    public static class StatIds
    {
        public const string Attack = "attack";
        public const string Magic = "magic";
        public const string Defense = "defense";
        public const string CritChance = "crit_chance";
        public const string CritDamage = "crit_damage";
        public const string Haste = "haste";
        public const string BreakPower = "break_power";
    }

    /// <summary>
    /// A fighter's RPG stat sheet (GAME_DESIGN §1 "Character stats & resources", locked
    /// 2026-06-13). Damage is derived from these, not flat per-ability. Defaults are
    /// <b>neutral</b>: with an all-default sheet the derived formula reproduces the PoC
    /// flat numbers exactly.
    /// </summary>
    public sealed class StatSheet
    {
        /// <summary>Scales physical ability &amp; auto-attack damage: ×(1 + Attack/100).</summary>
        public int Attack;

        /// <summary>Scales magic ability damage (and support potency, later): ×(1 + Magic/100).</summary>
        public int Magic;

        /// <summary>Reduces incoming damage: defMult = 100/(100+Defense). May be negative.</summary>
        public int Defense;

        /// <summary>% chance to crit (0–100). 0 = never (no RNG drawn).</summary>
        public int CritChance;

        /// <summary>Crit multiplier applied on a crit (default ×1.5).</summary>
        public double CritDamage = 1.5;

        /// <summary>% reduction to cooldowns and cast times. Folds the old two reward lines.</summary>
        public int Haste;

        /// <summary>Multiplies break damage dealt (default ×1.0).</summary>
        public double BreakPower = 1.0;

        /// <summary>Restore 1 spell gem every N ticks; 0 = off. Set on the base sheet
        /// (interval deltas don't compose additively, so it has no gear-delta key).</summary>
        public int GemRegenIntervalTicks;

        public StatSheet Clone() => (StatSheet)MemberwiseClone();

        /// <summary>Additively apply a single gear stat delta (Appendix A).</summary>
        public void AddDelta(string stat, double value)
        {
            switch (stat)
            {
                case StatIds.Attack: Attack += (int)value; break;
                case StatIds.Magic: Magic += (int)value; break;
                case StatIds.Defense: Defense += (int)value; break;
                case StatIds.CritChance: CritChance += (int)value; break;
                case StatIds.CritDamage: CritDamage += value; break;
                case StatIds.Haste: Haste += (int)value; break;
                case StatIds.BreakPower: BreakPower += value; break;
                default:
                    throw new ArgumentException($"Unknown stat id '{stat}'", nameof(stat));
            }
        }
    }
}
