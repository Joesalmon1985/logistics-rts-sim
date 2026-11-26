using System;
using System.IO;
using System.Linq;
using God.Recording;
using God.Runner.Demog;
using God.Runner.Warehouse;
using God.Sim;
using God.Sim.Population;
using God.Sim.Population.Households;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M4.45 (owner D4 Â§10): the demographic report's figures reconcile with the warehouse of the same run (Full, SQLite) and
    /// with the population ledger; the report never changes a run (the same hash with and without it); and the command runs
    /// multi-seed and writes its report and CSVs.
    /// </summary>
    [TestFixture]
    public sealed class DemogReportTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("demog"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private static long Q(string db, string sql) { using (var d = WarehouseDb.OpenRead(db)) return Convert.ToInt64(d.Scalar(sql) ?? 0L); }

        [Test]
        public void ReportFigures_ReconcileWithTheWarehouse_M4_45()
        {
            var spec = TestUtil.Spec("demog_established_6x20", 3, 1200 * 12 + 5);
            spec.Level = RecordingLevel.Full;
            spec.Sqlite = new WarehouseOptions { ForeignKeys = true };
            spec.RunDir = Path.Combine(_dir, "full"); spec.OutRoot = _dir; spec.SkipJsonl = true;
            var rec = new DemogRecorder();
            spec.ExtraRecorders.Add(() => rec);
            var res = RunEngine.Run(spec);
            Assert.IsNull(res.Error, res.Error);
            Assert.AreEqual(0, res.ExitCode, string.Join("; ", res.Messages));
            string db = res.WarehousePath;
            var s = DemogReport.Summarise("full", 3, rec, res.ExitCode, 0);
            TestContext.WriteLine($"start {rec.Start} end {rec.Final} births {rec.Births} deaths {rec.Deaths} person-days {rec.PersonDaysTotal} care {rec.CareTicksTotal} food in {rec.FoodProducedTotal} out {rec.FoodConsumedTotal}");
            Assert.Greater(rec.Births, 0); Assert.Greater(rec.Deaths, 0);

            Assert.AreEqual(rec.Births, Q(db, "SELECT COUNT(*) FROM fact_birth"), "births");
            Assert.AreEqual(rec.Deaths, Q(db, "SELECT COUNT(*) FROM fact_death"), "deaths");
            for (int c = 1; c < 9; c++) Assert.AreEqual(rec.DeathsByCause[c], Q(db, "SELECT COUNT(*) FROM fact_death WHERE cause_code = " + c), "deaths of cause " + c);
            Assert.AreEqual(rec.Years.Sum(y => y.DeathsByCause.Sum()), rec.Deaths, "the yearly causes sum to the deaths");
            Assert.AreEqual(rec.Final, rec.Start + rec.Births + rec.Arrivals - rec.Deaths, "population change reconciles exactly");
            Assert.IsTrue(s.Reconciles);
            Assert.AreEqual(rec.Final, res.Sim.State.Population.AliveCount);
            Assert.AreEqual(rec.PersonDaysTotal, Q(db, "SELECT COUNT(*) FROM fact_person_day"), "person-days");
            Assert.AreEqual(rec.CareTicksTotal, Q(db, "SELECT COALESCE(SUM(ticks_care), 0) FROM fact_person_day"), "caregiving ticks");
            Assert.AreEqual(rec.ChildFedUnitsTotal, Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_social_day WHERE kind_code = 134"), "food units fed to children");
            Assert.AreEqual(rec.HouseholdDaysTotal, Q(db, "SELECT COUNT(*) FROM fact_household_day"), "household-days");
            Assert.AreEqual(rec.HouseholdMemberDaysTotal, Q(db, "SELECT COALESCE(SUM(members), 0) FROM fact_household_day"), "household member-days");
            string edible = string.Join(",", Enum.GetValues(typeof(Ware)).Cast<Ware>().Where(DemogRecorder.Edible).Select(w => ((int)w).ToString()));
            Assert.AreEqual(rec.FoodProducedTotal, Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_production WHERE direction = 1 AND ware_key IN (" + edible + ")"), "food produced");
            Assert.AreEqual(rec.FoodConsumedTotal, Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_production WHERE direction = -1 AND ware_key IN (" + edible + ")"), "food consumed");
            Assert.AreEqual(rec.ConsumedByReason[(int)ConsumeReason.Spoiled], Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_production WHERE direction = -1 AND reason_code = 1 AND ware_key IN (" + edible + ")"), "food spoiled");
            // and with the sim's own goods ledger (conservation: produced - consumed = held)
            var led = res.Sim.State.Population.Ledger;
            long prod = 0, cons = 0;
            foreach (Ware w in Enum.GetValues(typeof(Ware))) if (DemogRecorder.Edible(w) && (int)w < Wares.PersonalCount) { prod += led.ProducedOf(w); cons += led.ConsumedOf(w); }
            Assert.AreEqual(prod, rec.LedgerProducedAtStart + rec.FoodProducedTotal, "ledger produced (the founders' food was made before the first tick)");
            Assert.AreEqual(cons, rec.LedgerConsumedAtStart + rec.FoodConsumedTotal, "ledger consumed");
            // reproductive relationships: the events reach the warehouse's generic event table
            Assert.AreEqual(res.Sim.State.Households.Stats.Unions, Q(db, "SELECT COUNT(*) FROM fact_event WHERE event_code = 148"), "reproductive relationships formed");

            // the report never changes a run
            var plain = RunEngine.Run(TestUtil.Spec("demog_established_6x20", 3, 1200 * 12 + 5));
            Assert.AreEqual(plain.FinalHash, res.FinalHash, "same hash with and without the report and the warehouse");
        }

        [Test]
        public void Command_MultiSeed_WritesReportAndCsv()
        {
            int code = TestUtil.Cli(out string o, out string e, "demog", "report", "--scenario", "demog_established_6x20", "--seeds", "1-2", "--days", "4", "--quiet",
                "--arm", "base", "--arm", "no-outside: tuning.Households.Partner.IndependentUnions=0", "--out", _dir);
            Assert.AreEqual(0, code, e + o);
            StringAssert.Contains("## base (2 seeds", o);
            StringAssert.Contains("## no-outside (2 seeds", o);
            Assert.IsTrue(File.Exists(Path.Combine(_dir, "runs.csv")));
            Assert.AreEqual(5, File.ReadAllLines(Path.Combine(_dir, "runs.csv")).Length, "a header and one row per arm and seed");
            Assert.IsTrue(File.ReadAllText(Path.Combine(_dir, "yearly.csv")).Length > 100);
        }
    }
}
