using System.Collections.Generic;
using RankE.Sim;

namespace RankE.Game
{
    /// <summary>
    /// The live, editable source of combat numbers the Combat Tuning editor window pokes
    /// while you playtest. Sim data (<see cref="CombatTuning"/>, each ability's
    /// <see cref="AbilityDef"/>) is created fresh per fight, so we keep one held copy here
    /// and have every new fight CLONE from it (see <c>BattleDriver.Begin</c> +
    /// <c>MatchController.StartMatch</c>). That makes edits apply on the NEXT fight only:
    /// editing this profile mid-fight never touches the running, deterministic battle —
    /// it works off its own clone. Press Rematch to apply.
    ///
    /// Seeded from <see cref="DefaultContent"/>, then overlaid with the startup preset
    /// (<c>TuningPresets/Default.json</c>, see <see cref="TuningPresetStore.StartupName"/>) if one
    /// is saved — so tuned numbers survive domain reloads and play-exits without touching C#.
    /// Presentation knobs live on their ScriptableObject assets, not here.
    ///
    /// Tests must stay hermetic: never read <see cref="Active"/> from a test — use
    /// <see cref="FromDefaults"/> (or TestKit fixtures) so results don't depend on whatever
    /// preset happens to be on disk.
    /// </summary>
    public sealed class TuningProfile
    {
        public CombatTuning Tuning;

        /// <summary>Ability id → editable definition (cooldowns, cast/lock ticks, effect amounts).
        /// The shared library both builds select from.</summary>
        public readonly Dictionary<string, AbilityDef> Abilities = new Dictionary<string, AbilityDef>();

        /// <summary>The two per-character builds (stats + ability selection + gear). Distinct from
        /// the shared globals/library above; each fight resolves these into FighterConfigs.</summary>
        public FighterBuild Player;
        public FighterBuild Adversary;

        /// <summary>The loaded opponent (build + AI logic + visual) when a scenario references one
        /// by id; null = use <see cref="Adversary"/> with the default sparring rhythm. Set by
        /// <see cref="TuningPreset.Apply"/>; read by <c>MatchController.BeginFight</c>.</summary>
        public OpponentDef Opponent;

        static TuningProfile active;

        /// <summary>The shared profile the window edits and fights clone from. Lazily seeded
        /// from defaults + the startup preset; statics reset each play entry, so the preset
        /// re-applies every session.</summary>
        public static TuningProfile Active => active ?? (active = SeedFromStartup());

        static TuningProfile SeedFromStartup()
        {
            var profile = FromDefaults();
            try
            {
                var preset = TuningPresetStore.LoadStartup();
                if (preset == null) return profile;
                preset.Apply(profile, null); // numbers/builds/opponent; visuals need a loadout — CombatBootstrap applies them
                UnityEngine.Debug.Log($"[Tuning] Seeded from startup preset \"{TuningPresetStore.StartupName}\" ({TuningPresetStore.Dir}). Code-default edits are masked by it.");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Tuning] Startup preset \"{TuningPresetStore.StartupName}\" failed to load — using code defaults. {e.Message}");
                profile = FromDefaults(); // Apply may have half-overlaid; re-seed clean
            }
            return profile;
        }

        public static TuningProfile FromDefaults()
        {
            var profile = new TuningProfile { Tuning = DefaultContent.CreateTuning() };
            foreach (var kv in DefaultContent.CreateContent().Abilities)
                profile.Abilities[kv.Key] = kv.Value.Clone();
            profile.Player = FighterBuild.DefaultPlayer();
            profile.Adversary = FighterBuild.DefaultAdversary();
            return profile;
        }

        /// <summary>Re-seed this profile's numbers back to the content defaults.</summary>
        public void ResetToDefaults()
        {
            Tuning = DefaultContent.CreateTuning();
            Abilities.Clear();
            foreach (var kv in DefaultContent.CreateContent().Abilities)
                Abilities[kv.Key] = kv.Value.Clone();
            Player = FighterBuild.DefaultPlayer();
            Adversary = FighterBuild.DefaultAdversary();
            Opponent = null;
        }

        /// <summary>
        /// Select a data-driven opponent (or null for the inline adversary): sets
        /// <see cref="Opponent"/> and overlays its build onto <see cref="Adversary"/> in place,
        /// so panel closures holding the build stay valid. Switching back to null keeps the last
        /// overlaid build editable — handy as a starting point for inline tweaks.
        /// </summary>
        public void SetOpponent(OpponentDef opponent)
        {
            Opponent = opponent;
            if (opponent == null) return;
            TuningPreset.ApplyBuild(Adversary, opponent.build);
            if (!string.IsNullOrEmpty(opponent.displayName))
                Adversary.Name = opponent.displayName;
        }

        /// <summary>
        /// A fresh <see cref="ContentDb"/> with every ability entry replaced by this profile's
        /// tuned clone — THE content-resolution path for a fight, shared by
        /// <c>BattleDriver.Begin</c> (play) and <see cref="SweepScenario"/> (headless sweeps)
        /// so the two can never drift.
        /// </summary>
        public ContentDb CreateContentDb()
        {
            var content = DefaultContent.CreateContent();
            var ids = new List<string>(content.Abilities.Keys);
            foreach (var id in ids)
            {
                var tuned = CloneAbility(id);
                if (tuned != null) content.Abilities[id] = tuned;
            }
            return content;
        }

        /// <summary>A fresh clone of the tuned definition for an ability, or null if unknown.</summary>
        public AbilityDef CloneAbility(string id)
        {
            if (id != null && Abilities.TryGetValue(id, out var def) && def != null)
                return def.Clone();
            return null;
        }

        /// <summary>
        /// Replace each ability in <paramref name="loadout"/> (matched by id) with a clone of
        /// the tuned definition, so a built FighterConfig reflects the profile. Unknown ids
        /// (none today) are left as-is.
        /// </summary>
        public void ApplyTo(List<AbilityDef> loadout)
        {
            if (loadout == null) return;
            for (int i = 0; i < loadout.Count; i++)
            {
                var tuned = CloneAbility(loadout[i]?.Id);
                if (tuned != null) loadout[i] = tuned;
            }
        }
    }
}
