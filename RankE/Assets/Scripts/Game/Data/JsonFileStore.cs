using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Shared file IO for repo-versioned JSON catalogues: one folder per catalogue at
    /// <c>{project}/&lt;Catalogue&gt;/</c> (a sibling of <c>Assets/</c>, so files are committed
    /// and shared but Unity doesn't import them), one JSON file per entry, JsonUtility
    /// serialization. Used by <see cref="TuningPresetStore"/> and <see cref="OpponentStore"/>;
    /// future catalogues (rewards, encounters, …) get their own thin store over this.
    ///
    /// Phase 7 note: player builds can't read <c>{project}/</c> — when the first standalone
    /// build lands, add a build step copying the catalogues into StreamingAssets and switch
    /// <see cref="DirFor"/> to <c>Application.streamingAssetsPath</c> outside the editor.
    /// </summary>
    public static class JsonFileStore
    {
        /// <summary>Absolute folder for a catalogue; <c>{project}/&lt;catalogue&gt;</c>.</summary>
        public static string DirFor(string catalogue)
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", catalogue));

        /// <summary>Existing entry names (file stems), sorted; empty if the folder is missing.</summary>
        public static List<string> List(string dir)
        {
            var names = new List<string>();
            if (!Directory.Exists(dir)) return names;
            foreach (var f in Directory.GetFiles(dir, "*.json"))
                names.Add(Path.GetFileNameWithoutExtension(f));
            names.Sort();
            return names;
        }

        public static void Save<T>(string dir, string name, T obj) where T : class
        {
            if (string.IsNullOrWhiteSpace(name) || obj == null) return;
            Directory.CreateDirectory(dir);
            File.WriteAllText(PathFor(dir, name), JsonUtility.ToJson(obj, true));
        }

        /// <summary>The entry, or null if the file is missing.</summary>
        public static T Load<T>(string dir, string name) where T : class
        {
            var path = PathFor(dir, name);
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }

        public static void Delete(string dir, string name)
        {
            var path = PathFor(dir, name);
            if (File.Exists(path)) File.Delete(path);
        }

        public static bool Exists(string dir, string name) => File.Exists(PathFor(dir, name));

        static string PathFor(string dir, string name) => Path.Combine(dir, Sanitize(name) + ".json");

        static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }
    }
}
