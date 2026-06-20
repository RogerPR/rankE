using System;

namespace RankE.Sim
{
    /// <summary>
    /// Headless AI-vs-AI fight batches — the balance tool for every later phase.
    /// Pure C#: callable from edit-mode tests and the editor command line alike.
    /// </summary>
    public static class BattleRunner
    {
        public static BattleStats Run(
            int fights,
            int seed,
            Func<FighterConfig> makeA,
            Func<FighterConfig> makeB,
            Func<IBehavior> makeBehaviorA,
            Func<IBehavior> makeBehaviorB,
            ContentDb content,
            CombatTuning tuning)
        {
            var stats = new BattleStats();
            var master = new Random(seed);
            for (int i = 0; i < fights; i++)
            {
                var battle = new Battle(makeA(), makeB(), content, tuning, master.Next());
                stats.Record(RunSingle(battle, makeBehaviorA(), makeBehaviorB()));
            }
            return stats;
        }

        public static Battle RunSingle(Battle battle, IBehavior behaviorA, IBehavior behaviorB)
        {
            while (!battle.IsOver && battle.CurrentTick < battle.Tuning.MaxTicks)
            {
                battle.SubmitIntent(0, behaviorA.Decide(battle, 0));
                battle.SubmitIntent(1, behaviorB.Decide(battle, 1));
                battle.Step();
            }
            return battle;
        }

        /// <summary>PoC mirror match: default kits, PoC behavior profile on both sides.</summary>
        public static BattleStats RunDefault(int fights, int seed)
        {
            return Run(
                fights,
                seed,
                () => DefaultContent.DefaultConfig("A"),
                () => DefaultContent.DefaultConfig("B"),
                () => new PocBehaviorProfile(),
                () => new PocBehaviorProfile(),
                DefaultContent.CreateContent(),
                DefaultContent.CreateTuning());
        }
    }
}
