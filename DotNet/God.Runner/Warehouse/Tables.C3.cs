namespace God.Runner.Warehouse
{
    /// <summary>C3's table statements (OWNER: C3): migration <c>0016_employment.sql</c>.</summary>
    public static partial class Tables
    {
        public static readonly TableDef FactEmploymentOpen = new TableDef("fact_employment", 160, "run_key", "contract_id", "employer_party_key", "employer_kind_code", "employer_id",
            "agent_person_key", "worker_person_key", "asset_id", "pay_unit_kind_code", "pay_unit_ref", "pay_amount", "per_code", "shift_ticks", "control_code", "from_tick", "from_date_key");
        public static readonly TableDef FactEmploymentClose = new TableDef("fact_employment", 161,
            "UPDATE fact_employment SET to_tick = @p2, to_date_key = @p3, end_reason_code = @p4, ended_by_code = @p5, arrears_units = @p6, units_earned = @p7, units_paid = @p8 "
            + "WHERE run_key = @p0 AND contract_id = @p1", 9);
        public static readonly TableDef FactJobDay = new TableDef("fact_job_day", 162, "run_key", "contract_id", "date_key", "worker_person_key", "work_ticks", "output_count",
            "paid_units", "unpaid_units", "status_code");
        public static readonly TableDef FactEmploymentEvent = new TableDef("fact_employment_event", 163, "run_key", "tick", "seq", "date_key", "contract_id", "kind_code",
            "payer_person_key", "worker_person_key", "ware_key", "quantity", "units", "arrears_units", "by_code", "reason_code", "obligation_id", "lot_id", "control_code");
    }
}
