using System.Collections.Generic;

namespace RankE.Sim
{
    /// <summary>
    /// The game's default content roster — abilities, statuses, gear, and the cross-cutting
    /// <see cref="CombatTuning"/>. Originally transcribed from functional_POC/constants.py +
    /// abilities_impl.py, extended with the GAME_DESIGN §1 PROPOSED additions (break damage,
    /// combo tags, Falling Star, Lunge), and since tuned for the shipped game — this is the
    /// single source of truth for combat numbers. The control panel / <c>TuningProfile</c>
    /// clone and edit these per fight. Richer authored content arrives in Phase 6 and will
    /// likely live in ScriptableObject-backed data.
    /// </summary>
    public static class DefaultContent
    {
        // ability ids
        public const string SlashId = "slash";
        public const string BashId = "bash";
        public const string FireballId = "fireball";
        public const string VampiroId = "vampiro";
        public const string ParryId = "parry";
        public const string KickId = "kick";
        public const string BlockId = "block";
        public const string RiposteId = "riposte";
        public const string InterruptCastId = "interrupt_cast";
        public const string AutoAttackId = "auto_attack";
        public const string FallingStarId = "falling_star";
        public const string LungeId = "lunge";

        // status ids
        public const string StunStatus = "stun";
        public const string PoisonStatus = "poison";
        public const string RegenStatus = "regen";
        public const string ParryStatus = "parry";
        public const string BrokenStatus = "broken";
        public const string DistanceStatus = "distance";
        public const string BlockPhysStatus = "block_phys";  // Block's −60% physical reduction
        public const string PhysShieldStatus = "phys_shield"; // Block's physical absorb pool
        public const string EmpoweredStatus = "empowered";    // combo reward: next hit ×2

        // combo colour ids (mapped to on-screen colours by the UI)
        public const string ColorRed = "red";
        public const string ColorYellow = "yellow";

        // passive ids
        public const string AutoAttackPassiveId = "auto_attack";

        public static CombatTuning CreateTuning() => new CombatTuning();

        public static ContentDb CreateContent()
        {
            return new ContentDb()
                .Add(new StatusDef { Id = StunStatus, Name = "Stun", BlocksActions = true, CancelsCast = true })
                .Add(new StatusDef { Id = PoisonStatus, Name = "Poison", HpPerInterval = -3, IntervalTicks = 20 })
                .Add(new StatusDef { Id = RegenStatus, Name = "Regen", HpPerInterval = 2, IntervalTicks = 20 })
                .Add(new StatusDef { Id = ParryStatus, Name = "Parry" })
                .Add(new StatusDef { Id = BrokenStatus, Name = "Broken", BlocksActions = true, CancelsCast = true, DamageTakenMult = 1.5 })
                .Add(new StatusDef { Id = DistanceStatus, Name = "Distance", IsDistance = true })
                .Add(new StatusDef { Id = BlockPhysStatus, Name = "Block", DamageTakenMult = 0.4, DamageSchoolFilter = Schools.Physical })
                .Add(new StatusDef { Id = PhysShieldStatus, Name = "Shield", DamageSchoolFilter = Schools.Physical })
                .Add(new StatusDef { Id = EmpoweredStatus, Name = "Empowered", DamageDealtMult = 2.0, ConsumeOnDamageDealt = true })
                .Add(Slash())
                .Add(Bash())
                .Add(Fireball())
                .Add(Vampiro())
                .Add(Parry())
                .Add(Kick())
                .Add(Block())
                .Add(Riposte())
                .Add(InterruptCast())
                .Add(AutoAttack())
                .Add(FallingStar())
                .Add(Lunge());
        }

        // Combo colours over the PoC kit: Slash=red, Bash=yellow (the tutorial's two colours).
        // Other abilities are combo-neutral until they're assigned a colour.

        public static AbilityDef Slash() => new AbilityDef
        {
            Id = SlashId,
            Name = "Slash",
            Gcd = GcdClass.Normal,
            CooldownTicks = 60, // 3s — matches the enemy's telegraphed Slash beat
            Parriable = true,
            IsMelee = true,
            ComboColor = ColorRed,
            Effects = new List<EffectDef> { EffectDef.Damage(10) },
        };

        public static AbilityDef Bash() => new AbilityDef
        {
            Id = BashId,
            Name = "Bash",
            Gcd = GcdClass.Normal,
            CooldownTicks = 160,    // ported from the tutorial preset
            PreLockTicks = 10,      // 0.5s wind-up — the heavy hit lands after the swing
            Parriable = true,
            IsMelee = true,
            ComboColor = ColorYellow,
            Effects = new List<EffectDef>
            {
                EffectDef.Damage(5),
                EffectDef.Status(EffectTarget.Opponent, StunStatus, 80),
                EffectDef.Break(20),
            },
        };

