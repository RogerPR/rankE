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
        public const string ApplyShield = "apply_shield";
        public const string ClearStatus = "clear_status";
        public const string InterruptCast = "interrupt_cast";
    }

    /// <summary>
    /// Damage school: picks which offense stat scales a Damage effect (GAME_DESIGN §1).
    /// physical→Attack, magic/support→Magic, true→unscaled. Open string ids.
    /// </summary>
    public static class Schools
    {
        public const string Physical = "physical";
        public const string Magic = "magic";
        public const string True = "true";
        public const string Support = "support";
    }

    public sealed class EffectDef
    {
        public EffectTarget Target = EffectTarget.Opponent;
        public string Kind = EffectKinds.Damage;
        public int Amount;

        /// <summary>Which offense stat scales this effect (Damage only). Default physical.</summary>
        public string School = Schools.Physical;

        /// <summary>Status id for ApplyStatus/ClearStatus; for InterruptCast, an
        /// optional status applied only when the interrupt succeeds (PoC kick stun).</summary>
        public string StatusId;

        public int DurationTicks;

        /// <summary>Independent copy (the tuning tools edit clones so the live fight is untouched).</summary>
        public EffectDef Clone() => new EffectDef
        {
            Target = Target,
            Kind = Kind,
            Amount = Amount,
            School = School,
            StatusId = StatusId,
            DurationTicks = DurationTicks,
        };

        public static EffectDef Damage(int amount, string school = Schools.Physical) =>
            new EffectDef { Kind = EffectKinds.Damage, Target = EffectTarget.Opponent, Amount = amount, School = school };

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

        /// <summary>Grant an absorb shield: applies <paramref name="statusId"/> with its absorb
        /// pool set to <paramref name="amount"/> (so the pool size is tunable per ability).</summary>
        public static EffectDef Shield(EffectTarget target, string statusId, int amount, int durationTicks) =>
            new EffectDef
            {
                Kind = EffectKinds.ApplyShield,
                Target = target,
                StatusId = statusId,
                Amount = amount,
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
