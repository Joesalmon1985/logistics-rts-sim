using System.Globalization;
using System.Text;
using God.Train.Eval;
using God.Train.Honesty;

namespace God.Train.Report
{
    /// <summary>
    /// The training report (markdown). The verdict is recomputed from the results and the thresholds (never copied from a stored
    /// field), stated first, and the failed measures are named. A self-test or a fallback can never produce a passed verdict.
    /// </summary>
    public static class ReportWriter
    {
        public static string Write(EvalResults r, Thresholds t)
        {
            var v = Verdict.Compute(r, t);
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            if (r.SelfTest) sb.Append("# ").Append(Evaluator.SelfTestLabel).Append("\n\n");
            else sb.Append("# Founder training report\n\n");
            sb.Append("**").Append(v.Headline).Append("**\n\n");
            if (!v.Tr01Passed)
            {
                sb.Append("Failed or missing gate measures: ").Append(string.Join("; ", v.FailedGate)).Append(".\n\n");
            }
            sb.Append("Recommendation: ").Append(v.Recommendation).Append(".\n\n");
            if (v.ReportedNotGate.Count > 0) sb.Append("Reported, not part of the TR-01 gate (R86): ").Append(string.Join("; ", v.ReportedNotGate)).Append(".\n\n");
            sb.Append("- World: ").Append(r.World).Append(r.SelfTest ? " (" + Evaluator.SelfTestLabel + ")" : "").Append('\n');
            sb.Append("- Thresholds file version ").Append(r.ThresholdsVersion).Append(", SHA-256 `").Append(r.ThresholdsHash).Append("`\n");
            sb.Append("- Seeds: ").Append(r.Seeds.Count > 0 ? r.Seeds[0] + "-" + r.Seeds[r.Seeds.Count - 1] : "none").Append(" (").Append(r.Seeds.Count).Append("), held-out layouts in seed order\n");
            sb.Append("- Population ").Append(r.Population).Append(", ").Append(r.Days).Append(" days of ").Append(r.TicksPerDay).Append(" ticks, need mask 0x").Append(r.NeedMask.ToString("X3")).Append(", pain band ").Append(r.PainBand).Append('\n');
            if (r.Invalid) sb.Append("- **INVALID:** ").Append(r.InvalidReason).Append('\n');
            sb.Append("- Rollout time ").Append(r.RolloutSecondsTotal.ToString("0.00", inv)).Append(" s, ").Append(r.TicksPerSecondPerWorker.ToString("0", inv)).Append(" ticks/s per worker\n\n");
            sb.Append("## Measures by arm\n\n| Arm | Brain | ");
            foreach (var m in t.Measures) sb.Append(m.Id).Append(" | ");
            sb.Append("\n|---|---|"); foreach (var _ in t.Measures) sb.Append("---|"); sb.Append('\n');
            foreach (var a in r.Arms)
            {
                sb.Append("| ").Append(a.Name).Append(" | ").Append(a.Brain).Append(" | ");
                foreach (var spec in t.Measures)
                {
                    var m = a.Measures.Find(x => x.Id == spec.Id);
                    if (m == null) sb.Append("not run | ");
                    else if (!m.Evaluable) sb.Append("not evaluable | ");
                    else sb.Append(spec.HasCount ? $"{m.Count}/{m.Of}" : m.Value.ToString("0.000", inv)).Append(m.Met ? " met" : " not met").Append(" | ");
                }
                sb.Append('\n');
            }
            sb.Append('\n');
            foreach (var a in r.Arms)
            {
                sb.Append("### ").Append(a.Name).Append(" (").Append(a.Brain).Append(a.WeightsSha256.Length > 0 ? ", weights " + a.WeightsSha256.Substring(0, 16) : "").Append(")\n\n");
                foreach (var m in a.Measures) if (m.Note.Length > 0) sb.Append("- ").Append(m.Id).Append(": ").Append(m.Note).Append('\n');
                sb.Append("\n| Seed | Layout | Adults alive / start | Pain-band person-ticks | Return | Decisions |\n|---|---|---|---|---|---|\n");
                foreach (var s in a.PerSeed)
                    sb.Append("| ").Append(s.Seed).Append(" | ").Append(s.Layout).Append(" | ").Append(s.AdultsAlive).Append('/').Append(s.Adults0)
                      .Append(" | ").Append(s.PainTicksHungerThirst).Append('/').Append(s.PersonTicks).Append(" | ").Append(s.Return).Append(" | ").Append(s.Decisions).Append(" |\n");
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
