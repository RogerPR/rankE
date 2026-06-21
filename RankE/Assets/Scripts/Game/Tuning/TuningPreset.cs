using System;
using System.Collections.Generic;
using System.Reflection;
using RankE.Sim;

namespace RankE.Game
{
    /// <summary>
    /// A serializable snapshot of everything that defines a tunable fight: the global combat
    /// rules, every ability's tunable numbers, both fighter builds, and the chosen visuals
    /// (including the enemy). Used to save named fixtures — early/mid/late-game fights — and
    /// reload them across sessions (see <see cref="TuningPresetStore"/>).
    ///
    /// <para>Only numbers are serialized: <see cref="Apply"/> starts from
    /// <see cref="TuningProfile.FromDefaults"/> and overlays the saved values, so we never
    /// serialize the full ability/effect/status graph. Gear is stored by id and rebuilt from
    /// <see cref="LoadoutPools.GearById"/>; visuals are stored by display name and re-resolved,
    /// so presets degrade gracefully if the rosters change. JsonUtility-friendly (no
    /// dictionaries — every map is a list of named entries).</para>
    /// </summary>
    [Serializable]
    public sealed class TuningPreset
    {
        public string name;
        public List<NumberEntry> tuning = new List<NumberEntry>();
        public List<AbilityOverride> abilities = new List<AbilityOverride>();
        public FighterBuildDto player;
        public FighterBuildDto adversary;
        public string playerVisualName;
        public string enemyVisualName;

        /// <summary>If set, the adversary is resolved from <c>Opponents/&lt;opponentId&gt;.json</c>
        /// (build + AI logic + visual) and overrides the inline <see cref="adversary"/>. Empty =
        /// use the inline adversary build with the default sparring rhythm.</summary>
        public string opponentId;

        // ---- capture ----

        public static TuningPreset Capture(TuningProfile profile, DebugLoadout loadout)
        {
            var p = new TuningPreset
            {
                tuning = CaptureNumbers(profile.Tuning),
                player = CaptureBuild(profile.Player),
                adversary = CaptureBuild(profile.Adversary),
                playerVisualName = loadout != null ? loadout.PlayerVisualName : null,
                enemyVisualName = loadout != null ? loadout.EnemyVisualName : null,
                opponentId = profile.Opponent != null ? profile.Opponent.id : null,
            };

            var ids = new List<string>(profile.Abilities.Keys);
            ids.Sort();
            foreach (var id in ids)
            {
                var def = profile.Abilities[id];
                if (def == null) continue;
                var ov = new AbilityOverride { id = id, numbers = CaptureNumbers(def) };
                for (int i = 0; i < def.Effects.Count; i++)
                {
                    var e = def.Effects[i];
                    ov.effects.Add(new EffectAmount { index = i, amount = e.Amount, durationTicks = e.DurationTicks });
                }
                p.abilities.Add(ov);
            }
            return p;
        }

        static FighterBuildDto CaptureBuild(FighterBuild b)
        {
            var dto = new FighterBuildDto
            {
                name = b.Name,
                maxHp = b.MaxHp,
                spellGems = b.SpellGems,
                stats = CaptureNumbers(b.Stats),
                passiveIds = b.PassiveIds != null ? new List<string>(b.PassiveIds) : new List<string>(),
                mainSlotCount = b.MainSlotCount,
                abilityIds = b.AbilityIds != null ? new List<string>(b.AbilityIds) : new List<string>(),
                gearIds = new List<string>(),
            };
            if (b.Gear != null)
                foreach (var g in b.Gear)
                    if (g != null) dto.gearIds.Add(g.Id);
            return dto;
        }

        // ---- apply (overlay onto fresh defaults) ----

