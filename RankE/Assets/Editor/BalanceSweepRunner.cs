using System;
using System.Reflection;
using RankE.Game;
using RankE.Sim;
using UnityEditor;
using UnityEngine;

namespace RankE.Editor
{
    /// <summary>
    /// Command-line entry point for headless balance sweeps:
    ///   Unity -batchmode -projectPath RankE \
    ///     -executeMethod RankE.Editor.BalanceSweepRunner.Run \
    ///     -fights 1000 -seed 42 -logFile /tmp/ranke-sweep.log
    ///
    /// With no scenario args it runs the pinned PoC mirror match (the regression baseline).
    /// Scenario args (all optional, combinable):
    ///   -preset X       tuning preset from TuningPresets/X.json (player build vs its adversary/opponent)
    ///   -opponent Y     force the adversary to Opponents/Y.json
    ///   -presetB Z      A/B mode — also run preset Z and print a side-by-side diff
    ///   -param F -values 24,30,36
    ///                   range mode — run once per value of F, where F is a CombatTuning field
    ///                   (e.g. GcdTicks) or an ability field as id.Field (e.g. slash.CooldownTicks)
    /// Scenario runs use the PoC AI as the "player" — relative comparisons, not human win rates.
    /// Exits the editor itself in batch mode (do not pass -quit).
    /// </summary>
    public static class BalanceSweepRunner
    {
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string name)
            {
                for (int i = 0; i < args.Length - 1; i++)
                    if (args[i] == name) return args[i + 1];
                return null;
            }

            int fights = int.TryParse(Arg("-fights"), out var f) ? f : 1000;
            int seed = int.TryParse(Arg("-seed"), out var s) ? s : 12345;
            string preset = Arg("-preset");
            string opponent = Arg("-opponent");
            string presetB = Arg("-presetB");
            string param = Arg("-param");
            string values = Arg("-values");

            int exitCode = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (preset == null && opponent == null && presetB == null && param == null)
                {
                    // The pinned baseline path — byte-comparable across sessions.
                    var stats = BattleRunner.RunDefault(fights, seed);
                    Log(fights, seed, sw, "PoC mirror match\n" + stats.Summary());
                }
                else
                {
                    var scenario = SweepScenario.FromPreset(preset, opponent);
                    if (presetB != null)
                    {
                        var scenarioB = SweepScenario.FromPreset(presetB, opponent);
                        var a = scenario.Run(fights, seed);
                        var b = scenarioB.Run(fights, seed);
                        Log(fights, seed, sw, "A/B (PoC AI as player — relative comparison only)\n"
                            + BattleStats.Compare(scenario.Label, a, scenarioB.Label, b));
                    }
                    else if (param != null)
                    {
                        if (string.IsNullOrEmpty(values))
                            throw new ArgumentException("-param requires -values v1,v2,…");
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine($"param range {param} (PoC AI as player — relative comparison only)");
                        foreach (var raw in values.Split(','))
                        {
                            double v = double.Parse(raw.Trim(), System.Globalization.CultureInfo.InvariantCulture);
                            SetParam(scenario.Profile, param, v);
                            var stats = scenario.Run(fights, seed);
                            sb.AppendLine($"--- {param} = {v} ({scenario.Label})");
                            sb.Append(stats.Summary());
                        }
                        Log(fights, seed, sw, sb.ToString());
                    }
                    else
                    {
                        var stats = scenario.Run(fights, seed);
                        Log(fights, seed, sw, $"{scenario.Label} (PoC AI as player — relative comparison only)\n"
                            + stats.Summary());
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[BalanceSweep] FAILED: " + e.Message);
                exitCode = 1;
            }

            if (Application.isBatchMode)
                EditorApplication.Exit(exitCode);
        }

        static void Log(int fights, int seed, System.Diagnostics.Stopwatch sw, string body)
        {
            sw.Stop();
            Debug.Log($"[BalanceSweep] {fights} fights, seed {seed}, {sw.ElapsedMilliseconds} ms — {body}");
        }

        /// <summary>Set a swept numeric field: "GcdTicks" on CombatTuning, or "slash.CooldownTicks"
        /// on an ability in the profile's library. Throws on unknown names — fail loudly.</summary>
        internal static void SetParam(TuningProfile profile, string param, double value)
        {
            int dot = param.IndexOf('.');
            if (dot < 0)
            {
                SetNumericField(profile.Tuning, typeof(CombatTuning), param, value);
                return;
            }
            var abilityId = param.Substring(0, dot);
            var fieldName = param.Substring(dot + 1);
            if (!profile.Abilities.TryGetValue(abilityId, out var def) || def == null)
                throw new ArgumentException($"Unknown ability \"{abilityId}\" in -param {param}");
            SetNumericField(def, typeof(AbilityDef), fieldName, value);
        }

        static void SetNumericField(object target, Type type, string name, double value)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
                throw new ArgumentException($"{type.Name} has no public field \"{name}\"");
            if (field.FieldType == typeof(int)) field.SetValue(target, (int)Math.Round(value));
            else if (field.FieldType == typeof(double)) field.SetValue(target, value);
            else throw new ArgumentException($"{type.Name}.{name} is not a numeric field");
        }
    }
}
