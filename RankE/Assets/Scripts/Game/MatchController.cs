using System;
using RankE.Sim;
using UnityEngine;

namespace RankE.Game
{
    public enum MatchState
    {
        Loadout,
        Countdown,
        Fighting,
        Paused,
        Result,
    }

    /// <summary>
    /// Match flow state machine: Loadout → Countdown(3·2·1) → Fighting → Result →
    /// rematch or back to loadout. Owns no UI — the UI layer subscribes to
    /// StateChanged and calls the public transitions. A rematch builds a fresh
    /// Battle with a new seed; nothing is reloaded.
    /// </summary>
    public sealed class MatchController : MonoBehaviour
    {
        public const float CountdownSeconds = 3f;

        /// <summary>≈0.5s enemy attack telegraph (GAME_DESIGN §1 enemy AI).</summary>
        public int EnemyTelegraphTicks = 10;

        public DebugLoadout Loadout { get; } = new DebugLoadout();
        public MatchState State { get; private set; } = MatchState.Loadout;
        public float CountdownRemaining { get; private set; }

        /// <summary>Winner of the last finished fight (0 = player) or -1.</summary>
        public int LastWinner { get; private set; } = -1;

        public BattleDriver Driver { get; private set; }
        public PlayerInputController Input { get; private set; }

        public event Action<MatchState> StateChanged;

        readonly System.Random seeds = new System.Random();

        public void Init(BattleDriver driver, PlayerInputController input)
        {
            Driver = driver;
            Input = input;
            Driver.PlayerBuffer = Input.Buffer;
            Driver.BattleEnded += OnBattleEnded;
            Input.PausePressed += TogglePause;
            SetState(MatchState.Loadout);
        }

        /// <summary>Loadout → Countdown: builds both configs and a fresh battle.</summary>
        public void StartMatch()
        {
            if (State != MatchState.Loadout && State != MatchState.Result) return;
            BeginFight();
        }

        public void Rematch() => StartMatch();

        /// <summary>
        /// Dev/tuning: rebuild the fight immediately from ANY state. Tuning edits apply on the
        /// next fight (each fight clones the live TuningProfile in BattleDriver.Begin), so this
        /// is how the Combat Tuning window applies a change without playing the current fight
        /// out to its end. Pure flow — no sim/gameplay logic here.
        /// </summary>
        public void RestartFight() => BeginFight();

        /// <summary>Build both configs + a fresh battle and run the countdown. No state guard.</summary>
        void BeginFight()
        {
            var player = Loadout.BuildPlayerConfig();
            var enemy = PocContent.DefaultConfig(Loadout.EnemyVisualName);
            Driver.Begin(player, enemy, new PocBehaviorProfile(), EnemyTelegraphTicks, seeds.Next());
            Input.SetLoadout(player.Abilities);
            Input.Buffer.Clear();

            CountdownRemaining = CountdownSeconds;
            SetState(MatchState.Countdown);
        }

        public void BackToLoadout()
        {
            if (State != MatchState.Result) return;
            SetState(MatchState.Loadout);
        }

        /// <summary>
        /// Abandon the current fight from the pause menu and return to the loadout screen.
        /// Unlike <see cref="BackToLoadout"/> (Result-only), this is reachable mid-fight; it
        /// just stops the driver and changes state — no sim/gameplay logic.
        /// </summary>
        public void QuitToLoadout()
        {
            Driver.Running = false;
            SetState(MatchState.Loadout);
        }

        public void TogglePause()
        {
            if (State == MatchState.Fighting)
            {
                Driver.Running = false;
                SetState(MatchState.Paused);
            }
            else if (State == MatchState.Paused)
            {
                Driver.Running = true;
                SetState(MatchState.Fighting);
            }
        }

        void Update()
        {
            if (State != MatchState.Countdown) return;
            CountdownRemaining -= Time.deltaTime;
            if (CountdownRemaining <= 0f)
            {
                Driver.Running = true;
                SetState(MatchState.Fighting);
            }
        }

        void OnBattleEnded(int winner)
        {
            LastWinner = winner;
            SetState(MatchState.Result);
        }

        void SetState(MatchState next)
        {
            State = next;
            Input.SetCombatEnabled(next == MatchState.Fighting);
            Input.SetMetaEnabled(next == MatchState.Fighting || next == MatchState.Paused);
            StateChanged?.Invoke(next);
        }
    }
}
