namespace God.Runner.Warehouse
{
    /// <summary>M5's table statements (OWNER: M5): migration <c>0006_social.sql</c>.</summary>
    public static partial class Tables
    {
        public static readonly TableDef FactSocialInteraction = new TableDef("fact_social_interaction", 70, "run_key", "tick", "seq", "date_key", "kind_code",
            "a_person_key", "b_person_key", "message_id", "deal_id", "act_code", "topic_kind_code", "topic_id", "topic_sub", "quantity",
            "counter_kind_code", "counter_id", "counter_sub", "time_code", "location_key", "listeners", "dist", "value_before", "value_after",
            "fam_before", "fam_after", "cause_code", "cause_ref", "verb", "ignored", "truth_flag", "sample_weight");

        public static readonly TableDef FactTransactionOpen = new TableDef("fact_transaction", 71, "run_key", "deal_id", "opened_tick", "opened_date_key", "kind_code",
            "a_person_key", "b_person_key", "message_id", "a_ware_key", "a_count", "b_ware_key", "b_count", "same_household");

        /// <summary>ExchangeClosed: the outcome, what passed and the believed gains; the other side of an open offer becomes known (after the opening row, order 72).</summary>
        public static readonly TableDef FactTransactionClose = new TableDef("fact_transaction", 72,
            "UPDATE fact_transaction SET closed_tick = @p1, closed_date_key = @p2, outcome_code = @p3, a_given = @p4, b_given = @p5, a_gain = @p6, b_gain = @p7, "
            + "b_person_key = COALESCE(b_person_key, @p8) WHERE run_key = @p0 AND deal_id = @p9", 10);

        public static readonly TableDef FactTransactionLine = new TableDef("fact_transaction_line", 73, "run_key", "tick", "seq", "date_key", "deal_id", "step", "kind_code",
            "giver_person_key", "receiver_person_key", "lot_id", "ware_key", "quantity", "location_key", "kin_band_code", "same_household", "answers_message_id",
            "true_r", "sample_weight");

        /// <summary>PersonDaySocial: M5's columns of the person-day row NpcDay writes (after it, order 74; the two arrive on the same tick).</summary>
        public static readonly TableDef FactPersonDaySocial = new TableDef("fact_person_day", 74,
            "UPDATE fact_person_day SET soc_rel_count = @p1, soc_mean_trust = @p2, soc_friends = @p3, soc_enemies = @p4, soc_grudge_sum = @p5, soc_sent = @p6, "
            + "soc_received = @p7, soc_misread = @p8, soc_gifts_given = @p9, soc_gifts_received = @p10, soc_deals = @p11, soc_cheated = @p12 "
            + "WHERE run_key = @p0 AND date_key = @p13 AND person_key = @p14", 15);
    }
}
