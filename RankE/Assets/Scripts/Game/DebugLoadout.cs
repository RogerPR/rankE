using System;
using System.Collections.Generic;
using RankE.Sim;

namespace RankE.Game
{
    /// <summary>
    /// Debug-only loadout selection over the PoC content pools (Phase 2 checkbox).
    /// Real acquisition arrives via run rewards in Phase 4; this just lets a tester
    /// assemble a FighterConfig. Plain C# so screens stay dumb.
    /// </summary>
    public sealed class DebugLoadout
    {
        static readonly Func<GearDef>[] Stances =
            { PocContent.StanceRock, PocContent.StanceWind, PocContent.StanceWater };
        static readonly Func<GearDef>[] Weapons =
            { PocContent.WeaponSword, PocContent.WeaponDagger, PocContent.WeaponWand };
        static readonly Func<GearDef>[] Armors =
            { PocContent.ArmorLight, PocContent.ArmorMedium, PocContent.ArmorHeavy };

        /// <summary>Pool for the 4 main bar slots; quick slots are fixed Parry+Kick.</summary>
        static readonly Func<AbilityDef>[] AbilityPool =
        {
            PocContent.Slash, PocContent.Bash, PocContent.Fireball,
            PocContent.Vampiro, PocContent.FallingStar, PocContent.Lunge,
        };

        public int StanceIndex;
        public int WeaponIndex;
        public int ArmorIndex;
        public readonly int[] AbilityIndices = { 0, 1, 2, 3 };

        // Presentation-only model selection (player character + enemy monster). These
        // pick which prefab FighterStage spawns; they don't touch the sim FighterConfig,
        // except the enemy's display name is taken from the chosen monster.
        // Lazy: Resources.Load can't run from a field initializer/constructor.
        FighterVisualRegistry visualsCache;
        FighterVisualRegistry Visuals =>
            visualsCache != null ? visualsCache : (visualsCache = FighterVisualRegistry.Load());

        public int PlayerVisualIndex;
        public int EnemyVisualIndex;

        public int PlayerVisualCount => Visuals != null ? Visuals.Players.Count : 0;
        public int EnemyVisualCount => Visuals != null ? Visuals.Monsters.Count : 0;

        public string PlayerVisualName =>
            PlayerVisualCount > 0 ? Visuals.PlayerAt(PlayerVisualIndex).DisplayName : "(no art)";
        public string EnemyVisualName =>
            EnemyVisualCount > 0 ? Visuals.MonsterAt(EnemyVisualIndex).DisplayName : "Bandit";

        public string StanceName => Stances[StanceIndex]().Name;
        public string WeaponName => Weapons[WeaponIndex]().Name;
        public string ArmorName => Armors[ArmorIndex]().Name;
        public string AbilityName(int slot) => AbilityPool[AbilityIndices[slot]]().Name;

        public void CyclePlayerVisual(int dir)
        {
            if (PlayerVisualCount > 0) PlayerVisualIndex = Wrap(PlayerVisualIndex + dir, PlayerVisualCount);
        }

        public void CycleEnemyVisual(int dir)
        {
            if (EnemyVisualCount > 0) EnemyVisualIndex = Wrap(EnemyVisualIndex + dir, EnemyVisualCount);
        }

        public void CycleStance(int dir) => StanceIndex = Wrap(StanceIndex + dir, Stances.Length);
        public void CycleWeapon(int dir) => WeaponIndex = Wrap(WeaponIndex + dir, Weapons.Length);
        public void CycleArmor(int dir) => ArmorIndex = Wrap(ArmorIndex + dir, Armors.Length);

        /// <summary>Cycle one bar slot, skipping abilities already in other slots.</summary>
        public void CycleAbility(int slot, int dir)
        {
            int idx = AbilityIndices[slot];
            for (int hop = 0; hop < AbilityPool.Length; hop++)
            {
                idx = Wrap(idx + dir, AbilityPool.Length);
                if (!Taken(idx, slot))
                {
                    AbilityIndices[slot] = idx;
                    return;
                }
            }
        }

        bool Taken(int poolIndex, int exceptSlot)
        {
            for (int s = 0; s < AbilityIndices.Length; s++)
                if (s != exceptSlot && AbilityIndices[s] == poolIndex) return true;
            return false;
        }

        public FighterConfig BuildPlayerConfig(string name = "Player")
        {
            var abilities = new List<AbilityDef>();
            foreach (var i in AbilityIndices)
                abilities.Add(AbilityPool[i]());
            abilities.Add(PocContent.Parry());
            abilities.Add(PocContent.Kick());

            return new FighterConfig
            {
                Name = name,
                MaxHp = 500,
                SpellGems = 8,
                AutoAttack = PocContent.AutoAttack(),
                Abilities = abilities,
                Build = new BuildState
                {
                    Gear = new List<GearDef>
                    {
                        Stances[StanceIndex](),
                        Weapons[WeaponIndex](),
                        Armors[ArmorIndex](),
                    },
                },
            };
        }

        static int Wrap(int v, int n) => ((v % n) + n) % n;
    }
}
