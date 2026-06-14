using RankE.Sim;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Drives one fighter's Animator from sim events, rig-agnostically: it never names
    /// a clip, it asks the <see cref="FighterVisualDef"/> for the Animator state that
    /// represents a semantic action (or a specific ability id) and CrossFades to it.
    /// One-shots (attack/hit/block/riposte) hold for a tunable window then the poll in
    /// <see cref="Update"/> returns to Idle / Cast / Broken based on read-only sim state.
    /// Pure presentation — reads events and Fighter state, computes nothing.
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

        Animator anim;
        BattleDriver driver;
        FighterVisualDef def;
        int index;

        string currentState;
        float busyUntil;   // realtime; while > now, a one-shot is playing
        float brokenUntil; // realtime; while > now, hold the Broken pose
        bool dead;

        public void Bind(BattleDriver driver, int index, FighterVisualDef def, Animator animator)
        {
            this.driver = driver;
            this.index = index;
            this.def = def;
            anim = animator;
            if (anim != null) anim.applyRootMotion = false; // sim owns position, not root motion

            busyUntil = brokenUntil = 0f;
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
            if (anim == null || dead) { /* still allow death below */ }

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
                {
                    var state = def.StateForAbility(ev.AbilityId) ?? def.StateFor(AnimAction.Attack);
                    PlayOneShot(state, attackSeconds);
                    break;
                }
                case SimEventType.Parried when ev.Actor == index:
                    PlayOneShot(def.StateFor(AnimAction.Block), blockSeconds);
                    break;
                case SimEventType.RiposteTriggered when ev.Actor == index:
                    PlayOneShot(def.StateFor(AnimAction.Riposte, def.StateFor(AnimAction.Attack)), riposteSeconds);
                    break;
                case SimEventType.Damaged when ev.Target == index:
                    PlayOneShot(def.StateFor(AnimAction.Hit), hitSeconds);
                    break;
                case SimEventType.Broken when ev.Target == index:
                    brokenUntil = Time.time + brokenSeconds;
                    PlayOneShot(def.StateFor(AnimAction.Broken, def.StateFor(AnimAction.Hit)), brokenSeconds);
                    break;
            }
        }

        void Update()
        {
            if (anim == null || dead) return;
            if (Time.time < busyUntil) return; // let the current one-shot finish

            var f = driver != null && driver.Battle != null ? driver.Battle.Fighters[index] : null;
            if (f != null && f.IsCasting)
                Ensure(def.StateFor(AnimAction.Cast));
            else if (Time.time < brokenUntil)
                Ensure(def.StateFor(AnimAction.Broken, def.StateFor(AnimAction.Hit)));
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
