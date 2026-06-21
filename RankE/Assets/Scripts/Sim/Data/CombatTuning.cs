namespace RankE.Sim
{
    /// <summary>
    /// Cross-cutting combat numbers (everything not owned by a single ability/status).
    /// Defaults are the game's shipping values (a notch slower than the original PoC) plus
    /// the GAME_DESIGN §1 PROPOSED values for the break bar and combos. All times in
    /// ticks (20/s).
    /// </summary>
    public sealed class CombatTuning
    {
        // GCD — 1.5s normal / 0.3s quick (a calmer cadence than the PoC's 1.0s).
        public int GcdTicks = 30;
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

        // Colour-sequence combo (player only). Completing the displayed colour order grants
        // the EMPOWERED status; the next damaging ability is doubled, then it's consumed.
        public int ComboMinLen = 3;               // shortest random sequence
        public int ComboMaxLen = 5;               // longest random sequence
        public string EmpoweredStatusId = "empowered";
        public int EmpoweredDurationTicks = 600;  // 30s window to spend the empowered hit

        /// <summary>Safety cap for headless runs; fights hitting it count as draws.</summary>
        public int MaxTicks = 24000;

        /// <summary>Flat copy so the tuning tool edits a source each new fight clones from.</summary>
        public CombatTuning Clone() => (CombatTuning)MemberwiseClone();
    }
}
