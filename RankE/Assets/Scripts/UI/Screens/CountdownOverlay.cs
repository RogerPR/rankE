using RankE.Game;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>3·2·1 countdown before each fight (Phase 2 checkbox).</summary>
    public sealed class CountdownOverlay : MonoBehaviour
    {
        MatchController match;
        GameObject root;
        Text number;

        public void Init(MatchController match, Transform parent)
        {
            this.match = match;
            var rt = UiFactory.Rect("Countdown", parent);
            UiFactory.PlaceStretch(rt);
            root = rt.gameObject;

            number = UiFactory.Label("Number", rt, "3", 160, Color.white);
            UiFactory.PlaceFixed((RectTransform)number.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 80f), new Vector2(400f, 200f));
        }

        public void Show(bool on) => root.SetActive(on);

        void Update()
        {
            if (!root.activeSelf || match == null) return;
            number.text = Mathf.CeilToInt(Mathf.Max(0.01f, match.CountdownRemaining)).ToString();
        }
    }
}