        public static AbilityDef Fireball() => new AbilityDef
        {
            Id = FireballId,
            Name = "Fireball",
            Gcd = GcdClass.Normal,
            CooldownTicks = 160,
            CastTicks = 40,
            Interruptible = true,
            GemCost = 1,
            Effects = new List<EffectDef> { EffectDef.Damage(40, Schools.Magic) },
        };

        public static AbilityDef Vampiro() => new AbilityDef
        {
            Id = VampiroId,
            Name = "Vampiro",
            Gcd = GcdClass.Normal,
            CooldownTicks = 240,
            CastTicks = 60,
            Interruptible = true,
            GemCost = 1,
            Effects = new List<EffectDef>
            {
                EffectDef.Status(EffectTarget.Self, RegenStatus, 120),
                EffectDef.Status(EffectTarget.Opponent, PoisonStatus, 160),
            },
        };

        public static AbilityDef Parry() => new AbilityDef
        {
            Id = ParryId,
            Name = "Parry",
            Gcd = GcdClass.Quick,
            CooldownTicks = 120,
            Effects = new List<EffectDef> { EffectDef.Status(EffectTarget.Self, ParryStatus, 12) },
        };

        public static AbilityDef Kick() => new AbilityDef
        {
            Id = KickId,
            Name = "Kick",
            Gcd = GcdClass.Quick,
            CooldownTicks = 240,
            IsMelee = true,
            Effects = new List<EffectDef>
            {
                EffectDef.Damage(2),
                EffectDef.Interrupt(StunStatus, 60),
            },
        };

        /// <summary>PROPOSED quick defense vs physical: two independent layers for 2s — a −60%
        /// physical-damage status and a small physical absorb shield (pool size = the shield
        /// effect's Amount). Magic (e.g. Fireball) bypasses both, so Kick stays the answer to
        /// casts. Numbers are tunable starting points.</summary>
        public static AbilityDef Block() => new AbilityDef
        {
            Id = BlockId,
            Name = "Block",
            Gcd = GcdClass.Quick,
            CooldownTicks = 120, // 6s
            Effects = new List<EffectDef>
            {
                EffectDef.Status(EffectTarget.Self, BlockPhysStatus, 40),   // 2s, −60% physical
                EffectDef.Shield(EffectTarget.Self, PhysShieldStatus, 10, 40), // 2s, soaks 10 physical
            },
        };

        /// <summary>Automatic counter at a full riposte bar; +30 break is PROPOSED.</summary>
        public static AbilityDef Riposte() => new AbilityDef
        {
            Id = RiposteId,
            Name = "Riposte",
            Gcd = GcdClass.None,
            Effects = new List<EffectDef>
            {
                EffectDef.Damage(40),
                EffectDef.Status(EffectTarget.Opponent, StunStatus, 100),
                EffectDef.Break(30),
            },
        };

        public static AbilityDef InterruptCast() => new AbilityDef
        {
            Id = InterruptCastId,
            Name = "Stop Casting",
            Gcd = GcdClass.Quick,
            UsableWhileCasting = true,
            Effects = new List<EffectDef> { EffectDef.InterruptSelf() },
        };

        // ---- passives ----

        /// <summary>The auto-attack passive: a periodic melee swing (<see cref="AutoAttack"/> is
        /// the swing whose CooldownTicks is the interval). Carried by default fighters; the
        /// tutorial fighters omit it, so they make no auto-attacks.</summary>
        public static PassiveDef AutoAttackPassive() => new PassiveDef
        {
            Id = AutoAttackPassiveId,
            Name = "Auto-attack",
            Kind = PassiveKinds.AutoAttack,
            AbilityId = AutoAttackId,
        };

        /// <summary>All known passive ids (for pickers/iteration). Grows with content.</summary>
        public static readonly string[] PassiveIds = { AutoAttackPassiveId };

        /// <summary>Resolve a passive id to its definition, or null if unknown.</summary>
        public static PassiveDef Passive(string id)
        {
            switch (id)
            {
                case AutoAttackPassiveId: return AutoAttackPassive();
                default: return null;
            }
        }

        public static AbilityDef AutoAttack() => new AbilityDef
        {
            Id = AutoAttackId,
            Name = "Auto-attack",
            Gcd = GcdClass.None,
            CooldownTicks = 60, // interval (3s) — slower chip damage
            Parriable = true,
            IsMelee = true,
            Effects = new List<EffectDef> { EffectDef.Damage(5) },
        };

        /// <summary>PROPOSED delayed-ability example from GAME_DESIGN §1.</summary>
        public static AbilityDef FallingStar() => new AbilityDef
        {
            Id = FallingStarId,
            Name = "Falling Star",
            Gcd = GcdClass.Normal,
            CooldownTicks = 200,
            DelayTicks = 60,
            GemCost = 1,
            Effects = new List<EffectDef> { EffectDef.Damage(30, Schools.Magic) },
        };

