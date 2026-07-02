using RankE.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        [Tooltip("Move/resize each HUD panel here — applied when the HUD is built. " +
            "No recompile needed; edits persist in the scene.")]
        [SerializeField] HudLayout layout = new HudLayout();

        MatchController match;
        BattleDriver driver;

        GameObject canvasGo;
        GameObject hudGroup;
        AbilityBarView abilityBar;
        FloatingCombatTextSpawner floatingText;
        ControlPanelScreen controlPanel;
        AbilitiesEditorScreen abilities;
        CharacterCreatorScreen creator;
        CountdownOverlay countdown;
        ResultScreen result;
        GameObject pauseOverlay;
        Button pauseFirstButton;

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
            Build();
        }

        /// <summary>
        /// Dev tool (Tools ▸ RANK E ▸ Rebuild HUD): tear the whole UI down and rebuild it in
        /// place, so inspector edits to <see cref="HudLayout"/>/UiSkin show mid-fight without
        /// restarting. The fight itself is untouched — only view objects are recreated.
        /// (Code edits still need a play-exit: a domain reload wipes the running battle.)
        /// </summary>
        public void Rebuild()
        {
            Teardown();
            Build();
        }

        void Teardown()
        {
            if (match != null) match.StateChanged -= OnStateChanged;
            // Everything lives on/under the canvas (widgets on hudGroup, screens on the canvas
            // GO), so one immediate destroy runs every widget's OnDestroy unsubscribe now,
            // before Build() subscribes fresh instances. The EventSystem GO survives.
            if (canvasGo != null) DestroyImmediate(canvasGo);
            canvasGo = null;
            hudGroup = null;
            abilityBar = null;
            floatingText = null;
            controlPanel = null;
            abilities = null;
            creator = null;
            countdown = null;
            result = null;
            pauseOverlay = null;
            pauseFirstButton = null;
        }

        void Build()
        {
            UiFactory.EnsureEventSystem();
            var canvas = UiFactory.CreateCanvas("HUD Canvas");
            canvasGo = canvas.gameObject;

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

            if (layout == null) layout = new HudLayout();

            hudGroup.AddComponent<TopStatusBar>().Init(driver, match, hudRt, layout.topBarHeight);
            hudGroup.AddComponent<BreakBarView>().Init(driver, hudRt, bodies);
            // Both fighters get a timing bar near them. The player's only ever shows cast/lock;
            // the enemy's also shows the telegraph wind-up (the NEXT panel still lists the queue).
            var playerCast = hudGroup.AddComponent<CastBarView>();
            playerCast.Init(driver, hudRt, 0, layout.playerCast);
            var enemyCast = hudGroup.AddComponent<CastBarView>();
            enemyCast.Init(driver, hudRt, 1, layout.enemyCast);
            abilityBar = hudGroup.AddComponent<AbilityBarView>();
            abilityBar.Init(driver, hudRt, layout.abilityBar);
            var playerStatuses = hudGroup.AddComponent<StatusColumnView>();
            playerStatuses.Init(driver, hudRt, 0, layout.playerStatuses);
            var enemyStatuses = hudGroup.AddComponent<StatusColumnView>();
            enemyStatuses.Init(driver, hudRt, 1, layout.enemyStatuses);
            hudGroup.AddComponent<NextActionsView>().Init(driver, hudRt, layout.nextActions);
            hudGroup.AddComponent<ComboRiposteView>().Init(driver, hudRt, layout.combo);
            floatingText = hudGroup.AddComponent<FloatingCombatTextSpawner>();
            floatingText.Init(driver, hudRt, bodies);

            // Overlays/screens, on top of the HUD in hierarchy order.
            pauseOverlay = BuildPauseOverlay(canvas.transform);
            countdown = canvas.gameObject.AddComponent<CountdownOverlay>();
            countdown.Init(match, canvas.transform);
            result = canvas.gameObject.AddComponent<ResultScreen>();
            result.Init(match, canvas.transform);
            controlPanel = canvas.gameObject.AddComponent<ControlPanelScreen>();
            controlPanel.Init(match, canvas.transform);
            abilities = canvas.gameObject.AddComponent<AbilitiesEditorScreen>();
            abilities.Init(canvas.transform); // pre-fight ability-number editor, opened from the panel
            creator = canvas.gameObject.AddComponent<CharacterCreatorScreen>();
            creator.Init(match, canvas.transform);
            controlPanel.SetAbilitiesScreen(abilities);
            controlPanel.SetCreator(creator);

            match.StateChanged += OnStateChanged;
            OnStateChanged(match.State);
            // A rebuild can land mid-fight; the normal ability-bar rebind only happens on
            // Countdown, so bind the fresh bar to the running battle here.
            if (match.State == MatchState.Fighting || match.State == MatchState.Paused)
                abilityBar.Rebind();
        }

        GameObject BuildPauseOverlay(Transform parent)
        {
            var panel = UiFactory.Panel("PauseOverlay", parent, UiSkin.Palette.OverlayDim);
            panel.raycastTarget = true; // swallow clicks so they don't reach HUD widgets behind
            UiFactory.PlaceStretch((RectTransform)panel.transform);
            var box = UiFactory.Frame("PauseBox", panel.transform);
            layout.pauseBox.Apply((RectTransform)box.transform);

            var title = UiFactory.Label("Title", box.transform, "PAUSED", 56,
                UiSkin.Palette.GoldAccent);
            UiFactory.PlaceFixed((RectTransform)title.transform, new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(500f, 70f));

            // Return to combat / Restart combat / Return to initial screen, stacked.
            pauseFirstButton = PauseButton(box.transform, "RETURN TO COMBAT", 130f, () => match.TogglePause());
            PauseButton(box.transform, "RESTART COMBAT", 30f, () => match.RestartFight());
            PauseButton(box.transform, "RETURN TO INITIAL SCREEN", -70f, () => match.QuitToLoadout());

            return panel.gameObject;
        }

        static Button PauseButton(Transform box, string label, float y,
            UnityEngine.Events.UnityAction onClick)
        {
            var btn = UiFactory.TextButton(label, box, label, 30, onClick);
            UiFactory.PlaceFixed((RectTransform)btn.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0f, y), new Vector2(440f, 70f));
            return btn;
        }

        void OnDestroy()
        {
            if (match != null) match.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(MatchState state)
        {
            hudGroup.SetActive(state != MatchState.Loadout);
            controlPanel.Show(state == MatchState.Loadout);
            if (state != MatchState.Loadout) { creator.Hide(); abilities.Hide(); }
            countdown.Show(state == MatchState.Countdown);
            result.Show(state == MatchState.Result);
            pauseOverlay.SetActive(state == MatchState.Paused);

            if (state == MatchState.Paused && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(pauseFirstButton.gameObject);

            if (state == MatchState.Countdown)
            {
                abilityBar.Rebind();
                floatingText.ClearAll();
            }
        }
    }
}
