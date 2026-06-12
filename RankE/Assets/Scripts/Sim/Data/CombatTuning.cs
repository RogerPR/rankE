namespace RankE.Sim
{
    /// <summary>
    /// Cross-cutting combat numbers (everything not owned by a single ability/status).
    /// Defaults are the PoC constants plus the GAME_DESIGN §1 PROPOSED values for the
    /// break bar and combos. All times in ticks (20/s).
    /// </summary>
    public sealed class CombatTuning
    {
        // GCD (PoC: 1.0s / 0.3s)
        public int GcdTicks = 20;
        public int QuickGcdTicks = 6;

        // Parry → riposte (PoC counter + GAME_DESIGN break numbers)
        public string ParryStatusId = "parry";
        public int ParryRiposteGain = 2;
        public int RiposteCounterMax = 8;
        public int ParryBreakToAttacker = 15;
        public string RiposteAbilityId = "riposte";

        // Break bar (GAME_DESIGN §1b PROPOSED)
        public int BreakMax = 100;
        public int BreakDecayGraceTicks = 60;     // 3s without break damage
        public int BreakDecayIntervalTicks = 10;  // then -1 per 10 ticks = -2/s
        public int BreakDecayAmount = 1;
        public string BrokenStatusId = "broken";
        public int BrokenDurationTicks = 50;      // 2.5s

        // Combos (GAME_DESIGN §1c PROPOSED)
        public int ComboWindowTicks = 80;         // 4s between steps
        public double FinisherEffectMult = 1.5;
        public int FinisherBonusBreak = 10;
        public int FinisherGemRefund = 1;

        /// <summary>Safety cap for headless runs; fights hitting it count as draws.</summary>
        public int MaxTicks = 24000;
    }
}
