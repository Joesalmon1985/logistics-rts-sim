-- Warehouse schema v7 (F1, SHARED_ARCHITECTURAL_CONTRACTS 18.2): the divine core. Grains (also in meta_table_grain once
-- D1 maps them): dim_god one row per god (GodId 1..FactionCount, created once, never removed); dim_god_history one row per
-- interval of a god's seat, policy and Mythic mode (type 2; to_tick null while open); fact_god_faction one row per spell of
-- a god's association with a faction (to_tick null while open); fact_resource_ledger one row per miracle creation or
-- destruction (ResourceCreated / ResourceDestroyed, LedgerId). The person-action tables gain control_code (0 own, 1
-- compelled, 2 possessed, 3 avatar; filled from the event, never inferred) and intervention_id (the InterventionId of a
-- divine cause, null otherwise). Every table is empty, and every new column null, while Divinity is off. References only
-- 0001-0006. Integers only.

CREATE TABLE dim_god (                                       -- type 1: one god (GodId = faction id + 1)
  god_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, god_id INTEGER NOT NULL,
  name TEXT NOT NULL,                                       -- procedural (Names.God), never player text
  origin_faction_key INTEGER REFERENCES dim_faction (faction_key),  -- history only, never an authority
  created_tick INTEGER NOT NULL, created_date_key INTEGER REFERENCES dim_date (date_key),
  avatar_body_id INTEGER);                                  -- 0x40000000 + GodId once manifested, else null
CREATE UNIQUE INDEX ux_dim_god ON dim_god (run_key, god_id);

CREATE TABLE dim_god_history (                               -- type 2: who drives the god and how its Mythic is charged
  god_hist_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, god_key INTEGER NOT NULL REFERENCES dim_god (god_key),
  from_tick INTEGER NOT NULL, to_tick INTEGER,
  seat_code INTEGER NOT NULL,                               -- GodSeat: 0 none, 1 human, 2 ai
  policy_name TEXT, mythic_mode_code INTEGER,               -- DivinityMode
  change_reason TEXT);
CREATE INDEX ix_dgh_god ON dim_god_history (god_key, from_tick);

CREATE TABLE fact_god_faction (                              -- S (spells): a god's association with a faction
  run_key INTEGER NOT NULL, god_key INTEGER NOT NULL REFERENCES dim_god (god_key),
  faction_key INTEGER NOT NULL REFERENCES dim_faction (faction_key),
  from_tick INTEGER NOT NULL, to_tick INTEGER, kind_code INTEGER NOT NULL);   -- 1 origin; later kinds appended by D1/D6
CREATE INDEX ix_fgf_god ON fact_god_faction (god_key, from_tick);

CREATE TABLE fact_resource_ledger (                          -- E: one ResourceCreated (direction 1) or ResourceDestroyed (-1)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  ledger_id INTEGER NOT NULL, intervention_id INTEGER NOT NULL, god_key INTEGER REFERENCES dim_god (god_key),
  direction INTEGER NOT NULL, ware_key INTEGER NOT NULL REFERENCES dim_ware (ware_key), quantity INTEGER NOT NULL,
  lot_id INTEGER, holder_kind_code INTEGER, holder_id INTEGER, location_key INTEGER REFERENCES dim_location (location_key));
CREATE INDEX ix_frl_intervention ON fact_resource_ledger (intervention_id);

-- the code sets of the divine core (global rows)
INSERT INTO dim_code (code_set, code, name, description) VALUES
  ('control_source', 0, 'own', 'the person''s own policy decided'),
  ('control_source', 1, 'compelled', 'a god''s compulsion overrode the choice'),
  ('control_source', 2, 'possessed', 'a god drove the body'),
  ('control_source', 3, 'avatar', 'a god''s avatar body'),
  ('cause_kind', 0, 'none', 'no divine cause'),
  ('cause_kind', 1, 'suggestion', 'InterventionKind.Suggestion'),
  ('cause_kind', 2, 'command', 'InterventionKind.Command'),
  ('cause_kind', 3, 'compulsion', 'InterventionKind.Compulsion'),
  ('cause_kind', 4, 'possession', 'InterventionKind.Possession'),
  ('cause_kind', 5, 'commandment', 'InterventionKind.Commandment'),
  ('cause_kind', 6, 'miracle', 'InterventionKind.Miracle'),
  ('cause_kind', 7, 'avatar', 'InterventionKind.Avatar'),
  ('mythic_category', 1, 'credit', 'regeneration credited at day start'),
  ('mythic_category', 2, 'spend', 'a divine action''s cost'),
  ('mythic_category', 3, 'upkeep', 'a continuing control''s cost'),
  ('mythic_category', 4, 'config', 'a sandbox configuration change');

-- the person-action tables: who controlled the act and which intervention caused it
ALTER TABLE fact_decision ADD COLUMN control_code INTEGER;
ALTER TABLE fact_decision ADD COLUMN intervention_id INTEGER;
ALTER TABLE fact_production ADD COLUMN control_code INTEGER;
ALTER TABLE fact_production ADD COLUMN intervention_id INTEGER;
ALTER TABLE fact_ownership ADD COLUMN control_code INTEGER;
ALTER TABLE fact_ownership ADD COLUMN intervention_id INTEGER;
ALTER TABLE fact_care ADD COLUMN control_code INTEGER;
ALTER TABLE fact_care ADD COLUMN intervention_id INTEGER;
ALTER TABLE fact_social_interaction ADD COLUMN control_code INTEGER;
ALTER TABLE fact_social_interaction ADD COLUMN intervention_id INTEGER;
ALTER TABLE fact_transaction_line ADD COLUMN control_code INTEGER;
ALTER TABLE fact_transaction_line ADD COLUMN intervention_id INTEGER;
