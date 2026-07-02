using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RankE.Sim
{
    /// <summary>Aggregated results of a batch of headless fights — the balance tool's output.</summary>
    public sealed class BattleStats
    {
        public int Fights;
        public int WinsA;
        public int WinsB;
        public int Draws;
        public long TotalTicks;
        public int MinTicks = int.MaxValue;
        public int MaxTicks;
        public int Parries;
        public int Ripostes;
        public int Breaks;
        public int CombosCompleted;
        public readonly Dictionary<string, int> AbilityUses = new Dictionary<string, int>();

        public float AverageSeconds => Fights == 0 ? 0 : TotalTicks / (float)Fights / SimConstants.TicksPerSecond;

        public void Record(Battle battle)
        {
            Fights++;
            if (battle.Winner == 0) WinsA++;
            else if (battle.Winner == 1) WinsB++;
            else Draws++;

            int ticks = battle.CurrentTick;
            TotalTicks += ticks;
            if (ticks < MinTicks) MinTicks = ticks;
            if (ticks > MaxTicks) MaxTicks = ticks;

            foreach (var ev in battle.Events)
            {
                switch (ev.Type)
                {
                    case SimEventType.AbilityUsed:
                        AbilityUses[ev.AbilityId] = AbilityUses.TryGetValue(ev.AbilityId, out var n) ? n + 1 : 1;
                        break;
                    case SimEventType.Parried: Parries++; break;
                    case SimEventType.RiposteTriggered: Ripostes++; break;
                    case SimEventType.Broken: Breaks++; break;
                    case SimEventType.ComboCompleted: CombosCompleted++; break;
                }
            }
        }

        /// <summary>Side-by-side A/B diff of two batches (e.g. two tunings) — win rates,
        /// durations, event counts and per-ability usage — for eyeballing what a change did.</summary>
        public static string Compare(string labelA, BattleStats a, string labelB, BattleStats b)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{"",-22}| {Trim(labelA),-22}| {Trim(labelB),-22}");
            sb.AppendLine(new string('-', 70));
            Row(sb, "fights", a.Fights.ToString(), b.Fights.ToString());
            Row(sb, "wins A (player)", Pct(a.WinsA, a.Fights), Pct(b.WinsA, b.Fights));
            Row(sb, "wins B (enemy)", Pct(a.WinsB, a.Fights), Pct(b.WinsB, b.Fights));
            Row(sb, "draws", Pct(a.Draws, a.Fights), Pct(b.Draws, b.Fights));
            Row(sb, "avg duration", $"{a.AverageSeconds:F1}s", $"{b.AverageSeconds:F1}s");
            Row(sb, "min/max ticks", $"{a.MinTicks}/{a.MaxTicks}", $"{b.MinTicks}/{b.MaxTicks}");
            Row(sb, "parries /fight", PerFight(a.Parries, a.Fights), PerFight(b.Parries, b.Fights));
            Row(sb, "ripostes /fight", PerFight(a.Ripostes, a.Fights), PerFight(b.Ripostes, b.Fights));
            Row(sb, "breaks /fight", PerFight(a.Breaks, a.Fights), PerFight(b.Breaks, b.Fights));
            Row(sb, "combos /fight", PerFight(a.CombosCompleted, a.Fights), PerFight(b.CombosCompleted, b.Fights));

            var ids = new SortedSet<string>(a.AbilityUses.Keys);
            ids.UnionWith(b.AbilityUses.Keys);
            if (ids.Count > 0) sb.AppendLine("ability uses /fight:");
            foreach (var id in ids)
            {
                a.AbilityUses.TryGetValue(id, out var ua);
                b.AbilityUses.TryGetValue(id, out var ub);
                Row(sb, "  " + id, PerFight(ua, a.Fights), PerFight(ub, b.Fights));
            }
            return sb.ToString();
        }

        static void Row(StringBuilder sb, string name, string va, string vb)
            => sb.AppendLine($"{name,-22}| {va,-22}| {vb,-22}");

        static string Pct(int n, int total) => total == 0 ? "-" : $"{n} ({100f * n / total:F1}%)";

        static string PerFight(int n, int fights) => fights == 0 ? "-" : $"{(float)n / fights:F2}";

        static string Trim(string s) => string.IsNullOrEmpty(s) ? "?" : (s.Length <= 22 ? s : s.Substring(0, 21) + "…");

        public string Summary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"fights: {Fights}  winsA: {WinsA}  winsB: {WinsB}  draws: {Draws}");
            sb.AppendLine($"duration: avg {AverageSeconds:F1}s  min {MinTicks}t  max {MaxTicks}t");
            sb.AppendLine($"parries: {Parries}  ripostes: {Ripostes}  breaks: {Breaks}  combos: {CombosCompleted}");
            sb.AppendLine("ability uses:");
            foreach (var kv in AbilityUses.OrderByDescending(kv => kv.Value))
                sb.AppendLine($"  {kv.Key}: {kv.Value}");
            return sb.ToString();
        }
    }
}
