using System.Collections.Generic;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Reaction cues the view spawns off discrete sim events (hit, heal, parry, …).
    /// A presentation vocabulary (not game content), so a closed enum is fine — same
    /// rationale as <see cref="AnimAction"/>.
    /// </summary>
    public enum VfxCue
    {
        Hit,
        Heal,
        Parry,
        Break,
        Riposte,
        Death,
    }

    /// <summary>How an ability's projectile decoration travels to its target.</summary>
    public enum ProjectileMode
    {
        None,   // no projectile (melee/instant) — the impact is the target's Hit cue
        Travel, // hand → target, roughly horizontal (Fireball)
        Fall,   // descends onto the target from above (Falling Star)
    }

    /// <summary>
    /// Per-ability VFX binding (ability id → the prefabs that show it). Every prefab is
    /// optional; a null slot is skipped gracefully, mirroring <c>FighterVisualDef.StateFor</c>.
    /// </summary>
    [System.Serializable]
    public sealed class AbilityVfxDef
    {
        public string AbilityId;

        /// <summary>Looping aura on the caster while the cast bar runs (CastStarted→Completed).</summary>
        public GameObject CastAura;

        /// <summary>One-shot flash at the hand/muzzle on release (AbilityUsed).</summary>
        public GameObject Muzzle;

        /// <summary>Travelling decoration (see <see cref="Mode"/>); null = no projectile.</summary>
        public GameObject Projectile;

        /// <summary>Burst at the point of impact (projectile arrival).</summary>
        public GameObject Impact;

        public ProjectileMode Mode = ProjectileMode.None;

        /// <summary>Travel/fall time in seconds; 0 = derive from the ability's cast/delay ticks.</summary>
        public float TravelSeconds;
    }

    /// <summary>A reaction cue → its prefab.</summary>
    [System.Serializable]
    public struct CueVfx
    {
        public VfxCue Cue;
        public GameObject Prefab;
    }

    /// <summary>
    /// The catalogue of skill VFX: per-ability projectile/muzzle/cast prefabs plus the
    /// reaction-cue prefabs, populated by the editor <c>ArtSetupBuilder</c> from the
    /// imported VFX pack. Stored under Resources so the runtime can load it (and pull its
    /// referenced prefabs into the build) without a scene reference, exactly like
    /// <see cref="FighterVisualRegistry"/>. The global feel knobs below are serialized so
    /// the user tunes juice in the Inspector with no recompile (view components that read
    /// them are <c>AddComponent</c>'d at runtime). Pure presentation data.
    /// </summary>
    public sealed class AbilityVfxRegistry : ScriptableObject
    {
        public const string ResourcePath = "RankE/AbilityVfxRegistry";

        public List<AbilityVfxDef> Abilities = new List<AbilityVfxDef>();
        public List<CueVfx> Cues = new List<CueVfx>();

        [Header("Global feel knobs (tune in the Inspector — no recompile)")]
        [Tooltip("Uniform scale applied to every spawned effect.")]
        public float VfxScale = 1f;

        [Tooltip("Projectile travel time when an ability sets TravelSeconds = 0 and no tick "
            + "timing applies.")]
        public float DefaultTravelSeconds = 0.28f;

        [Tooltip("Height above the target a Fall projectile (Falling Star) starts from.")]
        public float FallHeight = 6f;

        [Tooltip("Chest height (metres above the anchor) used as the spawn/aim point when no "
            + "hand bone is found, and as the projectile target height.")]
        public float ChestHeight = 1.1f;

        [Tooltip("Safety lifetime (seconds) for any spawned effect lacking its own auto-destroy.")]
        public float CueLifetime = 2.5f;

        static AbilityVfxRegistry cached;

        /// <summary>Load the singleton registry from Resources (null if not built yet).</summary>
        public static AbilityVfxRegistry Load()
        {
            if (cached == null)
                cached = Resources.Load<AbilityVfxRegistry>(ResourcePath);
            return cached;
        }

        /// <summary>Per-ability binding, or null if the ability has none.</summary>
        public AbilityVfxDef DefFor(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId)) return null;
            for (int i = 0; i < Abilities.Count; i++)
                if (Abilities[i] != null && Abilities[i].AbilityId == abilityId)
                    return Abilities[i];
            return null;
        }

        /// <summary>Prefab for a reaction cue, or null if unmapped.</summary>
        public GameObject PrefabFor(VfxCue cue)
        {
            for (int i = 0; i < Cues.Count; i++)
                if (Cues[i].Cue == cue && Cues[i].Prefab != null)
                    return Cues[i].Prefab;
            return null;
        }
    }
}
