-- Indexes (DATA_WAREHOUSE_DESIGN 4.4), created at the end of a load, then ANALYZE and PRAGMA optimize.
CREATE INDEX ix_dim_building_faction ON dim_building (faction_key);
CREATE INDEX ix_fact_production_day ON fact_production (date_key, faction_key, ware_key);
CREATE INDEX ix_fact_production_building ON fact_production (building_key);
CREATE INDEX ix_fact_combat_day ON fact_combat (date_key, kind_code);
CREATE INDEX ix_fact_command_day ON fact_command (date_key);
CREATE INDEX ix_fact_event_code ON fact_event (event_code, tick);
CREATE INDEX ix_fact_kpi ON fact_kpi (kpi_key, period_from_day);
CREATE INDEX ix_faction_hist ON dim_faction_history (faction_key, valid_from_tick);
