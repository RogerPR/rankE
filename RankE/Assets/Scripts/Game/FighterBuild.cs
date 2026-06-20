using System;
using System.Collections.Generic;
using RankE.Sim;

namespace RankE.Game
{
    /// <summary>
    /// The editable, persistent <i>spec</i> for one fighter — the per-character "build":
    /// base stats, which abilities it carries (ids into the shared ability library), and gear.
    /// Distinct from the per-fight <see cref="FighterConfig"/> it resolves to via
    /// <see cref="ToConfig"/>: a build outlives fights (it lives on <see cref="TuningProfile"/>),
    /// while a config is rebuilt fresh each fight so the running sim stays a clean clone.
    ///
    /// Ability <i>numbers</i> (cooldown/cast/damage) are shared and live in the library
    /// (<see cref="TuningProfile.Abilities"/>); a build only <i>selects</i> ids. Gear/stats are
    /// what make two fighters differ. In Phase 4 builds become roguelite reward/item driven;
    /// for now they are seeded from PoC defaults and edited in the in-game tuning panel.
    /// Pure data — no gameplay logic, no UnityEngine.
    /// </summary>
    public sealed class FighterBuild
    {
        public string Name = "Fighter";
        public int MaxHp = 500;
        public int SpellGems = 8;

        /// <summary>Editable base stat sheet; gear deltas layer on at battle start (Fighter ctor).</summary>
        public StatSheet Stats = new StatSheet();

        public string AutoAttackId = DefaultContent.AutoAttackId;

        /// <summary>How many of <see cref="AbilityIds"/> are swappable main slots; the tail
        /// (Parry/Kick) are fixed quick slots.</summary>
        public int MainSlotCount = 4;

        /// <summary>Full loadout as library ids (main slots first, then fixed quick slots).</summary>
        public List<string> AbilityIds = new List<string>();

        /// <summary>Equipped gear (stance/weapon/armor today; open category later).</summary>
        public List<GearDef> Gear = new List<GearDef>();

        /// <summary>Resolve to a fresh sim config for one fight: ability ids → library clones,
        /// stats/HP/gems copied, gear shared (read-only during battle).</summary>
        public FighterConfig ToConfig(TuningProfile profile)
        {
            var abilities = new List<AbilityDef>();
            if (AbilityIds != null)
            {
                foreach (var id in AbilityIds)
                {
                    var def = profile.CloneAbility(id);
                    if (def != null) abilities.Add(def);
                }
            }
            return new FighterConfig
            {
                Name = Name,
                MaxHp = MaxHp,
                SpellGems = SpellGems,
                Stats = Stats != null ? Stats.Clone() : new StatSheet(),
                AutoAttack = profile.CloneAbility(AutoAttackId),
                Abilities = abilities,
                Build = new BuildState
                {
                    Gear = Gear != null ? new List<GearDef>(Gear) : new List<GearDef>(),
                },
            };
        }

        /// <summary>Display name of the ability in a slot (from the library), or "(none)" when
        /// the slot is empty/unknown.</summary>
        public string AbilityName(TuningProfile profile, int slot)
        {
            if (AbilityIds == null || slot < 0 || slot >= AbilityIds.Count) return "(none)";
            var id = AbilityIds[slot];
            if (string.IsNullOrWhiteSpace(id)) return "(none)";
            if (profile != null && profile.Abilities.TryGetValue(id, out var def) && def != null)
                return def.Name ?? id;
            return id;
        }

        static List<string> DefaultLoadoutIds() => new List<string>
        {
            DefaultContent.SlashId, DefaultContent.BashId, DefaultContent.FireballId,
            DefaultContent.VampiroId, DefaultContent.ParryId, DefaultContent.KickId,
        };

        /// <summary>The Phase-2 default player build (Rock / Sword / Light, neutral stats).</summary>
        public static FighterBuild DefaultPlayer() => new FighterBuild
        {
            Name = "Player",
            AbilityIds = DefaultLoadoutIds(),
            Gear = new List<GearDef>
            {
                DefaultContent.StanceRock(), DefaultContent.WeaponSword(), DefaultContent.ArmorLight(),
            },
        };

        /// <summary>The default sparring opponent (no gear, neutral stats) — the editable
        /// twin of the old hardcoded <c>DefaultContent.DefaultConfig</c>.</summary>
        public static FighterBuild DefaultAdversary() => new FighterBuild
        {
            Name = "Adversary",
            // Slow, readable sparring opponent: one main attack (Slash) on a telegraphed
            // rhythm plus the two reactive quick slots. One main + two quick.
            MainSlotCount = 1,
            AbilityIds = new List<string>
            {
                DefaultContent.SlashId, DefaultContent.ParryId, DefaultContent.KickId,
            },
            Gear = new List<GearDef>(),
        };
    }

    /// <summary>
    /// Shared selection pools + cycling helpers for builds, used by both the loadout picker
    /// and the in-game tuning panel so there is one source of truth for "what can be picked".
    /// </summary>
    public static class LoadoutPools
    {
        /// <summary>The empty-slot sentinel: an ability id of "" means the slot carries nothing.</summary>
        public const string NoneId = "";

        /// <summary>Abilities pickable in a main slot.</summary>
        public static readonly string[] MainAbilities =
        {
            DefaultContent.SlashId, DefaultContent.BashId, DefaultContent.FireballId,
            DefaultContent.VampiroId, DefaultContent.FallingStarId, DefaultContent.LungeId,
        };

