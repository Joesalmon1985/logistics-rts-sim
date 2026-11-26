-- Warehouse schema v2 (M3 population events): dim_person, fact_death, fact_person_day, fact_learning, and the personal-goods columns of fact_production.
-- Grains (also in meta_table_grain): dim_person one row per person ever alive in the run; fact_death one row per NpcDied;
-- fact_person_day one row per living person x sim day (NpcDay); fact_production one row per WaresProduced, Produced or Consumed event;
-- fact_learning one row per learning event (kind_code = dim_code 'learning_kind'). Integers only. Later milestones add 0003_..., never edit this file.

CREATE TABLE meta_table_grain (
  table_name TEXT PRIMARY KEY, grain TEXT NOT NULL, source_events TEXT NOT NULL) WITHOUT ROWID;

CREATE TABLE dim_person (                                   -- type 1: death fields are filled once
  person_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL REFERENCES dim_run (run_key),
  npc_id INTEGER NOT NULL UNIQUE,                           -- the sim's persistent Npc.Id
  faction_key INTEGER REFERENCES dim_faction (faction_key), is_founder INTEGER NOT NULL,
  spawn_tick INTEGER, spawn_date_key INTEGER REFERENCES dim_date (date_key),
  spawn_location_key INTEGER REFERENCES dim_location (location_key),
  death_tick INTEGER, death_date_key INTEGER REFERENCES dim_date (date_key), death_cause_code INTEGER,   -- dim_code 'death_cause'
  age_ticks_at_death INTEGER);

CREATE TABLE fact_death (                                   -- E: one NpcDied
  death_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL,
  date_key INTEGER NOT NULL REFERENCES dim_date (date_key), person_key INTEGER NOT NULL REFERENCES dim_person (person_key),
  faction_key INTEGER REFERENCES dim_faction (faction_key), cause_code INTEGER NOT NULL,
  location_key INTEGER REFERENCES dim_location (location_key), age_ticks INTEGER NOT NULL);

CREATE TABLE fact_person_day (                              -- S: one living person x sim day (the day that just ended)
  run_key INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  person_key INTEGER NOT NULL REFERENCES dim_person (person_key), faction_key INTEGER REFERENCES dim_faction (faction_key),
  tick INTEGER NOT NULL, age_days INTEGER NOT NULL, location_key INTEGER REFERENCES dim_location (location_key),
  health INTEGER NOT NULL, nutrition INTEGER NOT NULL,
  hunger INTEGER NOT NULL, fatigue INTEGER NOT NULL, cold INTEGER NOT NULL, fear INTEGER NOT NULL, pain INTEGER NOT NULL,                  -- drives at day end, permille
  mean_hunger INTEGER NOT NULL, mean_fatigue INTEGER NOT NULL, mean_cold INTEGER NOT NULL, mean_fear INTEGER NOT NULL, mean_pain INTEGER NOT NULL,
  ticks_idle INTEGER NOT NULL, ticks_eat INTEGER NOT NULL, ticks_forage INTEGER NOT NULL, ticks_sleep INTEGER NOT NULL, ticks_rest INTEGER NOT NULL,
  ticks_explore INTEGER NOT NULL, ticks_gather INTEGER NOT NULL, ticks_build INTEGER NOT NULL, ticks_flee INTEGER NOT NULL, ticks_carry INTEGER NOT NULL,
  ticks_other INTEGER NOT NULL,                             -- the eleven Activity values, in order; their sum is the ticks lived that day
  walk_ticks INTEGER NOT NULL, meals INTEGER NOT NULL, has_bed INTEGER NOT NULL, carry_units INTEGER NOT NULL,
  decisions INTEGER NOT NULL, learned_share INTEGER NOT NULL, explore_share INTEGER NOT NULL, reward_sum INTEGER NOT NULL, punish_sum INTEGER NOT NULL,
  associations INTEGER NOT NULL, conjunctions INTEGER NOT NULL, mode_code INTEGER NOT NULL,                                               -- dim_code 'learning_mode'
  info_layer INTEGER NOT NULL DEFAULT 1,
  PRIMARY KEY (date_key, person_key)) WITHOUT ROWID;

-- fact_production gains the personal-goods rows (Produced, Consumed); the stage 3 rows (WaresProduced) keep direction 1 and a building.
ALTER TABLE fact_production ADD COLUMN person_key INTEGER REFERENCES dim_person (person_key);
ALTER TABLE fact_production ADD COLUMN direction INTEGER NOT NULL DEFAULT 1;      -- 1 goods entered (WaresProduced, Produced), -1 left (Consumed)
ALTER TABLE fact_production ADD COLUMN reason_code INTEGER;                       -- dim_code 'produce_reason' or 'consume_reason' by direction
ALTER TABLE fact_production ADD COLUMN source_id INTEGER;                         -- Produced: the wild source id
ALTER TABLE fact_production ADD COLUMN hut_id INTEGER;                            -- Consumed: the hut built with it
ALTER TABLE fact_production ADD COLUMN with_tool INTEGER;                         -- Produced: a tool was used

CREATE TABLE fact_learning (                                -- E (sampled for kind 1): one association-store change
  learning_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL,
  date_key INTEGER NOT NULL REFERENCES dim_date (date_key), person_key INTEGER NOT NULL REFERENCES dim_person (person_key),
  faction_key INTEGER REFERENCES dim_faction (faction_key),
  kind_code INTEGER NOT NULL,                               -- dim_code 'learning_kind': 1 association_updated, 2 conjunction_recruited, 3 conjunction_freed, 4 episode_closed
  verb INTEGER,                                             -- kinds 1 and 4
  feature_key INTEGER,                                      -- kind 1: the association key; kind 4: the episode key
  delta INTEGER, new_stw INTEGER, new_ltw INTEGER, origin_code INTEGER,                       -- kind 1 (origin: dim_code 'learning_origin')
  slot INTEGER, feature_a INTEGER, feature_b INTEGER, feature_c INTEGER, strength INTEGER,    -- kinds 2 and 3 (strength: kind 2)
  return_value INTEGER, days_elapsed INTEGER,                                                 -- kind 4
  sample_weight INTEGER NOT NULL DEFAULT 1);
