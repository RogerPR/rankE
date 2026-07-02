using RankE.UI;
using UnityEditor;
using UnityEngine;

namespace RankE.Editor
{
    /// <summary>
    /// Play-mode UI iteration helpers. "Rebuild HUD" (Cmd/Ctrl+Shift+H) tears down and
    /// rebuilds the programmatic HUD so HudLayout/UiSkin inspector edits show mid-fight
    /// without restarting — see <see cref="HudRoot.Rebuild"/>.
    /// </summary>
    public static class HudTools
    {
        [MenuItem("Tools/RANK E/Rebuild HUD %#h")]
        public static void RebuildHud()
        {
            var hud = Object.FindFirstObjectByType<HudRoot>();
            hud.Rebuild();
            Debug.Log("[HudTools] HUD rebuilt.");
        }

        [MenuItem("Tools/RANK E/Rebuild HUD %#h", true)]
        public static bool RebuildHudValidate()
            => Application.isPlaying && Object.FindFirstObjectByType<HudRoot>() != null;
    }
}