        public void Apply(TuningProfile profile, DebugLoadout loadout)
        {
            // Reset to defaults first, then overlay. Mutates the profile in place so any UI
            // closures holding the profile reference stay valid after a load.
            profile.ResetToDefaults();

            ApplyNumbers(profile.Tuning, tuning);

            if (abilities != null)
                foreach (var ov in abilities)
                {
                    if (ov == null || !profile.Abilities.TryGetValue(ov.id, out var def) || def == null) continue;
                    ApplyNumbers(def, ov.numbers);
                    if (ov.effects != null)
                        foreach (var e in ov.effects)
                            if (e.index >= 0 && e.index < def.Effects.Count)
                            {
                                def.Effects[e.index].Amount = e.amount;
                                def.Effects[e.index].DurationTicks = e.durationTicks;
                            }
                }

            ApplyBuild(profile.Player, player);
            ApplyBuild(profile.Adversary, adversary);

            // An opponent reference owns the adversary (build + AI logic + visual): load it and
            // override the inline build. Falls back gracefully if the file is missing.
            profile.Opponent = string.IsNullOrEmpty(opponentId) ? null : OpponentStore.Load(opponentId);
            if (profile.Opponent != null)
            {
                ApplyBuild(profile.Adversary, profile.Opponent.build);
                if (!string.IsNullOrEmpty(profile.Opponent.displayName))
                    profile.Adversary.Name = profile.Opponent.displayName;
            }

            if (loadout != null)
            {
                loadout.SetPlayerVisualByName(playerVisualName);
                var enemyVisual = profile.Opponent != null && !string.IsNullOrEmpty(profile.Opponent.visualName)
                    ? profile.Opponent.visualName : enemyVisualName;
                loadout.SetEnemyVisualByName(enemyVisual);
            }
        }

        /// <summary>Overlay a build DTO onto an editable build (shared with opponent loading).</summary>
        internal static void ApplyBuild(FighterBuild b, FighterBuildDto dto)
        {
            if (b == null || dto == null) return;
            b.Name = dto.name;
            b.MaxHp = dto.maxHp;
            b.SpellGems = dto.spellGems;
            ApplyNumbers(b.Stats, dto.stats);
            b.PassiveIds = dto.passiveIds != null ? new List<string>(dto.passiveIds) : new List<string>();
            b.MainSlotCount = dto.mainSlotCount;
            b.AbilityIds = dto.abilityIds != null ? new List<string>(dto.abilityIds) : new List<string>();
            b.Gear = new List<GearDef>();
            if (dto.gearIds != null)
                foreach (var id in dto.gearIds)
                {
                    var g = LoadoutPools.GearById(id);
                    if (g != null) b.Gear.Add(g);
                }
        }

        // ---- reflection over public int/double fields (shared with the panel's row builders) ----

        static List<NumberEntry> CaptureNumbers(object obj)
        {
            var list = new List<NumberEntry>();
            if (obj == null) return list;
            foreach (var f in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(int))
                    list.Add(new NumberEntry { name = f.Name, value = (int)f.GetValue(obj) });
                else if (f.FieldType == typeof(double))
                    list.Add(new NumberEntry { name = f.Name, value = (double)f.GetValue(obj) });
            }
            return list;
        }

        static void ApplyNumbers(object obj, List<NumberEntry> values)
        {
            if (obj == null || values == null) return;
            var type = obj.GetType();
            foreach (var entry in values)
            {
                var f = type.GetField(entry.name, BindingFlags.Public | BindingFlags.Instance);
                if (f == null) continue;
                if (f.FieldType == typeof(int)) f.SetValue(obj, (int)Math.Round(entry.value));
                else if (f.FieldType == typeof(double)) f.SetValue(obj, entry.value);
            }
        }
    }

    [Serializable]
    public sealed class NumberEntry
    {
        public string name;
        public double value;
    }

    [Serializable]
    public sealed class AbilityOverride
    {
        public string id;
        public List<NumberEntry> numbers = new List<NumberEntry>();
        public List<EffectAmount> effects = new List<EffectAmount>();
    }

    [Serializable]
    public sealed class EffectAmount
    {
        public int index;
        public int amount;
        public int durationTicks;
    }

    [Serializable]
    public sealed class FighterBuildDto
    {
        public string name;
        public int maxHp;
        public int spellGems;
        public List<NumberEntry> stats = new List<NumberEntry>();
        public List<string> passiveIds = new List<string>();
        public int mainSlotCount;
        public List<string> abilityIds = new List<string>();
        public List<string> gearIds = new List<string>();
    }
}
