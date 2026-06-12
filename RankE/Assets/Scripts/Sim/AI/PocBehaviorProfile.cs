namespace RankE.Sim
{
    /// <summary>
    /// BehaviorProfile v1: a direct port of the PoC's decide_what_to_do priority list
    /// (character.py). Evaluates ordered condition → action rules top to bottom — the
    /// same shape the Phase 5 gambit engine will generalize into data.
    /// </summary>
    public sealed class PocBehaviorProfile : IBehavior
    {
        /// <summary>PoC: force a slash every ~5s so fights always end.</summary>
        public int ForcedSlashDecisions = 100;

        int decisions;
        bool started;

        public string Decide(Battle battle, int selfIndex)
        {
            var me = battle.Fighters[selfIndex];
            var opp = battle.Fighters[1 - selfIndex];

            if (!started)
            {
                // Jitter the decision counter so two identical profiles don't play a
                // mirror match in perfect lockstep (the PoC AI only ever faced a human).
                started = true;
                decisions = battle.Rng.Next(ForcedSlashDecisions);
            }

            decisions++;

            if (!me.CanAct) return null;

            if (decisions > ForcedSlashDecisions)
            {
                // Redraw instead of resetting to 0: a mutual stun can re-synchronize
                // two mirrored profiles, and a fresh offset breaks the lockstep again.
                decisions = battle.Rng.Next(ForcedSlashDecisions);
                return PocContent.SlashId;
            }

            if (IsReady(me, PocContent.ParryId))
                return PocContent.ParryId;

            if (opp.IsCasting && IsReady(me, PocContent.KickId))
            {
                // PoC: kick chance rises as the opponent's cast nears completion.
                return battle.Rng.NextDouble() < 0.2 / (opp.CastRemaining + 1)
                    ? PocContent.KickId
                    : null;
            }

            if (me.Hp < me.MaxHp * 0.6 && IsReady(me, PocContent.VampiroId) && me.SpellGems > 0)
                return PocContent.VampiroId;

            if (IsReady(me, PocContent.FireballId) && !me.IsCasting && me.SpellGems > 1)
                return PocContent.FireballId;

            if (IsReady(me, PocContent.BashId))
                return PocContent.BashId;

            return null;
        }

        static bool IsReady(Fighter f, string abilityId)
        {
            var a = f.GetAbility(abilityId);
            return a != null && a.IsReady;
        }
    }
}
