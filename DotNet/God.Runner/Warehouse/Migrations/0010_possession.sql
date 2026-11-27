-- Warehouse schema v10 (D4, SHARED_ARCHITECTURAL_CONTRACTS 18.2; the contracts' name fact_possession governs, LEAD_RULINGS R7):
-- possession. References only 0001-0009. Grain: fact_possession one row per possession event: started (PossessionStarted 845,
-- kind 1), ended (PossessionEnded 846, kind 2, with the end reason and the spell's totals) or an act the god started in the
-- body (PossessedActionStarted 847, kind 3, high volume: written when rows are wanted), so every spell opens and closes
-- exactly once (CT1). intervention_id is the one InterventionId (contracts 1). Empty while Divinity is off. Integers only.

CREATE TABLE fact_possession (                               -- E: a possession started (1), ended (2) or started an act (3)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, god_key INTEGER REFERENCES dim_god (god_key), person_key INTEGER REFERENCES dim_person (person_key),
  kind_code INTEGER NOT NULL, location_key INTEGER REFERENCES dim_location (location_key),
  superseded_compulsion_id INTEGER, superseded_avatar_id INTEGER,                  -- kind 1
  end_reason_code INTEGER, start_tick INTEGER, ticks INTEGER,                      -- kind 2
  inputs_applied INTEGER, inputs_refused INTEGER, actions INTEGER,                 -- kind 2
  verb INTEGER, target_kind_code INTEGER, target_id INTEGER, target_node INTEGER, action_tick INTEGER,   -- kind 3
  control_code INTEGER NOT NULL);                                                  -- 2: possessed
CREATE INDEX ix_fpo_intervention ON fact_possession (intervention_id);
