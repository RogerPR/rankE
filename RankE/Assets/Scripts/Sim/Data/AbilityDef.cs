using System.Collections.Generic;

namespace RankE.Sim
{
    public enum GcdClass
    {
        /// <summary>Outside the GCD system (auto-attack, automatic riposte).</summary>
        None,

        /// <summary>Normal GCD (PoC: 1.0s).</summary>
        Normal,

        /// <summary>Quick action: ignores the GCD, triggers a short one (PoC: 0.3s).</summary>
        Quick,
    }

    /// <summary>Combo tags are open string ids (GAME_DESIGN §1c).</summary>
    public static class ComboTags
    {
        public const string Opener = "opener";
        public const string Linker = "linker";
        public const string Finisher = "finisher";
    }

    /// <summary>
    /// Pure data definition of an ability. All times are in ticks (20/s).
    /// </summary>
    public sealed class AbilityDef
    {
        public string Id;
        public string Name;
        public GcdClass Gcd = GcdClass.Normal;
        public int CooldownTicks;

        /// <summary>Cast bar length; 0 = instant. For auto-attacks CooldownTicks is the interval.</summary>
        public int CastTicks;

        /// <summary>Whether an opponent's interrupt (kick) can cancel the cast.</summary>
        public bool Interruptible = true;

        /// <summary>If > 0, effects land this many ticks after use (delayed ability).</summary>
        public int DelayTicks;

        /// <summary>Animation lock before the effect frame (you can't act; stun cancels).</summary>
        public int PreLockTicks;

        /// <summary>Animation lock after the effect frame (you can't act).</summary>
        public int PostLockTicks;

        /// <summary>ComboTags.* or null (null = neutral: neither advances nor resets).</summary>
        public string ComboTag;

        /// <summary>A parry window on the defender negates this ability's opponent effects.</summary>
        public bool Parriable;

        /// <summary>Melee abilities whiff while a Distance status is active on either fighter.</summary>
        public bool IsMelee;

        /// <summary>Spell gems consumed on commit (instants: on use; casts: on completion).</summary>
        public int GemCost;

        /// <summary>Usable while casting (PoC ACTIONS_WHILE_CAST, e.g. interrupt_cast).</summary>
        public bool UsableWhileCasting;

        public List<EffectDef> Effects = new List<EffectDef>();
    }
}
