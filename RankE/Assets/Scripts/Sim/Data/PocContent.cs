using System.Collections.Generic;

namespace RankE.Sim
{
    /// <summary>
    /// The PoC roster transcribed from functional_POC/constants.py + abilities_impl.py,
    /// extended with the GAME_DESIGN §1 PROPOSED additions (break damage, combo tags,
    /// Falling Star, Lunge). This is dev/reference content; real content arrives in
    /// Phase 6 and will likely live in ScriptableObject-backed data.
    /// </summary>
    public static class PocContent
    {
        // ability ids
        public const string SlashId = "slash";
        public const string BashId = "bash";
        public const string FireballId = "fireball";
        public const string VampiroId = "vampiro";
        public const string ParryId = "parry";
        public const string KickId = "kick";
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
                .Add(Slash())
                .Add(Bash())
                .Add(Fireball())
                .Add(Vampiro())
                .Add(Parry())
                .Add(Kick())
                .Add(Riposte())
                .Add(InterruptCast())
                .Add(AutoAttack())
                .Add(FallingStar())
                .Add(Lunge());
        }

        // Combo tag assignment over the PoC kit is PROPOSED: Slash=Opener,
        // Bash=Linker, Fireball=Finisher; Vampiro is neutral (support).

        public static AbilityDef Slash() => new AbilityDef
        {
            Id = SlashId,
            Name = "Slash",
            Gcd = GcdClass.Normal,
            CooldownTicks = 80,
            Parriable = true,
            IsMelee = true,
            ComboTag = ComboTags.Opener,
            Effects = new List<EffectDef> { EffectDef.Damage(10) },
        };

        public static AbilityDef Bash() => new AbilityDef
        {
            Id = BashId,
            Name = "Bash",
            Gcd = GcdClass.Normal,
            CooldownTicks = 600,
            Parriable = true,
            IsMelee = true,
            ComboTag = ComboTags.Linker,
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
            ComboTag = ComboTags.Finisher,
            Effects = new List<EffectDef> { EffectDef.Damage(40) },
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

        public static AbilityDef AutoAttack() => new AbilityDef
        {
            Id = AutoAttackId,
            Name = "Auto-attack",
            Gcd = GcdClass.None,
            CooldownTicks = 40, // interval
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
            Effects = new List<EffectDef> { EffectDef.Damage(30) },
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

        // Gear (PoC stances/weapons/armor as build-state drops)

        public static GearDef StanceRock() => new GearDef
        { Id = "stance_rock", Name = "Rock Stance", Slot = "stance", AutoAttackDamageOverride = 8 };

        public static GearDef StanceWind() => new GearDef
        {
            Id = "stance_wind", Name = "Wind Stance", Slot = "stance", MaxHpMult = 0.9,
            AbilityCooldownMults = new Dictionary<string, double> { { ParryId, 0.5 } },
        };

        public static GearDef StanceWater() => new GearDef
        { Id = "stance_water", Name = "Water Stance", Slot = "stance", MaxHpMult = 0.8, GemsOverride = 7 };

        public static GearDef WeaponSword() => new GearDef
        { Id = "weapon_sword", Name = "Sword", Slot = "weapon" };

        public static GearDef WeaponDagger() => new GearDef
        { Id = "weapon_dagger", Name = "Dagger", Slot = "weapon", AutoAttackIntervalMult = 0.8, AutoAttackDamageMult = 0.9 };

        public static GearDef WeaponWand() => new GearDef
        { Id = "weapon_wand", Name = "Wand", Slot = "weapon", CastTimeMult = 0.8 };

        public static GearDef ArmorLight() => new GearDef
        { Id = "armor_light", Name = "Light Armor", Slot = "armor", DamageTakenMult = 0.9, CooldownMult = 0.9 };

        public static GearDef ArmorMedium() => new GearDef
        { Id = "armor_medium", Name = "Medium Armor", Slot = "armor" };

        public static GearDef ArmorHeavy() => new GearDef
        { Id = "armor_heavy", Name = "Heavy Armor", Slot = "armor", DamageTakenMult = 1.1, CooldownMult = 1.1 };
    }
}
