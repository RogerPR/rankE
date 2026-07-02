using RankE.Game;
using RankE.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// The opponent's upcoming non-quick actions, as a horizontal row of circular icons to the
    /// right of the opponent cast bar, reading left→right from soonest to latest. The leftmost
    /// (largest) circle is the imminent action, nearest the cast bar; its orange radial ring is
    /// the cadence countdown that fills as the next beat approaches. The two follow-ups are
    /// half-size static icons rising into the future. The action that's actually
    /// telegraphing/casting shows on the cast bar, not here, so this is a pure <i>future</i>
    /// list with no duplication. The plan's committed look-ahead means what's shown is exactly
    /// what will fire. Pure view: reads the enemy's <see cref="IActionPlan"/>, never decides.
    /// </summary>
    public sealed class NextActionsView : MonoBehaviour
    {
        const int CellCount = 3;
        const float BigSize = 64f;
        const float SmallSize = BigSize * 0.5f;
        const float Gap = 16f;

        sealed class Cell
        {
            public GameObject Root;
            public Image Icon;
            public Image Ring; // imminent cell only; null for follow-ups
        }

        BattleDriver driver;
        readonly Cell[] cells = new Cell[CellCount];

        static Color ImminentFill => UiSkin.Palette.ImminentFill;
        static readonly Color KnobTint = new Color(0.12f, 0.12f, 0.16f, 0.92f);

        public void Init(BattleDriver driver, Transform parent, HudPlacement placement)
        {
            this.driver = driver;

            var container = UiFactory.Rect("NextActions", parent);
            placement.Apply(container);

            // Left→right: imminent (big) nearest the cast bar, then the two follow-ups.
            float x = 0f;
            for (int i = 0; i < CellCount; i++)
            {
                float size = i == 0 ? BigSize : SmallSize;
                cells[i] = BuildCell(container, i, x, size, withRing: i == 0);
                x += size + Gap;
            }
        }

        Cell BuildCell(Transform container, int index, float x, float size, bool withRing)
        {
            var root = UiFactory.Rect($"Cell{index}", container);
            UiFactory.PlaceFixed(root, new Vector2(0f, 0.5f), new Vector2(x, 0f),
                new Vector2(size, size));

            // Circular knob background.
            var bg = UiFactory.Panel("Knob", root, KnobTint);
            bg.sprite = UiFactory.KnobSprite;
            bg.preserveAspect = true;
            UiFactory.PlaceStretch((RectTransform)bg.transform);

            // Imminent ring: a coloured radial fill that grows as the next beat approaches.
            Image ring = null;
            if (withRing)
            {
                ring = UiFactory.Panel("Ring", root, ImminentFill);
                ring.sprite = UiFactory.KnobSprite;
                ring.preserveAspect = true;
                ring.type = Image.Type.Filled;
                ring.fillMethod = Image.FillMethod.Radial360;
                ring.fillOrigin = (int)Image.Origin360.Top;
                ring.fillClockwise = true;
                ring.fillAmount = 0f;
                UiFactory.PlaceStretch((RectTransform)ring.transform);
            }

            // Ability icon on top, centred.
            var icon = UiFactory.Icon("Icon", root, null);
            UiFactory.PlaceFixed((RectTransform)icon.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size * 0.7f, size * 0.7f));

            return new Cell { Root = root.gameObject, Icon = icon, Ring = ring };
        }

        void Update()
        {
            var battle = driver != null ? driver.Battle : null;
            var plan = driver != null ? driver.EnemyPlan : null;
            if (battle == null)
            {
                for (int i = 0; i < CellCount; i++) cells[i].Root.SetActive(false);
                return;
            }

            // Pure future queue: the plan's committed look-ahead, soonest first. The action
            // that's actually telegraphing/casting is shown on the cast bar, not here.
            var ids = plan != null ? plan.Upcoming(CellCount) : System.Array.Empty<string>();

            // Cadence countdown to the next beat — the imminent (first) cell's radial ring.
            float imminentFill = plan != null && plan.IntervalTicks > 0
                ? 1f - Mathf.Clamp01((float)plan.TicksUntilNextAction / plan.IntervalTicks)
                : 0f;

            var skin = UiFactory.Skin;
            for (int i = 0; i < CellCount; i++)
            {
                var cell = cells[i];
                if (i >= ids.Count)
                {
                    cell.Root.SetActive(false);
                    continue;
                }
                cell.Root.SetActive(true);

                var sprite = skin != null ? skin.IconFor(ids[i]) : null;
                cell.Icon.sprite = sprite;
                cell.Icon.enabled = sprite != null;

                if (cell.Ring != null) cell.Ring.fillAmount = imminentFill;
            }
        }
    }
}
