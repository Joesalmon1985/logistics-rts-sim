-- Warehouse schema v1 (M13a core, the subset the current events can fill). DATA_WAREHOUSE_DESIGN 3.4-3.6.
-- Conventions: integers everywhere; every run-scoped table carries run_key; spells are half-open [valid_from, valid_to)
-- with valid_to NULL and is_current = 1 while open; surrogate keys are separate from sim ids (natural keys are columns).
-- Later milestones add tables in 0002_..., never edit this file.

-- ---------------------------------------------------------------- metadata
CREATE TABLE meta_event_type (
  code INTEGER NOT NULL, version INTEGER NOT NULL, name TEXT NOT NULL, domain TEXT NOT NULL,
  status TEXT NOT NULL, high_volume INTEGER NOT NULL,
  PRIMARY KEY (code, version)) WITHOUT ROWID;

CREATE TABLE meta_event_map (
  code INTEGER NOT NULL, version INTEGER NOT NULL, target_table TEXT NOT NULL, mapper TEXT NOT NULL, note TEXT,
  PRIMARY KEY (code, version),
  FOREIGN KEY (code, version) REFERENCES meta_event_type (code, version)) WITHOUT ROWID;

CREATE TABLE meta_load (
  load_id INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, first_day INTEGER NOT NULL, last_day INTEGER NOT NULL,
  rows_by_table TEXT NOT NULL, dropped_zero_spells INTEGER NOT NULL,
  loaded_at TEXT, wall_seconds REAL);                       -- wall clock: here and dim_run only

-- ---------------------------------------------------------------- dimensions
CREATE TABLE dim_run (
  run_key INTEGER PRIMARY KEY, run_id TEXT NOT NULL UNIQUE, experiment_id TEXT, arm TEXT,
  scenario_name TEXT NOT NULL, scenario_hash TEXT NOT NULL, seed INTEGER NOT NULL, replicate INTEGER NOT NULL,
  git_commit TEXT NOT NULL, sim_version TEXT, save_version INTEGER, schema_version INTEGER NOT NULL,
  life_scale INTEGER, ticks_per_day INTEGER, days_per_year INTEGER, faction_count INTEGER,
  victory TEXT, recorder_level TEXT, record_domains TEXT, sample_every INTEGER,
  host TEXT, final_tick INTEGER, final_hash TEXT, stop_reason TEXT, exit_code INTEGER,
  wall_started TEXT, wall_seconds REAL);

CREATE TABLE dim_date (
  date_key INTEGER PRIMARY KEY,                             -- sim day, day 0 first
  run_key INTEGER NOT NULL REFERENCES dim_run (run_key),
  sim_year INTEGER NOT NULL, day_of_year INTEGER NOT NULL,
  tick_start INTEGER NOT NULL, tick_end INTEGER NOT NULL, is_year_start INTEGER NOT NULL);

CREATE TABLE dim_code (                                     -- small enums: unit_kind, attack_outcome, combat_kind...
  code_set TEXT NOT NULL, code INTEGER NOT NULL, name TEXT NOT NULL, description TEXT,
  PRIMARY KEY (code_set, code)) WITHOUT ROWID;

CREATE TABLE dim_ware (                                     -- global; ware_key = the Ware enum value
  ware_key INTEGER PRIMARY KEY, name TEXT NOT NULL, category TEXT NOT NULL, is_raw INTEGER NOT NULL,
  ref_value INTEGER);                                       -- NULL until the economy defines WareRefValue

CREATE TABLE dim_faction (
  faction_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL REFERENCES dim_run (run_key),
  faction_id INTEGER NOT NULL UNIQUE, name TEXT NOT NULL, colour_rgb INTEGER NOT NULL, is_human INTEGER NOT NULL);

CREATE TABLE dim_faction_history (                          -- type 2: controller, eliminated
  faction_hist_key INTEGER PRIMARY KEY, faction_key INTEGER NOT NULL REFERENCES dim_faction (faction_key),
  valid_from_tick INTEGER NOT NULL, valid_to_tick INTEGER, valid_from_day INTEGER NOT NULL, valid_to_day INTEGER,
  is_current INTEGER NOT NULL, controller TEXT NOT NULL, eliminated INTEGER NOT NULL, change_reason TEXT);

