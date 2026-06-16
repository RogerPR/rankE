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

        [Header("Ability icons (id → sprite; built by keyword, override here)")]
        public List<AbilityIcon> Icons = new List<AbilityIcon>();

        static UiSkin cached;
        static bool tried;

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
    }
}
