using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// F1 neutrality pins P1, P4, P8 (SHARED_ARCHITECTURAL_CONTRACTS section 16, F1 spec F1-P1/P4/P8). With the three new
    /// switches (<c>Divinity.Enabled</c>, <c>Orgs.Enabled</c>, <c>Civ.Enabled</c>) at 0, every part hash of every checkpoint of
    /// band_6x20, m4_families, ibs_families, watch6 and the demog_* scenarios, seeds 1-3, equals the base (unity/poc
    /// <c>11c3e43</c>, Checkpoint A, F0 merged). The base values were captured by running this file on the unmodified base
    /// before the first F1 commit. Each value is a digest (FNV-1a over tick, part count and every part hash of every
    /// checkpoint), so one value per scenario and seed pins every part at every checkpoint.
    /// Horizon: 10 sim days (12000 ticks), a checkpoint every 600 ticks.
    /// P1, P4 and P8 share the run: while every switch is 0 they are the same statement; each test switches all three off
    /// explicitly (contracts 16: pins switch every new module off except the one under test, and F1 tests none).
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class F1NeutralityTests
    {
        internal const long Ticks = 12000, Every = 600;
        internal static readonly string[] Scenarios = { "band_6x20", "m4_families", "ibs_families", "watch6", "demog_balanced", "demog_established_6x20", "demog_favourable", "demog_hostile" };

        // "scenario/seed" -> digest of every part of every checkpoint (captured at 11c3e43)
        internal static readonly Dictionary<string, ulong> Base = new Dictionary<string, ulong>
        {
            ["band_6x20/1"] = 0xc059aef8ff82ba20UL,
            ["band_6x20/2"] = 0x555cfe76c2df33d2UL,
            ["band_6x20/3"] = 0x2521c9b5e5bd487bUL,
            ["m4_families/1"] = 0xc7789e9d5fb7dbdeUL,
            ["m4_families/2"] = 0xff0fb1f1f776af44UL,
            ["m4_families/3"] = 0x67bb20af51ecd850UL,
            ["ibs_families/1"] = 0x632d212ca715768fUL,
            ["ibs_families/2"] = 0x408ad01a3e42b5c3UL,
            ["ibs_families/3"] = 0xa71e0bece940df89UL,
            ["watch6/1"] = 0xdd4001b92780cae8UL,
            ["watch6/2"] = 0xcaddf14aa91c1ffcUL,
            ["watch6/3"] = 0x1ec6d25aa6a6c4fdUL,
            ["demog_balanced/1"] = 0xd36426cf938bef41UL,
            ["demog_balanced/2"] = 0x617001cd74b798e0UL,
            ["demog_balanced/3"] = 0x0f437863c034b866UL,
            ["demog_established_6x20/1"] = 0xd36426cf938bef41UL,
            ["demog_established_6x20/2"] = 0x617001cd74b798e0UL,
            ["demog_established_6x20/3"] = 0x0f437863c034b866UL,
            ["demog_favourable/1"] = 0x12cdffc50569810eUL,
            ["demog_favourable/2"] = 0x04ff1cc7e1372e45UL,
            ["demog_favourable/3"] = 0x2c366048e71dac53UL,
            ["demog_hostile/1"] = 0x5728f3a6e9d7f59dUL,
            ["demog_hostile/2"] = 0xd71e16d96ed14d5bUL,
            ["demog_hostile/3"] = 0xb17c6f10f600fe12UL,
        };

        internal static string[] OffSets() => SwitchesExist() ? new[] { "tuning.Divinity.Enabled=0", "tuning.Orgs.Enabled=0", "tuning.Civ.Enabled=0" } : new string[0];

        private static bool SwitchesExist() => typeof(God.Sim.SimTuning).GetField("Divinity") != null;

        internal static ulong Digest(RunResult r)
        {
            ulong h = 14695981039346656037UL;
            void Mix(ulong v) { for (int i = 0; i < 8; i++) { h ^= (v >> (8 * i)) & 0xff; h *= 1099511628211UL; } }
            foreach (var c in r.Checkpoints)
            {
                Mix((ulong)c.Tick); Mix((ulong)c.Parts.Length);
                foreach (var p in c.Parts) Mix(p);
            }
            return h;
        }

        internal static RunSpec Spec(string scenario, ulong seed, params string[] extra)
        {
            var spec = TestUtil.Spec(scenario, seed, Ticks, OffSets().Concat(extra).ToArray());
            spec.Scenario.Checks.Remove("smoke");
            spec.CheckpointEvery = Every;
            return spec;
        }

        private static Dictionary<string, ulong> _current;

        internal static Dictionary<string, ulong> Current()
        {
            if (_current != null) return _current;
            var keys = new List<string>(); var specs = new List<RunSpec>();
            foreach (var sc in Scenarios)
                for (ulong seed = 1; seed <= 3; seed++) { keys.Add(sc + "/" + seed); specs.Add(Spec(sc, seed)); }
            var res = TestUtil.RunAll(specs);
            var d = new Dictionary<string, ulong>();
            for (int i = 0; i < res.Length; i++)
            {
                Assert.AreEqual(0, res[i].ExitCode, keys[i] + ": " + res[i].Error);
                d[keys[i]] = Digest(res[i]);
            }
            return _current = d;
        }

        private static void AssertBase(string pin)
        {
            var cur = Current();
            var sb = new StringBuilder();
            foreach (var k in cur.Keys) sb.Append("            [\"" + k + "\"] = 0x" + cur[k].ToString("x16") + "UL,\n");
            TestContext.WriteLine(sb.ToString());
            Assert.AreEqual(Scenarios.Length * 3, Base.Count, "base digests present\n" + sb);
            foreach (var k in Base.Keys) Assert.AreEqual(Base[k], cur[k], pin + ": " + k + " differs from the base 11c3e43");
        }

        [Test] public void Divinity_Off_Reproduces_Base() => AssertBase("P1");
        [Test] public void Orgs_Off_Reproduces_Base() => AssertBase("P4");
        [Test] public void Civ_Off_Reproduces_Base() => AssertBase("P8");
    }
}
