namespace RankE.Sim
{
    /// <summary>Passive kinds are open string ids (like <see cref="EffectKinds"/>/<see cref="Schools"/>)
    /// so new passive behaviors are new data + a handler, not a hardcoded enum. Only
    /// <see cref="AutoAttack"/> is wired today; the rest of the catalogue arrives with content.</summary>
    public static class PassiveKinds
    {
        /// <summary>A periodic melee swing on a fixed interval (the former always-on auto-attack).</summary>
        public const string AutoAttack = "auto_attack";
    }

    /// <summary>
    /// Pure data definition of a passive skill a fighter carries — an always-on trait rather
    /// than an activated ability. First-class and data-driven so a build can mix any number of
    /// them (auto-attack is now just the first one). Resolved per fight from a build's passive
    /// id list; a fighter with no passives makes no auto-attacks.
    /// </summary>
    public sealed class PassiveDef
    {
        public string Id;
        public string Name;

        /// <summary>Which behavior this passive drives (a <see cref="PassiveKinds"/> id).</summary>
        public string Kind = PassiveKinds.AutoAttack;

        /// <summary>For the auto-attack kind: the ability id of the periodic swing (its
        /// CooldownTicks is the interval). Unused by other kinds.</summary>
        public string AbilityId;

        public PassiveDef Clone() => new PassiveDef
        {
            Id = Id,
            Name = Name,
            Kind = Kind,
            AbilityId = AbilityId,
        };
    }
}
