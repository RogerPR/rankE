using System.Collections.Generic;

namespace RankE.Game
{
    /// <summary>
    /// Reads/writes <see cref="OpponentDef"/> files as JSON under <c>RankE/Opponents/</c>
    /// (committed to the repo — see <see cref="JsonFileStore"/>).
    /// </summary>
    public static class OpponentStore
    {
        /// <summary>Folder holding opponent files; <c>{project}/Opponents</c>.</summary>
        public static string Dir => JsonFileStore.DirFor("Opponents");

        /// <summary>Existing opponent ids (file stems), sorted; empty if the folder is missing.</summary>
        public static List<string> List() => JsonFileStore.List(Dir);

        public static void Save(string id, OpponentDef def)
        {
            if (string.IsNullOrWhiteSpace(id) || def == null) return;
            def.id = id;
            JsonFileStore.Save(Dir, id, def);
        }

        public static OpponentDef Load(string id) => JsonFileStore.Load<OpponentDef>(Dir, id);

        public static bool Exists(string id) => JsonFileStore.Exists(Dir, id);
    }
}
