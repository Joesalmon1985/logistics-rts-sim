namespace God.Runner.Warehouse
{
    /// <summary>C1's table statements (OWNER: C1): migration <c>0015_ownership_work.sql</c>.</summary>
    public static partial class Tables
    {
        public static readonly TableDef FactOwnershipSpellOpen = new TableDef("fact_ownership_spell", 150, "run_key", "spell_key", "asset_id", "kind_code",
            "owner_party_key", "owner_kind_code", "owner_id", "shares_permille", "basis_code", "from_tick", "from_date_key", "location_key");
        public static readonly TableDef FactOwnershipSpellClose = new TableDef("fact_ownership_spell", 151,
            "UPDATE fact_ownership_spell SET to_tick = @p2, to_date_key = @p3, end_code = @p4 WHERE run_key = @p0 AND spell_key = @p1", 5);
        public static readonly TableDef FactUseRightOpen = new TableDef("fact_use_right", 152, "run_key", "right_key", "asset_id", "holder_party_key", "use_kind_code",
            "granted_by_party_key", "until_day", "from_tick", "from_date_key");
        public static readonly TableDef FactUseRightClose = new TableDef("fact_use_right", 153,
            "UPDATE fact_use_right SET to_tick = @p2, to_date_key = @p3, end_reason_code = @p4 WHERE run_key = @p0 AND right_key = @p1", 5);
        public static readonly TableDef FactEconRelOpen = new TableDef("fact_economic_relationship", 154, "run_key", "relationship_key", "a_party_key", "b_party_key",
            "kind_code", "asset_id", "from_tick", "from_date_key");
        public static readonly TableDef FactEconRelClose = new TableDef("fact_economic_relationship", 155,
            "UPDATE fact_economic_relationship SET to_tick = @p2, to_date_key = @p3 WHERE run_key = @p0 AND relationship_key = @p1", 4);
        public static readonly TableDef FactAssetUse = new TableDef("fact_asset_use", 156, "run_key", "tick", "seq", "date_key", "asset_id", "worker_person_key",
            "owner_party_key", "control_code", "intervention_id", "ware_key", "quantity", "right_code", "sample_weight");
        public static readonly TableDef FactTheft = new TableDef("fact_theft", 157, "run_key", "tick", "seq", "date_key", "kind_code", "lot_id", "asset_id", "ware_key",
            "quantity", "owner_party_key", "taker_person_key", "location_key", "from_holder_kind_code", "from_holder_id");
        public static readonly TableDef FactSourceClaim = new TableDef("fact_source_claim", 158, "run_key", "tick", "seq", "date_key", "source_id", "claimant_person_key", "basis_code");
    }
}
