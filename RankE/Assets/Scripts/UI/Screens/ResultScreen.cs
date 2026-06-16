using RankE.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>Victory/defeat screen with the restart loop (Phase 2 checkbox).</summary>
    public sealed class ResultScreen : MonoBehaviour
    {
        MatchController match;
        GameObject root;
        Text title;
        Button rematchButton;

        public void Init(MatchController match, Transform parent)
        {
            this.match = match;

            var panel = UiFactory.Panel("Result", parent, new Color(0f, 0f, 0f, 0.82f));
            UiFactory.PlaceStretch((RectTransform)panel.transform);
            root = panel.gameObject;

            // Framed content box centred over the dim overlay.
            var box = UiFactory.Frame("ResultBox", panel.transform);
            UiFactory.PlaceFixed((RectTransform)box.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 560f));

            title = UiFactory.Label("Title", box.transform, "", 84, Color.white);
            UiFactory.PlaceFixed((RectTransform)title.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 160f), new Vector2(800f, 100f));

            rematchButton = UiFactory.TextButton("Rematch", box.transform, "REMATCH", 32,
                () => match.Rematch());
            UiFactory.PlaceFixed((RectTransform)rematchButton.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(340f, 70f));

            var loadout = UiFactory.TextButton("ChangeLoadout", box.transform, "CHANGE LOADOUT", 32,
                () => match.BackToLoadout());
            UiFactory.PlaceFixed((RectTransform)loadout.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0f, -100f), new Vector2(340f, 70f));
        }

        public void Show(bool on)
        {
            root.SetActive(on);
            if (!on) return;

            title.text = match.LastWinner == 0 ? "VICTORY"
                : match.LastWinner == 1 ? "DEFEAT"
                : "DRAW";
            title.color = match.LastWinner == 0 ? new Color(1f, 0.9f, 0.3f)
                : new Color(0.9f, 0.35f, 0.3f);

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(rematchButton.gameObject);
        }
    }
}
