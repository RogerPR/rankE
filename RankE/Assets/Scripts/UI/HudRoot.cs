using RankE.Game;
using UnityEngine;

namespace RankE.UI
{
    /// <summary>
    /// Builds the entire combat UI programmatically at startup (canvas, HUD
    /// widgets, screens) and shows/hides them per match state. Lives on the same
    /// GameObject as CombatBootstrap, which constructs the Game layer in Awake;
    /// this binds to it in Start. Pure view: subscribes and polls, never decides.
    /// </summary>
    public sealed class HudRoot : MonoBehaviour
    {
        MatchController match;
        BattleDriver driver;

        GameObject hudGroup;
        AbilityBarView abilityBar;
        FloatingCombatTextSpawner floatingText;
        LoadoutPickerScreen picker;
        CountdownOverlay countdown;
        ResultScreen result;
        GameObject pauseOverlay;

        void Start()
        {
            var bootstrap = GetComponent<CombatBootstrap>();
            if (bootstrap == null || bootstrap.Match == null)
            {
                Debug.LogError("HudRoot: CombatBootstrap missing on the same GameObject.");
                return;
            }
            match = bootstrap.Match;
            driver = match.Driver;

            UiFactory.EnsureEventSystem();
            var canvas = UiFactory.CreateCanvas("HUD Canvas");

            var playerGo = GameObject.Find("PlayerCapsule");
            var enemyGo = GameObject.Find("EnemyCapsule");
            var bodies = new[]
            {
                playerGo != null ? playerGo.transform : null,
                enemyGo != null ? enemyGo.transform : null,
            };

            // Fight HUD (hidden on the loadout screen).
            var hudRt = UiFactory.Rect("Hud", canvas.transform);
            UiFactory.PlaceStretch(hudRt);
            hudGroup = hudRt.gameObject;

            hudGroup.AddComponent<HpBarView>().Init(driver, hudRt);
            hudGroup.AddComponent<BreakBarView>().Init(driver, hudRt, bodies);
            var playerCast = hudGroup.AddComponent<CastBarView>();
            playerCast.Init(driver, hudRt, 0);
            var enemyCast = hudGroup.AddComponent<CastBarView>();
            enemyCast.Init(driver, hudRt, 1);
            abilityBar = hudGroup.AddComponent<AbilityBarView>();
            abilityBar.Init(driver, hudRt);
            var playerStatuses = hudGroup.AddComponent<StatusColumnView>();
            playerStatuses.Init(driver, hudRt, 0);
            var enemyStatuses = hudGroup.AddComponent<StatusColumnView>();
            enemyStatuses.Init(driver, hudRt, 1);
            hudGroup.AddComponent<EnemyIntentView>().Init(driver, hudRt);
            hudGroup.AddComponent<ComboRiposteView>().Init(driver, hudRt);
            floatingText = hudGroup.AddComponent<FloatingCombatTextSpawner>();
            floatingText.Init(driver, hudRt, bodies);

            // Overlays/screens, on top of the HUD in hierarchy order.
            pauseOverlay = BuildPauseOverlay(canvas.transform);
            countdown = canvas.gameObject.AddComponent<CountdownOverlay>();
            countdown.Init(match, canvas.transform);
            result = canvas.gameObject.AddComponent<ResultScreen>();
            result.Init(match, canvas.transform);
            picker = canvas.gameObject.AddComponent<LoadoutPickerScreen>();
            picker.Init(match, canvas.transform);

            match.StateChanged += OnStateChanged;
            OnStateChanged(match.State);
        }

        static GameObject BuildPauseOverlay(Transform parent)
        {
            var panel = UiFactory.Panel("PauseOverlay", parent, new Color(0f, 0f, 0f, 0.6f));
            UiFactory.PlaceStretch((RectTransform)panel.transform);
            var label = UiFactory.Label("Label", panel.transform,
                "PAUSED\n(Esc / Start to resume)", 60, Color.white);
            UiFactory.PlaceStretch((RectTransform)label.transform);
            return panel.gameObject;
        }

        void OnDestroy()
        {
            if (match != null) match.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(MatchState state)
        {
            hudGroup.SetActive(state != MatchState.Loadout);
            picker.Show(state == MatchState.Loadout);
            countdown.Show(state == MatchState.Countdown);
            result.Show(state == MatchState.Result);
            pauseOverlay.SetActive(state == MatchState.Paused);

            if (state == MatchState.Countdown)
            {
                abilityBar.Rebind();
                floatingText.ClearAll();
            }
        }
    }
}
