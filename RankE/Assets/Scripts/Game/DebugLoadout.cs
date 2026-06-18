namespace RankE.Game
{
    /// <summary>
    /// Debug-only loadout selection (Phase 2 checkbox). Owns the <b>presentation</b> choices
    /// for the picker — player/enemy visual prefab + the modular character appearance — and
    /// exposes the player <b>build</b> (stats/abilities/gear) by delegating to
    /// <see cref="TuningProfile.Active"/>.<see cref="TuningProfile.Player"/>, so the picker and
    /// the in-game tuning panel edit one shared player build. Real acquisition arrives via run
    /// rewards in Phase 4. Plain C# so screens stay dumb.
    /// </summary>
    public sealed class DebugLoadout
    {
        TuningProfile Profile => TuningProfile.Active;
        FighterBuild Build => Profile.Player;

        // Presentation-only model selection (player character + enemy monster). These
        // pick which prefab FighterStage spawns; they don't touch the sim build, except
        // the enemy's display name is taken from the chosen monster.
        // Lazy: Resources.Load can't run from a field initializer/constructor.
        FighterVisualRegistry visualsCache;
        FighterVisualRegistry Visuals =>
            visualsCache != null ? visualsCache : (visualsCache = FighterVisualRegistry.Load());

        public int PlayerVisualIndex;
        public int EnemyVisualIndex;

        // Character creator: when on, FighterStage assembles the player from Appearance
        // (modular base + accessories) instead of spawning a sample prefab.
        public bool UseCustomAppearance;

        CharacterPartCatalogue partsCache;
        CharacterPartCatalogue Parts =>
            partsCache != null ? partsCache : (partsCache = CharacterPartCatalogue.Load());

        CharacterAppearance appearance;
        public CharacterAppearance Appearance =>
            appearance ?? (appearance = CharacterAppearance.Default(Parts));

        public CharacterPartCatalogue Catalogue => Parts;

        public int PlayerVisualCount => Visuals != null ? Visuals.Players.Count : 0;
        public int EnemyVisualCount => Visuals != null ? Visuals.Monsters.Count : 0;

        public string PlayerVisualName =>
            PlayerVisualCount > 0 ? Visuals.PlayerAt(PlayerVisualIndex).DisplayName : "(no art)";
        public string EnemyVisualName =>
            EnemyVisualCount > 0 ? Visuals.MonsterAt(EnemyVisualIndex).DisplayName : "Bandit";

        // Build view — reads/writes the shared player build via the loadout pools.
        public string StanceName => LoadoutPools.GearName(Build.Gear, LoadoutPools.Stances);
        public string WeaponName => LoadoutPools.GearName(Build.Gear, LoadoutPools.Weapons);
        public string ArmorName => LoadoutPools.GearName(Build.Gear, LoadoutPools.Armors);
        public string AbilityName(int slot) => Build.AbilityName(Profile, slot);

        public void CyclePlayerVisual(int dir)
        {
            if (PlayerVisualCount > 0) PlayerVisualIndex = Wrap(PlayerVisualIndex + dir, PlayerVisualCount);
        }

        public void CycleEnemyVisual(int dir)
        {
            if (EnemyVisualCount > 0) EnemyVisualIndex = Wrap(EnemyVisualIndex + dir, EnemyVisualCount);
        }

        public void CycleStance(int dir) => LoadoutPools.CycleGear(Build.Gear, LoadoutPools.Stances, dir);
        public void CycleWeapon(int dir) => LoadoutPools.CycleGear(Build.Gear, LoadoutPools.Weapons, dir);
        public void CycleArmor(int dir) => LoadoutPools.CycleGear(Build.Gear, LoadoutPools.Armors, dir);

        /// <summary>Cycle one main bar slot, skipping abilities already in other main slots.</summary>
        public void CycleAbility(int slot, int dir) =>
            LoadoutPools.CycleAbility(Build.AbilityIds, slot, dir, Build.MainSlotCount);

        static int Wrap(int v, int n) => ((v % n) + n) % n;
    }
}
