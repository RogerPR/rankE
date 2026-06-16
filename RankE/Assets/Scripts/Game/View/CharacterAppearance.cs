using System.Collections.Generic;

namespace RankE.Game
{
    /// <summary>
    /// A custom player look, stored as ids (base body + per-category accessory choice).
    /// Pure data — no prefab/engine references — so it stays a plain object that a
    /// future save system can serialize. Resolved against a <see cref="CharacterPartCatalogue"/>
    /// by <see cref="CharacterAssembler"/>. Presentation-only.
    /// </summary>
    public sealed class CharacterAppearance
    {
        public string BaseId;

        /// <summary>categoryId → chosen partId. Absent or empty = "(none)" for that slot.</summary>
        public readonly Dictionary<string, string> Choices = new Dictionary<string, string>();

        public string Get(string categoryId) =>
            Choices.TryGetValue(categoryId, out var v) ? v : null;

        public void Set(string categoryId, string partId)
        {
            if (string.IsNullOrEmpty(partId)) Choices.Remove(categoryId);
            else Choices[categoryId] = partId;
        }

        public void Clear(string categoryId) => Choices.Remove(categoryId);

        /// <summary>First base body, no accessories.</summary>
        public static CharacterAppearance Default(CharacterPartCatalogue cat)
        {
            var a = new CharacterAppearance();
            if (cat != null && cat.Bases.Count > 0) a.BaseId = cat.Bases[0].Id;
            return a;
        }

        /// <summary>Random base + a random part-or-none for every optional category.</summary>
        public static CharacterAppearance Random(CharacterPartCatalogue cat, System.Random rng)
        {
            var a = new CharacterAppearance();
            if (cat == null) return a;
            if (cat.Bases.Count > 0) a.BaseId = cat.Bases[rng.Next(cat.Bases.Count)].Id;

            foreach (var c in cat.Categories)
            {
                // For optional slots, ~1-in-(N+1) chance of "(none)" so looks stay varied.
                int span = c.Optional ? c.Parts.Count + 1 : c.Parts.Count;
                if (span <= 0) continue;
                int pick = rng.Next(span);
                if (pick < c.Parts.Count) a.Set(c.Id, c.Parts[pick].Id);
            }
            return a;
        }
    }
}
