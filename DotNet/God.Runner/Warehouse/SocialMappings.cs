using God.Sim.Social;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// M5's warehouse mappers (OWNER: M5): gestures, decoded messages, responses, trust changes, evictions, recognition errors
    /// and betrayals into <c>fact_social_interaction</c>; deals into <c>fact_transaction</c> (opened, then closed) and their
    /// hand-overs into <c>fact_transaction_line</c>; gifts also into the <c>fact_social_day</c> aggregate; the social day into
    /// <c>fact_person_day</c>'s soc_ columns. Persistent ids: MessageId, DealId, lot ids and NpcId (through dim_person).
    /// Analytics-only truth (Honest, Mismatch, TrueR) is written at Full only.
    /// </summary>
    public static class SocialMappings
    {
        /// <summary>The grain of each M5 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("fact_social_interaction", "one row per social or communication event (kind_code 1 gesture, 2 decoded, 3 response, 4 trust, 5 evicted, 6 recognition error, 7 betrayal); high-volume kinds sampled below Full", "GestureMade, MessageDecoded, ResponseChosen, TrustChanged, RelationEvicted, RecognitionError, BetrayalRecorded"),
            ("fact_transaction", "one row per deal (DealId): opened by ExchangeOpened, closed by ExchangeClosed", "ExchangeOpened, ExchangeClosed"),
            ("fact_transaction_line", "one row per hand-over of a deal (ExchangeStep or GiftGiven)", "ExchangeStep, GiftGiven"),
        };

        public const int KindGesture = 1, KindDecoded = 2, KindResponse = 3, KindTrust = 4, KindEvicted = 5, KindRecognition = 6, KindBetrayal = 7;

        private static object P(MapContext c, int npc) => npc > 0 ? (object)c.Person(npc).Key : null;

        private static void Row(MapContext c, int kind, int a, int b, object msg, object deal, object act, object tk, object tid, object tsub, object qty,
            object ck, object cid, object csub, object time, object loc, object listeners, object dist, object v0, object v1, object f0, object f1,
            object cause, object causeRef, object verb, object ignored, object truth)
        {
            if (!c.RowWanted) return;
            c.Insert(Tables.FactSocialInteraction, c.Tick, c.Seq, c.DateKey(), (long)kind, P(c, a), P(c, b), msg, deal, act, tk, tid, tsub, qty, ck, cid, csub, time, loc,
                listeners, dist, v0, v1, f0, f1, cause, causeRef, verb, ignored, truth, (long)c.Weight);
        }

        private static object W(God.Sim.Ware w) => w != God.Sim.Ware.None ? (object)(long)(int)w : null;

        public static void Register(EventMapRegistry r)
        {
            r.Register<GestureMade>("fact_social_interaction", "social", (c, e) => Row(c, KindGesture, e.Speaker, e.Target, (long)e.MessageId, (long)e.DealId, (long)(int)e.Act,
                (long)(int)e.TopicKind, (long)e.TopicId, (long)e.TopicSub, (long)e.Quantity, (long)(int)e.CounterKind, (long)e.CounterId, (long)e.CounterSub, (long)(int)e.Time,
                c.NodeLocation(e.Node), (long)e.Listeners, null, null, null, null, null, null, e.Presented ? 1L : 0L, null, null, c.Full ? (object)(e.Honest ? 1L : 0L) : null));
            r.Register<MessageDecoded>("fact_social_interaction", "social", (c, e) => Row(c, KindDecoded, e.Listener, e.Speaker, (long)e.MessageId, (long)e.DealId, (long)(int)e.Act,
                (long)(int)e.TopicKind, (long)e.TopicId, (long)e.TopicSub, (long)e.Quantity, (long)(int)e.CounterKind, (long)e.CounterId, (long)e.CounterSub, (long)(int)e.Time,
                null, null, (long)e.Dist, null, null, null, null, null, (long)e.Target, null, null, c.Full ? (object)(e.Mismatch ? 1L : 0L) : null));
            r.Register<ResponseChosen>("fact_social_interaction", "social", (c, e) => Row(c, KindResponse, e.Listener, e.Speaker, (long)e.MessageId, null, null,
                null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, (long)e.Verb, e.Ignored ? 1L : 0L, null));
            r.Register<TrustChanged>("fact_social_interaction", "social", (c, e) => Row(c, KindTrust, e.A, e.B, null, null, null, null, null, null, null, null, null, null, null,
                null, null, null, (long)e.Before, (long)e.After, (long)e.FamBefore, (long)e.FamAfter, (long)(int)e.Cause, (long)e.CauseRef, null, null, null));
            r.Register<RelationEvicted>("fact_social_interaction", "social", (c, e) => Row(c, KindEvicted, e.Owner, e.Evicted, null, null, null, null, null, null, null, null, null, null, null,
                null, null, null, (long)e.Importance, (long)e.Trust, (long)e.Familiarity, (long)e.Familiarity, (long)e.Grudge, null, null, null, null));
            r.Register<RecognitionError>("fact_social_interaction", "social", (c, e) => Row(c, KindRecognition, e.Observer, e.Observed, null, null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, (long)e.Familiarity, (long)e.Familiarity, (long)e.Kind, null, null, null, null));
            r.Register<BetrayalRecorded>("fact_social_interaction", "social", (c, e) => Row(c, KindBetrayal, e.Victim, e.Offender, null, (long)e.DealId, null, null, null, null, null, null, null, null, null,
                null, null, null, (long)e.Severity, (long)e.Value, null, null, (long)e.Kind, null, null, null, null));
            r.Register<ExchangeOpened>("fact_transaction", "deal", (c, e) =>
            {
                if (!c.RowWanted) return;
                c.Insert(Tables.FactTransactionOpen, (long)e.DealId, c.Tick, c.DateKey(), (long)(int)e.Kind, P(c, e.A), P(c, e.B), (long)e.MessageId,
                    W(e.AWare), (long)e.ACount, W(e.BWare), (long)e.BCount, e.SameHousehold ? 1L : 0L);
            });
            r.Register<ExchangeClosed>("fact_transaction", "deal", (c, e) =>
            {
                if (!c.RowWanted) return;
                c.Insert(Tables.FactTransactionClose, c.Tick, c.DateKey(), (long)(int)e.Outcome, (long)e.AGiven, (long)e.BGiven, (long)e.AGain, (long)e.BGain, P(c, e.B), (long)e.DealId);
            });
            r.Register<ExchangeStep>("fact_transaction_line", "deal_line", (c, e) => Line(c, e, 211, null, null, null, null));
            r.Register<GiftGiven>("fact_transaction_line", "deal_line", (c, e) =>
            {
                // the help-like aggregate (SHARED_CONTRACTS 8, I-BS.3): every gift by the giver's believed kin band, before sampling
                var g = c.Person(e.Giver);
                c.Aggregate(Tables.FactSocialDay, new object[] { c.DateKey(), g.FactionKey ?? 0L, 213L, (long)(int)e.GiverKinBand }, null, 1, e.Count);
                Line(c, e, 213, (long)(int)e.GiverKinBand, e.SameHousehold ? 1L : 0L, (long)e.Answers, c.Full ? (object)(long)e.TrueR : null);
            });
            r.Register<PersonDaySocial>("fact_person_day", "person_day_social", (c, e) =>
            {
                if (!c.RowWanted) return;
                c.Insert(Tables.FactPersonDaySocial, (long)e.RelCount, (long)e.MeanTrust, (long)e.Friends, (long)e.Enemies, (long)e.GrudgeSum, (long)e.Sent, (long)e.Received,
                    (long)e.Misread, (long)e.GiftsGiven, (long)e.GiftsReceived, (long)e.Deals, (long)e.Cheated, c.DateKeyOfDay(e.Day), c.Person(e.Npc).Key);
            });
        }

        private static void Line(MapContext c, DealStepBase e, int kind, object band, object sameHousehold, object answers, object trueR)
        {
            if (!c.RowWanted) return;
            c.Insert(Tables.FactTransactionLine, c.Tick, c.Seq, c.DateKey(), (long)e.DealId, (long)e.Step, (long)kind, P(c, e.Giver), P(c, e.Receiver), (long)e.Lot,
                (long)(int)e.Ware, (long)e.Count, c.NodeLocation(e.Node), band, sameHousehold, answers, trueR, (long)c.Weight);
        }
    }
}