CREATE TABLE dim_location (                                 -- hexes up front, nodes as they are first named
  location_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL REFERENCES dim_run (run_key),
  kind TEXT NOT NULL, idx INTEGER NOT NULL, hex_index INTEGER, terrain TEXT, dice_number INTEGER,
  hex_q INTEGER, hex_r INTEGER, node_i INTEGER, node_j INTEGER,
  UNIQUE (kind, idx));

CREATE TABLE dim_building (                                 -- type 1: ticks and owner are filled as events arrive
  building_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL REFERENCES dim_run (run_key),
  building_id INTEGER NOT NULL UNIQUE,
  faction_key INTEGER REFERENCES dim_faction (faction_key), initial_faction_key INTEGER REFERENCES dim_faction (faction_key),
  kind TEXT, size_code INTEGER, is_hq INTEGER NOT NULL,
  node_location_key INTEGER REFERENCES dim_location (location_key), hex_location_key INTEGER REFERENCES dim_location (location_key),
  placed_tick INTEGER, completed_tick INTEGER, destroyed_tick INTEGER, removed_tick INTEGER, level INTEGER);

CREATE TABLE dim_kpi (                                      -- structure only; the KPI engine arrives with M13b
  kpi_key INTEGER PRIMARY KEY, code TEXT NOT NULL, version INTEGER NOT NULL, name TEXT NOT NULL, unit TEXT,
  source_table TEXT, definition TEXT, UNIQUE (code, version));

-- ---------------------------------------------------------------- facts
CREATE TABLE fact_faction_day (                             -- S: faction x day, from SnapshotBuilder.FactionDay
  run_key INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  faction_key INTEGER NOT NULL REFERENCES dim_faction (faction_key), tick INTEGER NOT NULL,
  eliminated INTEGER NOT NULL, victory_points INTEGER NOT NULL, hqs INTEGER NOT NULL, citadels INTEGER NOT NULL,
  buildings INTEGER NOT NULL, buildings_built INTEGER NOT NULL, units INTEGER NOT NULL, walkers INTEGER NOT NULL,
  stock_total INTEGER NOT NULL, wars_active INTEGER NOT NULL, envoys_out INTEGER NOT NULL,
  info_layer INTEGER NOT NULL DEFAULT 1,
  PRIMARY KEY (date_key, faction_key)) WITHOUT ROWID;

CREATE TABLE fact_faction_day_stock (                       -- S: HQ stock per ware (non-zero only)
  run_key INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  faction_key INTEGER NOT NULL REFERENCES dim_faction (faction_key), ware_key INTEGER NOT NULL REFERENCES dim_ware (ware_key),
  quantity INTEGER NOT NULL,
  PRIMARY KEY (date_key, faction_key, ware_key)) WITHOUT ROWID;

CREATE TABLE fact_production (                              -- E: one workshop cycle or harvester delivery (WaresProduced)
  production_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL,
  date_key INTEGER NOT NULL REFERENCES dim_date (date_key), faction_key INTEGER REFERENCES dim_faction (faction_key),
  building_key INTEGER REFERENCES dim_building (building_key), ware_key INTEGER NOT NULL REFERENCES dim_ware (ware_key),
  quantity INTEGER NOT NULL, building_kind TEXT, sample_weight INTEGER NOT NULL DEFAULT 1);

CREATE TABLE fact_production_day (                          -- S (aggregate): day x building x ware, taken before sampling
  run_key INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  building_key INTEGER NOT NULL REFERENCES dim_building (building_key), ware_key INTEGER NOT NULL REFERENCES dim_ware (ware_key),
  faction_key INTEGER NOT NULL, quantity INTEGER NOT NULL, cycles INTEGER NOT NULL,   -- the faction that owned the building at the event
  PRIMARY KEY (date_key, building_key, ware_key, faction_key)) WITHOUT ROWID;

CREATE TABLE fact_combat (                                  -- E: kind_code = dim_code 'combat_kind' (attack, unit_died, building_destroyed)
  combat_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL,
  date_key INTEGER NOT NULL REFERENCES dim_date (date_key), kind_code INTEGER NOT NULL,
  faction_key INTEGER REFERENCES dim_faction (faction_key), unit_id INTEGER, unit_kind_code INTEGER,
  weapon_ware_key INTEGER REFERENCES dim_ware (ware_key), target_kind_code INTEGER, target_id INTEGER,
  outcome_code INTEGER, damage INTEGER, killer_unit_id INTEGER, disbanded INTEGER,
  building_key INTEGER REFERENCES dim_building (building_key), x_mm INTEGER, z_mm INTEGER,
  sample_weight INTEGER NOT NULL DEFAULT 1);

