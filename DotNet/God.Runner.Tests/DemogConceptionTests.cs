using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Recording;
using God.Runner.Demog;
using God.Sim;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// F2 (owner D4): <c>Households.Life.ConceptionMode</c>. F2-T1: mode 0 (unset or explicit) hashes exactly as the base
    /// (unity/poc 11c3e43, F0 Checkpoint A); F2-T4: a save taken with mode 1 goes on exactly as the unsaved run; F2-T3
    /// ([Explicit], long): completed fertility under mode 1 vs mode 0 over 20 seeds, reported with a Mann-Whitney p, no threshold.
    /// </summary>
    [TestFixture]
    public sealed class DemogConceptionTests
    {
        /// <summary>Final hashes (fold of every state hash part) at tick 12000 (10 days), taken with the base's own runner (11c3e43, 27 Nov 2026).</summary>
        private static readonly Dictionary<(string, ulong), ulong> Base = new Dictionary<(string, ulong), ulong>
        {
            [("m4_families", 1)] = 0x4e097b3731e62136UL, [("m4_families", 2)] = 0x561a1f8ef03ccfc3UL, [("m4_families", 3)] = 0x7d58acabb1163bd5UL,
            [("demog_favourable", 1)] = 0x55a810bf69f22ec1UL, [("demog_favourable", 2)] = 0x08e4eeae3e8ccbc2UL, [("demog_favourable", 3)] = 0xb27453be66bfd56fUL,
        };

        [Test]
        public void ConceptionMode0_IsByteIdentical_F2_T1()
        {
            var keys = Base.Keys.ToList();
            var specs = keys.Select(k => TestUtil.Spec(k.Item1, k.Item2, 12000))
                .Concat(keys.Select(k => TestUtil.Spec(k.Item1, k.Item2, 12000, "tuning.Households.Life.ConceptionMode=0"))).ToList();
            var res = TestUtil.RunAll(specs);
            for (int i = 0; i < res.Length; i++)
            {
                var k = keys[i % keys.Count];
                Assert.AreEqual(0, res[i].ExitCode, res[i].Error);
                Assert.AreEqual(Base[k].ToString("x16"), res[i].FinalHash.ToString("x16"), k + (i < keys.Count ? " unset" : " explicit 0") + ": mode 0 must hash as the base");
            }
        }

        [Test]
        public void SaveLoad_ConceptionMode1_F2_T4()
        {
            var sc = TestUtil.Load("demog_favourable", "tuning.Households.Life.ConceptionMode=1");
            var sim = SimBuilder.Create(sc.ToSetup(2), sc.ToTuning()).Sim;
            var twin = SimBuilder.Create(sc.ToSetup(2), sc.ToTuning()).Sim;
            Assert.AreEqual(1, sim.State.Tuning.Households.Life.ConceptionMode);
            long saveAt = 1200L * 20;
            while (sim.State.Tick < saveAt) { sim.Step(); sim.DrainEvents(); twin.Step(); twin.DrainEvents(); }
            var loaded = SimBuilder.FromSave(SaveFile.FromJson(SaveFile.From(sim).ToJson())).Sim;
            Assert.AreEqual(1, loaded.State.Tuning.Households.Life.ConceptionMode, "the flag survives the save");
            Assert.AreEqual(twin.StateHash(), loaded.StateHash());
            for (int i = 0; i < 1200 * 5; i++)
            {
                loaded.Step(); loaded.DrainEvents(); twin.Step(); twin.DrainEvents();
                if (i % 200 == 0) Assert.AreEqual(twin.StateHash(), loaded.StateHash(), "tick " + i + " after the load");
            }
            Assert.AreEqual(twin.StateHash(), loaded.StateHash());
        }

        [Test, Explicit("F2-T3: 40 runs of 15 sim years (about 25 minutes on 8 cores); a report, not a target")]
        public void ConceptionMode1_RaisesCompletedFertility_F2_T3()
        {
            var rows = new Dictionary<int, List<double>>();
            foreach (int mode in new[] { 0, 1 })
            {
                var sc = TestUtil.Load("demog_favourable", "tuning.Households.Life.ConceptionMode=" + mode);
                sc.MaxTicks = 0; sc.MaxDays = 0; sc.MaxYears = 15;
                var recs = new DemogRecorder[20];
                var specs = new List<RunSpec>();
                for (int k = 0; k < 20; k++)
                {
                    var rec = recs[k] = new DemogRecorder();
                    var spec = new RunSpec { Scenario = sc, Seed = (ulong)(k + 1), Quiet = true, WriteReplay = false, Level = RecordingLevel.Off };
                    spec.ExtraRecorders.Add(() => rec);
                    specs.Add(spec);
                }
                var res = TestUtil.RunAll(specs, 8);
                rows[mode] = Enumerable.Range(0, 20).Select(k => DemogReport.Summarise("m" + mode, (ulong)(k + 1), recs[k], res[k].ExitCode, 0).CompletedFertility).Where(x => !double.IsNaN(x)).ToList();
            }
            double p = MannWhitneyP(rows[0], rows[1]);
            TestContext.WriteLine($"completed fertility: mode 0 {rows[0].Average():F2} (n {rows[0].Count}), mode 1 {rows[1].Average():F2} (n {rows[1].Count}); Mann-Whitney two-sided p {p:F4}");
            Assert.AreEqual(rows[0].Count > 0, true);
        }

        /// <summary>Two-sided Mann-Whitney U p by the normal approximation with tie correction.</summary>
        public static double MannWhitneyP(IList<double> a, IList<double> b)
        {
            var all = a.Select(x => (x, 0)).Concat(b.Select(x => (x, 1))).OrderBy(t => t.x).ToList();
            int n = all.Count; var ranks = new double[n]; double tie = 0;
            for (int i = 0; i < n;)
            {
                int j = i; while (j + 1 < n && all[j + 1].x == all[i].x) j++;
                double r = (i + j) / 2.0 + 1; for (int k = i; k <= j; k++) ranks[k] = r;
                double t = j - i + 1; tie += t * t * t - t; i = j + 1;
            }
            double r1 = 0; for (int i = 0; i < n; i++) if (all[i].Item2 == 0) r1 += ranks[i];
            double n1 = a.Count, n2 = b.Count, u = r1 - n1 * (n1 + 1) / 2, mu = n1 * n2 / 2;
            double sd = Math.Sqrt(n1 * n2 / 12.0 * ((n + 1) - tie / (n * (double)(n - 1))));
            if (sd == 0) return 1;
            double z = (Math.Abs(u - mu) - 0.5) / sd;
            return Math.Min(1, 2 * (1 - Phi(z)));
        }

        private static double Phi(double z)
        {
            // Abramowitz-Stegun 26.2.17
            double t = 1 / (1 + 0.2316419 * Math.Abs(z));
            double d = 0.3989423 * Math.Exp(-z * z / 2);
            double p = d * t * (0.3193815 + t * (-0.3565638 + t * (1.781478 + t * (-1.821256 + t * 1.330274))));
            return z >= 0 ? 1 - p : p;
        }
    }
}
