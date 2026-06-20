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
    /// Seeded from <see cref="PocContent"/>; in-memory only (a domain reload / play-exit
    /// resets it — the window's "Copy as JSON" dumps good values to paste back into
    /// PocContent). Presentation knobs live on their ScriptableObject assets, not here.
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
            var profile = new TuningProfile { Tuning = PocContent.CreateTuning() };
            foreach (var kv in PocContent.CreateContent().Abilities)
                profile.Abilities[kv.Key] = kv.Value.Clone();
            profile.Player = FighterBuild.DefaultPlayer();
            profile.Adversary = FighterBuild.DefaultAdversary();
            ApplyPaceDefaults(profile);
            return profile;
        }

        /// <summary>Re-seed this profile's numbers back to the PoC defaults.</summary>
        public void ResetToDefaults()
        {
            Tuning = PocContent.CreateTuning();
            Abilities.Clear();
            foreach (var kv in PocContent.CreateContent().Abilities)
                Abilities[kv.Key] = kv.Value.Clone();
            Player = FighterBuild.DefaultPlayer();
            Adversary = FighterBuild.DefaultAdversary();
            ApplyPaceDefaults(this);
        }

        /// <summary>
        /// The game's shipped tuning sits a notch slower than the raw PoC reference numbers in
        /// <see cref="PocContent"/> (which stays PoC-faithful for the sim tests). This is the one
        /// place the slower-pace defaults live; the control panel edits them further. Keeps the
        /// enemy's ~3s Slash rhythm honest (Slash is off cooldown by the next beat).
        /// </summary>
        static void ApplyPaceDefaults(TuningProfile profile)
        {
            profile.Tuning.GcdTicks = 30; // 1.5s normal GCD (PoC 1.0s) — calmer player cadence
            SetCooldown(profile, PocContent.SlashId, 60);      // 3s — aligns with the enemy beat
            SetCooldown(profile, PocContent.AutoAttackId, 60); // 3s — slower chip damage
        }

        static void SetCooldown(TuningProfile profile, string id, int ticks)
        {
            if (profile.Abilities.TryGetValue(id, out var def) && def != null)
                def.CooldownTicks = ticks;
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
