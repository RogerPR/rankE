using RankE.Game;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Vertical HP bars at the far left/right edges plus name, HP numbers and
    /// spell-gem count for both fighters (top bar of the sketch). Polls sim state.
    /// </summary>
    public sealed class HpBarView : MonoBehaviour
    {
        BattleDriver driver;
        readonly Image[] fills = new Image[2];
        readonly Text[] names = new Text[2];
        readonly Text[] hpTexts = new Text[2];
        readonly Text[] gemTexts = new Text[2];

        public void Init(BattleDriver driver, Transform parent)
        {
            this.driver = driver;

            for (int i = 0; i < 2; i++)
            {
                bool left = i == 0;
                var anchor = new Vector2(left ? 0f : 1f, 0.5f);
                float x = left ? 16f : -16f;

                fills[i] = UiFactory.Bar($"HpBar{i}", parent,
                    new Color(0f, 0f, 0f, 0.6f), new Color(0.2f, 0.85f, 0.3f),
                    Image.FillMethod.Vertical, out var bg);
                UiFactory.PlaceFixed((RectTransform)bg.transform, anchor, new Vector2(x, 0f),
                    new Vector2(34f, 620f));

                var corner = new Vector2(left ? 0f : 1f, 1f);
                names[i] = UiFactory.Label($"Name{i}", parent, "—", 30, Color.white,
                    left ? TextAnchor.UpperLeft : TextAnchor.UpperRight);
                UiFactory.PlaceFixed((RectTransform)names[i].transform, corner,
                    new Vector2(left ? 70f : -70f, -14f), new Vector2(300f, 34f));

                hpTexts[i] = UiFactory.Label($"Hp{i}", parent, "", 24, Color.white,
                    left ? TextAnchor.UpperLeft : TextAnchor.UpperRight);
                UiFactory.PlaceFixed((RectTransform)hpTexts[i].transform, corner,
                    new Vector2(left ? 70f : -70f, -50f), new Vector2(300f, 28f));

                gemTexts[i] = UiFactory.Label($"Gems{i}", parent, "", 22,
                    new Color(0.55f, 0.75f, 1f),
                    left ? TextAnchor.UpperLeft : TextAnchor.UpperRight);
                UiFactory.PlaceFixed((RectTransform)gemTexts[i].transform, corner,
                    new Vector2(left ? 70f : -70f, -80f), new Vector2(300f, 26f));
            }
        }

        void Update()
        {
            var battle = driver != null ? driver.Battle : null;
            if (battle == null) return;

            for (int i = 0; i < 2; i++)
            {
                var f = battle.Fighters[i];
                float frac = f.MaxHp > 0 ? Mathf.Clamp01((float)f.Hp / f.MaxHp) : 0f;
                fills[i].fillAmount = frac;
                fills[i].color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.85f, 0.3f), frac);
                names[i].text = f.Name;
                hpTexts[i].text = $"{Mathf.Max(0, f.Hp)} / {f.MaxHp}";
                gemTexts[i].text = f.MaxSpellGems > 0 ? $"Gems {f.SpellGems}/{f.MaxSpellGems}" : "";
            }
        }
    }
}
