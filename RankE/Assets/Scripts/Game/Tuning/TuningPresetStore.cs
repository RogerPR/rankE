using System.Collections.Generic;

namespace RankE.Game
{
    /// <summary>
    /// Reads/writes <see cref="TuningPreset"/> fixtures as JSON files under
    /// <c>RankE/TuningPresets/</c> (committed to the repo — see <see cref="JsonFileStore"/>).
    /// Dev/playtest tool — used by the in-game control panel and the editor window.
    /// </summary>
    public static class TuningPresetStore
    {
        /// <summary>The well-known preset name auto-applied at boot (see
        /// <see cref="TuningProfile.Active"/>). "SET STARTUP" in the panel / "Save as startup"
        /// in the tuning window write it; delete the file to fall back to code defaults.</summary>
        public const string StartupName = "Default";

        /// <summary>Folder holding the preset files; <c>{project}/TuningPresets</c>.</summary>
        public static string Dir => JsonFileStore.DirFor("TuningPresets");

        /// <summary>The startup preset, or null if none saved.</summary>
        public static TuningPreset LoadStartup() => Load(StartupName);

        /// <summary>Existing preset names (file stems), sorted; empty if the folder is missing.</summary>
        public static List<string> List() => JsonFileStore.List(Dir);

        public static void Save(string name, TuningPreset preset)
        {
            if (string.IsNullOrWhiteSpace(name) || preset == null) return;
            preset.name = name;
            JsonFileStore.Save(Dir, name, preset);
        }

        public static TuningPreset Load(string name) => JsonFileStore.Load<TuningPreset>(Dir, name);

        public static void Delete(string name) => JsonFileStore.Delete(Dir, name);

        public static bool Exists(string name) => JsonFileStore.Exists(Dir, name);
    }
}
