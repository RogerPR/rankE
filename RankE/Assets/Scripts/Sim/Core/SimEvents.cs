namespace RankE.Sim
{
    public enum SimEventType
    {
        AbilityUsed,
        CastStarted,
        CastCompleted,
        CastInterrupted,
        AbilityWhiffed,
        Damaged,
        Healed,
        Parried,
        RiposteTriggered,
        BreakDamaged,
        Broken,
        GemRegenerated,
        StatusApplied,
        StatusExpired,
        ComboAdvanced,
        ComboCompleted,
        ComboReset,
        DelayedScheduled,
        DelayedFired,
        FighterDied,
    }

    /// <summary>
    /// One entry in a battle's ordered event log. The view layer (Phase 2) renders
    /// exclusively from these; tests compare serialized logs for determinism.
    /// </summary>
    public sealed class SimEvent
    {
        public int Tick;
        public SimEventType Type;

        /// <summary>Fighter index that caused/owns the event.</summary>
        public int Actor;

        /// <summary>Fighter index affected, or -1.</summary>
        public int Target = -1;

        public string AbilityId;
        public string StatusId;
        public int Amount;

        public override string ToString() =>
            $"{Tick}:{Type}:{Actor}>{Target}:{AbilityId}:{StatusId}:{Amount}";
    }
}
