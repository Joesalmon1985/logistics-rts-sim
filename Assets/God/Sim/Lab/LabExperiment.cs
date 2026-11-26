using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>
    /// One lab experiment preset (STRATEGY_BENCHMARKS 4.6): its id, its reproduction status, its expected results, and a
    /// run that measures them. A run is a pure function of (config, seed).
    /// </summary>
    public abstract class LabExperiment
    {
        public abstract string Id { get; }
        public abstract string Title { get; }
        /// <summary>Book pages (PDF numbering of the research notes).</summary>
        public abstract string Source { get; }
        /// <summary>The overall label (the weakest part's label when parts differ; see <see cref="StatusText"/>).</summary>
        public abstract ReproStatus Status { get; }
        /// <summary>The label as worded in the Reproduction status table, with what is the book's and what is ours.</summary>
        public abstract string StatusText { get; }
        /// <summary>One line: what the checks expect.</summary>
        public abstract string ExpectedSummary { get; }

        protected abstract void Execute(LabConfig cfg, LabResult res, CheckList checks, List<string> notes);

        public LabResult Run(LabConfig cfg)
        {
            if (cfg == null) cfg = new LabConfig();
            var res = new LabResult { Experiment = Id, Seed = cfg.Seed, Status = Status, StatusText = StatusText };
            var checks = new CheckList(Status, Id);
            var notes = new List<string>();
            Execute(cfg, res, checks, notes);
            res.Checks = checks.ToArray();
            res.Notes = notes.ToArray();
            res.ResultHash = LabHash.Of(res);
            return res;
        }
    }

    /// <summary>Builds the checks of one run. Values are micro units (millionths).</summary>
    public sealed class CheckList
    {
        private readonly List<Check> _list = new List<Check>();
        private readonly ReproStatus _status;
        private readonly string _source;

        public CheckList(ReproStatus status, string source) { _status = status; _source = source; }

        public Check[] ToArray() => _list.ToArray();
        public int Count => _list.Count;

        public Check Add(string name, long measuredMicro, string expected, bool pass, CheckKind kind = CheckKind.Check, int decimals = 3, string note = null, ReproStatus? status = null, string source = null)
        {
            var c = new Check
            {
                Name = name, MeasuredMicro = measuredMicro, Expected = expected, Pass = pass, Kind = kind, Decimals = decimals, Note = note,
                Status = status ?? _status, Source = source ?? _source,
            };
            _list.Add(c);
            return c;
        }

        /// <summary>|measured - target| &lt;= tol (all micro).</summary>
        public Check Near(string name, long measuredMicro, long targetMicro, long tolMicro, int decimals = 3, CheckKind kind = CheckKind.Check, string note = null, ReproStatus? status = null, string source = null)
        {
            long d = measuredMicro - targetMicro;
            if (d < 0) d = -d;
            string exp = Fx.FormatScaled(targetMicro / Fx.Pow10(6 - decimals), decimals) + " +/- " + Fx.FormatScaled(tolMicro / Fx.Pow10(6 - decimals), decimals);
            return Add(name, measuredMicro, exp, d <= tolMicro, kind, decimals, note, status, source);
        }

        /// <summary>lo &lt;= measured &lt;= hi (micro).</summary>
        public Check Between(string name, long measuredMicro, long loMicro, long hiMicro, int decimals = 3, CheckKind kind = CheckKind.Check, string note = null, ReproStatus? status = null, string source = null)
        {
            string exp = Fx.FormatScaled(loMicro / Fx.Pow10(6 - decimals), decimals) + " to " + Fx.FormatScaled(hiMicro / Fx.Pow10(6 - decimals), decimals);
            return Add(name, measuredMicro, exp, measuredMicro >= loMicro && measuredMicro <= hiMicro, kind, decimals, note, status, source);
        }

        public Check Above(string name, long measuredMicro, long boundMicro, int decimals = 3, CheckKind kind = CheckKind.Check, string note = null, ReproStatus? status = null, string source = null)
            => Add(name, measuredMicro, "> " + Fx.FormatScaled(boundMicro / Fx.Pow10(6 - decimals), decimals), measuredMicro > boundMicro, kind, decimals, note, status, source);

        public Check Below(string name, long measuredMicro, long boundMicro, int decimals = 3, CheckKind kind = CheckKind.Check, string note = null, ReproStatus? status = null, string source = null)
            => Add(name, measuredMicro, "< " + Fx.FormatScaled(boundMicro / Fx.Pow10(6 - decimals), decimals), measuredMicro < boundMicro, kind, decimals, note, status, source);

        public Check AtLeast(string name, long measuredMicro, long boundMicro, int decimals = 3, CheckKind kind = CheckKind.Check, string note = null, ReproStatus? status = null, string source = null)
            => Add(name, measuredMicro, ">= " + Fx.FormatScaled(boundMicro / Fx.Pow10(6 - decimals), decimals), measuredMicro >= boundMicro, kind, decimals, note, status, source);

        public Check AtMost(string name, long measuredMicro, long boundMicro, int decimals = 3, CheckKind kind = CheckKind.Check, string note = null, ReproStatus? status = null, string source = null)
            => Add(name, measuredMicro, "<= " + Fx.FormatScaled(boundMicro / Fx.Pow10(6 - decimals), decimals), measuredMicro <= boundMicro, kind, decimals, note, status, source);

        /// <summary>A yes/no check; measured is 1 or 0.</summary>
        public Check True(string name, bool ok, string expected, CheckKind kind = CheckKind.Check, string note = null, ReproStatus? status = null, string source = null)
            => Add(name, ok ? 1000000 : 0, expected, ok, kind, 0, note, status, source);

        public Check Report(string name, long measuredMicro, string expected, int decimals = 3, string note = null, ReproStatus? status = null, string source = null)
            => Add(name, measuredMicro, expected, true, CheckKind.Report, decimals, note, status, source);
    }

    /// <summary>The result hash (FNV-1a 64 over every number of the result, in a fixed order).</summary>
    public static class LabHash
    {
        public static ulong Of(LabResult r)
        {
            ulong h = 14695981039346656037UL;
            h = Str(h, r.Experiment);
            h = U64(h, r.Seed);
            h = U64(h, (ulong)r.Status);
            foreach (var s in r.Strategies) h = Str(h, s);
            foreach (var row in r.SharesPerGeneration) foreach (var v in row) h = U64(h, (ulong)v);
            foreach (var row in r.MeanPayoffX1000) foreach (var v in row) h = U64(h, (ulong)v);
            foreach (var c in r.Checks)
            {
                h = Str(h, c.Name);
                h = U64(h, (ulong)c.MeasuredMicro);
                h = Str(h, c.Expected);
                h = U64(h, c.Pass ? 1UL : 0UL);
                h = U64(h, (ulong)c.Kind);
                h = U64(h, (ulong)c.Status);
            }
            return h;
        }

        private static ulong Str(ulong h, string s)
        {
            if (s != null) foreach (char c in s) { h ^= c; h *= 1099511628211UL; }
            h ^= 0xFF; h *= 1099511628211UL;
            return h;
        }

        private static ulong U64(ulong h, ulong v)
        {
            unchecked
            {
                for (int i = 0; i < 8; i++) { h ^= (v >> (8 * i)) & 0xFF; h *= 1099511628211UL; }
            }
            return h;
        }

        public static string Hex(ulong h) => h.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Small helpers shared by the experiments.</summary>
    public static class LabMath
    {
        /// <summary>Q30 → micro.</summary>
        public static long Micro(long fx) => Fx.ToMicro(fx);

        /// <summary>x1000 → micro.</summary>
        public static long MicroX1000(long x1000) => x1000 * 1000;

        public static long MicroRatio(long num, long den) => Fx.MulDiv(1000000, num, den);

        /// <summary>Mean of series[g][i] over generations [from, to] inclusive (Q30).</summary>
        public static long Mean(long[][] series, int i, int from, int to)
        {
            long sum = 0; int n = 0;
            for (int g = from; g <= to && g < series.Length; g++) { sum += series[g][i]; n++; }
            return n > 0 ? sum / n : 0;
        }

        /// <summary>Standard deviation of series[g][i] over [from, to] (Q30).</summary>
        public static long Sd(long[][] series, int i, int from, int to)
        {
            long mean = Mean(series, i, from, to);
            long sum = 0; int n = 0;
            for (int g = from; g <= to && g < series.Length; g++) { long d = series[g][i] - mean; sum += Fx.Mul(d, d); n++; }
            return n > 0 ? Fx.Sqrt(sum / n) : 0;
        }

        /// <summary>First generation at which series[g][i] is below the bound (or -1).</summary>
        public static int FirstBelow(long[][] series, int i, long bound)
        {
            for (int g = 0; g < series.Length; g++) if (series[g][i] < bound) return g;
            return -1;
        }

        public static long[][] Thin(long[][] series, int every)
        {
            if (every <= 1) return series;
            var l = new List<long[]>();
            for (int g = 0; g < series.Length; g += every) l.Add(series[g]);
            if ((series.Length - 1) % every != 0) l.Add(series[series.Length - 1]);
            return l.ToArray();
        }

        /// <summary>The replicator run for a number of generations from x0; returns [gen][i] including gen 0.</summary>
        public static long[][] RunReplicator(Replicator rep, long[] x0, int gens)
        {
            var x = (long[])x0.Clone();
            var series = new long[gens + 1][];
            series[0] = (long[])x.Clone();
            for (int g = 1; g <= gens; g++) { rep.Step(x); series[g] = (long[])x.Clone(); }
            return series;
        }

        /// <summary>Runs the replicator and returns the final shares only.</summary>
        public static long[] Final(Replicator rep, long[] x0, int gens)
        {
            var x = (long[])x0.Clone();
            for (int g = 0; g < gens; g++) rep.Step(x);
            return x;
        }

        public static long[,] Matrix(long[][] rows)
        {
            int k = rows.Length;
            var m = new long[k, k];
            for (int i = 0; i < k; i++) for (int j = 0; j < k; j++) m[i, j] = rows[i][j];
            return m;
        }
    }
}
