using RankE.Game;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// The top status strip from the sketch: each fighter's name, a horizontal HP bar with
    /// numbers, and spell-gem count, plus a far-right menu cluster (functional pause). Gold and
    /// the item row are drawn as clearly-inert placeholders — those systems don't exist yet.
    /// Replaces the old edge-anchored vertical HP bars. Pure view: polls sim state.
    /// </summary>
    public sealed class TopStatusBar : MonoBehaviour
    {
        BattleDriver driver;
        readonly Image[] hpFills = new Image[2];
        readonly Text[] names = new Text[2];
        readonly Text[] hpTexts = new Text[2];
        readonly Text[] gemTexts = new Text[2];

        static Color HpGreen => UiSkin.Palette.HpFull;
        static Color HpRed => UiSkin.Palette.HpDanger;
        static readonly Color Inert = new Color(1f, 1f, 1f, 0.16f);

        public void Init(BattleDriver driver, MatchController match, Transform parent, float barHeight)
        {
            this.driver = driver;

            var strip = UiFactory.Panel("TopBar", parent, new Color(0f, 0f, 0f, 0.5f));
            var rt = (RectTransform)strip.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -barHeight);
            rt.offsetMax = new Vector2(0f, 0f);

            BuildFighter(parent, 0); // player, left
            BuildFighter(parent, 1); // opponent, right
            BuildPlayerExtras(parent);
            BuildMenuCluster(parent, match);
        }

        void BuildFighter(Transform parent, int i)
        {
            bool left = i == 0;
            var nameAnchor = new Vector2(left ? 0f : 1f, 1f);
            float nameX = left ? 24f : -210f;
            var align = left ? TextAnchor.UpperLeft : TextAnchor.UpperRight;

            names[i] = UiFactory.Label($"Name{i}", parent, "—", 28, Color.white, align);
            UiFactory.PlaceFixed((RectTransform)names[i].transform, nameAnchor,
                new Vector2(nameX, -10f), new Vector2(360f, 32f));

            float barX = left ? 24f : -210f;
            hpFills[i] = UiFactory.Bar($"Hp{i}", parent, UiSkin.Palette.BarTrough, HpGreen,
                Image.FillMethod.Horizontal, out var bg);
            // Mirror the opponent's bar so both deplete toward the screen centre.
            hpFills[i].fillOrigin = (int)(left ? Image.OriginHorizontal.Left : Image.OriginHorizontal.Right);
            UiFactory.PlaceFixed((RectTransform)bg.transform, new Vector2(left ? 0f : 1f, 1f),
                new Vector2(barX, -46f), new Vector2(560f, 28f));

            hpTexts[i] = UiFactory.Label($"HpText{i}", bg.transform, "", 20, Color.white);
            UiFactory.PlaceStretch((RectTransform)hpTexts[i].transform);

            float gemX = left ? 600f : -780f;
            gemTexts[i] = UiFactory.Label($"Gems{i}", parent, "", 22,
                UiSkin.Palette.StatText, align);
            UiFactory.PlaceFixed((RectTransform)gemTexts[i].transform, new Vector2(left ? 0f : 1f, 1f),
                new Vector2(gemX, -46f), new Vector2(170f, 28f));
        }

        // Gold + an item row — placeholders for systems that don't exist yet (sketch top-left).
        void BuildPlayerExtras(Transform parent)
        {
            var gold = UiFactory.Label("Gold", parent, "$ 0", 22,
                new Color(1f, 0.85f, 0.35f), TextAnchor.UpperLeft);
            UiFactory.PlaceFixed((RectTransform)gold.transform, new Vector2(0f, 1f),
                new Vector2(600f, -10f), new Vector2(170f, 26f));

            for (int s = 0; s < 6; s++)
            {
                var slot = UiFactory.Panel($"Item{s}", parent, Inert);
                UiFactory.PlaceFixed((RectTransform)slot.transform, new Vector2(0f, 1f),
                    new Vector2(24f + s * 28f, -84f), new Vector2(22f, 22f));
            }
        }

        void BuildMenuCluster(Transform parent, MatchController match)
        {
            var pause = UiFactory.TextButton("Menu", parent, "II", 24,
                () => { if (match != null) match.TogglePause(); });
            UiFactory.PlaceFixed((RectTransform)pause.transform, new Vector2(1f, 1f),
                new Vector2(-20f, -14f), new Vector2(56f, 44f));

            for (int s = 0; s < 2; s++)
            {
                var icon = UiFactory.Panel($"MenuIcon{s}", parent, Inert);
                UiFactory.PlaceFixed((RectTransform)icon.transform, new Vector2(1f, 1f),
                    new Vector2(-86f - s * 50f, -16f), new Vector2(40f, 40f));
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
                hpFills[i].fillAmount = frac;
                hpFills[i].color = Color.Lerp(HpRed, HpGreen, frac);
                names[i].text = f.Name;
                hpTexts[i].text = $"{Mathf.Max(0, f.Hp)} / {f.MaxHp}";
                gemTexts[i].text = f.MaxSpellGems > 0 ? $"◆ {f.SpellGems}/{f.MaxSpellGems}" : "";
            }
        }
    }
}
