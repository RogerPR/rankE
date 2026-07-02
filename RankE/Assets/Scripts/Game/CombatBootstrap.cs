using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Scene entry point — the only component hand-placed in CombatScene. Builds the
    /// whole Game layer in Awake (driver, input, match flow, fighter stage); the UI
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
            ApplyStartupVisuals();

            var playerGo = GameObject.Find("PlayerCapsule");
            var enemyGo = GameObject.Find("EnemyCapsule");
            if (playerGo == null || enemyGo == null)
            {
                Debug.LogError("CombatBootstrap: PlayerCapsule/EnemyCapsule not found in scene.");
                return;
            }

            // The capsules become invisible anchors; FighterStage spawns the real models
            // under them per fight and binds the body/animation views.
            var stage = gameObject.AddComponent<FighterStage>();
            stage.Init(driver, Match, playerGo.transform, enemyGo.transform);
        }

        /// <summary>
        /// The startup preset's numbers seed <see cref="TuningProfile.Active"/> on first touch,
        /// but its visual choices need a loadout — apply them here, where one finally exists.
        /// </summary>
        void ApplyStartupVisuals()
        {
            try
            {
                TuningPresetStore.LoadStartup()?.ApplyVisuals(TuningProfile.Active, Match.Loadout);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("CombatBootstrap: startup preset visuals skipped — " + e.Message);
            }
        }
    }
}
