-- Warehouse schema v5 (M4, SHARED_CONTRACTS 8.2, F0.14): life cycle, households, kinship, care and estates. Grains (also in
-- meta_table_grain): fact_birth one row per NpcBorn; fact_migration one row per NpcImmigrated or NpcEmigrated;
-- dim_household one row per household ever formed (dissolution filled once); fact_household_membership one row per
-- membership interval (HouseholdJoined opens it, HouseholdLeft closes it; an open interval has a null to_tick);
-- fact_care one row per ChildFed or CareGiven; fact_inheritance one row per EstateTransfer (asset id = the lot id);
-- fact_household_day one row per household x sim day (HouseholdDay). M4's dim_person and fact_person_day columns are
-- added here (nullable; filled from NpcGenome, NpcBorn, NpcImmigrated and PersonDayLife). Integers only.

-- dim_person: generation and the birth phenotype (analytics; the mind never reads them)
ALTER TABLE dim_person ADD COLUMN generation INTEGER;
ALTER TABLE dim_person ADD COLUMN max_age_milli INTEGER;
ALTER TABLE dim_person ADD COLUMN strength INTEGER;
ALTER TABLE dim_person ADD COLUMN size_pheno INTEGER;
ALTER TABLE dim_person ADD COLUMN health_pheno INTEGER;
ALTER TABLE dim_person ADD COLUMN fertility_pheno INTEGER;
ALTER TABLE dim_person ADD COLUMN lifespan_pheno INTEGER;
ALTER TABLE dim_person ADD COLUMN features INTEGER;
ALTER TABLE dim_person ADD COLUMN complexion INTEGER;
ALTER TABLE dim_person ADD COLUMN build INTEGER;
ALTER TABLE dim_person ADD COLUMN nurture_gain INTEGER;
ALTER TABLE dim_person ADD COLUMN kin_weight INTEGER;

-- fact_person_day: M4's columns of the person-day row (PersonDayLife; life_stage_code and household_id are F0's)
ALTER TABLE fact_person_day ADD COLUMN age_milli INTEGER;
ALTER TABLE fact_person_day ADD COLUMN partner_person_key INTEGER;
ALTER TABLE fact_person_day ADD COLUMN care_given_ticks INTEGER;
ALTER TABLE fact_person_day ADD COLUMN care_received_ticks INTEGER;
ALTER TABLE fact_person_day ADD COLUMN pregnant INTEGER;
ALTER TABLE fact_person_day ADD COLUMN ill INTEGER;
ALTER TABLE fact_person_day ADD COLUMN growth INTEGER;
CREATE INDEX IF NOT EXISTS ix_person_day_person ON fact_person_day (person_key, date_key);

CREATE TABLE fact_birth (                                   -- E: one birth
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  person_key INTEGER NOT NULL REFERENCES dim_person (person_key),
  mother_person_key INTEGER REFERENCES dim_person (person_key),
  father_person_key INTEGER REFERENCES dim_person (person_key),      -- believed (the mother's partner)
  bio_father_person_key INTEGER REFERENCES dim_person (person_key),  -- analytics-only truth
  household_id INTEGER, faction_key INTEGER, location_key INTEGER, sex INTEGER, generation INTEGER, genome_hash INTEGER);

CREATE TABLE fact_migration (                               -- E: one arrival (direction 1) or departure (-1)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  person_key INTEGER NOT NULL REFERENCES dim_person (person_key), faction_key INTEGER, direction INTEGER NOT NULL, location_key INTEGER, age_milli INTEGER);

CREATE TABLE dim_household (                                -- type 1: one household ever formed
  household_id INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, faction_key INTEGER, formed_tick INTEGER, formed_date_key INTEGER,
  founder_person_key INTEGER, formed_reason_code INTEGER, hut_id INTEGER, dissolved_tick INTEGER, dissolved_date_key INTEGER, lots_dropped INTEGER);

CREATE TABLE fact_household_membership (                    -- S (spells): one membership interval of one person
  membership_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, person_key INTEGER NOT NULL REFERENCES dim_person (person_key),
  household_id INTEGER NOT NULL, from_tick INTEGER NOT NULL, from_date_key INTEGER, to_tick INTEGER, to_date_key INTEGER,
  join_reason_code INTEGER, leave_reason_code INTEGER);

CREATE TABLE fact_care (                                    -- E: one care act (134 ChildFed: food handed over; 136 CareGiven: tending or food to a patient)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  kind_code INTEGER NOT NULL, carer_person_key INTEGER REFERENCES dim_person (person_key), cared_person_key INTEGER REFERENCES dim_person (person_key),
  faction_key INTEGER, ticks INTEGER, ware_key INTEGER, quantity INTEGER, lot_id INTEGER, kin_band_code INTEGER, true_r INTEGER, arrangement_id INTEGER,
  sample_weight INTEGER NOT NULL DEFAULT 1);

CREATE TABLE fact_inheritance (                             -- E: one estate transfer (asset id = lot id)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  estate_person_key INTEGER REFERENCES dim_person (person_key), asset_kind_code INTEGER NOT NULL, asset_id INTEGER NOT NULL,
  ware_key INTEGER, quantity INTEGER NOT NULL, to_party_key INTEGER REFERENCES dim_party (party_key), reason_code INTEGER NOT NULL,
  executor_person_key INTEGER REFERENCES dim_person (person_key));

CREATE TABLE fact_household_day (                           -- S: one household x sim day
  run_key INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key), household_id INTEGER NOT NULL, faction_key INTEGER,
  hut_id INTEGER, members INTEGER, children INTEGER, elders INTEGER, store_food INTEGER, store_units INTEGER,
  PRIMARY KEY (date_key, household_id)) WITHOUT ROWID;
