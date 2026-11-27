-- Warehouse schema v11 (D4 holding D5's slot, SHARED_ARCHITECTURAL_CONTRACTS 18.2): the manifested avatar. References only
-- 0001-0010. Grain: fact_avatar_manifestation one row per avatar event: manifested (AvatarManifested 850, kind 1), dismissed
-- (AvatarDismissed 851, kind 2), destroyed (AvatarDestroyed 852, kind 3), a blow (AvatarDamaged 853, kind 4) or an act the
-- god started in its body (AvatarActionStarted 854, kind 5, high volume: written when rows are wanted). Every manifestation
-- opens once (1) and closes once (2 or 3). The avatar body never has a dim_person row (contracts 4.2): body_id is the
-- AvatarBodyId (0x40000000 + GodId). Empty while Divinity is off. Integers only.

CREATE TABLE fact_avatar_manifestation (                     -- E: manifested 1, dismissed 2, destroyed 3, damaged 4, act 5
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, god_key INTEGER REFERENCES dim_god (god_key), body_id INTEGER NOT NULL,
  kind_code INTEGER NOT NULL, location_key INTEGER REFERENCES dim_location (location_key),
  health INTEGER, created INTEGER, superseded_possession_id INTEGER,               -- kind 1 (health at manifestation), 4 (after the blow)
  end_reason_code INTEGER, start_tick INTEGER, ticks INTEGER, lots_dropped INTEGER, actions INTEGER, observers INTEGER,
  damage_taken INTEGER,                                                             -- kinds 2, 3
  by_person_key INTEGER REFERENCES dim_person (person_key), by_intervention_id INTEGER, cause_code INTEGER, cooldown_until INTEGER, -- 3, 4
  amount INTEGER,                                                                   -- kind 4
  verb INTEGER, target_kind_code INTEGER, target_id INTEGER, target_node INTEGER, action_tick INTEGER, attending INTEGER,  -- kind 5
  control_code INTEGER NOT NULL);                                                   -- 3: avatar
CREATE INDEX ix_fam_intervention ON fact_avatar_manifestation (intervention_id);
