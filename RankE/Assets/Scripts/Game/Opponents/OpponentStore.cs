using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Reads/writes <see cref="OpponentDef"/> files as JSON under <c>RankE/Opponents/</c> (a
    /// sibling of <c>Assets/</c>, so they're committed and shared but Unity doesn't import them).
    /// Mirrors <see cref="TuningPresetStore"/>; plain file IO, no asset database.
    /// </summary>
    public static class OpponentStore
    {
        /// <summary>Folder holding opponent files; <c>{project}/Opponents</c>.</summary>
        public static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Opponents"));

        static string PathFor(string id) => Path.Combine(Dir, Sanitize(id) + ".json");

        /// <summary>Existing opponent ids (file stems), sorted; empty if the folder is missing.</summary>
        public static List<string> List()
        {
            var ids = new List<string>();
            if (!Directory.Exists(Dir)) return ids;
            foreach (var f in Directory.GetFiles(Dir, "*.json"))
                ids.Add(Path.GetFileNameWithoutExtension(f));
            ids.Sort();
            return ids;
        }

        public static void Save(string id, OpponentDef def)
        {
            if (string.IsNullOrWhiteSpace(id) || def == null) return;
            def.id = id;
            Directory.CreateDirectory(Dir);
            File.WriteAllText(PathFor(id), JsonUtility.ToJson(def, true));
        }

        public static OpponentDef Load(string id)
        {
            var path = PathFor(id);
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<OpponentDef>(File.ReadAllText(path));
        }

        public static bool Exists(string id) => File.Exists(PathFor(id));

        static string Sanitize(string id)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                id = id.Replace(c, '_');
            return id.Trim();
        }
    }
}
