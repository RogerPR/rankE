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
        Image icon;
        Text label;
        float interruptedFlash;

        static readonly Color CastColor = new Color(0.95f, 0.85f, 0.3f);

        public void Init(BattleDriver driver, Transform parent, int fighterIndex, HudPlacement placement)
        {
            this.driver = driver;
            index = fighterIndex;
            bool left = fighterIndex == 0;

            // The caster's casting indicator sits near their fighter (sketch: icon + bar by the
            // player). The icon shows which spell is being cast.
            var group = UiFactory.Rect($"CastGroup{fighterIndex}", parent);
            placement.Apply(group);
            root = group.gameObject;

            var iconFrame = UiFactory.Frame("CastIcon", group);
            UiFactory.PlaceFixed((RectTransform)iconFrame.transform, new Vector2(left ? 0f : 1f, 0.5f),
                Vector2.zero, new Vector2(56f, 56f));
            icon = UiFactory.Icon("Icon", iconFrame.transform, null);
            UiFactory.PlaceFixed((RectTransform)icon.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(44f, 44f));

            fill = UiFactory.Bar($"CastBar{fighterIndex}", group,
                new Color(0f, 0f, 0f, 0.6f), CastColor, Image.FillMethod.Horizontal, out var bg);
            UiFactory.PlaceFixed((RectTransform)bg.transform, new Vector2(left ? 0f : 1f, 0.5f),
                new Vector2(left ? 66f : -66f, 0f), new Vector2(354f, 28f));

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
            var skin = UiFactory.Skin;
            var sprite = skin != null ? skin.IconFor(f.Casting.Def.Id) : null;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
    }
}
