-- Warehouse schema v15 (C1, SHARED_ARCHITECTURAL_CONTRACTS 18.2 0015): ownership spells, use rights and economic
-- relationships of productive assets, plus their event tables. Grains: fact_ownership_spell one row per spell of one
-- owner holding one asset (AssetBuilt or AssetTransferred opens it; AssetTransferred or AssetDestroyed closes it; to_tick
-- null while open; half-open from_tick to to_tick; one owner holds 1000 permille in C1); fact_use_right one row per spell
-- of one use right (UseRightGranted opens, UseRightEnded closes); fact_economic_relationship one row per spell of a
-- relationship between two parties (C1 writes kind 1, a use-right holder using an asset of the owner; C3 adds employment
-- kinds); fact_asset_use one row per completed work unit (AssetUsed; sampled below Full); fact_theft one row per
-- TheftCommitted (analytics-only truth: written at Full only); fact_source_claim one row per SourceClaimed. References
-- only 0001-0007. Integers only.

CREATE TABLE fact_ownership_spell (                          -- S (spells): one owner of one asset
  spell_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, asset_id INTEGER NOT NULL,
  kind_code INTEGER NOT NULL,                               -- the asset kind (C1 AssetTable: 1 field, 2 workshop, 3 store)
  owner_party_key INTEGER REFERENCES dim_party (party_key), owner_kind_code INTEGER NOT NULL, owner_id INTEGER NOT NULL,
  shares_permille INTEGER NOT NULL, basis_code INTEGER NOT NULL,
  from_tick INTEGER NOT NULL, from_date_key INTEGER REFERENCES dim_date (date_key),
  to_tick INTEGER, to_date_key INTEGER REFERENCES dim_date (date_key), end_code INTEGER,   -- 1 transferred, 2 destroyed
  location_key INTEGER REFERENCES dim_location (location_key));
CREATE INDEX ix_fos_asset ON fact_ownership_spell (asset_id, from_tick);

CREATE TABLE fact_use_right (                                -- S (spells): one use right of one holder on one asset
  right_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, asset_id INTEGER NOT NULL,
  holder_party_key INTEGER REFERENCES dim_party (party_key), use_kind_code INTEGER NOT NULL,
  granted_by_party_key INTEGER REFERENCES dim_party (party_key), until_day INTEGER,
  from_tick INTEGER NOT NULL, from_date_key INTEGER REFERENCES dim_date (date_key),
  to_tick INTEGER, to_date_key INTEGER REFERENCES dim_date (date_key), end_reason_code INTEGER);
CREATE INDEX ix_fur_asset ON fact_use_right (asset_id, from_tick);

CREATE TABLE fact_economic_relationship (                    -- S (spells): one relationship of two parties
  relationship_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL,
  a_party_key INTEGER REFERENCES dim_party (party_key), b_party_key INTEGER REFERENCES dim_party (party_key),
  kind_code INTEGER NOT NULL, asset_id INTEGER,
  from_tick INTEGER NOT NULL, from_date_key INTEGER REFERENCES dim_date (date_key),
  to_tick INTEGER, to_date_key INTEGER REFERENCES dim_date (date_key));
CREATE INDEX ix_fer_a ON fact_economic_relationship (a_party_key, from_tick);

CREATE TABLE fact_asset_use (                                -- E: one completed work unit
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  asset_id INTEGER NOT NULL, worker_person_key INTEGER REFERENCES dim_person (person_key),
  owner_party_key INTEGER REFERENCES dim_party (party_key), control_code INTEGER, intervention_id INTEGER,
  ware_key INTEGER REFERENCES dim_ware (ware_key), quantity INTEGER, right_code INTEGER, sample_weight INTEGER NOT NULL);
CREATE INDEX ix_fau_asset ON fact_asset_use (asset_id, tick);

CREATE TABLE fact_theft (                                    -- E (truth, Full only): one classified taking without permission
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  kind_code INTEGER NOT NULL,                               -- 1 goods, 2 use of an asset
  lot_id INTEGER, asset_id INTEGER, ware_key INTEGER REFERENCES dim_ware (ware_key), quantity INTEGER,
  owner_party_key INTEGER REFERENCES dim_party (party_key), taker_person_key INTEGER REFERENCES dim_person (person_key),
  location_key INTEGER REFERENCES dim_location (location_key), from_holder_kind_code INTEGER, from_holder_id INTEGER);

CREATE TABLE fact_source_claim (                             -- E: one first-arrival claim (recorded, never enforced)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  source_id INTEGER NOT NULL, claimant_person_key INTEGER REFERENCES dim_person (person_key), basis_code INTEGER NOT NULL);

INSERT INTO dim_code (code_set, code, name, description) VALUES
  ('asset_kind', 1, 'field', 'a worked plot: roots from labour'),
  ('asset_kind', 2, 'workshop', 'a recipe: a log into a plank'),
  ('asset_kind', 3, 'store', 'only a store'),
  ('use_kind', 1, 'work', 'may work the asset'),
  ('use_kind', 2, 'store', 'may put into and take from its store'),
  ('use_kind', 3, 'gather', 'may gather from it'),
  ('use_kind', 4, 'pass', 'may pass through it'),
  ('theft_kind', 1, 'goods', 'goods taken without the true permission'),
  ('theft_kind', 2, 'use', 'an asset worked without any right'),
  ('economic_relationship', 1, 'uses_asset_of', 'holds a use right on an asset of the other party');
