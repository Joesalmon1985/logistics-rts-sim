namespace God.Runner.Warehouse
{
    /// <summary>
    /// M4's table statements (migration 0005; OWNER: M4). <c>MapContext.Insert</c> puts the run key first, so every statement's
    /// first column or parameter is <c>run_key</c>. Orders: dimensions and the updates of F0 rows after the rows they update.
    /// </summary>
    public static partial class Tables
    {
        public static readonly TableDef DimPersonM4 = new TableDef("dim_person", 19,
            "UPDATE dim_person SET sex = @p2, generation = @p3, genome_hash = @p4, max_age_milli = @p5, strength = @p6, size_pheno = @p7, " +
            "health_pheno = @p8, fertility_pheno = @p9, lifespan_pheno = @p10, features = @p11, complexion = @p12, build = @p13, nurture_gain = @p14, " +
            "kin_weight = @p15, origin_code = COALESCE(origin_code, 0) WHERE run_key = @p0 AND person_key = @p1", 16);
        public static readonly TableDef DimPersonBirth = new TableDef("dim_person", 19,
            "UPDATE dim_person SET origin_code = 1, birth_tick = @p2, birth_date_key = @p3, cohort_year = @p4, mother_person_key = @p5, father_person_key = @p6, " +
            "bio_father_person_key = @p7, birth_faction_key = @p8, birth_location_key = @p9, birth_household_id = @p10 WHERE run_key = @p0 AND person_key = @p1", 11);
        public static readonly TableDef DimPersonArrival = new TableDef("dim_person", 19,
            "UPDATE dim_person SET origin_code = 2, birth_faction_key = @p2 WHERE run_key = @p0 AND person_key = @p1", 3);
        public static readonly TableDef DimHousehold = new TableDef("dim_household", 20, "run_key", "household_id", "faction_key", "formed_tick", "formed_date_key",
            "founder_person_key", "formed_reason_code", "hut_id");
        public static readonly TableDef DimHouseholdDissolved = new TableDef("dim_household", 21,
            "UPDATE dim_household SET dissolved_tick = @p2, dissolved_date_key = @p3, lots_dropped = @p4 WHERE run_key = @p0 AND household_id = @p1", 5);
        public static readonly TableDef FactBirth = new TableDef("fact_birth", 70, "run_key", "tick", "seq", "date_key", "person_key", "mother_person_key",
            "father_person_key", "bio_father_person_key", "household_id", "faction_key", "location_key", "sex", "generation", "genome_hash");
        public static readonly TableDef FactMigration = new TableDef("fact_migration", 71, "run_key", "tick", "seq", "date_key", "person_key", "faction_key", "direction", "location_key", "age_milli");
        public static readonly TableDef FactMembershipOpen = new TableDef("fact_household_membership", 72, "run_key", "membership_key", "person_key", "household_id",
            "from_tick", "from_date_key", "join_reason_code");
        public static readonly TableDef FactMembershipClose = new TableDef("fact_household_membership", 73,
            "UPDATE fact_household_membership SET to_tick = @p2, to_date_key = @p3, leave_reason_code = @p4 WHERE run_key = @p0 AND membership_key = @p1", 5);
        public static readonly TableDef FactCare = new TableDef("fact_care", 74, "run_key", "tick", "seq", "date_key", "kind_code", "carer_person_key", "cared_person_key",
            "faction_key", "ticks", "ware_key", "quantity", "lot_id", "kin_band_code", "true_r", "arrangement_id", "sample_weight");
        public static readonly TableDef FactInheritance = new TableDef("fact_inheritance", 75, "run_key", "tick", "seq", "date_key", "estate_person_key", "asset_kind_code",
            "asset_id", "ware_key", "quantity", "to_party_key", "reason_code", "executor_person_key");
        public static readonly TableDef FactHouseholdDay = new TableDef("fact_household_day", 76, "run_key", "date_key", "household_id", "faction_key", "hut_id", "members",
            "children", "elders", "store_food", "store_units");
        /// <summary>M4's columns of a person-day row (written after the row itself, order 62).</summary>
        public static readonly TableDef FactPersonDayM4 = new TableDef("fact_person_day", 77,
            "UPDATE fact_person_day SET life_stage_code = @p3, household_id = @p4, age_milli = @p5, partner_person_key = @p6, care_given_ticks = @p7, " +
            "care_received_ticks = @p8, pregnant = @p9, ill = @p10, growth = @p11 WHERE run_key = @p0 AND person_key = @p1 AND date_key = @p2", 12);
    }
}
