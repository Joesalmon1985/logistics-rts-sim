using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using God.Sim;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M4.27 and M4.33 (owner D2 §3): multigenerational demographic runs, reported and never asserted on survival (Hyp).
    /// Each run asserts only what must hold everywhere: no invariant failure, and population change reconciling exactly
    /// (start + births + arrivals - deaths - departures = end). Nothing is tuned to prevent collapse. Results go to
    /// <c>M4_SWEEP_OUT</c> (a CSV and a text summary) and the test output. Explicit: long (run in the background).
    /// </summary>
    [TestFixture, Category("M4-sweep"), Explicit("long demographic runs (M4.27, M4.33): run in the background")]
    public sealed class M4Sweeps
    {
        private static string Out => Environment.GetEnvironmentVariable("M4_SWEEP_OUT") ?? Path.Combine(Path.GetTempPath(), "m4_sweep");

        private sealed class Arm
        {
            public string Name; public int PerFaction; public int Fertility; public DiseaseSetting Disease; public int LifeScale; public int Days; public int Seeds;
        }

        private static readonly string[] Causes = { "none", "starvation", "wound", "disease", "old age", "childbirth", "exposure", "violence", "accident" };

        private static (M4Run r, Arm a) RunArm(Arm a, ulong seed) =>
            (M4Run.FromScenario("m4_band_6x20", seed, a.Days, t =>
            {
                // fertility: low, default, high daily conception at best (permille), the only change between those arms
                if (a.Fertility == 0) { t.Households.Life.ConceiveBasePermille = 200; t.Households.Life.ConceiveSpanPermille = 150; }
                if (a.Fertility == 2) { t.Households.Life.ConceiveBasePermille = 550; t.Households.Life.ConceiveSpanPermille = 400; }
            }, g => { g.StartNpcsPerFaction = a.PerFaction; g.Disease = a.Disease; g.LifeScale = a.LifeScale; }), a);

        private static void Report(string name, List<(M4Run r, Arm a)> runs)
        {
            Directory.CreateDirectory(Out);
            var csv = new StringBuilder("arm,seed,per_faction,fertility,disease,life_scale,days,start,births,arrivals,deaths,departures,end,reconciles,max_generation,"
                + string.Join(",", Causes.Skip(1).Select(c => "deaths_" + c.Replace(' ', '_'))) + ",child_deaths,extinct_factions,extinct_days,infections,fed,partnerships,invariants,pyramid\n");
            var txt = new StringBuilder();
            foreach (var g in runs.GroupBy(x => x.a.Name))
            {
                var rs = g.Select(x => x.r).ToList();
                var a = g.First().a;
                foreach (var r in rs)
                {
                    int extinct = Enumerable.Range(0, 6).Count(f => r.FactionExtinct(f));
                    csv.Append($"{a.Name},{r.Seed},{a.PerFaction},{a.Fertility},{a.Disease},{a.LifeScale},{a.Days},{r.Start},{r.Births},{r.Arrivals},{r.Deaths},{r.Departures},{r.End},"
                        + $"{(r.Start + r.Births + r.Arrivals - r.Deaths - r.Departures == r.End ? 1 : 0)},{r.MaxGeneration},"
                        + string.Join(",", Enumerable.Range(1, 8).Select(i => r.DeathsByCause[i])) + $",{r.ChildDeaths},{extinct},"
                        + string.Join(";", Enumerable.Range(0, 6).Where(f => r.FactionExtinct(f)).Select(f => "F" + f + "@" + r.FactionExtinctDay[f]))
                        + $",{r.Infections},{r.ChildFed},{r.PartnershipsFormed},{r.Invariants}," + string.Join(" ", r.Pyramid) + "\n");
                }
                double Mean(Func<M4Run, double> f) => rs.Average(f);
                double Ci(Func<M4Run, double> f) { if (rs.Count < 2) return 0; double m = rs.Average(f); double sd = Math.Sqrt(rs.Sum(x => (f(x) - m) * (f(x) - m)) / (rs.Count - 1)); return 1.96 * sd / Math.Sqrt(rs.Count); }
                int extinctRuns = rs.Count(r => r.End == 0);
                int extinctFactions = rs.Sum(r => Enumerable.Range(0, 6).Count(f => r.FactionExtinct(f)));
                txt.AppendLine($"{a.Name} ({rs.Count} seeds x {a.Days} days; {a.PerFaction}/faction, fertility {new[] { "low", "default", "high" }[a.Fertility]}, disease {a.Disease}, LifeScale {a.LifeScale}): "
                    + $"end {Mean(r => r.End):F1} +- {Ci(r => r.End):F1} of start {Mean(r => r.Start):F0}; births {Mean(r => r.Births):F1} +- {Ci(r => r.Births):F1}; deaths {Mean(r => r.Deaths):F1} "
                    + "(" + string.Join(", ", Enumerable.Range(1, 8).Where(i => rs.Sum(r => r.DeathsByCause[i]) > 0).Select(i => Causes[i] + " " + rs.Average(r => r.DeathsByCause[i]).ToString("F1"))) + "); "
                    + $"arrivals {Mean(r => r.Arrivals):F1}; max generation {Mean(r => r.MaxGeneration):F1}; whole-island extinctions {extinctRuns}/{rs.Count}; faction extinctions {extinctFactions}; "
                    + $"reconcile {rs.Count(r => r.Start + r.Births + r.Arrivals - r.Deaths - r.Departures == r.End)}/{rs.Count}; invariant failures {rs.Sum(r => r.Invariants)}");
                txt.AppendLine("   age pyramid at the end (0.25-year bins, summed): " + string.Join(" ", Enumerable.Range(0, 17).Select(b => rs.Sum(r => r.Pyramid[b]))));
            }
            File.WriteAllText(Path.Combine(Out, name + ".csv"), csv.ToString());
            File.WriteAllText(Path.Combine(Out, name + ".txt"), txt.ToString());
            TestContext.WriteLine(txt.ToString());
            foreach (var (r, a) in runs)
            {
                Assert.AreEqual(0, r.Invariants, a.Name + " seed " + r.Seed + ": " + string.Join("; ", r.InvariantSample));
                Assert.AreEqual(r.End, r.Start + r.Births + r.Arrivals - r.Deaths - r.Departures, a.Name + " seed " + r.Seed + ": population change reconciles exactly");
            }
        }

        private static List<(M4Run, Arm)> Run(IEnumerable<Arm> arms)
        {
            var jobs = arms.SelectMany(a => Enumerable.Range(1, a.Seeds).Select(s => (a, (ulong)s))).ToList();
            var res = new (M4Run, Arm)[jobs.Count];
            System.Threading.Tasks.Parallel.For(0, jobs.Count, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i => res[i] = RunArm(jobs[i].a, jobs[i].Item2));
            return res.ToList();
        }

        /// <summary>M4.27: the owner's default (m4_band) at 20 per faction on 20 seeds, 10 and 40 per faction on 10 seeds each, 15 sim years.</summary>
        [Test]
        public void Default_6xN_Generations_15Years_M4_27()
        {
            var arms = new[]
            {
                new Arm { Name = "6x20", PerFaction = 20, Fertility = 1, Disease = DiseaseSetting.On, LifeScale = 100, Days = 150, Seeds = 20 },
                new Arm { Name = "6x10", PerFaction = 10, Fertility = 1, Disease = DiseaseSetting.On, LifeScale = 100, Days = 150, Seeds = 10 },
                new Arm { Name = "6x40", PerFaction = 40, Fertility = 1, Disease = DiseaseSetting.On, LifeScale = 100, Days = 150, Seeds = 10 },
            };
            Report("m4_27_default", Run(arms));
        }

        /// <summary>
        /// M4.33: the sweep, sampled (not the full cross-product): every start size x disease x fertility at LifeScale 100
        /// (27 arms), and LifeScale 50 and 200 at default fertility for every start size with disease On (6 arms); 3 seeds per arm,
        /// 10 sim years.
        /// </summary>
        [Test]
        public void DemographicSweep_Sampled_M4_33()
        {
            var arms = new List<Arm>();
            foreach (int n in new[] { 10, 20, 40 })
                foreach (var d in new[] { DiseaseSetting.Off, DiseaseSetting.Mild, DiseaseSetting.On })
                    foreach (int f in new[] { 0, 1, 2 })
                        arms.Add(new Arm { Name = $"n{n}_{d}_f{f}_ls100", PerFaction = n, Fertility = f, Disease = d, LifeScale = 100, Days = 100, Seeds = 3 });
            foreach (int n in new[] { 10, 20, 40 })
                foreach (int ls in new[] { 50, 200 })
                    arms.Add(new Arm { Name = $"n{n}_On_f1_ls{ls}", PerFaction = n, Fertility = 1, Disease = DiseaseSetting.On, LifeScale = ls, Days = 100, Seeds = 3 });
            Report("m4_33_sweep", Run(arms));
        }
    }
}
