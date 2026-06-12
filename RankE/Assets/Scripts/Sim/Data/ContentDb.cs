using System.Collections.Generic;

namespace RankE.Sim
{
    /// <summary>Lookup tables for status and ability definitions used by a battle.</summary>
    public sealed class ContentDb
    {
        public readonly Dictionary<string, StatusDef> Statuses = new Dictionary<string, StatusDef>();
        public readonly Dictionary<string, AbilityDef> Abilities = new Dictionary<string, AbilityDef>();

        public ContentDb Add(StatusDef status)
        {
            Statuses[status.Id] = status;
            return this;
        }

        public ContentDb Add(AbilityDef ability)
        {
            Abilities[ability.Id] = ability;
            return this;
        }

        public StatusDef Status(string id)
        {
            if (!Statuses.TryGetValue(id, out var def))
                throw new KeyNotFoundException($"Unknown status id '{id}'");
            return def;
        }
    }
}
