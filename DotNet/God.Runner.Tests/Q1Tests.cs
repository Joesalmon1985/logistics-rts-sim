using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using God.Recording;
using God.Runner.Kpi;
using God.Runner.Sql;
using God.Runner.Warehouse;
using God.Sim;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// Q1 (analytics): the read-only <c>sql</c> command (Q1-T1), migration 0026 (W-D5-8 extended), the early KPI views and the
    /// KPI engine (K-D5-1, 2, 3, 5, 6 for the sources that exist), P7 recording neutrality on the stub-state run (W-D5-3) and
    /// the warehouse never feeding the sim (W-D5-10).
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class Q1Tests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("q1"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private string RunDb(string tag, string scenario, ulong seed, long ticks, RecordingLevel level = RecordingLevel.Full)
        {
            var spec = TestUtil.Spec(scenario, seed, ticks);
            spec.Scenario.Checks.Remove("smoke");
            spec.Level = level;
            spec.Sqlite = new WarehouseOptions { ForeignKeys = false, Async = false };
            spec.RunDir = Path.Combine(_dir, tag); spec.OutRoot = _dir; spec.SkipJsonl = true;
            var res = RunEngine.Run(spec);
            Assert.IsNull(res.Error, res.Error);
            return res.WarehousePath;
        }

        private static string Sha(string path) { using (var s = File.OpenRead(path)) return Convert.ToHexString(SHA256.HashData(s)); }

        // ------------------------------------------------------------------ Q1-T1: the sql command is read-only

        [Test]
        public void Sql_IsReadOnly()
        {
            string db = RunDb("sql", "band_6x20", 1, 2400);
            string before = Sha(db);
            long size = new FileInfo(db).Length;
            Assert.AreEqual(0, TestUtil.Cli(out var o, out var e, "sql", "--db", db, "--query", "SELECT COUNT(*) AS n FROM dim_person"), e);
            StringAssert.StartsWith("n\n", o.Replace("\r", ""));
            Assert.AreEqual(0, TestUtil.Cli(out o, out e, "sql", "--db", db, "--query", "WITH x AS (SELECT 1 AS a) SELECT a FROM x;", "--csv"), e);
            Assert.AreEqual(0, TestUtil.Cli(out o, out e, "sql", "--db", db, "--query", "SELECT replace('abc', 'b', 'c') AS r, name FROM pragma_table_info('dim_person') LIMIT 2"), e);
            foreach (var bad in new[]
            {
                "INSERT INTO dim_code VALUES ('x', 1, 'a', 'b')", "UPDATE dim_person SET npc_id = 0", "DELETE FROM dim_person", "DROP TABLE dim_person",
                "CREATE TABLE z (a)", "ALTER TABLE dim_person ADD COLUMN z", "ATTACH DATABASE 'x.db' AS x", "PRAGMA journal_mode = DELETE", "PRAGMA query_only = OFF",
                "SELECT 1; SELECT 2", "SELECT 1; DROP TABLE dim_person", "VACUUM", "WITH x AS (SELECT 1) DELETE FROM dim_person", "REPLACE INTO dim_code VALUES ('x', 1, 'a', 'b')",
                "", "   ", "SELECT 'unterminated",
            })
            {
                Assert.AreEqual(ExitCodes.InvalidInput, TestUtil.Cli(out o, out e, "sql", "--db", db, "--query", bad), "refused: " + bad);
                StringAssert.Contains("read-only", e + o, bad);
            }
            Assert.AreEqual(ExitCodes.InvalidInput, TestUtil.Cli(out o, out e, "sql", "--db", Path.Combine(_dir, "nope.sqlite"), "--query", "SELECT 1"), "a missing file is invalid input");
            Assert.AreEqual(ExitCodes.InvalidInput, TestUtil.Cli(out o, out e, "sql", "--db", db, "--query", "SELECT * FROM no_such_table"), "a failing query is invalid input");
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out o, out e, "sql", "--db", db));
            Assert.AreEqual(before, Sha(db), "the file's bytes are unchanged");
            Assert.AreEqual(size, new FileInfo(db).Length);
        }

        [Test]
        public void SqlGuard_AllowsCommentsQuotedSemicolonsAndKeywordLikeStrings()
        {
            Assert.IsNull(SqlGuard.Check("-- note\nSELECT ';' AS s, 'DROP' AS d /* DELETE */ FROM t;", out string st));
            StringAssert.Contains("';'", st);
            Assert.IsNull(SqlGuard.Check("SELECT \"update\" FROM t", out _), "a quoted identifier is fine");
            Assert.IsNotNull(SqlGuard.Check("SELECT 1 /* open", out _));
        }

        // ------------------------------------------------------------------ W-D5-8 extended: migration 0026

        [Test]
        public void Migration_0026_FromV7_AddsViewsAndKpiRows()
        {
            var all = WarehouseDb.Migrations();
            var m26 = all.Single(m => m.version == 26);
            Assert.AreEqual("0026_early_views.sql", m26.name);
            Assert.AreEqual(26, WarehouseDb.CurrentVersion);
            string path = Path.Combine(_dir, "v7.sqlite");
            using (var c = new SqliteConnection("Data Source=" + path + ";Pooling=False"))
            {
                c.Open();
                foreach (var m in all.Where(m => m.version <= 7)) Exec(c, m.sql);          // a v7 fixture (F1)
                Assert.AreEqual(0L, Scalar(c, "SELECT COUNT(*) FROM sqlite_master WHERE name = 'v_kpi_pop13'"));
                Exec(c, m26.sql);                                                           // 0026 references only 0001-0007
                foreach (var v in new[] { "v_population_day", "v_person_life", "v_god_day", "v_intervention_chain", "v_kpi_pop12", "v_kpi_pop13", "v_kpi_hlt10", "v_kpi_liv07" })
                {
                    Assert.AreEqual(1L, Scalar(c, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'view' AND name = '" + v + "'"), v);
                    Assert.AreEqual(0L, Scalar(c, "SELECT COUNT(*) FROM " + v), v + " is empty on an empty warehouse");
                }
                Assert.AreEqual(22L, Scalar(c, "SELECT COUNT(*) FROM dim_kpi"));
            }
        }

        private static void Exec(SqliteConnection c, string sql) { using (var cmd = c.CreateCommand()) { cmd.CommandText = sql; cmd.ExecuteNonQuery(); } }
        private static long Scalar(SqliteConnection c, string sql) { using (var cmd = c.CreateCommand()) { cmd.CommandText = sql; return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L); } }

        // ------------------------------------------------------------------ the KPI engine

        private static readonly string[] Implemented = { "POP-12", "POP-13", "HLT-10.median", "HLT-10.p10", "HLT-10.below", "LIV-07.short", "LIV-07.median_days" };

        [Test]
        public void KpiEngine_FillsFactKpi_EqualToTheViews_AndMissingIsNotZero()
        {
            string db = RunDb("kpi", "m4_families", 1, 60000);
            Assert.AreEqual(0, TestUtil.Cli(out var o, out var e, "kpi", "--db", db), e);
            using (var c = new SqliteConnection("Data Source=" + db + ";Mode=ReadOnly;Pooling=False"))
            {
                c.Open();
                // K-D5-1: the view equals fact_kpi.actual_value exactly
                foreach (var code in Implemented.Where(x => x != "POP-12"))
                {
                    string view = KpiEngine.ViewName(code);
                    Assert.AreEqual(0L, Scalar(c, "SELECT COUNT(*) FROM (SELECT period_from_day, actual_value FROM " + view + " WHERE kpi_code = '" + code + "' AND actual_value IS NOT NULL EXCEPT SELECT f.period_from_day, f.actual_value FROM fact_kpi f JOIN dim_kpi k ON k.kpi_key = f.kpi_key WHERE k.code = '" + code + "')"), code + " view rows missing from fact_kpi");
                    Assert.AreEqual(0L, Scalar(c, "SELECT COUNT(*) FROM (SELECT f.period_from_day, f.actual_value FROM fact_kpi f JOIN dim_kpi k ON k.kpi_key = f.kpi_key WHERE k.code = '" + code + "' AND f.actual_value IS NOT NULL EXCEPT SELECT period_from_day, actual_value FROM " + view + " WHERE kpi_code = '" + code + "')"), code + " fact_kpi rows not in the view");
                    Assert.Greater(Scalar(c, "SELECT COUNT(*) FROM fact_kpi f JOIN dim_kpi k ON k.kpi_key = f.kpi_key WHERE k.code = '" + code + "' AND f.actual_value IS NOT NULL"), 0, code + " has values on a population run with households");
                }
                // K-D5-5: a KPI whose source package has not merged is Missing with its reason code, never 0
                foreach (var code in new[] { "POP-14", "TIM-04", "PRD-01", "OWN-01", "DIV-01", "DIV-02", "DIV-03", "DIV-04", "DIV-05", "DIV-06", "DIV-09", "DIV-10", "DIV-13", "CMD-01", "CMD-05" })
                {
                    Assert.AreEqual(1L, Scalar(c, "SELECT COUNT(*) FROM fact_kpi f JOIN dim_kpi k ON k.kpi_key = f.kpi_key WHERE k.code = '" + code + "' AND f.actual_value IS NULL AND f.method_code = 1"), code);
                    Assert.AreEqual(0L, Scalar(c, "SELECT COUNT(*) FROM fact_kpi f JOIN dim_kpi k ON k.kpi_key = f.kpi_key WHERE k.code = '" + code + "' AND f.actual_value IS NOT NULL"), code + " must not report a value");
                }
                // K-D5-6 / K-D5-2: min-cell periods are NULL with status 3, never 0; POP-13 is 0 on every day
                Assert.AreEqual(0L, Scalar(c, "SELECT COUNT(*) FROM fact_kpi f JOIN dim_kpi k ON k.kpi_key = f.kpi_key WHERE f.actual_value IS NULL AND f.method_code = 3 AND f.sample_n IS NULL AND k.code = 'POP-13'"));
                Assert.Greater(Scalar(c, "SELECT COUNT(*) FROM fact_kpi f JOIN dim_kpi k ON k.kpi_key = f.kpi_key WHERE k.code = 'POP-13'"), 10);
                Assert.AreEqual(0L, Scalar(c, "SELECT COUNT(*) FROM fact_kpi f JOIN dim_kpi k ON k.kpi_key = f.kpi_key WHERE k.code = 'POP-13' AND COALESCE(f.actual_value, 1) <> 0"), "the accounting identity holds on every day");
            }
        }

        [Test]
        public void KpiEngine_MinCell_IsMissingNotZero()
        {
            string db = RunDb("mincell", "band_6x20", 1, 1200);     // under a day: too few person-years for POP-12
            Assert.AreEqual(0, TestUtil.Cli(out var o, out var e, "kpi", "--db", db), e);
            using (var c = new SqliteConnection("Data Source=" + db + ";Mode=ReadOnly;Pooling=False"))
            {
                c.Open();
                Assert.AreEqual(0L, Scalar(c, "SELECT COUNT(*) FROM fact_kpi f JOIN dim_kpi k ON k.kpi_key = f.kpi_key WHERE k.code = 'POP-12' AND f.actual_value IS NOT NULL"));
                Assert.Greater(Scalar(c, "SELECT COUNT(*) FROM fact_kpi f JOIN dim_kpi k ON k.kpi_key = f.kpi_key WHERE k.code = 'POP-12' AND f.actual_value IS NULL AND f.method_code IN (2, 3)"), 0);
            }
        }

        [Test]
        public void KpiEngine_Is_Idempotent_AndRunFlagFillsIt()
        {
            string o = Path.Combine(_dir, "runkpi");
            Assert.AreEqual(0, TestUtil.Cli(out var so, out var se, "run", "--scenario", "band_6x20", "--seed", "1", "--ticks", "2400", "--record", "full", "--sqlite", "--no-jsonl", "--no-replay", "--kpi", "--out", o), se + so);
            string db = Directory.GetFiles(o, "warehouse.sqlite", SearchOption.AllDirectories).Single();
            long n1, n2;
            using (var c = new SqliteConnection("Data Source=" + db + ";Mode=ReadOnly;Pooling=False")) { c.Open(); n1 = Scalar(c, "SELECT COUNT(*) FROM fact_kpi"); }
            Assert.Greater(n1, 0);
            Assert.AreEqual(0, TestUtil.Cli(out so, out se, "kpi", "--db", db), se);
            using (var c = new SqliteConnection("Data Source=" + db + ";Mode=ReadOnly;Pooling=False")) { c.Open(); n2 = Scalar(c, "SELECT COUNT(*) FROM fact_kpi"); }
            Assert.AreEqual(n1, n2, "a second pass replaces, never adds");
        }

        [Test]
        public void NoComposite_NewIds()
        {
            // K-D5-3: no code sums or weights one dimension with another; no obedience / faith / devotion score exists
            var rx = new System.Text.RegularExpressions.Regex("obedience|faith score|devotion|wellbeing score|composite|overall", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var sql = WarehouseDb.Migrations().Single(m => m.version == 26).sql;
            string db = RunDb("nocomp", "band_6x20", 1, 1200);
            using (var c = new SqliteConnection("Data Source=" + db + ";Mode=ReadOnly;Pooling=False"))
            {
                c.Open();
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT code, name, COALESCE(definition, '') FROM dim_kpi";
                    using (var r = cmd.ExecuteReader()) while (r.Read()) Assert.IsFalse(rx.IsMatch(r.GetString(1) + " " + r.GetString(2)), r.GetString(0));
                }
                // every v_kpi view row is for one code of one KPI family: the family of the view name
                foreach (var code in Implemented)
                    Assert.AreEqual(0L, Scalar(c, "SELECT COUNT(*) FROM " + KpiEngine.ViewName(code) + " WHERE kpi_code NOT LIKE '" + code.Split('.')[0] + "%'"), code);
            }
            Assert.IsFalse(rx.IsMatch(sql.Replace("overall", "")), "no composite wording in the 0026 views");
        }

        // ------------------------------------------------------------------ P7 / W-D5-3

        [Test]
        public void Recording_Neutral_On_New_Domains()
        {
            // the F1 stub-state run: the three switches on, so every new domain exists, recorded at every level, each new domain
            // alone at Full, and sampleEvery 1, 16 and 64: one hash sequence
            string[] domains = { "lending", "organisation", "service", "divine", "possession", "avatar", "miracle", "ledger", "commandment", "religion" };
            var sets = new[] { "tuning.Divinity.Enabled=1", "tuning.Orgs.Enabled=1", "tuning.Civ.Enabled=1" };
            RunSpec Base(string tag)
            {
                var s = TestUtil.Spec("band_6x20", 2, 2400, sets);
                s.Scenario.Checks.Remove("smoke");
                s.CheckpointEvery = 600; s.OutRoot = _dir; s.RunDir = Path.Combine(_dir, tag); s.SkipJsonl = true;
                return s;
            }
            var specs = new List<(string name, RunSpec spec)>();
            foreach (var lv in new[] { RecordingLevel.Off, RecordingLevel.Metrics, RecordingLevel.Summary, RecordingLevel.Full })
            {
                var s = Base("lv_" + lv); s.Level = lv;
                if (lv >= RecordingLevel.Metrics) s.Sqlite = new WarehouseOptions { ForeignKeys = false, Async = false };
                specs.Add(("level " + RecordingLevels.Name(lv), s));
            }
            foreach (var d in domains)
            {
                var s = Base("dom_" + d); s.Level = RecordingLevel.Off;
                var sc = s.Scenario.Clone(); sc.RecordDomains[d] = RecordingLevel.Full; s.Scenario = sc;
                s.Sqlite = new WarehouseOptions { ForeignKeys = false, Async = false };
                specs.Add(("domain " + d, s));
            }
            foreach (int every in new[] { 1, 16, 64 })
            {
                var s = Base("samp_" + every); s.Level = RecordingLevel.Full;
                var sc = s.Scenario.Clone(); sc.SampleEvery = every; s.Scenario = sc;
                s.Sqlite = new WarehouseOptions { ForeignKeys = false, Async = false };
                specs.Add(("sampleEvery " + every, s));
            }
            var res = TestUtil.RunAll(specs.Select(x => x.spec).ToList());
            for (int i = 0; i < res.Length; i++) Assert.AreEqual(0, res[i].ExitCode, specs[i].name + ": " + res[i].Error);
            var reference = TestUtil.Seq(res[0]);
            Assert.Greater(reference.Count, 3);
            for (int i = 1; i < res.Length; i++) CollectionAssert.AreEqual(reference, TestUtil.Seq(res[i]), specs[i].name);
            Assert.AreEqual(GodSim.AllHashPartNames.Count - 1, res[0].FinalParts.Length, "all hash parts compared (UN0: every part but \"unification\", which exists only in a unified game, where civ and organisations do not)");
        }

        // ------------------------------------------------------------------ W-D5-10

        [Test]
        public void Warehouse_NeverFeedsSim()
        {
            // the sim assembly cannot see the warehouse at all, and no policy implementation (any assembly) mentions it
            var simAsm = typeof(GodSim).Assembly;
            foreach (var r in simAsm.GetReferencedAssemblies())
            {
                Assert.AreNotEqual("Microsoft.Data.Sqlite", r.Name, "God.Sim must not reference Sqlite");
                Assert.IsFalse(r.Name.StartsWith("God.Runner", StringComparison.Ordinal), "God.Sim must not reference " + r.Name);
            }
            var policies = new[] { typeof(God.Sim.Policy.INpcPolicy), typeof(God.Sim.Policy.IFactionPolicy), typeof(God.Sim.Policy.IInstitutionPolicy), typeof(God.Sim.Policy.IGodPolicy) };
            var asms = new[] { simAsm, typeof(Cli).Assembly, typeof(Q1Tests).Assembly }.Distinct();
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            bool Bad(Type t) => t != null && (t.Namespace ?? "").StartsWith("God.Runner.Warehouse", StringComparison.Ordinal) || (t?.Namespace ?? "").StartsWith("Microsoft.Data.Sqlite", StringComparison.Ordinal);
            int checkedTypes = 0;
            foreach (var asm in asms)
                foreach (var t in asm.GetTypes().Where(x => policies.Any(p => p.IsAssignableFrom(x)) && !x.IsInterface))
                {
                    checkedTypes++;
                    foreach (var f in t.GetFields(all)) Assert.IsFalse(Bad(f.FieldType), t.FullName + "." + f.Name);
                    foreach (var m in t.GetMethods(all).Cast<MethodBase>().Concat(t.GetConstructors(all)))
                    {
                        foreach (var p in m.GetParameters()) Assert.IsFalse(Bad(p.ParameterType), t.FullName + "." + m.Name);
                        if (m is MethodInfo mi) Assert.IsFalse(Bad(mi.ReturnType), t.FullName + "." + m.Name);
                        var body = m.GetMethodBody();
                        if (body != null) foreach (var lv in body.LocalVariables) Assert.IsFalse(Bad(lv.LocalType), t.FullName + "." + m.Name + " local");
                    }
                }
            Assert.Greater(checkedTypes, 0, "at least the idle policies are checked");
        }
    }
}
