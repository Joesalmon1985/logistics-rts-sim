using System;
using System.IO;
using System.Linq;
using God.Recording;
using God.Runner.Warehouse;
using God.Sim;
using God.Sim.Social;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M5.17 (owner D2 §5): gestures, decoded messages, responses, barter offers and outcomes, gifts and social interactions
    /// reach the warehouse with persistent ids (MessageId, DealId, lot ids, persons) and reconcile exactly with the event
    /// stream; Full, Summary and Off give the same hash.
    /// </summary>
    [TestFixture]
    public class M5WarehouseTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("m5wh"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private (RunResult res, string db, CaptureRecorder cap) Run(string tag, RecordingLevel level, bool sqlite = true)
        {
            var spec = TestUtil.Spec("band_6x20", 3, 3 * 1200 + 5);
            spec.Scenario.Checks.Remove("smoke");
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

        private static long Q(string db, string sql) { using (var d = WarehouseDb.OpenRead(db)) return Convert.ToInt64(d.Scalar(sql) ?? 0L); }

        [Test]
        public void M5_17_Full_SocialTablesReconcileExactly_WithTheEventStream_AndRecordingIsNeutral()
        {
            var (res, db, cap) = Run("full", RecordingLevel.Full);
            var ev = cap.Events;
            int K(string kind) => (int)Q(db, "SELECT COUNT(*) FROM fact_social_interaction WHERE kind_code = " + kind);
            var g = ev.OfType<GestureMade>().ToList();
            Assert.Greater(g.Count, 100);
            Assert.AreEqual(g.Count, K("1"), "one row per GestureMade");
            Assert.AreEqual(ev.OfType<MessageDecoded>().Count(), K("2"), "one row per MessageDecoded");
            Assert.AreEqual(ev.OfType<ResponseChosen>().Count(), K("3"));
            Assert.AreEqual(ev.OfType<TrustChanged>().Count(), K("4"));
            Assert.AreEqual(ev.OfType<RelationEvicted>().Count(), K("5"));
            Assert.AreEqual(ev.OfType<RecognitionError>().Count(), K("6"));
            Assert.AreEqual(ev.OfType<BetrayalRecorded>().Count(), K("7"));
            Assert.AreEqual(g.Sum(x => (long)x.Listeners), Q(db, "SELECT SUM(listeners) FROM fact_social_interaction WHERE kind_code = 1"), "every listener's copy is a row");
            Assert.AreEqual(g.Select(x => x.MessageId).Distinct().Count(), (int)Q(db, "SELECT COUNT(DISTINCT message_id) FROM fact_social_interaction WHERE kind_code = 1"), "message ids persist");
            foreach (var a in g.GroupBy(x => (int)x.Act))
                Assert.AreEqual(a.Count(), (int)Q(db, "SELECT COUNT(*) FROM fact_social_interaction WHERE kind_code = 1 AND act_code = " + a.Key), "act " + (God.Sim.Comms.GestureAct)a.Key);
            Assert.AreEqual(ev.OfType<MessageDecoded>().Count(d => d.Mismatch), (int)Q(db, "SELECT COUNT(*) FROM fact_social_interaction WHERE kind_code = 2 AND truth_flag = 1"), "misreadings (truth, Full)");
            Assert.AreEqual(ev.OfType<TrustChanged>().Sum(t => (long)(t.After - t.Before)), Q(db, "SELECT COALESCE(SUM(value_after - value_before), 0) FROM fact_social_interaction WHERE kind_code = 4"));

            // deals: opened, closed by outcome, hand-overs with their lots
            var opened = ev.OfType<ExchangeOpened>().ToList(); var closed = ev.OfType<ExchangeClosed>().ToList();
            Assert.AreEqual(opened.Count, (int)Q(db, "SELECT COUNT(*) FROM fact_transaction"));
            Assert.AreEqual(closed.Count, (int)Q(db, "SELECT COUNT(*) FROM fact_transaction WHERE outcome_code IS NOT NULL"));
            foreach (var o in closed.GroupBy(c => (int)c.Outcome))
                Assert.AreEqual(o.Count(), (int)Q(db, "SELECT COUNT(*) FROM fact_transaction WHERE outcome_code = " + o.Key), "outcome " + (DealOutcome)o.Key);
            Assert.AreEqual(closed.Sum(c => (long)c.AGiven + c.BGiven), Q(db, "SELECT COALESCE(SUM(a_given + b_given), 0) FROM fact_transaction WHERE outcome_code IS NOT NULL"));
            var steps = ev.OfType<DealStepBase>().ToList();
            Assert.Greater(steps.Count, 10);
            Assert.AreEqual(steps.Count, (int)Q(db, "SELECT COUNT(*) FROM fact_transaction_line"));
            Assert.AreEqual(steps.Sum(s => (long)s.Count), Q(db, "SELECT SUM(quantity) FROM fact_transaction_line"));
            Assert.AreEqual(steps.Sum(s => (long)s.Lot), Q(db, "SELECT SUM(lot_id) FROM fact_transaction_line"), "lot ids persist");
            Assert.AreEqual(ev.OfType<GiftGiven>().Count(), (int)Q(db, "SELECT COUNT(*) FROM fact_transaction_line WHERE kind_code = 213"));
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_transaction_line l WHERE NOT EXISTS (SELECT 1 FROM fact_transaction t WHERE t.deal_id = l.deal_id)"), "every hand-over belongs to a deal");
            Assert.AreEqual(ev.OfType<GiftGiven>().Count(), Q(db, "SELECT COALESCE(SUM(events), 0) FROM fact_social_day WHERE kind_code = 213"), "the gift aggregate");

            // the social day on the person-day rows
            var days = ev.OfType<PersonDaySocial>().ToList();
            Assert.Greater(days.Count, 100);
            Assert.AreEqual(days.Count, (int)Q(db, "SELECT COUNT(*) FROM fact_person_day WHERE soc_rel_count IS NOT NULL"), "one person-day row each");
            Assert.AreEqual(days.Sum(d => (long)d.Sent), Q(db, "SELECT SUM(soc_sent) FROM fact_person_day"));
            Assert.AreEqual(days.Sum(d => (long)d.RelCount), Q(db, "SELECT SUM(soc_rel_count) FROM fact_person_day"));
            Assert.AreEqual(days.Sum(d => (long)d.GiftsGiven), Q(db, "SELECT SUM(soc_gifts_given) FROM fact_person_day"));

            // recording changes nothing: Summary and Off give the same hash
            var (sum, _, _) = Run("summary", RecordingLevel.Summary);
            var (off, _, _) = Run("off", RecordingLevel.Off, sqlite: false);
            Assert.AreEqual(res.FinalHash, sum.FinalHash, "Full and Summary");
            Assert.AreEqual(res.FinalHash, off.FinalHash, "Full and Off");
            Assert.Greater(Q(sum.WarehousePath, "SELECT COUNT(*) FROM fact_social_interaction"), 0);
            Assert.AreEqual(0, Q(sum.WarehousePath, "SELECT COUNT(*) FROM fact_social_interaction WHERE truth_flag IS NOT NULL"), "truth fields at Full only");
        }
    }
}
