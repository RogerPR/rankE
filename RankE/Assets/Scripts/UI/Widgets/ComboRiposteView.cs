using RankE.Game;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Bottom-left combo tracker (sketch) for both fighters: three O→L→F dots plus
    /// riposte-counter pips — you can read the enemy's chain and know when a parry
    /// is mandatory. Polls ComboStep / RiposteCounter.
    /// </summary>
    public sealed class ComboRiposteView : MonoBehaviour
    {
        BattleDriver driver;
        readonly Image[][] comboDots = new Image[2][];
        readonly Image[][] ripostePips = new Image[2][];
        readonly Text[] rowLabels = new Text[2];

        static readonly Color DotOff = new Color(1f, 1f, 1f, 0.18f);
        static readonly Color DotOn = new Color(0.85f, 0.5f, 1f);
        static readonly Color PipOn = new Color(1f, 0.95f, 0.3f);

        public void Init(BattleDriver driver, Transform parent, HudPlacement placement)
        {
            this.driver = driver;

            // Bottom-left, sitting just above the ability grid.
            var root = UiFactory.Rect("ComboTracker", parent);
            placement.Apply(root);

            for (int i = 0; i < 2; i++)
            {
                float y = i == 0 ? 58f : 0f;
                rowLabels[i] = UiFactory.Label($"Row{i}", root, "—", 18, Color.white,
                    TextAnchor.MiddleLeft);
                UiFactory.PlaceFixed((RectTransform)rowLabels[i].transform, new Vector2(0f, 0f),
                    new Vector2(0f, y + 26f), new Vector2(200f, 22f));

                comboDots[i] = new Image[3];
                for (int d = 0; d < 3; d++)
                {
                    comboDots[i][d] = UiFactory.Panel($"Dot{i}_{d}", root, DotOff);
                    UiFactory.PlaceFixed((RectTransform)comboDots[i][d].transform,
                        new Vector2(0f, 0f), new Vector2(d * 30f, y), new Vector2(24f, 24f));
                }

                ripostePips[i] = new Image[8];
                for (int p = 0; p < 8; p++)
                {
                    ripostePips[i][p] = UiFactory.Panel($"Pip{i}_{p}", root, DotOff);
                    UiFactory.PlaceFixed((RectTransform)ripostePips[i][p].transform,
                        new Vector2(0f, 0f), new Vector2(110f + p * 16f, y + 6f),
                        new Vector2(12f, 12f));
                }
            }
        }

        void Update()
        {
            var battle = driver != null ? driver.Battle : null;
            if (battle == null) return;

            for (int i = 0; i < 2; i++)
            {
                var f = battle.Fighters[i];
                rowLabels[i].text = f.Name;
                for (int d = 0; d < 3; d++)
                    comboDots[i][d].color = d < f.ComboStep ? DotOn : DotOff;
                for (int p = 0; p < ripostePips[i].Length; p++)
                    ripostePips[i][p].color = p < f.RiposteCounter ? PipOn : DotOff;
            }
        }
    }
}
