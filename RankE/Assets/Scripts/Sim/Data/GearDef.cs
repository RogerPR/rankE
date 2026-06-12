using System.Collections.Generic;

namespace RankE.Sim
{
    /// <summary>
    /// A piece of build-modifying gear (stance/weapon/armor today; runes etc. later —
    /// Slot is an open string id). Applied in list order at battle start, with integer
    /// truncation per step, matching the PoC's update_choices().
    /// </summary>
    public sealed class GearDef
    {
        public string Id;
        public string Name;

        /// <summary>"stance" | "weapon" | "armor" | future categories.</summary>
        public string Slot;

        public double MaxHpMult = 1.0;

        /// <summary>-1 = no override.</summary>
        public int GemsOverride = -1;

        /// <summary>-1 = no override (PoC rock stance sets auto-attack damage to 8).</summary>
        public int AutoAttackDamageOverride = -1;

        public double AutoAttackDamageMult = 1.0;
        public double AutoAttackIntervalMult = 1.0;

        /// <summary>Multiplies cast times of all cast abilities (PoC wand).</summary>
        public double CastTimeMult = 1.0;

        /// <summary>Multiplies every cooldown at use time (PoC armor extra_cooldown_mult).</summary>
        public double CooldownMult = 1.0;

        /// <summary>Multiplies incoming damage (PoC armor: light 0.9, heavy 1.1).</summary>
        public double DamageTakenMult = 1.0;

        /// <summary>Per-ability base cooldown multipliers (PoC wind stance halves parry).</summary>
        public Dictionary<string, double> AbilityCooldownMults = new Dictionary<string, double>();
    }

    /// <summary>
    /// The build a fighter enters battle with. Gear is acquired via run rewards
    /// (Phase 4), not pre-fight picks; the sim only consumes the result.
    /// </summary>
    public sealed class BuildState
    {
        public List<GearDef> Gear = new List<GearDef>();
    }
}
