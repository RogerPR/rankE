using RankE.Game;
using RankE.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// One fighter's cast bar (player top-left, enemy top-right per the sketch).
    /// Fill polls cast progress; a red flash marks an interrupted cast.
    /// </summary>
    public sealed class CastBarView : MonoBehaviour
    {
        BattleDriver driver;
        int index;
        GameObject root;
        Image fill;
        Text label;
        float interruptedFlash;

        static readonly Color CastColor = new Color(0.95f, 0.85f, 0.3f);

        public void Init(BattleDriver driver, Transform parent, int fighterIndex)
        {
            this.driver = driver;
            index = fighterIndex;
            bool left = fighterIndex == 0;

            fill = UiFactory.Bar($"CastBar{fighterIndex}", parent,
                new Color(0f, 0f, 0f, 0.6f), CastColor, Image.FillMethod.Horizontal, out var bg);
            root = bg.gameObject;
            UiFactory.PlaceFixed((RectTransform)bg.transform, new Vector2(left ? 0f : 1f, 1f),
                new Vector2(left ? 70f : -70f, -120f), new Vector2(380f, 26f));

            label = UiFactory.Label("CastLabel", bg.transform, "", 20, Color.white);
            UiFactory.PlaceStretch((RectTransform)label.transform);

            driver.SimEventEmitted += OnSimEvent;
        }

        void OnDestroy()
        {
            if (driver != null) driver.SimEventEmitted -= OnSimEvent;
        }

        void OnSimEvent(SimEvent ev)
        {
            if (ev.Type == SimEventType.CastInterrupted && ev.Target == index)
                interruptedFlash = 0.5f;
        }

        void Update()
        {
            var battle = driver != null ? driver.Battle : null;
            var f = battle != null ? battle.Fighters[index] : null;
            interruptedFlash = Mathf.Max(0f, interruptedFlash - Time.deltaTime);

            if (interruptedFlash > 0f)
            {
                root.SetActive(true);
                fill.fillAmount = 1f;
                fill.color = Color.red;
                label.text = "INTERRUPTED";
                return;
            }

            if (f == null || !f.IsCasting)
            {
                root.SetActive(false);
                return;
            }

            root.SetActive(true);
            int total = f.Casting.EffCastTicks;
            fill.fillAmount = total > 0 ? 1f - Mathf.Clamp01((float)f.CastRemaining / total) : 1f;
            fill.color = CastColor;
            label.text = f.Casting.Def.Name;
        }
    }
}
