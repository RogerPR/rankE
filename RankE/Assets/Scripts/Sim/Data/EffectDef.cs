using System.Collections.Generic;

namespace RankE.Sim
{
    public enum EffectTarget
    {
        Self,
        Opponent,
    }

    /// <summary>
    /// Effect kinds are open string ids so new kinds can be added without touching
    /// existing content. The interpreter lives in Battle.ApplyEffect.
    /// </summary>
    public static class EffectKinds
    {
        public const string Damage = "damage";
        public const string BreakDamage = "break_damage";
        public const string ApplyStatus = "apply_status";
        public const string ClearStatus = "clear_status";
        public const string InterruptCast = "interrupt_cast";
    }

    public sealed class EffectDef
    {
        public EffectTarget Target = EffectTarget.Opponent;
        public string Kind = EffectKinds.Damage;
        public int Amount;

        /// <summary>Status id for ApplyStatus/ClearStatus; for InterruptCast, an
        /// optional status applied only when the interrupt succeeds (PoC kick stun).</summary>
        public string StatusId;

        public int DurationTicks;

        public static EffectDef Damage(int amount) =>
            new EffectDef { Kind = EffectKinds.Damage, Target = EffectTarget.Opponent, Amount = amount };

        public static EffectDef Break(int amount) =>
            new EffectDef { Kind = EffectKinds.BreakDamage, Target = EffectTarget.Opponent, Amount = amount };

        public static EffectDef Status(EffectTarget target, string statusId, int durationTicks) =>
            new EffectDef
            {
                Kind = EffectKinds.ApplyStatus,
                Target = target,
                StatusId = statusId,
                DurationTicks = durationTicks,
            };

        public static EffectDef Clear(EffectTarget target, string statusId) =>
            new EffectDef { Kind = EffectKinds.ClearStatus, Target = target, StatusId = statusId };

        /// <summary>Interrupt the opponent's cast (respects Interruptible); optionally
        /// applies a status on success.</summary>
        public static EffectDef Interrupt(string statusOnSuccess = null, int statusDurationTicks = 0) =>
            new EffectDef
            {
                Kind = EffectKinds.InterruptCast,
                Target = EffectTarget.Opponent,
                StatusId = statusOnSuccess,
                DurationTicks = statusDurationTicks,
            };

        /// <summary>Cancel your own cast unconditionally (PoC interrupt_cast).</summary>
        public static EffectDef InterruptSelf() =>
            new EffectDef { Kind = EffectKinds.InterruptCast, Target = EffectTarget.Self };
    }
}
