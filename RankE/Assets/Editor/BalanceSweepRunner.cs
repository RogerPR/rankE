using System;
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
    /// Exits the editor itself in batch mode (do not pass -quit).
    /// </summary>
    public static class BalanceSweepRunner
    {
        public static void Run()
        {
            int fights = 1000;
            int seed = 12345;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-fights") int.TryParse(args[i + 1], out fights);
                if (args[i] == "-seed") int.TryParse(args[i + 1], out seed);
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var stats = BattleRunner.RunDefault(fights, seed);
            sw.Stop();

            Debug.Log($"[BalanceSweep] {fights} fights, seed {seed}, {sw.ElapsedMilliseconds} ms\n{stats.Summary()}");

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
    }
}
