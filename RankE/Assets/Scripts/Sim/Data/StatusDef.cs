namespace RankE.Sim
{
    /// <summary>
    /// Pure data definition of a status: a bag of behavior fields, so new statuses
    /// (burn, slow, guard…) are new data, not new code.
    /// </summary>
    public sealed class StatusDef
    {
        public string Id;
        public string Name;

        /// <summary>HP change applied every IntervalTicks (negative = DoT, positive = HoT).</summary>
        public int HpPerInterval;

        public int IntervalTicks;

        /// <summary>Stun-like: no abilities, no auto-attack while active.</summary>
        public bool BlocksActions;

        /// <summary>Cancels any in-progress cast or windup while active.</summary>
        public bool CancelsCast;

        /// <summary>Multiplier on damage taken while active (BROKEN: 1.5).</summary>
        public double DamageTakenMult = 1.0;

        /// <summary>Restrict <see cref="DamageTakenMult"/> and <see cref="AbsorbAmount"/> to a
        /// single damage school (a <c>Schools.*</c> id), e.g. a physical-only shield. Null =
        /// every school (the default, so existing statuses are unchanged).</summary>
        public string DamageSchoolFilter = null;

        /// <summary>Size of the absorb pool granted when this status is applied: it soaks
        /// incoming damage (of <see cref="DamageSchoolFilter"/>, if set) until depleted, then
        /// the status simply runs out its timer. 0 = no pool (the default).</summary>
        public int AbsorbAmount = 0;

        /// <summary>Fighters are separated: melee abilities whiff while active on either side.</summary>
        public bool IsDistance;
    }
}
