using System;
using System.Collections.Generic;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Catalogue of modular character pieces (base bodies + accessories grouped by
    /// attach point), populated by the editor <c>ArtSetupBuilder</c> from the Meshtint
    /// modular pack. Stored under Resources so the runtime can load it (and pull the
    /// referenced prefabs into the build) without a scene reference.
    ///
    /// The category list is <b>discovered</b> from the pack's folder layout, not
    /// enumerated in code — a new accessory subfolder becomes a new slot on rebuild.
    /// Pure presentation data; the sim never sees it.
    /// </summary>
    public sealed class CharacterPartCatalogue : ScriptableObject
    {
        public const string ResourcePath = "RankE/CharacterPartCatalogue";

        /// <summary>Selectable base bodies (Male + Female costume/colour variants).</summary>
        public List<PartEntry> Bases = new List<PartEntry>();

        /// <summary>One slot per discovered (attach-group, subcategory).</summary>
        public List<PartCategory> Categories = new List<PartCategory>();

        /// <summary>
        /// Canonical humanoid visual (shared controller + the action/ability anim maps
        /// every player share). The assembler clones this for a custom-built character
        /// so no per-part anim wiring is needed. Its <c>Prefab</c> is null.
        /// </summary>
        public FighterVisualDef HumanoidTemplate = new FighterVisualDef();

        static CharacterPartCatalogue cached;

        /// <summary>Load the singleton catalogue from Resources (null if not built yet).</summary>
        public static CharacterPartCatalogue Load()
        {
            if (cached == null)
                cached = Resources.Load<CharacterPartCatalogue>(ResourcePath);
            return cached;
        }

        public PartCategory CategoryById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Categories.Count; i++)
                if (Categories[i].Id == id) return Categories[i];
            return null;
        }

        public PartEntry BaseById(string id)
        {
            for (int i = 0; i < Bases.Count; i++)
                if (Bases[i].Id == id) return Bases[i];
            return Bases.Count > 0 ? Bases[0] : null;
        }
    }

    /// <summary>One selectable piece: a prefab plus display metadata.</summary>
    [Serializable]
    public sealed class PartEntry
    {
        public string Id;
        public string DisplayName;
        public GameObject Prefab;
    }

    /// <summary>
    /// One customization slot, e.g. (attach-group "+Head", subcategory "Helmet").
    /// <see cref="AttachBones"/> is the resolved set of bone names the part mounts to
    /// (two for mirrored parts like bracers); <see cref="Optional"/> slots allow a
    /// "(none)" choice. <see cref="Parts"/> includes every colour variant as a
    /// separate entry (the pack ships one prefab per colour).
    /// </summary>
    [Serializable]
    public sealed class PartCategory
    {
        public string Id;
        public string DisplayName;
        public string AttachGroup;
        public string[] AttachBones = Array.Empty<string>();
        public bool Optional = true;
        public List<PartEntry> Parts = new List<PartEntry>();

        public PartEntry PartById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Parts.Count; i++)
                if (Parts[i].Id == id) return Parts[i];
            return null;
        }
    }
}
