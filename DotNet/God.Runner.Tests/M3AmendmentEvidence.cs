using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>Small statistics for the M3 amendment tests: paired differences with a t-interval, and binomial tails.</summary>
    public static class Stat
    {
        /// <summary>Two-sided 97.5% Student t quantiles for df 1..30 (beyond 30 the normal 1.96 is close enough, and conservative values are used).</summary>
        private static readonly double[] T975 = { 0, 12.706, 4.303, 3.182, 2.776, 2.571, 2.447, 2.365, 2.306, 2.262, 2.228, 2.201, 2.179, 2.160, 2.145, 2.131, 2.120, 2.110, 2.101, 2.093, 2.086, 2.080, 2.074, 2.069, 2.064, 2.060, 2.056, 2.052, 2.048, 2.045, 2.042 };

        public static double TQuantile(int df) => df <= 0 ? double.NaN : df <= 30 ? T975[df] : df <= 40 ? 2.021 : df <= 60 ? 2.000 : df <= 120 ? 1.980 : 1.960;

        /// <summary>Mean and two-sided 95% t-interval of a sample.</summary>
        public static (double mean, double lo, double hi, double sd) Ci95(IReadOnlyList<double> xs)
        {
            int n = xs.Count;
            double m = xs.Average();
            double sd = n > 1 ? Math.Sqrt(xs.Sum(x => (x - m) * (x - m)) / (n - 1)) : 0;
            double h = TQuantile(n - 1) * sd / Math.Sqrt(n);
            return (m, m - h, m + h, sd);
        }

        public static string Fmt((double mean, double lo, double hi, double sd) c, double scale = 1, string unit = "") =>
            $"{c.mean * scale:F1}{unit} [{c.lo * scale:F1}, {c.hi * scale:F1}] (sd {c.sd * scale:F1})";

        /// <summary>P(X >= k) for X ~ Binomial(n, p).</summary>
        public static double BinomAtLeast(int n, double p, int k)
        {
            double s = 0;
            for (int i = k; i <= n; i++) s += Math.Exp(LogChoose(n, i) + i * Math.Log(p) + (n - i) * Math.Log(1 - p));
            return Math.Min(1, s);
        }

        private static double LogChoose(int n, int k)
        {
            double s = 0;
            for (int i = 1; i <= k; i++) s += Math.Log(n - k + i) - Math.Log(i);
            return s;
        }

        /// <summary>One-sided exact sign test: P(at least <paramref name="pos"/> positives of <paramref name="n"/> non-zero pairs | p = 1/2).</summary>
        public static double SignTestP(int pos, int n) => n == 0 ? 1 : BinomAtLeast(n, 0.5, pos);

        public static string Histogram(IEnumerable<double> pct, params int[] edges)
        {
            var a = pct.ToArray();
            var parts = new List<string>();
            for (int i = 0; i < edges.Length; i++) parts.Add($">={edges[i]}%: {a.Count(x => x >= edges[i])}");
            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// The evidence behind the M3 amendments (owner decision D3 section 5). Each test prints the per-seed distribution of one
    /// criterion and its control; nothing here is a gate. Run with <c>--filter Category=M3-evidence</c>.
    /// </summary>
    [Category("M3-evidence")]
    public class M3AmendmentEvidence
    {
        [Test, Explicit("evidence printout, not a gate")]
        public void LS1_PerSeedDistribution()
        {
            foreach (int days in new[] { 5, 10 })
            {
                var on = Watch.Seeds(1, 100, s => LearningScenarioTests.ForagerProbes(s, LearningMode.On, InstinctSet.None, 2, days));
                var fr = Watch.Seeds(1, 100, s => LearningScenarioTests.ForagerProbes(s, LearningMode.Frozen, InstinctSet.None, 2, days));
                var rateOn = on.Select(x => 100.0 * x.Within5 / Math.Max(1, x.Probes)).ToArray();
                var rateFr = fr.Select(x => 100.0 * x.Within5 / Math.Max(1, x.Probes)).ToArray();
                double pooled = Watch.Pct(on.Sum(x => x.Within5), on.Sum(x => x.Probes)), pooledFr = Watch.Pct(fr.Sum(x => x.Within5), fr.Sum(x => x.Probes));
                TestContext.WriteLine($"--- LS1 probes, run of {days} days (probes days 1-{days - 1})");
                TestContext.WriteLine($"probes per seed: min {on.Min(x => x.Probes)}, median {LearningScenarioTests.Median(on.Select(x => x.Probes))}, max {on.Max(x => x.Probes)}");
                TestContext.WriteLine($"learner pooled {pooled:F1}%, frozen pooled {pooledFr:F1}%");
                TestContext.WriteLine("learner per seed: " + Stat.Histogram(rateOn, 100, 95, 90, 80, 70, 50));
                TestContext.WriteLine("frozen  per seed: " + Stat.Histogram(rateFr, 100, 95, 90, 80, 70, 50));
                TestContext.WriteLine("learner per seed rate (mean, 95% CI): " + Stat.Fmt(Stat.Ci95(rateOn)) + "; frozen " + Stat.Fmt(Stat.Ci95(rateFr)));
                var diff = Enumerable.Range(0, 100).Select(i => rateOn[i] - rateFr[i]).ToArray();
                TestContext.WriteLine($"paired learner - frozen, points: {Stat.Fmt(Stat.Ci95(diff))}; learner better in {diff.Count(d => d > 0)}, equal {diff.Count(d => d == 0)}, worse {diff.Count(d => d < 0)}; >= 20 points in {diff.Count(d => d >= 20)}");
                var thinksOn = on.Select(x => x.MeanToEat).ToArray(); var thinksFr = fr.Select(x => x.MeanToEat).ToArray();
                TestContext.WriteLine($"mean thinks to eat: learner {Stat.Fmt(Stat.Ci95(thinksOn))}, frozen {Stat.Fmt(Stat.Ci95(thinksFr))}");
                TestContext.WriteLine("by day, learner: " + string.Join(" ", Enumerable.Range(1, days - 1).Select(d => $"d{d} {Watch.Pct(on.Sum(x => x.ByDayWithin[d]), on.Sum(x => x.ByDayProbes[d])):F1}% ({on.Sum(x => x.ByDayProbes[d])})")));
                TestContext.WriteLine("by day, frozen:  " + string.Join(" ", Enumerable.Range(1, days - 1).Select(d => $"d{d} {Watch.Pct(fr.Sum(x => x.ByDayWithin[d]), fr.Sum(x => x.ByDayProbes[d])):F1}%")));
                // what a homogeneous learner at the pooled rate would give per seed (sampling alone)
                double p = pooled / 100.0;
                double expected95 = on.Sum(x => x.Probes == 0 ? 0 : Stat.BinomAtLeast(x.Probes, p, (int)Math.Ceiling(0.95 * x.Probes)));
                double expected95at95 = on.Sum(x => x.Probes == 0 ? 0 : Stat.BinomAtLeast(x.Probes, 0.95, (int)Math.Ceiling(0.95 * x.Probes)));
                TestContext.WriteLine($"sampling alone: a learner at exactly {pooled:F1}% on every seed would put {expected95:F1} seeds at >= 95% (observed {rateOn.Count(r => r >= 95)}); a learner at exactly 95% would put only {expected95at95:F1} of 100 there");
            }
        }

        /// <summary>What the learner does instead on the probes it misses (days 3-9 of a 10-day run).</summary>
        [Test, Explicit("evidence printout, not a gate")]
        public void LS1_MissedProbes()
        {
            var all = Watch.Seeds(1, 100, s =>
            {
                var sim = LearningFixtures.Empty(s);
                int glade = LearningFixtures.Glade(sim);
                var npc = LearningFixtures.Npc(sim, glade, InstinctSet.None, LearningMode.On, hunger: 600);
                LearningFixtures.Bush(sim, SourceKind.Berries, LearningFixtures.Step(sim, glade, 0, 2));
                var w = new Watch(sim, npc.Id);
                int tpd = w.TicksPerDay;
                var probeAt = new List<long>();
                w.RunDays(10, tick =>
                {
                    int tod = (int)(tick % tpd);
                    if (tick / tpd >= 1 && (tod == 100 || tod == 400 || tod == 700 || tod == 1000) && npc.Alive && !npc.Action.Asleep)
                    { npc.Drives[Drive.Hunger] = 600000; npc.RewardSnap[Drive.Hunger] = 600000; probeAt.Add(tick); }
                });
                var rows = w.Rows[npc.Id];
                var missed = new List<string>();
                foreach (long p in probeAt.Where(p => p / tpd >= 3))
                {
                    var after = rows.Where(x => x.Tick > p).Take(5).ToList();
                    if (after.Count == 0 || after.Any(x => x.Verb == Verb.Eat)) continue;
                    var f = after[0];
                    missed.Add($"{VerbTable.Name(f.Verb)}{(f.Attends(ObjClass.BerryBush) ? "" : "(bush unseen)")} fat{f.Fatigue / 100} tod{p % tpd}");
                }
                return missed;
            }).SelectMany(x => x).ToList();
            TestContext.WriteLine($"missed probes days 3-9: {all.Count}; first choice after the probe: " + string.Join(", ", all.Select(m => m.Split(' ')[0]).GroupBy(x => x).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}")));
            TestContext.WriteLine("fatigue band (hundreds) at the miss: " + string.Join(", ", all.Select(m => m.Split(' ')[1]).GroupBy(x => x).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}")));
            TestContext.WriteLine("time of day of the miss: " + string.Join(", ", all.Select(m => m.Split(' ')[2]).GroupBy(x => x).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}")));
        }

        /// <summary>Where the learner's last misses come from: the same probes with the random exploration amplitude at 0.</summary>
        [Test, Explicit("evidence printout, not a gate")]
        public void LS1_WithoutExplorationNoise()
        {
            foreach (int eb in new[] { 150, 0 })
            {
                var on = Watch.Seeds(1, 100, s => LearningScenarioTests.ForagerProbes(s, LearningMode.On, InstinctSet.None, 2, 10, t => t.Learning.ExploreBase = eb));
                var fr = Watch.Seeds(1, 100, s => LearningScenarioTests.ForagerProbes(s, LearningMode.Frozen, InstinctSet.None, 2, 10, t => t.Learning.ExploreBase = eb));
                TestContext.WriteLine($"ExploreBase {eb}: learner {Watch.Pct(on.Sum(x => x.Within5), on.Sum(x => x.Probes)):F1}% of probes (days 5-9: {Watch.Pct(on.Sum(x => x.ByDayWithin.Skip(5).Take(5).Sum()), on.Sum(x => x.ByDayProbes.Skip(5).Take(5).Sum())):F1}%), frozen {Watch.Pct(fr.Sum(x => x.Within5), fr.Sum(x => x.Probes)):F1}%; learner eats when sated (days 2-9, away from probes) {Watch.Pct(on.Sum(x => x.FullEats), on.Sum(x => x.FullThinks)):F1}%");
            }
        }

        [Test, Explicit("evidence printout, not a gate")]
        public void L2_PerSeedDistribution()
        {
            var on = Watch.Seeds(1, 100, s => LearningScenarioTests.Natural(s, LearningMode.On));
            var fr = Watch.Seeds(1, 100, s => LearningScenarioTests.Natural(s, LearningMode.Frozen));
            var pOn = Watch.Seeds(1, 100, s => LearningScenarioTests.ForagerProbes(s, LearningMode.On, InstinctSet.None, 2, 5));
            var pFr = Watch.Seeds(1, 100, s => LearningScenarioTests.ForagerProbes(s, LearningMode.Frozen, InstinctSet.None, 2, 5));
            double full = Watch.Pct(on.Sum(x => x.FullEats), on.Sum(x => x.FullThinks)), fullFr = Watch.Pct(fr.Sum(x => x.FullEats), fr.Sum(x => x.FullThinks));
            double hungry = Watch.Pct(on.Sum(x => x.HungryEats), on.Sum(x => x.HungryThinks)), hungryFr = Watch.Pct(fr.Sum(x => x.HungryEats), fr.Sum(x => x.HungryThinks));
            TestContext.WriteLine($"L2 natural days 2-4: eats when full {full:F1}% of {on.Sum(x => x.FullThinks)} thinks (frozen {fullFr:F1}% of {fr.Sum(x => x.FullThinks)}); eats when hungry {hungry:F1}% (frozen {hungryFr:F1}%)");
            TestContext.WriteLine($"full thinks per seed: learner median {LearningScenarioTests.Median(on.Select(x => x.FullThinks))}, frozen median {LearningScenarioTests.Median(fr.Select(x => x.FullThinks))}");
            var idx = Enumerable.Range(0, 100).Where(i => on[i].FullThinks > 0 && fr[i].FullThinks > 0).ToArray();
            var rOn = idx.Select(i => 100.0 * on[i].FullEats / on[i].FullThinks).ToArray();
            var rFr = idx.Select(i => 100.0 * fr[i].FullEats / fr[i].FullThinks).ToArray();
            TestContext.WriteLine($"per seed full-eat rate ({idx.Length} seeds with full thinks in both): learner {Stat.Fmt(Stat.Ci95(rOn))}, frozen {Stat.Fmt(Stat.Ci95(rFr))}");
            var d = idx.Select((_, k) => rOn[k] - rFr[k]).ToArray();
            int neg = d.Count(x => x < 0), pos = d.Count(x => x > 0);
            TestContext.WriteLine($"paired learner - frozen: {Stat.Fmt(Stat.Ci95(d))} points; learner lower in {neg}, higher in {pos}; sign test p = {Stat.SignTestP(neg, neg + pos):G3}");
            TestContext.WriteLine("learner per seed <= 15%: " + rOn.Count(x => x <= 15) + ", <= 20%: " + rOn.Count(x => x <= 20) + "; frozen <= 20%: " + rFr.Count(x => x <= 20));
            // context sensitivity: the hungry minus full contrast, per seed
            var cOn = Enumerable.Range(0, 100).Where(i => on[i].FullThinks > 0 && on[i].HungryThinks > 0).Select(i => 100.0 * on[i].HungryEats / on[i].HungryThinks - 100.0 * on[i].FullEats / on[i].FullThinks).ToArray();
            var cFr = Enumerable.Range(0, 100).Where(i => fr[i].FullThinks > 0 && fr[i].HungryThinks > 0).Select(i => 100.0 * fr[i].HungryEats / fr[i].HungryThinks - 100.0 * fr[i].FullEats / fr[i].FullThinks).ToArray();
            TestContext.WriteLine($"hungry minus full eat rate per seed: learner {Stat.Fmt(Stat.Ci95(cOn))} ({cOn.Length} seeds), frozen {Stat.Fmt(Stat.Ci95(cFr))}");
            double pf = Watch.Pct(pOn.Sum(x => x.FullEats), pOn.Sum(x => x.FullThinks)), pff = Watch.Pct(pFr.Sum(x => x.FullEats), pFr.Sum(x => x.FullThinks));
            TestContext.WriteLine($"in the probe run (days 2-4 away from probes): learner eats when full {pf:F1}% (frozen {pff:F1}%); learner's full eats from the carry {pOn.Sum(x => x.FullEatCarry)}, at the bush {pOn.Sum(x => x.FullEatBush)}");
        }

        [Test, Explicit("evidence printout, not a gate")]
        public void L2_LongerNaturalRun()
        {
            var on = Watch.Seeds(1, 100, s => LearningScenarioTests.Natural(s, LearningMode.On, 12));
            var fr = Watch.Seeds(1, 100, s => LearningScenarioTests.Natural(s, LearningMode.Frozen, 12));
            TestContext.WriteLine("L2 natural run of 12 days, sated eat rate by day (0-based), learner: " + string.Join(" ", Enumerable.Range(2, 10).Select(d => $"d{d} {Watch.Pct(on.Sum(x => x.FullEatsByDay[d]), on.Sum(x => x.FullByDay[d])):F1}%")));
            TestContext.WriteLine("frozen: " + string.Join(" ", Enumerable.Range(2, 10).Select(d => $"d{d} {Watch.Pct(fr.Sum(x => x.FullEatsByDay[d]), fr.Sum(x => x.FullByDay[d])):F1}%")));
            foreach (var (a, b) in new[] { (2, 4), (5, 9), (7, 11) })
            {
                double Rate(LearningScenarioTests.NaturalResult x) { int n = 0, e = 0; for (int d = a; d <= b; d++) { n += x.FullByDay[d]; e += x.FullEatsByDay[d]; } return n == 0 ? double.NaN : 100.0 * e / n; }
                var rOn = on.Select(Rate).ToArray(); var rFr = fr.Select(Rate).ToArray();
                var diff = Enumerable.Range(0, 100).Select(i => rOn[i] - rFr[i]).ToArray();
                TestContext.WriteLine($"days {a}-{b}: learner pooled {Watch.Pct(on.Sum(x => Enumerable.Range(a, b - a + 1).Sum(d => x.FullEatsByDay[d])), on.Sum(x => Enumerable.Range(a, b - a + 1).Sum(d => x.FullByDay[d]))):F1}%, per seed {Stat.Fmt(Stat.Ci95(rOn))}, <= 15% in {rOn.Count(r => r <= 15)}; frozen {Stat.Fmt(Stat.Ci95(rFr))}; paired {Stat.Fmt(Stat.Ci95(diff))}, learner lower in {diff.Count(x => x < 0)}");
            }
        }

        /// <summary>Why the probed learner eats when sated: the learned term on its sated eating choices, by day.</summary>
        [Test, Explicit("evidence printout, not a gate")]
        public void L2_SatedEatingInTheProbeRun()
        {
            foreach (var mode in new[] { LearningMode.On, LearningMode.Frozen })
            foreach (bool probes in new[] { false, true })
            {
                var rows = Watch.Seeds(1, 100, s =>
                {
                    var sim = LearningFixtures.Empty(s);
                    int glade = LearningFixtures.Glade(sim);
                    var npc = LearningFixtures.Npc(sim, glade, InstinctSet.None, mode, hunger: 600);
                    LearningFixtures.Bush(sim, SourceKind.Berries, LearningFixtures.Step(sim, glade, 0, 2));
                    var w = new Watch(sim, npc.Id);
                    int tpd = w.TicksPerDay;
                    var probeAt = new List<long>();
                    w.RunDays(5, tick =>
                    {
                        int tod = (int)(tick % tpd);
                        if (probes && tick / tpd >= 1 && (tod == 100 || tod == 400 || tod == 700 || tod == 1000) && npc.Alive && !npc.Action.Asleep)
                        { npc.Drives[Drive.Hunger] = 600000; npc.RewardSnap[Drive.Hunger] = 600000; probeAt.Add(tick); }
                    });
                    int comfort = sim.State.Tuning.Population.ComfortHunger;
                    return w.Rows[npc.Id].Where(t => t.Day >= 2 && t.Attends(ObjClass.BerryBush) && t.Hunger <= comfort && !probeAt.Any(p => t.Tick > p && t.Tick - p < 150)).ToList();
                }).SelectMany(x => x).ToList();
                var eats = rows.Where(r => r.Verb == Verb.Eat).ToList();
                TestContext.WriteLine($"{mode}, probes {(probes ? "on" : "off")}: sated thinks {rows.Count}, eats {Watch.Pct(eats.Count, rows.Count):F1}%; by day " + string.Join(" ", Enumerable.Range(2, 3).Select(d => $"d{d} {Watch.Pct(rows.Count(r => r.Day == d && r.Verb == Verb.Eat), rows.Count(r => r.Day == d)):F1}%"))
                    + $"; on sated eats: mean learned term {(eats.Count == 0 ? 0 : eats.Average(r => r.Learned)):F0}, learning changed the winner in {Watch.Pct(eats.Count(r => r.LearnedChangedWinner), eats.Count):F1}%, mean hunger {(eats.Count == 0 ? 0 : eats.Average(r => r.Hunger)):F0}; all sated thinks mean hunger {rows.Average(r => r.Hunger):F0}");
            }
        }

        [Test, Explicit("evidence printout, not a gate")]
        public void L3_FillerChoiceAndValues()
        {
            foreach (var (name, shift, mode) in new[] { ("traces on", 2, LearningMode.On), ("traces off (TraceShift 16)", 16, LearningMode.On), ("frozen", 2, LearningMode.Frozen) })
            {
                var r = Watch.Seeds(1, 100, s => LearningScenarioTests2.DelayedRewardRun(s, shift, 200, mode));
                var aShare = r.Select(x => x.ALast50 * 100).ToArray();
                var fShare = r.Select(x => 100.0 * x.Filler33Last50 / Math.Max(1, x.FillerLast50)).ToArray();
                TestContext.WriteLine($"--- L3 {name}: TestA in the last 50 trials {Stat.Fmt(Stat.Ci95(aShare))}%; seeds >= 90%: {aShare.Count(x => x >= 90)}");
                TestContext.WriteLine($"  learned values (vp): TestA {r.Average(x => x.QA):F0}, TestB {r.Average(x => x.QB):F0}, filler 33 {r.Average(x => x.Q33):F0}, filler 34 {r.Average(x => x.Q34):F0}; |Q33 - Q34| mean {r.Average(x => Math.Abs(x.Q33 - x.Q34)):F0}, max {r.Max(x => Math.Abs(x.Q33 - x.Q34))}");
                TestContext.WriteLine($"  filler class 33 share of the last 100 filler choices: {Stat.Fmt(Stat.Ci95(fShare))}%; per seed within 50 +- 15: {fShare.Count(x => Math.Abs(x - 50) <= 15)}; min {fShare.Min():F0}, max {fShare.Max():F0}");
                TestContext.WriteLine($"  filler mean Q {r.Average(x => x.FillerQ):F0} vp (the published criterion: within +-100)");
            }
        }

        [Test, Explicit("evidence printout, not a gate")]
        public void L5_PlaceValueByDay40()
        {
            foreach (var (name, tune) in new (string, Action<SimTuning>)[] { ("default", null), ("decay off (DecayScalePermille 0)", t => t.Learning.DecayScalePermille = 0) })
            {
                var r = Watch.Seeds(1, 40, s => LearningScenarioTests2.Moves(s, false, 40, tune));
                int a = r.Sum(x => x.ToA[8] + x.ToA[9] + x.ToA[10]), b = r.Sum(x => x.ToB[8] + x.ToB[9] + x.ToB[10]);
                TestContext.WriteLine($"--- L5 {name}: days 8-10 trips to A {Watch.Pct(a, a + b):F1}%; deaths {r.Count(x => x.Died)}");
                TestContext.WriteLine("  per seed: day-5 sum of positive Ltw on PlaceRef(A) -> day 40 (max Ltw, gone): " + string.Join("; ", r.Select((x, i) => $"{i + 1}:{x.ASumLtwDay5}->{x.ASumLtwEnd} ({x.AMaxLtw}{(x.AEntriesGone ? ",gone" : "")})")));
                TestContext.WriteLine($"  gone (Ltw <= 0 or evicted) {r.Count(x => x.AEntriesGone)} of 40; <= 32 or gone {r.Count(x => x.AEntriesGone || x.ASumLtwEnd <= 32)}; day-40 value <= 1/4 of day 5 or gone {r.Count(x => x.AEntriesGone || 4 * x.ASumLtwEnd <= x.ASumLtwDay5)}; trips to A after day 7: {r.Sum(x => x.ToA.Skip(8).Sum())}");
                var frac = r.Where(x => x.ASumLtwDay5 > 0).Select(x => (double)x.ASumLtwEnd / x.ASumLtwDay5).ToArray();
                TestContext.WriteLine($"  day-40 / day-5 value: {Stat.Fmt(Stat.Ci95(frac), 100, "%")}");
            }
        }

        [Test, Explicit("evidence printout, not a gate")]
        public void L5_L6_Controls()
        {
            foreach (var mode in new[] { LearningMode.On, LearningMode.Frozen, LearningMode.Off })
            {
                var l5 = Watch.Seeds(1, 40, s => LearningScenarioTests2.Moves(s, false, 12, null, mode));
                var l6 = Watch.Seeds(1, 40, s => LearningScenarioTests2.Moves(s, true, 16, null, mode));
                int a = l5.Sum(x => x.ToA[8] + x.ToA[9] + x.ToA[10]), b = l5.Sum(x => x.ToB[8] + x.ToB[9] + x.ToB[10]);
                int a6 = l6.Sum(x => x.ToA[13] + x.ToA[14] + x.ToA[15]), b6 = l6.Sum(x => x.ToB[13] + x.ToB[14] + x.ToB[15]);
                int a67 = l5.Sum(x => x.ToA[6] + x.ToA[7]), b67 = l5.Sum(x => x.ToB[6] + x.ToB[7]);
                TestContext.WriteLine($"{mode}: L5 days 8-10 trips to A {Watch.Pct(a, a + b):F1}% ({a}/{a + b}); days 6-7 {Watch.Pct(a67, a67 + b67):F1}% ({a67}/{a67 + b67}); L6 days 13-15 to A {Watch.Pct(a6, a6 + b6):F1}% ({a6}/{a6 + b6}); deaths {l5.Count(x => x.Died)}/{l6.Count(x => x.Died)}");
                TestContext.WriteLine("  L5 by day A/B: " + string.Join(" ", Enumerable.Range(0, 12).Select(d => d + ":" + l5.Sum(x => x.ToA[d]) + "/" + l5.Sum(x => x.ToB[d]))));
            }
        }

        [Test, Explicit("evidence printout, not a gate")]
        public void L8_PerPairDistribution()
        {
            var rich = Watch.Seeds(1, 50, s => LearningScenarioTests2.Nurture(s, false));
            var bitter = Watch.Seeds(1, 50, s => LearningScenarioTests2.Nurture(s, true));
            var d = Enumerable.Range(0, 50).Select(i => 100 * (rich[i].shadeShare - bitter[i].shadeShare)).ToArray();
            TestContext.WriteLine($"L8 published measure (first 6 meal choices from day 5, learning on): difference {Stat.Fmt(Stat.Ci95(d))} points; >= 30 in {d.Count(x => x >= 30)}, > 0 in {d.Count(x => x > 0)}, = 0 in {d.Count(x => x == 0)}, < 0 in {d.Count(x => x < 0)}");
            TestContext.WriteLine("  per pair (rich/bitter nightshade share, %): " + string.Join(" ", Enumerable.Range(0, 50).Select(i => $"{i + 1}:{rich[i].shadeShare * 100:F0}/{bitter[i].shadeShare * 100:F0}")));
            foreach (var (name, nurture) in new[] { ("learned in nurture, frozen for the test", LearningMode.On), ("control: frozen from birth", LearningMode.Frozen) })
            {
                var r2 = Watch.Seeds(1, 50, s => LearningScenarioTests2.NurtureProbe(s, false, nurture));
                var b2 = Watch.Seeds(1, 50, s => LearningScenarioTests2.NurtureProbe(s, true, nurture));
                var d2 = Enumerable.Range(0, 50).Select(i => 100 * (r2[i].ShadeShare - b2[i].ShadeShare)).ToArray();
                TestContext.WriteLine($"--- L8 {name}: meal choices per NPC min {Math.Min(r2.Min(x => x.Choices), b2.Min(x => x.Choices))}, median {LearningScenarioTests.Median(r2.Select(x => x.Choices).Concat(b2.Select(x => x.Choices)))}; rich {r2.Average(x => x.ShadeShare):P1}, bitter {b2.Average(x => x.ShadeShare):P1}; deaths {r2.Count(x => x.Died) + b2.Count(x => x.Died)}");
                TestContext.WriteLine($"  difference {Stat.Fmt(Stat.Ci95(d2))} points; >= 30 in {d2.Count(x => x >= 30)}, >= 20 in {d2.Count(x => x >= 20)}, > 0 in {d2.Count(x => x > 0)}, < 0 in {d2.Count(x => x < 0)}");
            }
        }

        [Test, Explicit("evidence printout, not a gate")]
        public void L13_FirstFishChoice()
        {
            foreach (var (name, edible, mode) in new[] { ("raised on berries", true, LearningMode.On), ("control: berries never edible", false, LearningMode.On), ("control: raised on berries, frozen from birth", true, LearningMode.Frozen) })
            {
                var r = Watch.Seeds(1, 100, s => LearningScenarioTests2.GeneraliseDetail(s, edible, mode));
                var fo = r.Where(x => x.FirstVerbOnFish == Verb.ForageAt && x.FirstMeal > 0 && x.FirstFoodAction > 0).Select(x => x.FirstMeal - x.FirstFoodAction).ToArray();
                if (fo.Length > 0) TestContext.WriteLine($"  [{name}] seeds whose first act on the fish was ForageAt: {fo.Length}; thinks from that ForageAt to the first fish meal: " + string.Join(" ", fo));
                TestContext.WriteLine($"--- L13 {name}");
                TestContext.WriteLine($"  first Eat at the fish spot within 3 attended thinks (published count): {r.Count(x => x.EatSpotWithin3Attended)}");
                TestContext.WriteLine($"  first food action on the fish (Eat at the spot or ForageAt it) within 3 thinks: {r.Count(x => x.FirstFoodAction > 0 && x.FirstFoodAction <= 3)}; within 6: {r.Count(x => x.FirstFoodAction > 0 && x.FirstFoodAction <= 6)}");
                TestContext.WriteLine($"  first fish meal (from the spot or the carry) within 3 thinks {r.Count(x => x.FirstMeal > 0 && x.FirstMeal <= 3)}, 6 thinks {r.Count(x => x.FirstMeal > 0 && x.FirstMeal <= 6)}, ever {r.Count(x => x.FirstMeal > 0)}");
                TestContext.WriteLine("  first verb on the fish: " + string.Join(", ", r.GroupBy(x => x.FirstVerbOnFish).OrderByDescending(g => g.Count()).Select(g => $"{VerbTable.Name(g.Key)} {g.Count()}")));
                TestContext.WriteLine("  thinks to first food action: " + string.Join(" ", r.Select(x => x.FirstFoodAction)));
            }
        }
    }
}
