namespace God.Runner.Warehouse
{
    /// <summary>C6's table statements (OWNER: C6): migration <c>0018_organisations.sql</c> (with C8's Tier 2 welfare fact, R49).</summary>
    public static partial class Tables
    {
        public static readonly TableDef DimOrg = new TableDef("dim_org", 180, "run_key", "org_key", "template_code", "type_code", "ownership_code", "faction_key",
            "place_location_key", "store_bin_id", "founders_n", "origin_code", "founded_tick", "founded_date_key");
        public static readonly TableDef DimOrgDissolved = new TableDef("dim_org", 181,
            "UPDATE dim_org SET dissolved_tick = @p2, dissolved_date_key = @p3, dissolve_reason_code = @p4, dissolve_rule_code = @p5 WHERE run_key = @p0 AND org_key = @p1", 6);
        public static readonly TableDef DimOrgHistoryOpen = new TableDef("dim_org_history", 182, "run_key", "org_hist_key", "org_key", "status_code", "from_tick");
        public static readonly TableDef DimOrgHistoryClose = new TableDef("dim_org_history", 183,
            "UPDATE dim_org_history SET to_tick = @p2 WHERE run_key = @p0 AND org_hist_key = @p1", 3);
        public static readonly TableDef FactOrgMembershipOpen = new TableDef("fact_org_membership", 184, "run_key", "membership_key", "org_key", "person_key", "role_code",
            "office_index", "office_kind_code", "basis_code", "from_tick", "from_date_key");
        public static readonly TableDef FactOrgMembershipClose = new TableDef("fact_org_membership", 185,
            "UPDATE fact_org_membership SET to_tick = @p2, to_date_key = @p3, end_reason_code = @p4 WHERE run_key = @p0 AND membership_key = @p1", 5);
        public static readonly TableDef FactOrgExpenditure = new TableDef("fact_org_expenditure", 186, "run_key", "tick", "seq", "date_key", "org_key", "actor_person_key",
            "category_code", "ware_key", "quantity", "payee_person_key", "request_id", "control_code");
        public static readonly TableDef FactOrgDay = new TableDef("fact_org_day", 187, "run_key", "org_key", "day", "date_key", "status_code", "members", "staff", "offices_vacant",
            "store_units", "requests_open", "served", "refused", "expired", "deposits", "records", "decisions", "true_unmet");
        public static readonly TableDef FactOrgDayStore = new TableDef("fact_org_day_store", 188, "run_key", "org_key", "day", "ware_key", "quantity");
        public static readonly TableDef FactServiceRequest = new TableDef("fact_service_request", 189, "run_key", "request_id", "org_key", "kind_code", "requester_person_key",
            "subject_kind_code", "subject_id", "place_location_key", "received_by_person_key", "control_code", "true_need", "opened_tick", "opened_date_key", "status_code");
        public static readonly TableDef FactServiceAccepted = new TableDef("fact_service_request", 190,
            "UPDATE fact_service_request SET accepted_tick = @p2, provider_person_key = @p3, status_code = 2 WHERE run_key = @p0 AND request_id = @p1", 4);
        public static readonly TableDef FactServiceDelivered = new TableDef("fact_service_request", 191,
            "UPDATE fact_service_request SET delivered_tick = @p2, provider_person_key = @p3, status_code = 3 WHERE run_key = @p0 AND request_id = @p1", 4);
        public static readonly TableDef FactServiceRefused = new TableDef("fact_service_request", 192,
            "UPDATE fact_service_request SET refused_tick = @p2, refused_reason_code = @p3, status_code = 4 WHERE run_key = @p0 AND request_id = @p1", 4);
        public static readonly TableDef FactServiceExpired = new TableDef("fact_service_request", 193,
            "UPDATE fact_service_request SET expired_tick = @p2, status_code = 6 WHERE run_key = @p0 AND request_id = @p1", 3);
        public static readonly TableDef FactServiceRecorded = new TableDef("fact_service_request", 194,
            "UPDATE fact_service_request SET recorded_outcome = @p2 WHERE run_key = @p0 AND request_id = @p1", 3);
        public static readonly TableDef FactServiceDelivery = new TableDef("fact_service_delivery", 195, "run_key", "tick", "seq", "date_key", "request_id", "org_key",
            "provider_person_key", "ticks", "goods_used", "outcome_event_code", "control_code");
        public static readonly TableDef FactServiceOutcome = new TableDef("fact_service_outcome", 196, "run_key", "tick", "seq", "date_key", "request_id", "org_key",
            "recorder_person_key", "recorded_outcome", "true_outcome");
        public static readonly TableDef FactOrgDecision = new TableDef("fact_org_decision", 197, "run_key", "tick", "seq", "date_key", "org_key", "decision_id",
            "proposer_person_key", "source_code", "source_ref", "what_code", "a0", "a1", "outcome_code", "rule_code", "accept_n", "refuse_n", "absent_n", "counter_person_key");
        public static readonly TableDef FactWelfare = new TableDef("fact_welfare", 198, "run_key", "tick", "seq", "date_key", "org_key", "keeper_person_key",
            "recipient_person_key", "ware_key", "quantity", "request_id", "control_code");
    }
}
