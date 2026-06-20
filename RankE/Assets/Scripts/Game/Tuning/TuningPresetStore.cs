using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Reads/writes <see cref="TuningPreset"/> fixtures as JSON files under
    /// <c>RankE/TuningPresets/</c> (a sibling of <c>Assets/</c>, so the files are committed to
    /// the repo and shared, but Unity doesn't try to import them). Dev/playtest tool — used by
    /// the in-game control panel and the editor window. Plain file IO; no asset database.
    /// </summary>
    public static class TuningPresetStore
    {
        /// <summary>Folder holding the preset files; <c>{project}/TuningPresets</c>.</summary>
        public static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TuningPresets"));

        static string PathFor(string name) => Path.Combine(Dir, Sanitize(name) + ".json");

        /// <summary>Existing preset names (file stems), sorted; empty if the folder is missing.</summary>
        public static List<string> List()
        {
            var names = new List<string>();
            if (!Directory.Exists(Dir)) return names;
            foreach (var f in Directory.GetFiles(Dir, "*.json"))
                names.Add(Path.GetFileNameWithoutExtension(f));
            names.Sort();
            return names;
        }

        public static void Save(string name, TuningPreset preset)
        {
            if (string.IsNullOrWhiteSpace(name) || preset == null) return;
            preset.name = name;
            Directory.CreateDirectory(Dir);
            File.WriteAllText(PathFor(name), JsonUtility.ToJson(preset, true));
        }

        public static TuningPreset Load(string name)
        {
            var path = PathFor(name);
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<TuningPreset>(File.ReadAllText(path));
        }

        public static void Delete(string name)
        {
            var path = PathFor(name);
            if (File.Exists(path)) File.Delete(path);
        }

        public static bool Exists(string name) => File.Exists(PathFor(name));

        static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }
    }
}
