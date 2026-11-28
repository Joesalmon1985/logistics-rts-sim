using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using God.Sim.Unified;
using God.Train.Eval;
using God.Train.Honesty;

namespace God.Train.Report
{
    /// <summary>
    /// The slice-1 headless report of an evaluated held-out run (unified world): the gate table per arm, survival days per person,
    /// end-of-day need levels, accepted controls by control, causes of death, compared across arms. Reads the results file only.
    /// </summary>
    public static class SliceReport
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Write(EvalResults r, Thresholds t)
        {
            var sb = new StringBuilder();
            var v = Verdict.Compute(r, t);
            bool supplementary = r.Label.StartsWith("SUPPLEMENTARY");
            sb.Append("# Slice 1 held-out report (").Append(r.World).Append(" world)\n\n");
            sb.Append("- ").Append(r.Label).Append("; seeds ").Append(r.Seeds.First()).Append('-').Append(r.Seeds.Last()).Append(" (").Append(r.Seeds.Count).Append("), ")
              .Append(r.Population).Append(" persons per episode, ").Append(r.Days).Append(" days x ").Append(r.TicksPerDay).Append(" ticks; need mask 0x").Append(r.NeedMask.ToString("X3"))
              .Append("; pain band ").Append(r.PainBand).Append(" permille.\n");
            sb.Append("- Thresholds `TRAINING_THRESHOLDS.md` hash `").Append(r.ThresholdsHash).Append("` (v").Append(r.ThresholdsVersion).Append(").\n");
            sb.Append("- Layouts (seed mod 4): mirrored / rotated / regenerated / unfamiliar start, all on regenerated held-out boards; teacher-free, no oracle, divinity off.\n");
            if (supplementary) sb.Append("- **Supplementary run: the measures below use the T1-T7 formulas on a different population; they are not TR-01 results and no threshold is applied.**\n");
            sb.Append("- **Verdict: ").Append(v.Headline).Append("**");
            if (v.FailedGate.Count > 0) sb.Append(" (gate not met: ").Append(string.Join("; ", v.FailedGate)).Append(')');
            sb.Append(".\n\n");

            // ---- gate table
            sb.Append("## Measures by arm\n\n| Arm | Brain | T1 survival | T2 pain share | T3 shelter | T4 collection | T5 interruption | T6 full start | T7 hand-over |\n|---|---|---|---|---|---|---|---|---|\n");
            foreach (var a in r.Arms)
            {
                sb.Append("| ").Append(a.Name).Append(" | ").Append(a.Brain).Append(" (").Append(a.ArtifactStatus).Append(") |");
                foreach (var id in new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7" })
                {
                    var m = a.Measures.Find(x => x.Id == id);
                    if (m == null) { sb.Append(" - |"); continue; }
                    if (!m.Evaluable) { sb.Append(" not evaluable |"); continue; }
                    string val = m.Kind.StartsWith("seeds_with") ? m.Count + "/" + m.Of : m.Value.ToString("0.000", Inv);
                    sb.Append(' ').Append(val).Append(a.Name == "trained" && !supplementary ? (m.Met ? " met" : " **not met**") : "").Append(" |");
                }
                sb.Append('\n');
            }
            sb.Append("\nThresholds apply to the trained arm (T1 >= 0.90 and >= untrained + 0.40; T2 <= 0.10; T3 >= 14/20; T4 >= 10/20; T6 >= 0.75 each seed). ");
            var tr = r.Arm("trained");
            var t1 = tr?.Measures.Find(x => x.Id == "T1");
            if (t1 != null && !string.IsNullOrEmpty(t1.Note)) sb.Append("T1: ").Append(t1.Note).Append(". ");
            sb.Append("T3, T4, T5, T6, T7 cannot be evaluated in slice 1 (no buildings, construction, collection, hand-over or 72-person start are merged), so TR-01 cannot pass here whatever the survival result.\n\n");

            // ---- survival
            sb.Append("## Survival\n\n| Arm | adults alive at day 10 | mean survival days per person | deaths by day 1/2/3/4/5/6/7/8/9/10 |\n|---|---|---|---|\n");
            foreach (var a in r.Arms)
            {
                var ps = a.PerSeed.SelectMany(x => x.People).ToList();
                int alive = ps.Count(p => p.DeathTick < 0);
                double mean = ps.Count == 0 ? 0 : ps.Average(p => SurvivalDays(p, r));
                var byDay = new int[r.Days + 1];
                foreach (var p in ps) if (p.DeathTick >= 0) byDay[Math.Min(r.Days, (int)(p.DeathTick / r.TicksPerDay) + 1)]++;
                sb.Append("| ").Append(a.Name).Append(" | ").Append(alive).Append('/').Append(ps.Count).Append(" | ").Append(mean.ToString("0.00", Inv)).Append(" | ")
                  .Append(string.Join(" ", Enumerable.Range(1, r.Days).Select(d => byDay[d].ToString(Inv)))).Append(" |\n");
            }
            sb.Append("\nSurvival days per person (each seed: the persons in id order; 10.0 = alive at the end):\n\n| Seed | Layout |");
            foreach (var a in r.Arms) sb.Append(' ').Append(a.Name).Append(" |");
            sb.Append("\n|---|---|"); foreach (var _ in r.Arms) sb.Append("---|"); sb.Append('\n');
            for (int i = 0; i < r.Seeds.Count; i++)
            {
                sb.Append("| ").Append(r.Seeds[i]).Append(" | ").Append(r.Arms[0].PerSeed[i].Layout).Append(" |");
                foreach (var a in r.Arms) sb.Append(' ').Append(string.Join(" ", a.PerSeed[i].People.Select(p => SurvivalDays(p, r).ToString("0.0", Inv)))).Append(" |");
                sb.Append('\n');
            }

            // ---- needs over time
            sb.Append("\n## Need levels over time\n\nMean pressure (permille) of the living persons at the end of each day; share of person-ticks in the hunger-or-thirst pain band in the last column.\n\n");
            foreach (var (label, sel) in new (string, Func<PersonResult, List<int>>)[] { ("Hunger", p => p.Hunger), ("Thirst", p => p.Thirst), ("Energy (fatigue)", p => p.Energy) })
            {
                sb.Append("**").Append(label).Append("**\n\n| Arm |"); for (int d = 1; d <= r.Days; d++) sb.Append(" d").Append(d).Append(" |");
                sb.Append("\n|---|"); for (int d = 1; d <= r.Days; d++) sb.Append("---|"); sb.Append('\n');
                foreach (var a in r.Arms)
                {
                    var ps = a.PerSeed.SelectMany(x => x.People).ToList();
                    sb.Append("| ").Append(a.Name).Append(" |");
                    for (int d = 0; d < r.Days; d++)
                    {
                        var vals = ps.Select(sel).Where(l => l.Count > d && l[d] >= 0).Select(l => l[d]).ToList();
                        sb.Append(' ').Append(vals.Count == 0 ? "-" : ((int)vals.Average()).ToString(Inv)).Append(" |");
                    }
                    sb.Append('\n');
                }
                sb.Append('\n');
            }
            sb.Append("| Arm | person-ticks in hunger-or-thirst pain band |\n|---|---|\n");
            foreach (var a in r.Arms)
            {
                long pain = a.PerSeed.Sum(x => x.PainTicksHungerThirst), n = a.PerSeed.Sum(x => x.PersonTicks);
                sb.Append("| ").Append(a.Name).Append(" | ").Append(pain).Append('/').Append(n).Append(" = ").Append((n == 0 ? 0 : (double)pain / n).ToString("0.000", Inv)).Append(" |\n");
            }

            // ---- actions
            sb.Append("\n## Accepted controls by control\n\nTotals over the 20 episodes (per living person-day in brackets).\n\n| Arm |");
            for (int c = 1; c <= 12; c++) sb.Append(' ').Append((Prim)c).Append(" |");
            sb.Append(" decisions |\n|---|"); for (int c = 1; c <= 13; c++) sb.Append("---|"); sb.Append('\n');
            foreach (var a in r.Arms)
            {
                double pdays = a.PerSeed.Sum(x => x.PersonTicks) / (double)Math.Max(1, r.TicksPerDay);
                sb.Append("| ").Append(a.Name).Append(" |");
                for (int c = 1; c <= 12; c++)
                {
                    long n = a.PerSeed.Sum(x => x.Applied != null && x.Applied.Length > c ? x.Applied[c] : 0);
                    sb.Append(' ').Append(n).Append(" (").Append((n / Math.Max(1e-9, pdays)).ToString("0.0", Inv)).Append(") |");
                }
                sb.Append(' ').Append(a.PerSeed.Sum(x => x.Decisions)).Append(" |\n");
            }

            // ---- causes
            sb.Append("\n## Causes of death\n\nRead from the last living tick (thirst at UN1's damage level 900, nutrition below the starvation level, both, else the legacy cause); the legacy body labels every health loss \"starvation\".\n\n| Arm | deaths | causes |\n|---|---|---|\n");
            foreach (var a in r.Arms)
            {
                var dead = a.PerSeed.SelectMany(x => x.People).Where(p => p.DeathTick >= 0).ToList();
                var causes = dead.GroupBy(p => string.IsNullOrEmpty(p.Cause) ? "unknown" : p.Cause).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count());
                sb.Append("| ").Append(a.Name).Append(" | ").Append(dead.Count).Append(" | ").Append(dead.Count == 0 ? "none" : string.Join(", ", causes)).Append(" |\n");
            }
            sb.Append("\nWeights: ");
            sb.Append(string.Join("; ", r.Arms.Select(a => a.Name + " `" + (a.WeightsSha256.Length > 16 ? a.WeightsSha256.Substring(0, 16) : a.WeightsSha256) + "...`")));
            sb.Append(". Rollout time ").Append(r.RolloutSecondsTotal.ToString("0", Inv)).Append(" s.\n");
            return sb.ToString().Replace("\r\n", "\n");
        }

        private static double SurvivalDays(PersonResult p, EvalResults r) => p.DeathTick < 0 ? r.Days : (double)p.DeathTick / r.TicksPerDay;
    }
}
