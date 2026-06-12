namespace RankE.Sim
{
    /// <summary>
    /// Decorator that telegraphs an inner behavior's attacks (GAME_DESIGN §1: enemies
    /// telegraph their next intended action; later maps shrink the telegraph time).
    /// Normal-GCD decisions are held for TelegraphTicks — visible to the UI via
    /// PendingIntent/TicksUntilCommit — then submitted. Quick actions (parry, kick)
    /// are reactive and pass through untelegraphed. The inner behavior is not
    /// consulted while a telegraph is pending, so its Rng consumption (and thus
    /// determinism per seed) is unaffected by when the UI reads the intent.
    /// A held intent can be stale by commit time (e.g. the holder got stunned);
    /// the sim safely no-ops it.
    /// </summary>
    public sealed class TelegraphBehavior : IBehavior
    {
        readonly IBehavior inner;
        readonly int telegraphTicks;

        string pending;
        int holdRemaining;

        public TelegraphBehavior(IBehavior inner, int telegraphTicks)
        {
            this.inner = inner;
            this.telegraphTicks = telegraphTicks;
        }

        /// <summary>Ability id this fighter is winding up to use, or null.</summary>
        public string PendingIntent => pending;

        /// <summary>Decide calls (= ticks) left before the pending intent is submitted.</summary>
        public int TicksUntilCommit => pending == null ? 0 : holdRemaining;

        public string Decide(Battle battle, int selfIndex)
        {
            if (pending != null)
            {
                holdRemaining--;
                if (holdRemaining > 0) return null;
                var committed = pending;
                pending = null;
                return committed;
            }

            var decision = inner.Decide(battle, selfIndex);
            if (decision == null || telegraphTicks <= 0) return decision;

            if (battle.Content.Abilities.TryGetValue(decision, out var def)
                && def.Gcd != GcdClass.Normal)
            {
                return decision;
            }

            pending = decision;
            holdRemaining = telegraphTicks;
            return null;
        }
    }
}
