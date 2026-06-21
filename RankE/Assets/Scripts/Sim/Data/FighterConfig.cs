using System.Collections.Generic;

namespace RankE.Sim
{
    /// <summary>Everything needed to instantiate one fighter for a battle.</summary>
    public sealed class FighterConfig
    {
        public string Name = "Fighter";
        public int MaxHp = 100;
        public int SpellGems;

        /// <summary>Base RPG stat sheet; gear stat deltas are layered on at battle start.</summary>
        public StatSheet Stats = new StatSheet();

        /// <summary>null = no auto-attack (handy in tests). CooldownTicks is the interval.
        /// Resolved from <see cref="Passives"/> (the auto-attack passive) on the build side.</summary>
        public AbilityDef AutoAttack;

        /// <summary>Resolved passive skills this fighter carries. Auto-attack is the one wired
        /// kind today (it also populates <see cref="AutoAttack"/>); other kinds are PROPOSED.</summary>
        public List<PassiveDef> Passives = new List<PassiveDef>();

        /// <summary>Active loadout (ability bar + quick slots), excluding the auto-attack.</summary>
        public List<AbilityDef> Abilities = new List<AbilityDef>();

        public BuildState Build = new BuildState();

        /// <summary>Only the human player runs the colour-sequence combo system: they accrue a
        /// combo sequence and earn the empowered hit. Enemies leave this false.</summary>
        public bool UsesComboSystem;
    }
}