        /// <summary>Abilities pickable in a quick slot (GcdClass.Quick).</summary>
        public static readonly string[] QuickAbilities =
        {
            DefaultContent.ParryId, DefaultContent.KickId,
        };

        public static readonly Func<GearDef>[] Stances =
            { DefaultContent.StanceRock, DefaultContent.StanceWind, DefaultContent.StanceWater };
        public static readonly Func<GearDef>[] Weapons =
            { DefaultContent.WeaponSword, DefaultContent.WeaponDagger, DefaultContent.WeaponWand, DefaultContent.WeaponGreataxe };
        public static readonly Func<GearDef>[] Armors =
            { DefaultContent.ArmorLight, DefaultContent.ArmorMedium, DefaultContent.ArmorHeavy };

        /// <summary>Every gear factory keyed by id, so a saved build can be rebuilt from ids.</summary>
        static readonly Func<GearDef>[] AllGear =
        {
            DefaultContent.StanceRock, DefaultContent.StanceWind, DefaultContent.StanceWater,
            DefaultContent.WeaponSword, DefaultContent.WeaponDagger, DefaultContent.WeaponWand, DefaultContent.WeaponGreataxe,
            DefaultContent.ArmorLight, DefaultContent.ArmorMedium, DefaultContent.ArmorHeavy,
        };

        /// <summary>A fresh gear def for an id (from <see cref="AllGear"/>), or null if unknown.</summary>
        public static GearDef GearById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var make in AllGear)
            {
                var g = make();
                if (g.Id == id) return g;
            }
            return null;
        }

        public static int Wrap(int v, int n) => n <= 0 ? 0 : ((v % n) + n) % n;

        /// <summary>Cycle a main ability slot, skipping ids already used in another main slot.</summary>
        public static void CycleAbility(List<string> ids, int slot, int dir, int mainCount)
        {
            if (ids == null || slot < 0 || slot >= ids.Count) return;
            int idx = Array.IndexOf(MainAbilities, ids[slot]);
            if (idx < 0) idx = 0;
            for (int hop = 0; hop < MainAbilities.Length; hop++)
            {
                idx = Wrap(idx + dir, MainAbilities.Length);
                string cand = MainAbilities[idx];
                if (!UsedInOtherMainSlot(ids, cand, slot, mainCount)) { ids[slot] = cand; return; }
            }
        }

        static bool UsedInOtherMainSlot(List<string> ids, string id, int exceptSlot, int mainCount)
        {
            int n = Math.Min(mainCount, ids.Count);
            for (int s = 0; s < n; s++)
                if (s != exceptSlot && ids[s] == id) return true;
            return false;
        }

        /// <summary>
        /// Cycle one slot through <paramref name="pool"/> plus a "(none)" option, skipping any
        /// id already used in a sibling slot of the same kind (the range
        /// [<paramref name="kindStart"/>, <paramref name="kindEnd"/>)). Empty slots are allowed,
        /// so a fighter can carry fewer than the full kit.
        /// </summary>
        public static void CycleSlot(List<string> ids, int slot, int dir, string[] pool,
            int kindStart, int kindEnd)
        {
            if (ids == null || slot < 0 || slot >= ids.Count || pool == null) return;

            var options = new List<string> { NoneId };
            options.AddRange(pool);
            int n = options.Count;

            int idx = options.IndexOf(ids[slot] ?? NoneId);
            if (idx < 0) idx = 0;
            for (int hop = 0; hop < n; hop++)
            {
                idx = Wrap(idx + dir, n);
                string cand = options[idx];
                if (cand == NoneId || !UsedInSiblingSlot(ids, cand, slot, kindStart, kindEnd))
                {
                    ids[slot] = cand;
                    return;
                }
            }
        }

        static bool UsedInSiblingSlot(List<string> ids, string id, int exceptSlot, int start, int end)
        {
            end = Math.Min(end, ids.Count);
            for (int s = Math.Max(0, start); s < end; s++)
                if (s != exceptSlot && ids[s] == id) return true;
            return false;
        }

        /// <summary>Replace the gear entry matching this pool's slot with the next option.</summary>
        public static void CycleGear(List<GearDef> gear, Func<GearDef>[] pool, int dir)
        {
            if (gear == null || pool == null || pool.Length == 0) return;
            string slot = pool[0]().Slot;
            int at = gear.FindIndex(g => g != null && g.Slot == slot);
            int poolIdx = at >= 0 ? IndexInPool(pool, gear[at].Id) : 0;
            poolIdx = Wrap(poolIdx + dir, pool.Length);
            var next = pool[poolIdx]();
            if (at >= 0) gear[at] = next; else gear.Add(next);
        }

        /// <summary>Name of the gear currently equipped in this pool's slot, or "(none)".</summary>
        public static string GearName(List<GearDef> gear, Func<GearDef>[] pool)
        {
            if (gear == null || pool == null || pool.Length == 0) return "(none)";
            string slot = pool[0]().Slot;
            var g = gear.Find(x => x != null && x.Slot == slot);
            return g != null ? g.Name : "(none)";
        }

        static int IndexInPool(Func<GearDef>[] pool, string id)
        {
            for (int i = 0; i < pool.Length; i++)
                if (pool[i]().Id == id) return i;
            return 0;
        }
    }
}
