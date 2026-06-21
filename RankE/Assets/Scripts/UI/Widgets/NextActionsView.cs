using System.Collections.Generic;
using System.Text;
using RankE.Game;
using RankE.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// The opponent's upcoming non-quick actions ("Future op actions" in the sketch): a
    /// top-right stack of three icon+bar rows above the opponent cast bar, forming one vertical
    /// timeline. The <b>bottom</b> row is imminent (nearest the cast bar) — its bar is the
    /// cadence countdown to the next beat; rows above are the queued follow-ups, dimmer and
    /// rising into the future. The action that is actually telegraphing/casting shows on the
    /// cast bar, not here, so this is a pure <i>future</i> list with no duplication. The plan's
    /// committed look-ahead means what's shown is exactly what will fire. Pure view: reads the
    /// enemy's <see cref="IActionPlan"/>, never decides anything.
    /// </summary>
    public sealed class NextActionsView : MonoBehaviour
    {
        const int Rows = 3;
        const float RowH = 78f;

        sealed class Row
        {
            public GameObject Root;
            public Image Icon;
            public Text NameLabel;
            public Image BarBg;
            public Image BarFill;
            public CanvasGroup Group;
        }

        BattleDriver driver;
        readonly Row[] rows = new Row[Rows];
        Text delayedLabel;
        readonly StringBuilder sb = new StringBuilder();
        float width = 384f; // panel width, from the layout config

        static readonly Color ImminentFill = new Color(0.95f, 0.35f, 0.2f);
        static readonly Color QueuedFill = new Color(0.55f, 0.55f, 0.62f);

        public void Init(BattleDriver driver, Transform parent, HudPlacement placement)
        {
            this.driver = driver;
            width = placement.size.x;

            var container = UiFactory.Rect("NextActions", parent);
            placement.Apply(container);

            var header = UiFactory.Label("Header", container, "OPPONENT — NEXT", 20,
                new Color(1f, 0.8f, 0.5f), TextAnchor.UpperRight);
            UiFactory.PlaceFixed((RectTransform)header.transform, new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(width, 26f));

            for (int r = 0; r < Rows; r++)
                rows[r] = BuildRow(container, r);

            delayedLabel = UiFactory.Label("Delayed", container, "", 18,
                new Color(1f, 0.85f, 0.45f), TextAnchor.UpperRight);
            UiFactory.PlaceFixed((RectTransform)delayedLabel.transform, new Vector2(1f, 1f),
                new Vector2(0f, -(30f + Rows * RowH)), new Vector2(width, 80f));
        }

        Row BuildRow(Transform container, int r)
        {
            // The imminent (soonest) beat lives in the BOTTOM row, nearest the cast bar below.
            bool imminent = r == Rows - 1;
            var root = UiFactory.Rect($"Row{r}", container);
            UiFactory.PlaceFixed(root, new Vector2(1f, 1f), new Vector2(0f, -(30f + r * RowH)),
                new Vector2(width, RowH - 8f));
            var group = root.gameObject.AddComponent<CanvasGroup>();

            // Icon on the right edge; the imminent row is largest.
            float iconSize = imminent ? 64f : 52f;
            var frame = UiFactory.Frame($"IconFrame{r}", root);
            UiFactory.PlaceFixed((RectTransform)frame.transform, new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(iconSize, iconSize));
            var icon = UiFactory.Icon("Icon", frame.transform, null);
            UiFactory.PlaceFixed((RectTransform)icon.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(iconSize - 12f, iconSize - 12f));
            var name = UiFactory.Label("Name", frame.transform, "", 14, Color.white);
            UiFactory.PlaceStretch((RectTransform)name.transform);

            // Countdown bar to the left of the icon.
            float barW = width - iconSize - 14f;
            var fill = UiFactory.Bar($"Bar{r}", root, new Color(0f, 0f, 0f, 0.6f),
                imminent ? ImminentFill : QueuedFill, Image.FillMethod.Horizontal, out var bg);
            UiFactory.PlaceFixed((RectTransform)bg.transform, new Vector2(1f, 0.5f),
                new Vector2(-(iconSize + 14f), 0f), new Vector2(barW, imminent ? 30f : 20f));

            return new Row { Root = root.gameObject, Icon = icon, NameLabel = name,
                BarBg = bg, BarFill = fill, Group = group };
        }

        void Update()
        {
            var battle = driver != null ? driver.Battle : null;
            var plan = driver != null ? driver.EnemyPlan : null;
            if (battle == null)
            {
                for (int r = 0; r < Rows; r++) rows[r].Root.SetActive(false);
                return;
            }

            // Pure future queue: the plan's committed look-ahead, soonest first. The action
            // that's actually telegraphing/casting is shown on the cast bar below, not here.
            var ids = plan != null ? plan.Upcoming(Rows) : System.Array.Empty<string>();

            // Cadence countdown to the next beat — rendered on the imminent (bottom) row.
            float imminentFill = plan != null && plan.IntervalTicks > 0
                ? 1f - Mathf.Clamp01((float)plan.TicksUntilNextAction / plan.IntervalTicks)
                : 0f;

            var skin = UiFactory.Skin;
            for (int r = 0; r < Rows; r++)
            {
                var row = rows[r];
                int dataIndex = (Rows - 1) - r; // bottom row = soonest (ids[0]); top = farthest
                if (dataIndex >= ids.Count)
                {
                    row.Root.SetActive(false);
                    continue;
                }
                row.Root.SetActive(true);
                row.Group.alpha = dataIndex == 0 ? 1f : 0.55f - dataIndex * 0.12f;

                string id = ids[dataIndex];
                var sprite = skin != null ? skin.IconFor(id) : null;
                row.Icon.sprite = sprite;
                row.Icon.enabled = sprite != null;
                string display = battle.Content.Abilities.TryGetValue(id, out var def) ? def.Name : id;
                row.NameLabel.text = sprite != null ? "" : display;
                row.BarFill.fillAmount = dataIndex == 0 ? imminentFill : 0f;
            }

            UpdateDelayed(battle);
        }

        // Delayed abilities in flight (e.g. Falling Star) from either fighter — they reshape
        // everyone's plans, so they're worth surfacing under the queue.
        void UpdateDelayed(Battle battle)
        {
            if (battle.Pending.Count == 0) { delayedLabel.text = ""; return; }
            sb.Length = 0;
            foreach (var p in battle.Pending)
            {
                float secs = (p.FireTick - battle.CurrentTick) * SimConstants.TickDuration;
                sb.AppendLine($"{battle.Fighters[p.Source].Name}: {p.Ability.Name} in {Mathf.Max(0f, secs):0.0}s");
            }
            delayedLabel.text = sb.ToString();
        }
    }
}
