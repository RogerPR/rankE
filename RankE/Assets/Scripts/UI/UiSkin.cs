using System.Collections.Generic;
using UnityEngine;

namespace RankE.UI
{
    /// <summary>An ability id → its bar icon sprite.</summary>
    [System.Serializable]
    public struct AbilityIcon
    {
        public string AbilityId;
        public Sprite Icon;
    }

    /// <summary>
    /// The wooden-UI theme catalogue: the sliced frame/button/bar sprites and the
    /// per-ability icons the HUD draws. Stored under Resources so the runtime can load
    /// it (and pull its referenced sprites into the build) without a scene reference,
    /// exactly like <see cref="RankE.Game.AbilityVfxRegistry"/> /
    /// <see cref="RankE.Game.FighterVisualRegistry"/>. Populated by the editor
    /// <c>UiSkinBuilder</c> from the imported Wooden UI pack; every slot is optional, so
    /// <see cref="UiFactory"/> degrades gracefully to its flat placeholder look when a
    /// sprite is missing or the skin isn't built yet. Pure presentation data — the user
    /// retunes slots/icons in the Inspector with no recompile.
    /// </summary>
    public sealed class UiSkin : ScriptableObject
    {
        public const string ResourcePath = "RankE/UiSkin";

        [Header("Sliced container sprites (9-slice borders set by the builder)")]
        [Tooltip("Default panel/menu background frame.")]
        public Sprite PanelFrame;

        [Tooltip("Ability slot frame (and other small framed cells).")]
        public Sprite SlotFrame;

        [Tooltip("Wooden plank used for title plates / header strips.")]
        public Sprite Plank;

        [Header("Button sprites (SpriteSwap transition)")]
        public Sprite Button;
        public Sprite ButtonHover;
        public Sprite ButtonPressed;

        [Header("Bar sprites")]
        [Tooltip("Shared bar trough/background (drawn behind every fill).")]
        public Sprite BarBackground;

        [Tooltip("Neutral fill strip, tinted at runtime by each bar's colour (HP red→green "
            + "danger lerp, cast yellow, break amber, …). Keep it light/neutral so the tint "
            + "reads — a colour-baked strip would fight the dynamic tint.")]
        public Sprite BarFill;

        [Header("Feel knobs (tune in the Inspector — no recompile)")]
        [Tooltip("Pixels-per-unit multiplier applied to sliced sprites so the wooden "
            + "borders read at a sensible thickness regardless of source resolution.")]
        public float PixelsPerUnitMultiplier = 1f;

        [Tooltip("Tint applied to framed panels (white = the sprite's own colour).")]
        public Color FrameTint = Color.white;

        [Header("HUD palette (tune in the Inspector — no recompile; use Tools ▸ RANK E ▸ "
            + "Rebuild HUD to see edits mid-fight)")]
        [Tooltip("HP bar fill at full health (lerps toward danger as HP drops).")]
        public Color HpFull = new Color(0.2f, 0.85f, 0.3f);

        [Tooltip("HP bar fill at empty health.")]
        public Color HpDanger = new Color(0.9f, 0.2f, 0.2f);

        [Tooltip("Cast-progress fill on both timing bars.")]
        public Color CastFill = new Color(0.95f, 0.85f, 0.3f);

        [Tooltip("Enemy telegraph wind-up fill.")]
        public Color TelegraphFill = new Color(1f, 0.5f, 0.15f);

        [Tooltip("Animation-lock fill on the timing bars.")]
        public Color LockFill = new Color(0.55f, 0.55f, 0.62f);

        [Tooltip("Break bar fill.")]
        public Color BreakFill = new Color(1f, 0.6f, 0.1f);

        [Tooltip("Radial ring on the opponent's imminent next action.")]
        public Color ImminentFill = new Color(0.95f, 0.35f, 0.2f);

        [Tooltip("Gold accent for titles/headers (pause title, …).")]
        public Color GoldAccent = new Color(1f, 0.86f, 0.5f);

        [Tooltip("Full-screen dim behind overlays (pause).")]
        public Color OverlayDim = new Color(0f, 0f, 0f, 0.6f);

        [Tooltip("Dark trough drawn behind every bar fill.")]
        public Color BarTrough = new Color(0f, 0f, 0f, 0.6f);

        [Tooltip("Cool info text: fighter names, spell-gem counters.")]
        public Color StatText = new Color(0.55f, 0.75f, 1f);

        [Header("Ability icons (id → sprite; built by keyword, override here)")]
        public List<AbilityIcon> Icons = new List<AbilityIcon>();

        static UiSkin cached;
        static bool tried;
        static UiSkin fallback;

        /// <summary>
        /// The palette source widgets read colours from: the built skin asset when present,
        /// otherwise a plain instance whose field initializers ARE the classic look — so an
        /// unbuilt skin (or an asset saved before a field existed) changes nothing.
        /// </summary>
        public static UiSkin Palette
        {
            get
            {
                var skin = Load();
                if (skin != null) return skin;
                if (fallback == null) fallback = CreateInstance<UiSkin>();
                return fallback;
            }
        }

        /// <summary>Load the singleton skin from Resources (null if not built yet).</summary>
        public static UiSkin Load()
        {
            if (cached == null && !tried)
            {
                cached = Resources.Load<UiSkin>(ResourcePath);
                tried = true; // avoid hammering Resources every frame before it's built
            }
            return cached;
        }

        /// <summary>Icon for an ability id, or null if unmapped.</summary>
        public Sprite IconFor(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId)) return null;
            for (int i = 0; i < Icons.Count; i++)
                if (Icons[i].Icon != null && Icons[i].AbilityId == abilityId)
                    return Icons[i].Icon;
            return null;
        }

        /// <summary>Map a sim combo-colour id (e.g. "red"/"yellow") to its on-screen colour.
        /// The single place colour ids become RGB; spare hues are pre-mapped for future
        /// abilities. Unknown ids fall back to neutral grey. Static so widgets can call it
        /// without a loaded skin.</summary>
        public static Color ComboColorFor(string colorId)
        {
            switch (colorId)
            {
                case "red": return new Color(0.90f, 0.22f, 0.22f);
                case "yellow": return new Color(0.96f, 0.82f, 0.20f);
                case "green": return new Color(0.30f, 0.80f, 0.35f);
                case "blue": return new Color(0.28f, 0.55f, 0.95f);
                case "purple": return new Color(0.66f, 0.40f, 0.90f);
                case "orange": return new Color(0.96f, 0.55f, 0.18f);
                default: return new Color(0.6f, 0.6f, 0.6f);
            }
        }
    }
}