        /// <summary>PROPOSED gap-closer: ends Distance (GAME_DESIGN §1 statuses).</summary>
        public static AbilityDef Lunge() => new AbilityDef
        {
            Id = LungeId,
            Name = "Lunge",
            Gcd = GcdClass.Normal,
            CooldownTicks = 100,
            Effects = new List<EffectDef>
            {
                EffectDef.Clear(EffectTarget.Self, DistanceStatus),
                EffectDef.Clear(EffectTarget.Opponent, DistanceStatus),
            },
        };

        public static List<AbilityDef> DefaultLoadout() => new List<AbilityDef>
        {
            Slash(), Bash(), Fireball(), Vampiro(), Parry(), Kick(),
        };

        /// <summary>PoC opponent numbers (500 HP, 8 gems) used for symmetric AI fights.</summary>
        public static FighterConfig DefaultConfig(string name) => new FighterConfig
        {
            Name = name,
            MaxHp = 500,
            SpellGems = 8,
            AutoAttack = AutoAttack(),
            Abilities = DefaultLoadout(),
        };

        // Gear (PoC stances/weapons/armor as build-state drops). Per Appendix A, gear now
        // grants stat-sheet deltas; structural shaping (auto-attack/cast/gems) stays on the
        // dedicated fields. Numbers are tunable starting points — tune via BattleRunner.

        // Stances — playstyle modifiers.
        public static GearDef StanceRock() => new GearDef
        {
            // Heavy hitter: +60% Attack makes the base-5 auto land for 8 (was a flat override).
            Id = "stance_rock", Name = "Rock Stance", Slot = "stance",
            StatDeltas = new Dictionary<string, double> { { StatIds.Attack, 60 } },
        };

        public static GearDef StanceWind() => new GearDef
        {
            // Nimble: trades HP for a much faster Parry (per-ability cd isn't a stat).
            Id = "stance_wind", Name = "Wind Stance", Slot = "stance", MaxHpMult = 0.9,
            AbilityCooldownMults = new Dictionary<string, double> { { ParryId, 0.5 } },
        };

        public static GearDef StanceWater() => new GearDef
        {
            // Caster: trades HP for gems and Magic.
            Id = "stance_water", Name = "Water Stance", Slot = "stance", MaxHpMult = 0.8, GemsOverride = 7,
            StatDeltas = new Dictionary<string, double> { { StatIds.Magic, 20 } },
        };

        // Weapons — set offense and shape the auto-attack.
        public static GearDef WeaponSword() => new GearDef
        {
            Id = "weapon_sword", Name = "Sword", Slot = "weapon",
            StatDeltas = new Dictionary<string, double> { { StatIds.Attack, 20 } },
        };

        public static GearDef WeaponDagger() => new GearDef
        {
            // Fast, weaker auto, crits more.
            Id = "weapon_dagger", Name = "Dagger", Slot = "weapon",
            AutoAttackIntervalMult = 0.8, AutoAttackDamageMult = 0.9,
            StatDeltas = new Dictionary<string, double> { { StatIds.Attack, 10 }, { StatIds.CritChance, 15 } },
        };

        public static GearDef WeaponWand() => new GearDef
        {
            Id = "weapon_wand", Name = "Wand", Slot = "weapon", CastTimeMult = 0.8,
            StatDeltas = new Dictionary<string, double> { { StatIds.Magic, 30 } },
        };

        public static GearDef WeaponGreataxe() => new GearDef
        {
            // Big, breaks hard, swings slow.
            Id = "weapon_greataxe", Name = "Greataxe", Slot = "weapon",
            StatDeltas = new Dictionary<string, double>
            { { StatIds.Attack, 40 }, { StatIds.BreakPower, 0.5 }, { StatIds.Haste, -20 } },
        };

        // Armor — trades defense for mobility (Defense/Haste stats, was damage/cooldown mults).
        public static GearDef ArmorLight() => new GearDef
        {
            Id = "armor_light", Name = "Light Armor", Slot = "armor",
            StatDeltas = new Dictionary<string, double>
            { { StatIds.Haste, 15 }, { StatIds.CritChance, 10 }, { StatIds.Defense, -20 } },
        };

        public static GearDef ArmorMedium() => new GearDef
        { Id = "armor_medium", Name = "Medium Armor", Slot = "armor" };

        public static GearDef ArmorHeavy() => new GearDef
        {
            Id = "armor_heavy", Name = "Heavy Armor", Slot = "armor", MaxHpMult = 1.2,
            StatDeltas = new Dictionary<string, double> { { StatIds.Defense, 30 }, { StatIds.Haste, -15 } },
        };
    }
}
