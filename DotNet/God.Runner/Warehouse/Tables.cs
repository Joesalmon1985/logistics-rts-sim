using System.Collections.Generic;
using System.Linq;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// One SQL statement the writer prepares once: an INSERT of a fact or dimension row, or an upsert or update of a type 1
    /// dimension. <see cref="Order"/> decides the order inside a day's transaction (dimensions before the facts that point at
    /// them), so a foreign key never sees a missing parent. <see cref="CountsAsRows"/> marks inserts that count in
    /// <c>meta_load.rows_by_table</c>. Later milestones declare their own <see cref="TableDef"/>s next to their mappers.
    /// </summary>
    public sealed class TableDef
    {
        public readonly string Name;
        public readonly string[] Columns;
        public readonly int Order;
        public readonly string Sql;
        public readonly bool CountsAsRows;

        /// <summary>An INSERT of <paramref name="columns"/> into <paramref name="name"/>.</summary>
        public TableDef(string name, int order, params string[] columns)
        {
            Name = name; Order = order; Columns = columns; CountsAsRows = true;
            Sql = "INSERT INTO " + name + " (" + string.Join(", ", columns) + ") VALUES (" + string.Join(", ", columns.Select((_, i) => "@p" + i)) + ")";
        }

        /// <summary>A custom statement (upsert, update) whose parameters are @p0, @p1... in order.</summary>
        public TableDef(string name, int order, string sql, int parameterCount)
        {
            Name = name; Order = order; Sql = sql; CountsAsRows = false;
            Columns = Enumerable.Range(0, parameterCount).Select(i => "p" + i).ToArray();
        }

        public override string ToString() => Name;
    }

    /// <summary>The statements of the M13a core tables. Column lists name only what the sim writes; the rest take defaults.</summary>
    public static class Tables
    {
        // ---- dimensions (order 10-30) ----
        public static readonly TableDef DimDate = new TableDef("dim_date", 10, "date_key", "run_key", "sim_year", "day_of_year", "tick_start", "tick_end", "is_year_start");
        public static readonly TableDef DimCode = new TableDef("dim_code", 11, "code_set", "code", "name", "description");
        public static readonly TableDef DimWare = new TableDef("dim_ware", 12, "ware_key", "name", "category", "is_raw");
        public static readonly TableDef DimFaction = new TableDef("dim_faction", 13, "faction_key", "run_key", "faction_id", "name", "colour_rgb", "is_human");
        public static readonly TableDef DimFactionHistory = new TableDef("dim_faction_history", 14, "faction_hist_key", "faction_key", "valid_from_tick", "valid_to_tick",
            "valid_from_day", "valid_to_day", "is_current", "controller", "eliminated", "change_reason");
        public static readonly TableDef DimFactionHistoryClose = new TableDef("dim_faction_history", 14,
            "UPDATE dim_faction_history SET valid_to_tick = @p1, valid_to_day = @p2, is_current = 0 WHERE faction_hist_key = @p0", 3);
        public static readonly TableDef DimFactionHistoryEliminateInPlace = new TableDef("dim_faction_history", 14,
            "UPDATE dim_faction_history SET eliminated = 1, change_reason = @p1 WHERE faction_hist_key = @p0", 2);
        public static readonly TableDef DimLocation = new TableDef("dim_location", 15, "location_key", "run_key", "kind", "idx", "hex_index", "terrain", "dice_number", "hex_q", "hex_r", "node_i", "node_j");
        public static readonly TableDef DimBuildingUpsert = new TableDef("dim_building", 16,
            "INSERT INTO dim_building (building_key, run_key, building_id, faction_key, initial_faction_key, kind, size_code, is_hq, node_location_key, hex_location_key, placed_tick, completed_tick, destroyed_tick, removed_tick, level) " +
            "VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14) " +
            "ON CONFLICT(building_key) DO UPDATE SET faction_key = excluded.faction_key, kind = excluded.kind, size_code = excluded.size_code, is_hq = excluded.is_hq, " +
            "node_location_key = excluded.node_location_key, hex_location_key = excluded.hex_location_key, placed_tick = excluded.placed_tick, " +
            "completed_tick = excluded.completed_tick, destroyed_tick = excluded.destroyed_tick, removed_tick = excluded.removed_tick, level = excluded.level", 15);
        public static readonly TableDef DimPersonUpsert = new TableDef("dim_person", 17,
            "INSERT INTO dim_person (person_key, run_key, npc_id, faction_key, is_founder, spawn_tick, spawn_date_key, spawn_location_key, death_tick, death_date_key, death_cause_code, age_ticks_at_death) " +
            "VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11) " +
            "ON CONFLICT(person_key) DO UPDATE SET faction_key = excluded.faction_key, is_founder = excluded.is_founder, spawn_tick = excluded.spawn_tick, spawn_date_key = excluded.spawn_date_key, " +
            "spawn_location_key = excluded.spawn_location_key, death_tick = excluded.death_tick, death_date_key = excluded.death_date_key, death_cause_code = excluded.death_cause_code, " +
            "age_ticks_at_death = excluded.age_ticks_at_death", 12);
        public static readonly TableDef MetaTableGrain = new TableDef("meta_table_grain", 7, "table_name", "grain", "source_events");
        public static readonly TableDef MetaEventType = new TableDef("meta_event_type", 5, "code", "version", "name", "domain", "status", "high_volume");
        public static readonly TableDef MetaEventMap = new TableDef("meta_event_map", 6, "code", "version", "target_table", "mapper", "note");

        // ---- facts (order 50 and up) ----
        public static readonly TableDef FactFactionDay = new TableDef("fact_faction_day", 50, "run_key", "date_key", "faction_key", "tick", "eliminated", "victory_points", "hqs", "citadels",
            "buildings", "buildings_built", "units", "walkers", "stock_total", "wars_active", "envoys_out");
        public static readonly TableDef FactFactionDayStock = new TableDef("fact_faction_day_stock", 51, "run_key", "date_key", "faction_key", "ware_key", "quantity");
        public static readonly TableDef FactProduction = new TableDef("fact_production", 52, "run_key", "tick", "seq", "date_key", "faction_key", "building_key", "ware_key", "quantity", "building_kind", "sample_weight");
        /// <summary>The personal-goods rows of fact_production (Produced, Consumed): same table, the columns the M3 migration added.</summary>
        public static readonly TableDef FactProductionPerson = new TableDef("fact_production", 52, "run_key", "tick", "seq", "date_key", "faction_key", "ware_key", "quantity", "sample_weight",
            "person_key", "direction", "reason_code", "source_id", "hut_id", "with_tool");
        public static readonly TableDef FactProductionDay = new TableDef("fact_production_day", 53, "run_key", "date_key", "building_key", "ware_key", "faction_key", "quantity", "cycles");
        public static readonly TableDef FactCombat = new TableDef("fact_combat", 54, "run_key", "tick", "seq", "date_key", "kind_code", "faction_key", "unit_id", "unit_kind_code", "weapon_ware_key",
            "target_kind_code", "target_id", "outcome_code", "damage", "killer_unit_id", "disbanded", "building_key", "x_mm", "z_mm", "sample_weight");
        public static readonly TableDef FactCombatDay = new TableDef("fact_combat_day", 55, "run_key", "date_key", "faction_key", "kind_code", "outcome_code", "events", "damage");
        public static readonly TableDef FactCommand = new TableDef("fact_command", 56, "run_key", "tick", "seq", "date_key", "applied", "command_type", "player_id", "faction_key", "reason", "message", "args");
        public static readonly TableDef FactEvent = new TableDef("fact_event", 57, "run_key", "tick", "seq", "date_key", "event_code", "event_version", "faction_key", "id1", "id2", "id3", "id4", "payload", "sample_weight");
        public static readonly TableDef FactEventDay = new TableDef("fact_event_day", 58, "run_key", "date_key", "event_code", "events");
        public static readonly TableDef FactDeath = new TableDef("fact_death", 61, "run_key", "tick", "seq", "date_key", "person_key", "faction_key", "cause_code", "location_key", "age_ticks");
        public static readonly TableDef FactPersonDay = new TableDef("fact_person_day", 62, "run_key", "date_key", "person_key", "faction_key", "tick", "age_days", "location_key", "health", "nutrition",
            "hunger", "fatigue", "cold", "fear", "pain", "mean_hunger", "mean_fatigue", "mean_cold", "mean_fear", "mean_pain",
            "ticks_idle", "ticks_eat", "ticks_forage", "ticks_sleep", "ticks_rest", "ticks_explore", "ticks_gather", "ticks_build", "ticks_flee", "ticks_carry", "ticks_other",
            "walk_ticks", "meals", "has_bed", "carry_units", "decisions", "learned_share", "explore_share", "reward_sum", "punish_sum", "associations", "conjunctions", "mode_code");
        public static readonly TableDef FactLearning = new TableDef("fact_learning", 63, "run_key", "tick", "seq", "date_key", "person_key", "faction_key", "kind_code", "verb", "feature_key",
            "delta", "new_stw", "new_ltw", "origin_code", "slot", "feature_a", "feature_b", "feature_c", "strength", "return_value", "days_elapsed", "sample_weight");
        /// <summary>The decision trace (M3b DecisionMade, migration 0003): one row per chosen intent, Full only.</summary>
        public static readonly TableDef FactDecision = new TableDef("fact_decision", 64, "run_key", "tick", "seq", "date_key", "person_key", "faction_key", "verb", "target_kind_code",
            "target_id", "target_location_key", "target_class", "total", "instinct", "need", "learned", "social", "culture", "personality", "cost", "explore",
            "candidates", "learned_changed_winner", "explore_won", "continued", "second_verb", "second_total", "sample_weight");
        public static readonly TableDef FactMetricDay = new TableDef("fact_metric_day", 59, "run_key", "date_key", "metric", "value");
        public static readonly TableDef FactRunMetric = new TableDef("fact_run_metric", 60, "run_key", "metric", "source", "value", "deterministic");
        public static readonly TableDef MetaLoad = new TableDef("meta_load", 99, "run_key", "first_day", "last_day", "rows_by_table", "dropped_zero_spells", "loaded_at", "wall_seconds");
    }
}
