using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Scene entry point — the only component hand-placed in CombatScene. Builds the
    /// whole Game layer in Awake (driver, input, match flow, capsule views); the UI
    /// layer's HudRoot (same GameObject) finds the MatchController in Start and
    /// builds the HUD on top. Everything else lives in code, not the scene.
    /// </summary>
    public sealed class CombatBootstrap : MonoBehaviour
    {
        public MatchController Match { get; private set; }

        void Awake()
        {
            var driver = gameObject.AddComponent<BattleDriver>();
            var input = gameObject.AddComponent<PlayerInputController>();
            Match = gameObject.AddComponent<MatchController>();
            Match.Init(driver, input);

            var playerGo = GameObject.Find("PlayerCapsule");
            var enemyGo = GameObject.Find("EnemyCapsule");
            if (playerGo == null || enemyGo == null)
            {
                Debug.LogError("CombatBootstrap: PlayerCapsule/EnemyCapsule not found in scene.");
                return;
            }

            var playerBody = playerGo.AddComponent<FighterViewBody>();
            var enemyBody = enemyGo.AddComponent<FighterViewBody>();
            playerBody.Bind(driver, 0, enemyGo.transform);
            enemyBody.Bind(driver, 1, playerGo.transform);

            Match.StateChanged += state =>
            {
                if (state == MatchState.Countdown)
                {
                    playerBody.ResetVisual();
                    enemyBody.ResetVisual();
                }
            };
        }
    }
}
