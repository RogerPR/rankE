using System;
using System.Collections.Generic;

namespace RankE.Sim
{
    /// <summary>One weighted option within a rotation step.</summary>
    public sealed class WeightedChoice
    {
        public readonly string Id;
        public readonly double Weight;

        public WeightedChoice(string id, double weight)
        {
            Id = id;
            Weight = weight;
        }
    }

    /// <summary>
    /// A readable enemy brain like <see cref="ScriptedRhythmBehavior"/>, but each rotation
    /// <i>step</i> is a weighted choice (e.g. "Slash" then "66% Slash / 33% Fireball"). A
    /// single-option step reproduces a fixed rotation and draws no RNG, so deterministic
    /// rotations stay byte-identical.
    ///
    /// The next several beats are <b>pre-rolled into a committed queue</b> (filled during
    /// <see cref="Decide"/>, off <see cref="Battle.Rng"/>), so <see cref="Upcoming"/> — which
    /// the view peeks every frame — never draws RNG and reports the <i>real</i> upcoming
    /// abilities for every row, not just the imminent one. Beats fire from the front of the
    /// queue and the tail refills, keeping the fight seed-deterministic. Wrapped by
    /// <see cref="TelegraphBehavior"/> for the visible wind-up.
    /// </summary>
    public sealed class WeightedRotationBehavior : IBehavior, IActionPlan
    {
        // Pre-rolled lookahead depth: covers the HUD's queue rows with margin so the view
        // always sees real rolls. Refilled on sim ticks only (never on the view's peek).
        const int Lookahead = 8;

        readonly List<List<WeightedChoice>> steps;
        readonly int intervalTicks;

        int rollCursor;            // next step index to roll when extending the queue
        int timer;                 // ticks until the next beat is offered; <=0 = due, waiting on readiness
        readonly List<string> committed = new List<string>(Lookahead); // FIFO; [0] = next beat

        public WeightedRotationBehavior(IReadOnlyList<IReadOnlyList<WeightedChoice>> steps, int intervalTicks)
        {
            this.steps = new List<List<WeightedChoice>>();
            if (steps != null)
                foreach (var s in steps)
                    this.steps.Add(s != null ? new List<WeightedChoice>(s) : new List<WeightedChoice>());
            this.intervalTicks = Math.Max(1, intervalTicks);
            timer = this.intervalTicks;
        }

        public int IntervalTicks => intervalTicks;
        public int TicksUntilNextAction => Math.Max(0, timer);

        public IReadOnlyList<string> Upcoming(int count)
        {
            // Pure peek: read the committed queue, never draw RNG. Before the first Decide the
            // queue may be empty — pad with each step's first option as a safe fallback.
            var list = new List<string>(Math.Max(0, count));
            for (int i = 0; i < count; i++)
            {
                if (i < committed.Count) list.Add(committed[i]);
                else if (steps.Count > 0) list.Add(FirstOption(steps[(rollCursor + (i - committed.Count)) % steps.Count]));
            }
            return list;
        }

        public string Decide(Battle battle, int selfIndex)
        {
            if (timer > 0) timer--;

            var me = battle.Fighters[selfIndex];
            if (!me.CanAct) return null;

            // Fill the committed lookahead first, so every RNG draw happens here (on sim ticks,
            // in a fixed order) and Upcoming() always reports real rolls.
            Refill(battle, Lookahead);

            // Rhythm beat: fire the head of the queue when due and off cooldown; else hold +
            // retry (the head stays put so the wind-up/plan doesn't flicker).
            if (timer <= 0 && committed.Count > 0)
            {
                var fired = committed[0];
                var ab = me.GetAbility(fired);
                if (ab != null && ab.IsReady)
                {
                    committed.RemoveAt(0);
                    Refill(battle, Lookahead); // top the tail back up immediately
                    timer = intervalTicks;
                    return fired;
                }
            }

            // Reactive quick defense between beats (never advances the rotation / plan).
            if (IsReady(me, DefaultContent.ParryId)) return DefaultContent.ParryId;
            var opp = battle.Fighters[1 - selfIndex];
            if (opp.IsCasting && IsReady(me, DefaultContent.KickId)) return DefaultContent.KickId;

            return null;
        }

        /// <summary>Pre-roll beats onto the committed queue until it reaches <paramref name="depth"/>.
        /// Single-option steps draw no RNG, so fixed rotations stay byte-identical.</summary>
        void Refill(Battle battle, int depth)
        {
            if (steps.Count == 0) return;
            while (committed.Count < depth)
            {
                committed.Add(Roll(battle, steps[rollCursor]));
                rollCursor = (rollCursor + 1) % steps.Count;
            }
        }

        /// <summary>Pick one option by weight off the battle RNG. Single-option steps return
        /// directly and draw no RNG (so fixed rotations stay deterministic).</summary>
        static string Roll(Battle battle, List<WeightedChoice> step)
        {
            if (step == null || step.Count == 0) return null;
            if (step.Count == 1) return step[0].Id;

            double total = 0;
            foreach (var c in step) total += Math.Max(0, c.Weight);
            if (total <= 0) return step[0].Id;

            double r = battle.Rng.NextDouble() * total;
            foreach (var c in step)
            {
                r -= Math.Max(0, c.Weight);
                if (r < 0) return c.Id;
            }
            return step[step.Count - 1].Id;
        }

        static string FirstOption(List<WeightedChoice> step) =>
            step != null && step.Count > 0 ? step[0].Id : null;

        static bool IsReady(Fighter f, string abilityId)
        {
            var a = f.GetAbility(abilityId);
            return a != null && a.IsReady;
        }
    }
}