CREATE TABLE fact_combat_day (                              -- S (aggregate): day x faction x kind x outcome, taken before sampling
  run_key INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  faction_key INTEGER NOT NULL, kind_code INTEGER NOT NULL, outcome_code INTEGER NOT NULL,
  events INTEGER NOT NULL, damage INTEGER NOT NULL,
  PRIMARY KEY (date_key, faction_key, kind_code, outcome_code)) WITHOUT ROWID;

CREATE TABLE fact_command (                                 -- E: a command applied or rejected at the tick boundary
  command_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL,
  date_key INTEGER NOT NULL REFERENCES dim_date (date_key), applied INTEGER NOT NULL, command_type TEXT NOT NULL,
  player_id INTEGER, faction_key INTEGER REFERENCES dim_faction (faction_key), reason TEXT, message TEXT, args TEXT);

CREATE TABLE fact_policy_change (                           -- E + H; no sim event fills it before the state milestone
  policy_change_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, tick INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  faction_key INTEGER REFERENCES dim_faction (faction_key), policy_code INTEGER NOT NULL, old_value INTEGER, new_value INTEGER,
  decided_by TEXT, valid_from_tick INTEGER NOT NULL, valid_to_tick INTEGER, valid_from_day INTEGER NOT NULL, valid_to_day INTEGER,
  is_current INTEGER NOT NULL);

CREATE TABLE fact_kpi (                                     -- S: actual vs reported; structure only until M13b
  kpi_fact_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, kpi_key INTEGER NOT NULL REFERENCES dim_kpi (kpi_key),
  faction_key INTEGER REFERENCES dim_faction (faction_key), institution_key INTEGER,
  period_from_day INTEGER NOT NULL, period_to_day INTEGER NOT NULL,
  actual_value INTEGER, reported_value INTEGER, report_tick INTEGER, reporter_institution_key INTEGER,
  method_code INTEGER, kpi_version INTEGER, lo_value INTEGER, hi_value INTEGER, sample_n INTEGER, coverage INTEGER);

CREATE TABLE fact_event (                                   -- E: any mapped event with no dedicated table (meta_event_map 'fact_event')
  event_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL,
  date_key INTEGER NOT NULL REFERENCES dim_date (date_key), event_code INTEGER NOT NULL, event_version INTEGER NOT NULL,
  faction_key INTEGER REFERENCES dim_faction (faction_key), id1 INTEGER, id2 INTEGER, id3 INTEGER, id4 INTEGER,
  payload TEXT, sample_weight INTEGER NOT NULL DEFAULT 1);

CREATE TABLE fact_event_day (                               -- S (aggregate): every recorded-domain event of the day, taken before sampling
  run_key INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key), event_code INTEGER NOT NULL,
  events INTEGER NOT NULL,
  PRIMARY KEY (date_key, event_code)) WITHOUT ROWID;

CREATE TABLE fact_run_metric (                              -- run totals from the Metrics recorder and the writer's own counters
  run_key INTEGER NOT NULL, metric TEXT NOT NULL, source TEXT NOT NULL, value INTEGER NOT NULL,
  deterministic INTEGER NOT NULL DEFAULT 1,                 -- 0 for wall-clock and queue counters
  PRIMARY KEY (run_key, metric)) WITHOUT ROWID;

CREATE TABLE fact_metric_day (                              -- the Metrics recorder's Latest at every day end
  run_key INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key), metric TEXT NOT NULL, value INTEGER NOT NULL,
  PRIMARY KEY (date_key, metric)) WITHOUT ROWID;

-- ---------------------------------------------------------------- stable views (redefined by later migrations)
CREATE VIEW v_faction_day AS
  SELECT d.date_key, d.sim_year, f.faction_id, f.name AS faction, x.*
  FROM fact_faction_day x JOIN dim_date d ON d.date_key = x.date_key JOIN dim_faction f ON f.faction_key = x.faction_key;

CREATE VIEW v_events AS
  SELECT e.tick, e.seq, e.date_key, t.name AS event, t.domain, e.faction_key, e.id1, e.id2, e.id3, e.id4, e.payload, e.sample_weight
  FROM fact_event e JOIN meta_event_type t ON t.code = e.event_code AND t.version = e.event_version;
