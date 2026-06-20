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
    /// Seeded from <see cref="DefaultContent"/>; in-memory only (a domain reload / play-exit
    /// resets it — the window's "Copy as JSON" dumps good values to paste back into
    /// DefaultContent). Presentation knobs live on their ScriptableObject assets, not here.
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

        static TuningProfile active;

        /// <summary>The shared profile the window edits and fights clone from (lazy defaults).</summary>
        public static TuningProfile Active => active ?? (active = FromDefaults());

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
