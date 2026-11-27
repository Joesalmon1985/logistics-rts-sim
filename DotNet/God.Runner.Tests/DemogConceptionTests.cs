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
    /// F2 (owner D4): <c>Households.Life.ConceptionMode</c>, default 1 since the Checkpoint B re-pin (R60). F2-T1: an explicit
    /// mode 0 reproduces the pre-re-pin base (unity/poc 11c3e43, F0 Checkpoint A) in every part except core; F2-T4: a save taken with mode 1 goes on exactly as the unsaved run; F2-T3
    /// ([Explicit], long): completed fertility under mode 1 vs mode 0 over 20 seeds of demog_f2_favourable, reported with a Mann-Whitney p, no threshold.
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

        /// <summary>
        /// Checkpoint B re-pin (LEAD_RULINGS R60): the parts at tick 12000 of the same base runs (flag unset meant mode 0 then),
        /// read from the replays of the unmodified Checkpoint B merge runner (unity/poc 148b901, whose final hashes equal
        /// <see cref="Base"/>). Part order is <c>GodSim.AllHashPartNames</c>.
        /// </summary>
        private static readonly Dictionary<(string, ulong), ulong[]> BaseParts = new Dictionary<(string, ulong), ulong[]>
        {
            [("demog_favourable", 1)] = new[] { 0xfe86d9af4733df05UL, 0x2b4e088e489c75ceUL, 0xc5392b61535cf8ecUL, 0x6239e5a66a1fc12dUL, 0xb678ae3e82c76bc3UL, 0x763445bdfcb87c55UL, 0xa8c7f832281a39c5UL, 0xb6bf3e4e7c70e96aUL, 0xbdc14245f0a8d3d7UL, 0x8e541fd5ae58c2b9UL, 0xd041ca79149269b0UL, 0x8c57530b81c19889UL }, // final 55a810bf69f22ec1
            [("demog_favourable", 2)] = new[] { 0x8f38e5672bd5dc2cUL, 0x2893c58abb4094f5UL, 0x1fef302c7a7afe04UL, 0xab68bee478db274dUL, 0xa8283d5dd3ab9883UL, 0x238164d091a42e38UL, 0xa8c7f832281a39c5UL, 0xc1afe2c8de3ca9e2UL, 0xc5caf8e5292ff030UL, 0xb6f8107f95a473dcUL, 0xbc767c22bb5d7590UL, 0x62c8798613ca3ac8UL }, // final 08e4eeae3e8ccbc2
            [("demog_favourable", 3)] = new[] { 0xaa91bf41576724bdUL, 0x060b8636f030c458UL, 0x960fa39c2332eea5UL, 0x6239e5a66a1fc12dUL, 0xed7e322fee64baa8UL, 0xb1bc00c0796bad5eUL, 0xa8c7f832281a39c5UL, 0x8afcd37dd73049acUL, 0x608b0e1130d38decUL, 0x32e44fc05c399320UL, 0xf4a3701708ae042dUL, 0x92943e697a8fcc49UL }, // final b27453be66bfd56f
            [("m4_families", 1)] = new[] { 0xfe86d9af4733df05UL, 0x2b4e088e489c75ceUL, 0xc5392b61535cf8ecUL, 0x6239e5a66a1fc12dUL, 0xb678ae3e82c76bc3UL, 0x4f2961caf76d836cUL, 0xa8c7f832281a39c5UL, 0xd91d31e4cee3253eUL, 0x408dd64b9de0c2e6UL, 0x20e305c1c56c60bdUL, 0x6086460836ef91edUL, 0xdae74c67a29fcceaUL }, // final 4e097b3731e62136
            [("m4_families", 2)] = new[] { 0x8f38e5672bd5dc2cUL, 0xc1f8385990432943UL, 0x1fef302c7a7afe04UL, 0xab68bee478db274dUL, 0xa8283d5dd3ab9883UL, 0x78af3c67bb237bf1UL, 0xa8c7f832281a39c5UL, 0x4e4dfca97c601656UL, 0xa6e1ed4499b47de2UL, 0x43129aa0adb2b656UL, 0x1eb6eeb4004feba3UL, 0x96fce36dd11ead85UL }, // final 561a1f8ef03ccfc3
            [("m4_families", 3)] = new[] { 0x706aab13a44a5ea5UL, 0x29fddec7186f947dUL, 0x3f567cb7d32763fbUL, 0x1da204996abd688dUL, 0x660608e878e06546UL, 0x4fd98d8c57470517UL, 0x4ccebf35e77df9dcUL, 0x1d567b34fe4acbefUL, 0xd15ee1005eed0e84UL, 0x2bd7678bccabc386UL, 0x18b6d06a1517a7d0UL, 0xbc1763d06b8eb1d5UL }, // final 7d58acabb1163bd5
        };

        /// <summary>Checkpoint B re-pin (R60): final hashes at tick 12000 with the flag unset, which is mode 1 since the re-pin.</summary>
        private static readonly Dictionary<(string, ulong), ulong> Repinned = new Dictionary<(string, ulong), ulong>
        {
            [("m4_families", 1)] = 0x8345d448306f8459UL, [("m4_families", 2)] = 0x94ac3afc879cf416UL, [("m4_families", 3)] = 0xd52cc1c5e24321efUL,
            [("demog_favourable", 1)] = 0xfd03d50ba778738aUL, [("demog_favourable", 2)] = 0xc2f7eb1255fcdba5UL, [("demog_favourable", 3)] = 0x1cd04d86d664b6b6UL,
        };

        /// <summary>
        /// F2-T1 after the Checkpoint B re-pin: an explicit mode 0 reproduces the pre-re-pin behaviour exactly, in every hash
        /// part except <c>core</c> (it holds the tuning JSON, where the explicit 0 is now written); the unset flag (mode 1)
        /// hashes as re-pinned.
        /// </summary>
        [Test]
        public void ConceptionMode0_ReproducesPreRepin_F2_T1()
        {
            var keys = Base.Keys.ToList();
            var specs = keys.Select(k => TestUtil.Spec(k.Item1, k.Item2, 12000, "tuning.Households.Life.ConceptionMode=0"))
                .Concat(keys.Select(k => TestUtil.Spec(k.Item1, k.Item2, 12000))).ToList();
            var res = TestUtil.RunAll(specs);
            int core = -1;
            for (int p = 0; p < GodSim.AllHashPartNames.Length; p++) if (GodSim.AllHashPartNames[p] == "core") core = p;
            Assert.GreaterOrEqual(core, 0);
            for (int i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                var r0 = res[i];
                Assert.AreEqual(0, r0.ExitCode, r0.Error);
                Assert.AreEqual(BaseParts[k].Length, r0.FinalParts.Length, k + ": part count");
                for (int p = 0; p < r0.FinalParts.Length; p++)
                    if (p != core) Assert.AreEqual(BaseParts[k][p].ToString("x16"), r0.FinalParts[p].ToString("x16"), k + " explicit 0, part " + GodSim.AllHashPartNames[p] + ": mode 0 must reproduce the base");
                var r1 = res[keys.Count + i];
                Assert.AreEqual(0, r1.ExitCode, r1.Error);
                Assert.AreEqual(BaseParts[k][core].ToString("x16"), r1.FinalParts[core].ToString("x16"), k + " unset: the default tuning JSON (core) is unchanged by the re-pin");
                Assert.AreEqual(Repinned[k].ToString("x16"), r1.FinalHash.ToString("x16"), k + " unset (mode 1): re-pinned hash");
            }
        }

        [Test]
        public void SaveLoad_ConceptionMode1_F2_T4()
        {
            var sc = TestUtil.Load("demog_f2_favourable", "tuning.Households.Life.ConceptionMode=1");
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
                var sc = TestUtil.Load("demog_f2_favourable", "tuning.Households.Life.ConceptionMode=" + mode);
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
