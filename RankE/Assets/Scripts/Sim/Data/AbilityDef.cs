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

        /// <summary>Combo colour id (e.g. "red"/"yellow") used by the player's colour-sequence
        /// combo. Open string id, mapped to an on-screen colour by the UI. Null = the ability is
        /// combo-neutral: it neither advances nor resets the sequence.</summary>
        public string ComboColor;

        /// <summary>A parry window on the defender negates this ability's opponent effects.</summary>
        public bool Parriable;

        /// <summary>Melee abilities whiff while a Distance status is active on either fighter.</summary>
        public bool IsMelee;

        /// <summary>Spell gems consumed on commit (instants: on use; casts: on completion).</summary>
        public int GemCost;

        /// <summary>Usable while casting (PoC ACTIONS_WHILE_CAST, e.g. interrupt_cast).</summary>
        public bool UsableWhileCasting;

        public List<EffectDef> Effects = new List<EffectDef>();

        /// <summary>Deep copy (incl. Effects) so the tuning tools edit a source the live fight clones from.</summary>
        public AbilityDef Clone()
        {
            var copy = new AbilityDef
            {
                Id = Id,
                Name = Name,
                Gcd = Gcd,
                CooldownTicks = CooldownTicks,
                CastTicks = CastTicks,
                Interruptible = Interruptible,
                DelayTicks = DelayTicks,
                PreLockTicks = PreLockTicks,
                PostLockTicks = PostLockTicks,
                ComboColor = ComboColor,
                Parriable = Parriable,
                IsMelee = IsMelee,
                GemCost = GemCost,
                UsableWhileCasting = UsableWhileCasting,
                Effects = new List<EffectDef>(Effects.Count),
            };
            for (int i = 0; i < Effects.Count; i++)
                copy.Effects.Add(Effects[i].Clone());
            return copy;
        }
    }
}
