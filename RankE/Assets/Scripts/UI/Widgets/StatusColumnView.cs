using System.Collections.Generic;
using RankE.Game;
using RankE.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// One fighter's status icon column at the screen edge (sketch: buff/debuff
    /// columns left and right). Each active status shows a colored tile, its name
    /// and remaining seconds; entries are pooled and synced from sim state.
    /// </summary>
    public sealed class StatusColumnView : MonoBehaviour
    {
        sealed class Entry
        {
            public GameObject Root;
            public Image Tile;
            public Text Label;
        }

        BattleDriver driver;
        int index;
        RectTransform column;
        readonly List<Entry> entries = new List<Entry>();

        static readonly Dictionary<string, Color> StatusColors = new Dictionary<string, Color>
        {
            { "stun", new Color(1f, 0.85f, 0.2f) },
            { "poison", new Color(0.4f, 0.8f, 0.2f) },
            { "regen", new Color(0.2f, 0.9f, 0.7f) },
            { "parry", new Color(0.9f, 0.9f, 1f) },
            { "broken", new Color(1f, 0.3f, 0.15f) },
            { "distance", new Color(0.4f, 0.6f, 1f) },
        };

        public void Init(BattleDriver driver, Transform parent, int fighterIndex, HudPlacement placement)
        {
            this.driver = driver;
            index = fighterIndex;

            // Down each side edge, below the top bar (and, on the right, below the
            // upcoming-actions panel) — the sketch's buff/debuff columns.
            column = UiFactory.Rect($"Statuses{fighterIndex}", parent);
            placement.Apply(column);
        }

        void Update()
        {
            var battle = driver != null ? driver.Battle : null;
            var statuses = battle != null ? battle.Fighters[index].Statuses : null;
            int count = statuses != null ? statuses.Count : 0;

            while (entries.Count < count) entries.Add(CreateEntry(entries.Count));
            for (int i = 0; i < entries.Count; i++)
            {
                bool active = i < count;
                entries[i].Root.SetActive(active);
                if (!active) continue;

                var st = statuses[i];
                var color = StatusColors.TryGetValue(st.Def.Id, out var c)
                    ? c : new Color(0.7f, 0.7f, 0.7f);
                entries[i].Tile.color = color;
                entries[i].Label.text =
                    $"{st.Def.Name} {st.Remaining * SimConstants.TickDuration:0.0}s";
            }
        }

        Entry CreateEntry(int row)
        {
            bool left = index == 0;
            var root = UiFactory.Rect($"Status{row}", column);
            UiFactory.PlaceFixed(root, new Vector2(left ? 0f : 1f, 1f),
                new Vector2(0f, -row * 40f), new Vector2(220f, 36f));

            var tile = UiFactory.Panel("Tile", root, Color.white);
            UiFactory.PlaceFixed((RectTransform)tile.transform, new Vector2(left ? 0f : 1f, 0.5f),
                Vector2.zero, new Vector2(30f, 30f));

            var label = UiFactory.Label("Label", root, "", 20, Color.white,
                left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            UiFactory.PlaceFixed((RectTransform)label.transform, new Vector2(left ? 0f : 1f, 0.5f),
                new Vector2(left ? 38f : -38f, 0f), new Vector2(180f, 32f));

            return new Entry { Root = root.gameObject, Tile = tile, Label = label };
        }
    }
}
