using System;
using System.Collections.Generic;

namespace RankE.Sim
{
    /// <summary>
    /// Exposes a fighter's planned (non-quick) actions so the view can show the player what
    /// is coming. Reactive quick actions (parry/kick) are deliberately NOT part of the plan —
    /// they're defensive reflexes, not a telegraphed rhythm.
    /// </summary>
    public interface IActionPlan
    {
        /// <summary>The next <paramref name="count"/> non-quick ability ids in commit order.</summary>
        IReadOnlyList<string> Upcoming(int count);

        /// <summary>Cadence-timer ticks left before the next planned action is offered.</summary>
        int TicksUntilNextAction { get; }

        /// <summary>Full cadence interval (denominator for the imminent-action bar).</summary>
        int IntervalTicks { get; }
    }

    /// <summary>
    /// A readable enemy brain: it performs a fixed, data-driven <i>rotation</i> of non-quick
    /// actions on a steady cadence (one beat every <see cref="IntervalTicks"/>), so its next
    /// moves are predictable and can be shown in the HUD's upcoming-actions panel. Between
    /// beats it still defends reactively with quick actions (Parry/Kick), which never advance
    /// the rotation. Cadence ≈ max(interval, the beat ability's cooldown): a beat waits if its
    /// ability isn't ready yet rather than dropping. Deterministic — no Rng use.
    ///
    /// Wrapped by <see cref="TelegraphBehavior"/> at the Game layer, which adds the visible
    /// wind-up before each beat lands.
    /// </summary>
    public sealed class ScriptedRhythmBehavior : IBehavior, IActionPlan
    {
        readonly List<string> rotation;
        readonly int intervalTicks;

        int index;
        int timer; // ticks until the next beat is offered; <=0 means "due, waiting on readiness"

        public ScriptedRhythmBehavior(IReadOnlyList<string> rotation, int intervalTicks)
        {
            this.rotation = new List<string>(rotation ?? Array.Empty<string>());
            this.intervalTicks = Math.Max(1, intervalTicks);
            timer = this.intervalTicks;
        }

        public int IntervalTicks => intervalTicks;
        public int TicksUntilNextAction => Math.Max(0, timer);

        public IReadOnlyList<string> Upcoming(int count)
        {
            var list = new List<string>(Math.Max(0, count));
            if (rotation.Count == 0) return list;
            for (int i = 0; i < count; i++)
                list.Add(rotation[(index + i) % rotation.Count]);
            return list;
        }

        public string Decide(Battle battle, int selfIndex)
        {
            if (timer > 0) timer--;

            var me = battle.Fighters[selfIndex];
            if (!me.CanAct) return null;

            // Rhythm beat: fire the current rotation entry once its cadence is due and the
            // ability is off cooldown. If not ready yet, hold (timer stays <=0) and retry.
            if (timer <= 0 && rotation.Count > 0)
            {
                var id = rotation[index];
                var ab = me.GetAbility(id);
                if (ab != null && ab.IsReady)
                {
                    index = (index + 1) % rotation.Count;
                    timer = intervalTicks;
                    return id;
                }
            }

            // Reactive quick defense between beats (never advances the rotation / plan).
            var opp = battle.Fighters[1 - selfIndex];
            if (IsReady(me, PocContent.ParryId)) return PocContent.ParryId;
            if (opp.IsCasting && IsReady(me, PocContent.KickId)) return PocContent.KickId;

            return null;
        }

        static bool IsReady(Fighter f, string abilityId)
        {
            var a = f.GetAbility(abilityId);
            return a != null && a.IsReady;
        }
    }
}
