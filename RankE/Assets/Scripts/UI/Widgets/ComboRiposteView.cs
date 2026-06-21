using RankE.Game;
using RankE.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Bottom-left combo tracker — PLAYER ONLY (the opponent never combos). Shows the player's
    /// current colour-sequence target as a row of coloured squares: already-matched colours read
    /// bright, the colours still to press read dim. Completing the sequence flashes the row (the
    /// reward is the empowered next hit). Player riposte-counter pips sit alongside.
    /// Polls <see cref="Fighter.ComboSequence"/> / ComboProgress / RiposteCounter.
    /// </summary>
    public sealed class ComboRiposteView : MonoBehaviour
    {
        const int MaxSquares = 8; // pool; covers CombatTuning.ComboMaxLen and future growth
        const int PlayerIndex = 0;

        BattleDriver driver;
        readonly Image[] comboSquares = new Image[MaxSquares];
        Image[] ripostePips;
        Text rowLabel;

        static readonly Color PendingFade = new Color(1f, 1f, 1f, 0.22f); // tint multiplier for unmatched
        static readonly Color PipOn = new Color(1f, 0.95f, 0.3f);
        static readonly Color PipOff = new Color(1f, 1f, 1f, 0.18f);

        float flashT; // 0..1 trigger pulse on combo completion

        public void Init(BattleDriver driver, Transform parent, HudPlacement placement)
        {
            this.driver = driver;

            var root = UiFactory.Rect("ComboTracker", parent);
            placement.Apply(root);

            rowLabel = UiFactory.Label("ComboLabel", root, "Combo", 18, Color.white, TextAnchor.MiddleLeft);
            UiFactory.PlaceFixed((RectTransform)rowLabel.transform, new Vector2(0f, 0f),
                new Vector2(0f, 30f), new Vector2(200f, 22f));

            for (int d = 0; d < MaxSquares; d++)
            {
                comboSquares[d] = UiFactory.Panel($"ComboSq{d}", root, PendingFade);
                UiFactory.PlaceFixed((RectTransform)comboSquares[d].transform,
                    new Vector2(0f, 0f), new Vector2(d * 30f, 0f), new Vector2(26f, 26f));
                comboSquares[d].gameObject.SetActive(false);
            }

            ripostePips = new Image[8];
            for (int p = 0; p < ripostePips.Length; p++)
            {
                ripostePips[p] = UiFactory.Panel($"Pip{p}", root, PipOff);
                UiFactory.PlaceFixed((RectTransform)ripostePips[p].transform,
                    new Vector2(0f, 0f), new Vector2(p * 16f, 36f), new Vector2(12f, 12f));
            }

            driver.SimEventEmitted += OnSimEvent;
        }

        void OnDestroy()
        {
            if (driver != null) driver.SimEventEmitted -= OnSimEvent;
        }

        void OnSimEvent(SimEvent ev)
        {
            if (ev.Type == SimEventType.ComboCompleted && ev.Actor == PlayerIndex)
                flashT = 1f;
        }

        void Update()
        {
            var battle = driver != null ? driver.Battle : null;
            if (battle == null) return;
            var f = battle.Fighters[PlayerIndex];

            flashT = Mathf.Max(0f, flashT - Time.deltaTime * 2.5f);
            // The whole row briefly swells + whitens on a completed combo.
            float pulse = 1f + 0.25f * flashT;

            var seq = f.ComboSequence;
            for (int d = 0; d < MaxSquares; d++)
            {
                bool show = d < seq.Count;
                if (comboSquares[d].gameObject.activeSelf != show)
                    comboSquares[d].gameObject.SetActive(show);
                if (!show) continue;

                Color c = UiSkin.ComboColorFor(seq[d]);
                if (d >= f.ComboProgress) c *= PendingFade;     // not yet pressed → dim
                c = Color.Lerp(c, Color.white, 0.6f * flashT);  // trigger flash
                comboSquares[d].color = c;
                comboSquares[d].rectTransform.localScale = Vector3.one * pulse;
            }

            for (int p = 0; p < ripostePips.Length; p++)
                ripostePips[p].color = p < f.RiposteCounter ? PipOn : PipOff;
        }
    }
}
