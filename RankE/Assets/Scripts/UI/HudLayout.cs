using System;
using UnityEngine;

namespace RankE.UI
{
    /// <summary>
    /// Inspector-editable placement for one HUD widget's root container: a screen anchor
    /// (0–1 on each axis; (0,0) = bottom-left, (1,1) = top-right, (0.5,0.5) = centre), a
    /// pixel offset from that anchor, and a size. <see cref="Apply"/> writes it onto a
    /// RectTransform exactly like <see cref="UiFactory.PlaceFixed"/>.
    /// </summary>
    [Serializable]
    public sealed class HudPlacement
    {
        public Vector2 anchor;
        public Vector2 offset;
        public Vector2 size;

        public HudPlacement() { }

        public HudPlacement(Vector2 anchor, Vector2 offset, Vector2 size)
        {
            this.anchor = anchor;
            this.offset = offset;
            this.size = size;
        }

        public void Apply(RectTransform rt) => UiFactory.PlaceFixed(rt, anchor, offset, size);
    }

    /// <summary>
    /// Data-driven positions for the in-fight HUD. Held by <see cref="HudRoot"/> as a
    /// serialized field, so every panel can be moved/resized in the Inspector (on the
    /// CombatBootstrap GameObject) without touching code — no recompile. The defaults here
    /// reproduce the hand-coded layout the widgets used to hard-code. Each field is the
    /// widget's *root container*; its children ride along, so moving the root moves the
    /// whole panel. Pure view config — no gameplay.
    /// </summary>
    [Serializable]
    public sealed class HudLayout
    {
        [Tooltip("Opponent's next-3 non-quick actions (top-right stack).")]
        public HudPlacement nextActions =
            new HudPlacement(new Vector2(1f, 1f), new Vector2(-24f, -132f), new Vector2(384f, 264f));

        [Tooltip("Player ability grid (bottom-left; main row + quick row).")]
        public HudPlacement abilityBar =
            new HudPlacement(new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(392f, 200f));

        [Tooltip("Combo / riposte tracker (bottom-left, above the ability grid).")]
        public HudPlacement combo =
            new HudPlacement(new Vector2(0f, 0f), new Vector2(40f, 250f), new Vector2(320f, 110f));

        [Tooltip("Player casting indicator (icon + bar near the player).")]
        public HudPlacement playerCast =
            new HudPlacement(new Vector2(0f, 0.5f), new Vector2(300f, 150f), new Vector2(420f, 56f));

        [Tooltip("Opponent timing bar (telegraph/cast/lock). Stacked directly under the " +
            "'next actions' queue so the two read as one top-right timeline.")]
        public HudPlacement enemyCast =
            new HudPlacement(new Vector2(1f, 1f), new Vector2(-24f, -404f), new Vector2(420f, 56f));

        [Tooltip("Player status/buff column (left edge).")]
        public HudPlacement playerStatuses =
            new HudPlacement(new Vector2(0f, 1f), new Vector2(24f, -440f), new Vector2(220f, 420f));

        [Tooltip("Opponent status/buff column (right edge).")]
        public HudPlacement enemyStatuses =
            new HudPlacement(new Vector2(1f, 1f), new Vector2(-24f, -440f), new Vector2(220f, 420f));

        [Tooltip("Height in pixels of the full-width top status strip.")]
        public float topBarHeight = 112f;
    }
}
