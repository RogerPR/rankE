using System;
using RankE.Sim;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// The sim↔view binding. Owns the Battle, maps real time onto 20Hz Step() calls
    /// with a fixed-timestep accumulator, and republishes new sim events in order.
    /// View code subscribes to SimEventEmitted for discrete moments and polls
    /// Battle/Fighter state for continuous bars. It never computes gameplay.
    /// </summary>
    public sealed class BattleDriver : MonoBehaviour
    {
        /// <summary>Cap catch-up steps per frame so a hitch can't spiral.</summary>
        const int MaxStepsPerFrame = 5;

        public Battle Battle { get; private set; }
        public TelegraphBehavior EnemyBehavior { get; private set; }

        /// <summary>Telegraph hold length of the current enemy, for intent-bar fills.</summary>
        public int EnemyTelegraphTicksTotal { get; private set; }
        public PlayerIntentBuffer PlayerBuffer { get; set; }

        /// <summary>Ticks only while true; pause flips this, never Time.timeScale.</summary>
        public bool Running { get; set; }

        /// <summary>Every sim event, in order, exactly once, right after its tick.</summary>
        public event Action<SimEvent> SimEventEmitted;

        public event Action TickAdvanced;

        /// <summary>Winner fighter index (0 = player, 1 = enemy, -1 = timeout draw).</summary>
        public event Action<int> BattleEnded;

        float accumulator;
        int eventCursor;

        public void Begin(FighterConfig player, FighterConfig enemy, IBehavior enemyAi,
            int telegraphTicks, int seed)
        {
            Battle = new Battle(player, enemy, PocContent.CreateContent(), PocContent.CreateTuning(), seed);
            EnemyBehavior = new TelegraphBehavior(enemyAi, telegraphTicks);
            EnemyTelegraphTicksTotal = telegraphTicks;
            accumulator = 0f;
            eventCursor = 0;
            Running = false; // MatchController flips this when the countdown ends
        }

        void Update()
        {
            if (Battle == null || !Running || Battle.IsOver) return;

            accumulator += Time.deltaTime;
            int steps = 0;
            while (accumulator >= SimConstants.TickDuration && steps < MaxStepsPerFrame)
            {
                accumulator -= SimConstants.TickDuration;
                steps++;
                StepOnce();
                if (Battle.IsOver)
                {
                    Running = false;
                    BattleEnded?.Invoke(Battle.Winner);
                    return;
                }
            }
            if (steps >= MaxStepsPerFrame)
                accumulator = 0f; // drop the backlog rather than fast-forwarding the fight
        }

        void StepOnce()
        {
            Battle.SubmitIntent(0, PlayerBuffer?.PeekForTick());
            Battle.SubmitIntent(1, EnemyBehavior.Decide(Battle, 1));
            Battle.Step();
            PlayerBuffer?.OnTick();

            for (; eventCursor < Battle.Events.Count; eventCursor++)
            {
                var ev = Battle.Events[eventCursor];
                PlayerBuffer?.NotifyEvent(ev, 0);
                SimEventEmitted?.Invoke(ev);
            }
            TickAdvanced?.Invoke();
        }
    }
}
