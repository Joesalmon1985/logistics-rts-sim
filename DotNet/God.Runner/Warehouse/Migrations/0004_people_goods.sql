-- Warehouse schema v4 (F0, SHARED_CONTRACTS F0.14): the people and goods seams M4 and M5 share. Extends the tables of
-- 0002 (dim_person, fact_person_day, fact_production) instead of creating them again, and adds dim_party, fact_ownership
-- and fact_social_day. Grains (also in meta_table_grain): dim_party one row per economic party seen in the run (exactly one
-- of person, household, faction or estate set); fact_ownership one row per OwnershipChanged (one ownership change of one
-- lot; spells are derived from it); fact_social_day one row per day x faction x kind x kin band (an aggregate M4 and M5
-- fill; empty in F0). M4 adds its columns in 0005_households.sql, M5 in 0006_social.sql. Integers only.

-- dim_person: every M4 column, nullable (filled by M4's NpcBorn / NpcImmigrated mappers)
ALTER TABLE dim_person ADD COLUMN origin_code INTEGER;            -- dim_code 'origin': 0 founder, 1 born, 2 immigrant
ALTER TABLE dim_person ADD COLUMN sex INTEGER;
ALTER TABLE dim_person ADD COLUMN birth_tick INTEGER;
ALTER TABLE dim_person ADD COLUMN birth_date_key INTEGER REFERENCES dim_date (date_key);
ALTER TABLE dim_person ADD COLUMN cohort_year INTEGER;
ALTER TABLE dim_person ADD COLUMN mother_person_key INTEGER REFERENCES dim_person (person_key);
ALTER TABLE dim_person ADD COLUMN father_person_key INTEGER REFERENCES dim_person (person_key);     -- believed (household) father
ALTER TABLE dim_person ADD COLUMN bio_father_person_key INTEGER REFERENCES dim_person (person_key); -- analytics-only truth
ALTER TABLE dim_person ADD COLUMN birth_faction_key INTEGER REFERENCES dim_faction (faction_key);
ALTER TABLE dim_person ADD COLUMN birth_location_key INTEGER REFERENCES dim_location (location_key);
ALTER TABLE dim_person ADD COLUMN birth_household_id INTEGER;
ALTER TABLE dim_person ADD COLUMN genome_hash INTEGER;

-- fact_person_day: NpcDay v2 adds two time-use columns (Activity.Care = 11, Activity.Social = 12) and M4's stage and household
ALTER TABLE fact_person_day ADD COLUMN ticks_care INTEGER NOT NULL DEFAULT 0;
ALTER TABLE fact_person_day ADD COLUMN ticks_social INTEGER NOT NULL DEFAULT 0;
ALTER TABLE fact_person_day ADD COLUMN life_stage_code INTEGER;  -- dim_code 'life_stage' (M4)
ALTER TABLE fact_person_day ADD COLUMN household_id INTEGER;     -- M4

-- fact_production: Produced and Consumed v2 carry the lot and its owner
ALTER TABLE fact_production ADD COLUMN lot_id INTEGER;            -- Produced: the new lot (0: used at once); Consumed: the lot left
ALTER TABLE fact_production ADD COLUMN into_lot_id INTEGER;       -- Produced: the id after merging on arrival
ALTER TABLE fact_production ADD COLUMN owner_kind_code INTEGER;   -- dim_code 'owner_kind'
ALTER TABLE fact_production ADD COLUMN owner_id INTEGER;

CREATE TABLE dim_party (                                    -- type 1: one economic party (DATA_WAREHOUSE 2)
  party_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL REFERENCES dim_run (run_key),
  party_kind_code INTEGER NOT NULL,                         -- dim_code 'owner_kind'
  party_id INTEGER NOT NULL,                                -- the sim id of its kind (NpcId, HouseholdId, FactionId, deceased NpcId)
  person_key INTEGER REFERENCES dim_person (person_key),   -- kind person, or the deceased of an estate
  household_id INTEGER, enterprise_id INTEGER, institution_id INTEGER,
  faction_key INTEGER REFERENCES dim_faction (faction_key),
  UNIQUE (party_kind_code, party_id));

CREATE TABLE fact_ownership (                               -- E: one OwnershipChanged (an asset's owner changed)
  ownership_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL,
  date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  asset_kind_code INTEGER NOT NULL,                         -- 1 = an item lot
  asset_id INTEGER NOT NULL,                                -- the lot id (after the move)
  ware_key INTEGER REFERENCES dim_ware (ware_key), quantity INTEGER NOT NULL,
  from_party_key INTEGER REFERENCES dim_party (party_key),  -- null: ownerless before
  to_party_key INTEGER REFERENCES dim_party (party_key),    -- null: ownerless after
  basis_code INTEGER NOT NULL,                              -- dim_code 'ownership_basis'
  actor_person_key INTEGER REFERENCES dim_person (person_key),
  sample_weight INTEGER NOT NULL DEFAULT 1);

CREATE TABLE fact_social_day (                              -- S (aggregate): help-like acts per day, faction, kind and believed kin band
  run_key INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  faction_key INTEGER NOT NULL,                             -- 0 when none
  kind_code INTEGER NOT NULL,                               -- the event code aggregated (134 ChildFed, 136 CareGiven, 213 GiftGiven, ...)
  kin_band_code INTEGER NOT NULL,                           -- dim_code 'kin_band'
  events INTEGER NOT NULL, quantity INTEGER NOT NULL,
  PRIMARY KEY (date_key, faction_key, kind_code, kin_band_code)) WITHOUT ROWID;
