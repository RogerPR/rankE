using System.Collections.Generic;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// The catalogue of selectable fighter visuals (player characters + monsters),
    /// populated by the editor <c>ArtSetupBuilder</c> from the imported art packs.
    /// Stored under Resources so the runtime can load it (and pull its referenced
    /// prefabs/controllers into the build) without a scene reference. Pure
    /// presentation data — open lists so the roster grows.
    /// </summary>
    public sealed class FighterVisualRegistry : ScriptableObject
    {
        public const string ResourcePath = "RankE/FighterVisualRegistry";

        public List<FighterVisualDef> Players = new List<FighterVisualDef>();
        public List<FighterVisualDef> Monsters = new List<FighterVisualDef>();

        static FighterVisualRegistry cached;

        /// <summary>Load the singleton registry from Resources (null if not built yet).</summary>
        public static FighterVisualRegistry Load()
        {
            if (cached == null)
                cached = Resources.Load<FighterVisualRegistry>(ResourcePath);
            return cached;
        }

        public FighterVisualDef PlayerAt(int index) => At(Players, index);
        public FighterVisualDef MonsterAt(int index) => At(Monsters, index);

        static FighterVisualDef At(List<FighterVisualDef> list, int index)
        {
            if (list == null || list.Count == 0) return null;
            int i = ((index % list.Count) + list.Count) % list.Count;
            return list[i];
        }
    }
}
