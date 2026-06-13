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

        /// <summary>null = no auto-attack (handy in tests). CooldownTicks is the interval.</summary>
        public AbilityDef AutoAttack;

        /// <summary>Active loadout (ability bar + quick slots), excluding the auto-attack.</summary>
        public List<AbilityDef> Abilities = new List<AbilityDef>();

        public BuildState Build = new BuildState();
    }
}
