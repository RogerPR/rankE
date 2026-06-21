using RankE.Sim;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Drives one fighter's Animator from sim events, rig-agnostically: it never names
    /// a clip, it asks the <see cref="FighterVisualDef"/> for the Animator state that
    /// represents a semantic action (or a specific ability id) and CrossFades to it.
    /// One-shots (attack/hit/block/riposte) hold for a tunable window then the poll in
    /// <see cref="Update"/> returns to Idle / Cast / Stunned based on read-only sim state.
    ///
    /// <para>Timing-aware: an ability with a pre-effect wind-up (PreLock/Delay ticks) holds a
    /// short anticipation pose and fires the strike one-shot so its contact lands on the effect
    /// tick — read from the sim's ability data only, never written back. Pure presentation.</para>
    /// </summary>
    public sealed class FighterAnimator : MonoBehaviour
    {
        // --- Tunable timing knobs (seconds). Real feel is dialed in by playing. ---
        [SerializeField] float crossFade = 0.08f;
        [SerializeField] float attackSeconds = 0.5f;
        [SerializeField] float hitSeconds = 0.3f;
        [SerializeField] float blockSeconds = 0.45f;
        [SerializeField] float riposteSeconds = 0.85f;
        [SerializeField] float brokenSeconds = 2.5f;

        /// <summary>How far into the strike clip the "contact" sits — the strike starts this
        /// long before the effect tick so the swing connects when the hit lands.</summary>
        [SerializeField] float strikeContactSeconds = 0.18f;

        Animator anim;
        BattleDriver driver;
        FighterVisualDef def;
        int index;

        string currentState;
        float busyUntil;   // realtime; while > now, a one-shot is playing
        bool dead;

        // Pending wind-up strike: a scheduled one-shot whose contact aligns with the effect tick.
        string pendingStrikeState;
        float pendingStrikeAt;

        public void Bind(BattleDriver driver, int index, FighterVisualDef def, Animator animator)
        {
            this.driver = driver;
            this.index = index;
            this.def = def;
            anim = animator;
            if (anim != null) anim.applyRootMotion = false; // sim owns position, not root motion

            busyUntil = 0f;
            pendingStrikeState = null;
            dead = false;
            currentState = null;
            PlayHard(def.StateFor(AnimAction.Idle));

            driver.SimEventEmitted += OnSimEvent;
        }

        void OnDestroy()
        {
            if (driver != null) driver.SimEventEmitted -= OnSimEvent;
        }

        void OnSimEvent(SimEvent ev)
        {
            if (ev.Type == SimEventType.FighterDied && ev.Actor == index)
            {
                dead = true;
                PlayHard(def.StateFor(AnimAction.Die));
                return;
            }
            if (dead) return;

            switch (ev.Type)
            {
                case SimEventType.AbilityUsed when ev.Actor == index:
                    BeginAttack(ev.AbilityId);
                    break;
                case SimEventType.Parried when ev.Actor == index:
                    PlayOneShot(def.StateFor(AnimAction.Block), blockSeconds);
                    break;
                case SimEventType.RiposteTriggered when ev.Actor == index:
                    PlayOneShot(def.StateFor(AnimAction.Riposte, def.StateFor(AnimAction.Attack)), riposteSeconds);
                    break;
                case SimEventType.Damaged when ev.Target == index:
                    // Don't stomp the sustained stun pose with a fleeting hit reaction.
                    if (!IsStunned()) PlayOneShot(def.StateFor(AnimAction.Hit), hitSeconds);
                    break;
            }
        }

        /// <summary>Play the strike now, or — for a wind-up/delayed ability — hold an anticipation
        /// pose and schedule the strike so its contact lands on the effect tick.</summary>
        void BeginAttack(string abilityId)
        {
            var state = def.StateForAbility(abilityId) ?? def.StateFor(AnimAction.Attack);
            float lead = EffectLeadSeconds(abilityId);

            if (lead > strikeContactSeconds)
            {
                // Anticipation now; strike later so the swing connects on the effect tick.
                var windup = def.StateFor(AnimAction.Windup,
                    def.StateFor(AnimAction.Cast, def.StateFor(AnimAction.Idle)));
                pendingStrikeState = state;
                pendingStrikeAt = Time.time + lead - strikeContactSeconds;
                if (CrossFade(windup, force: true))
                    busyUntil = pendingStrikeAt; // hold the anticipation until the strike fires
            }
            else
            {
                PlayOneShot(state, attackSeconds);
            }
        }

        /// <summary>Seconds between the AbilityUsed event and the effect landing, from the sim's
        /// ability data (read-only). Casts already emit AbilityUsed at completion, so their lead
        /// is 0; wind-up/delayed instants lead by their PreLock/Delay ticks.</summary>
        float EffectLeadSeconds(string abilityId)
        {
            var ab = Ability(abilityId);
            if (ab == null) return 0f;
            return (ab.PreLockTicks + ab.DelayTicks) * SimConstants.TickDuration;
        }

        AbilityDef Ability(string abilityId)
        {
            var content = driver != null && driver.Battle != null ? driver.Battle.Content : null;
            if (content == null || string.IsNullOrEmpty(abilityId)) return null;
            return content.Abilities.TryGetValue(abilityId, out var d) ? d : null;
        }

        bool IsStunned()
        {
            var f = driver != null && driver.Battle != null ? driver.Battle.Fighters[index] : null;
            return f != null && f.HasBlockingStatus;
        }

        void Update()
        {
            if (anim == null || dead) return;

            // Stun overrides one-shots: hold a clear stunned pose for the whole status duration.
            if (IsStunned())
            {
                pendingStrikeState = null;
                Ensure(def.StateFor(AnimAction.Stunned, def.StateFor(AnimAction.Broken, def.StateFor(AnimAction.Hit))));
                return;
            }

            // Fire a scheduled wind-up strike once its time arrives.
            if (pendingStrikeState != null && Time.time >= pendingStrikeAt)
            {
                var s = pendingStrikeState;
                pendingStrikeState = null;
                PlayOneShot(s, attackSeconds);
            }

            if (Time.time < busyUntil) return; // let the current one-shot / anticipation finish

            var f = driver != null && driver.Battle != null ? driver.Battle.Fighters[index] : null;
            if (f != null && f.IsCasting)
                Ensure(def.StateFor(AnimAction.Cast));
            else
                Ensure(def.StateFor(AnimAction.Idle));
        }

        // --- Playback helpers (all CrossFade by state name, with HasState guards) ---

        void PlayOneShot(string state, float holdSeconds)
        {
            if (CrossFade(state, force: true))
                busyUntil = Time.time + Mathf.Max(0.05f, holdSeconds);
        }

        void Ensure(string state) => CrossFade(state, force: false);

        void PlayHard(string state)
        {
            currentState = null;
            CrossFade(state, force: true);
        }

        bool CrossFade(string state, bool force)
        {
            if (anim == null || string.IsNullOrEmpty(state)) return false;
            if (!force && state == currentState) return false;
            int hash = Animator.StringToHash(state);
            if (!anim.HasState(0, hash)) return false; // graceful: missing state → no-op
            anim.CrossFadeInFixedTime(hash, crossFade);
            currentState = state;
            return true;
        }
    }
}
