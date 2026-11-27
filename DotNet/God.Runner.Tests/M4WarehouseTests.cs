using System;
using System.IO;
using System.Linq;
using God.Recording;
using God.Runner.Warehouse;
using God.Sim;
using God.Sim.Estate;
using God.Sim.Population;
using God.Sim.Population.Households;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M4.35 (owner D2 §5): births, deaths, the age distribution (person-day), household membership intervals, caregiving
    /// and inheritance transfers reach the warehouse with persistent person, household and lot ids and reconcile exactly with
    /// the sim's events; Full, Summary and Off give the same hash.
    /// </summary>
    [TestFixture]
    public sealed class M4WarehouseTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("m4wh"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private static long Q(string db, string sql) { using (var d = WarehouseDb.OpenRead(db)) return Convert.ToInt64(d.Scalar(sql) ?? 0L); }

        private (RunResult res, string db, CaptureRecorder cap) Run(string tag, RecordingLevel level, bool sqlite = true)
        {
            var spec = TestUtil.Spec("m4_band_6x20", 3, 1200 * 9 + 5);
            spec.Level = level;
            if (sqlite) spec.Sqlite = new WarehouseOptions { ForeignKeys = true };
            spec.RunDir = Path.Combine(_dir, tag); spec.OutRoot = _dir; spec.SkipJsonl = true;
            var cap = new CaptureRecorder();
            spec.ExtraRecorders.Add(() => cap);
            var res = RunEngine.Run(spec);
            Assert.IsNull(res.Error, res.Error);
            Assert.AreEqual(0, res.ExitCode, string.Join("; ", res.Messages));
            return (res, res.WarehousePath, cap);
        }

        [Test]
        public void Full_M4Tables_ReconcileWithTheEvents_AndRecordingIsNeutral_M4_35()
        {
            var (full, db, cap) = Run("full", RecordingLevel.Full);
            var ev = cap.Events;
            var born = ev.OfType<NpcBorn>().ToList();
            var died = ev.OfType<NpcDied>().ToList();
            var joined = ev.OfType<HouseholdJoined>().ToList(); var left = ev.OfType<HouseholdLeft>().ToList();
            var fed = ev.OfType<ChildFed>().ToList(); var cared = ev.OfType<CareGiven>().ToList();
            var transfers = ev.OfType<EstateTransfer>().ToList();
            var lifeDays = ev.OfType<PersonDayLife>().ToList();
            var hhDays = ev.OfType<HouseholdDay>().ToList();
            TestContext.WriteLine($"births {born.Count}, deaths {died.Count}, joins {joined.Count}, leaves {left.Count}, fed {fed.Count}, care {cared.Count}, transfers {transfers.Count}, person-days {lifeDays.Count}, household-days {hhDays.Count}");
            Assert.Greater(born.Count, 0); Assert.Greater(joined.Count, 0); Assert.Greater(fed.Count, 0); Assert.Greater(lifeDays.Count, 0);

            // births: one row each, persistent person keys, and the person dimension knows its origin and parents
            Assert.AreEqual(born.Count, Q(db, "SELECT COUNT(*) FROM fact_birth"));
            Assert.AreEqual(born.Count, Q(db, "SELECT COUNT(*) FROM dim_person WHERE origin_code = 1"));
            Assert.AreEqual(born.Count, Q(db, "SELECT COUNT(*) FROM fact_birth b JOIN dim_person p ON p.person_key = b.person_key JOIN dim_person m ON m.person_key = b.mother_person_key"));
            Assert.AreEqual(born.Sum(b => (long)b.Npc), Q(db, "SELECT SUM(p.npc_id) FROM fact_birth b JOIN dim_person p ON p.person_key = b.person_key"), "the same people");
            Assert.AreEqual(born.Sum(b => (long)b.Mother), Q(db, "SELECT SUM(p.npc_id) FROM fact_birth b JOIN dim_person p ON p.person_key = b.mother_person_key"), "the same mothers");
            Assert.AreEqual(ev.OfType<NpcGenome>().Count(), Q(db, "SELECT COUNT(*) FROM dim_person WHERE sex IS NOT NULL AND generation IS NOT NULL"), "genome and trait columns for every M4 person");
            // deaths
            Assert.AreEqual(died.Count, Q(db, "SELECT COUNT(*) FROM fact_death"));
            // membership intervals: one per join, closed by each leave, never overlapping, the dead have none open
            Assert.AreEqual(joined.Count, Q(db, "SELECT COUNT(*) FROM fact_household_membership"));
            Assert.AreEqual(left.Count, Q(db, "SELECT COUNT(*) FROM fact_household_membership WHERE to_tick IS NOT NULL"));
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_household_membership a JOIN fact_household_membership b ON a.person_key = b.person_key AND a.membership_key < b.membership_key " +
                "WHERE b.from_tick < COALESCE(a.to_tick, 9223372036854775807) AND a.from_tick < COALESCE(b.to_tick, 9223372036854775807) AND NOT (a.to_tick = b.from_tick)"), "intervals of one person never overlap");
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_household_membership m JOIN dim_person p ON p.person_key = m.person_key WHERE p.death_tick IS NOT NULL AND m.to_tick IS NULL"), "the last interval ends at death");
            Assert.AreEqual(ev.OfType<HouseholdFormed>().Count(), Q(db, "SELECT COUNT(*) FROM dim_household"));
            Assert.AreEqual(ev.OfType<HouseholdDissolved>().Count(), Q(db, "SELECT COUNT(*) FROM dim_household WHERE dissolved_tick IS NOT NULL"));
            // care: every act a row at Full, and the daily aggregate exact
            Assert.AreEqual(fed.Count, Q(db, "SELECT COUNT(*) FROM fact_care WHERE kind_code = 134"));
            Assert.AreEqual(cared.Count, Q(db, "SELECT COUNT(*) FROM fact_care WHERE kind_code = 136"));
            Assert.AreEqual(fed.Sum(f => (long)f.Lot), Q(db, "SELECT COALESCE(SUM(lot_id), 0) FROM fact_care WHERE kind_code = 134"), "with the lot ids");
            Assert.AreEqual(fed.Sum(f => (long)f.Count), Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_social_day WHERE kind_code = 134"));
            Assert.AreEqual(cared.Sum(f => (long)f.Ticks), Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_social_day WHERE kind_code = 136"), "caregiving ticks");
            // inheritance: one row per transfer, asset id = lot id
            Assert.AreEqual(transfers.Count, Q(db, "SELECT COUNT(*) FROM fact_inheritance"));
            Assert.AreEqual(transfers.Sum(t => (long)t.AssetId), Q(db, "SELECT COALESCE(SUM(asset_id), 0) FROM fact_inheritance"));
            Assert.AreEqual(transfers.Sum(t => (long)t.Count), Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_inheritance"));
            // the age distribution: M4's columns of the person-day row, exact by age band
            Assert.AreEqual(lifeDays.Count, Q(db, "SELECT COUNT(*) FROM fact_person_day WHERE age_milli IS NOT NULL"));
            foreach (var g in lifeDays.GroupBy(d => d.AgeMilli / 250))
                Assert.AreEqual(g.Count(), Q(db, "SELECT COUNT(*) FROM fact_person_day WHERE age_milli / 250 = " + g.Key), "age band " + g.Key);
            Assert.AreEqual(lifeDays.Sum(d => (long)d.CareGivenTicks), Q(db, "SELECT COALESCE(SUM(care_given_ticks), 0) FROM fact_person_day"));
            Assert.AreEqual(lifeDays.Count(d => d.Stage == LifeStage.Child), Q(db, "SELECT COUNT(*) FROM fact_person_day WHERE life_stage_code = 1"));
            Assert.AreEqual(hhDays.Count, Q(db, "SELECT COUNT(*) FROM fact_household_day"));
            using (var d = WarehouseDb.OpenRead(db))
            {
                Assert.AreEqual(0, WarehouseRecorder.ForeignKeyViolations(d));
                foreach (var g in HouseholdMappings.Grains) Assert.AreEqual(1L, (long)d.Scalar("SELECT COUNT(*) FROM meta_table_grain WHERE table_name = '" + g.table + "'"), g.table);
            }
            // recording neutrality: Summary and Off give the same hash as Full
            var (sum, _, _) = Run("summary", RecordingLevel.Summary);
            var (off, _, _) = Run("off", RecordingLevel.Off, sqlite: false);
            Assert.AreEqual(off.FinalHash, full.FinalHash, "Full = Off");
            Assert.AreEqual(off.FinalHash, sum.FinalHash, "Summary = Off");
        }
    }
}
