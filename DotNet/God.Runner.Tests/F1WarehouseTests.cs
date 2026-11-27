using System;
using System.IO;
using System.Linq;
using God.Recording;
using God.Runner.Warehouse;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// F1-T14 (F1 spec 13; W-D5-8 <c>Migration_0007_To_Current</c>, extended by Q1): the divine-core migration. A v6 database
    /// migrates to v7; the new tables exist and are empty for a switches-off run; recording never changes a hash.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class F1WarehouseTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("f1wh"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private static readonly string[] NewTables = { "dim_god", "dim_god_history", "fact_god_faction", "fact_resource_ledger" };
        private static readonly string[] ActionTables = { "fact_decision", "fact_production", "fact_ownership", "fact_care", "fact_social_interaction", "fact_transaction_line" };

        private static void Exec(SqliteConnection c, string sql) { using (var cmd = c.CreateCommand()) { cmd.CommandText = sql; cmd.ExecuteNonQuery(); } }
        private static long Scalar(SqliteConnection c, string sql) { using (var cmd = c.CreateCommand()) { cmd.CommandText = sql; return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L); } }

        [Test]
        public void Warehouse_0007_Migrates()
        {
            var all = WarehouseDb.Migrations();
            Assert.IsTrue(all.Any(m => m.version == 7 && m.name == "0007_divine_core.sql"));
            Assert.GreaterOrEqual(WarehouseDb.CurrentVersion, 7, "schema v7 after F1 (0026 is Q1's early views)");
            string path = Path.Combine(_dir, "v6.sqlite");
            using (var c = new SqliteConnection("Data Source=" + path + ";Pooling=False"))
            {
                c.Open();
                foreach (var m in all.Where(m => m.version <= 6)) Exec(c, m.sql);   // a v6 fixture database
                Assert.AreEqual(0, Scalar(c, "SELECT COUNT(*) FROM sqlite_master WHERE name = 'dim_god'"));
                Exec(c, all.Single(m => m.version == 7).sql);                           // 0007 references only 0001-0006
                foreach (var t in NewTables)
                {
                    Assert.AreEqual(1, Scalar(c, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '" + t + "'"), t);
                    Assert.AreEqual(0, Scalar(c, "SELECT COUNT(*) FROM " + t), t + " is empty");
                }
                foreach (var t in ActionTables)
                    foreach (var col in new[] { "control_code", "intervention_id" })
                        Assert.AreEqual(1, Scalar(c, "SELECT COUNT(*) FROM pragma_table_info('" + t + "') WHERE name = '" + col + "'"), t + "." + col);
                Assert.AreEqual(4, Scalar(c, "SELECT COUNT(*) FROM dim_code WHERE code_set = 'control_source'"));
                Assert.AreEqual(8, Scalar(c, "SELECT COUNT(*) FROM dim_code WHERE code_set = 'cause_kind'"));
                Assert.Greater(Scalar(c, "SELECT COUNT(*) FROM dim_code WHERE code_set = 'mythic_category'"), 0);
            }
        }

        [Test]
        public void Warehouse_SwitchesOff_DivineTablesEmpty_RecordingNeutral()
        {
            var spec = TestUtil.Spec("band_6x20", 1, 2400);
            spec.Scenario.Checks.Remove("smoke");
            spec.CheckpointEvery = 600;
            spec.Level = RecordingLevel.Full;
            spec.Sqlite = new WarehouseOptions { ForeignKeys = true, Async = false };
            spec.RunDir = Path.Combine(_dir, "full"); spec.OutRoot = _dir; spec.SkipJsonl = true;
            var plain = TestUtil.Spec("band_6x20", 1, 2400);
            plain.Scenario.Checks.Remove("smoke");
            plain.CheckpointEvery = 600;
            var a = RunEngine.Run(spec); var b = RunEngine.Run(plain);
            Assert.IsNull(a.Error, a.Error); Assert.IsNull(b.Error, b.Error);
            CollectionAssert.AreEqual(TestUtil.Seq(b), TestUtil.Seq(a), "recording at Full with the divine core schema changes no hash");
            using (var d = WarehouseDb.OpenRead(a.WarehousePath))
            {
                Assert.AreEqual(WarehouseDb.CurrentVersion, d.SchemaVersion());
                foreach (var t in NewTables) Assert.AreEqual(0L, Convert.ToInt64(d.Scalar("SELECT COUNT(*) FROM " + t)), t);
                foreach (var t in ActionTables)
                    Assert.AreEqual(0L, Convert.ToInt64(d.Scalar("SELECT COUNT(*) FROM " + t + " WHERE control_code IS NOT NULL OR intervention_id IS NOT NULL")), t);
            }
        }
    }
}
