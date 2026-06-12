using RankE.Sim;

namespace RankE.Game
{
    /// <summary>
    /// Sticky input buffer between human button presses (any time during a frame)
    /// and the sim's per-tick intent slot. A press stores one ability id (latest
    /// wins) and is resubmitted every tick until the sim acknowledges it with an
    /// AbilityUsed/CastStarted event for that ability, or it expires. This gives
    /// GCD-edge forgiveness — pressing slightly before the GCD ends still fires —
    /// without any sim change. Plain C#: unit-tested headlessly.
    /// </summary>
    public sealed class PlayerIntentBuffer
    {
        /// <summary>Ticks an unacknowledged press survives (6 ≈ 0.3s).</summary>
        public int ExpiryTicks = 6;

        string pending;
        int age;

        public string Pending => pending;

        public void Press(string abilityId)
        {
            pending = abilityId;
            age = 0;
        }

        /// <summary>The intent to submit for the upcoming tick (null = none).</summary>
        public string PeekForTick() => pending;

        /// <summary>Feed every sim event after a Step; clears the buffer on ack.</summary>
        public void NotifyEvent(SimEvent ev, int playerIndex)
        {
            if (pending == null || ev.Actor != playerIndex || ev.AbilityId != pending) return;
            if (ev.Type == SimEventType.AbilityUsed || ev.Type == SimEventType.CastStarted)
                pending = null;
        }

        /// <summary>Call once per sim tick (after NotifyEvent) to age out stale presses.</summary>
        public void OnTick()
        {
            if (pending != null && ++age >= ExpiryTicks)
                pending = null;
        }

        public void Clear()
        {
            pending = null;
            age = 0;
        }
    }
}
