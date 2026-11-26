-- Warehouse schema v3 (M3b decision trace): fact_decision, one row per DecisionMade (code 334).
-- Grain (also in meta_table_grain): one row per chosen intent of one person at one think tick. DecisionMade is emitted only when
-- the learning domain records at Full (EventBuffer.TraceDecisions), and the mapper writes it only at Full, so the table is empty
-- at Summary and below. Integers only. Later migrations add 0004_..., never edit this file.

CREATE TABLE fact_decision (                                -- E (Full only): one DecisionMade
  decision_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL,
  date_key INTEGER NOT NULL REFERENCES dim_date (date_key), person_key INTEGER NOT NULL REFERENCES dim_person (person_key),
  faction_key INTEGER REFERENCES dim_faction (faction_key),
  verb INTEGER NOT NULL,                                    -- the learning verb chosen (Verb)
  target_kind_code INTEGER NOT NULL,                        -- IntentTarget
  target_id INTEGER NOT NULL, target_location_key INTEGER REFERENCES dim_location (location_key), target_class INTEGER NOT NULL,
  total INTEGER NOT NULL,                                   -- = instinct + learned + social + culture + personality - cost + explore
  instinct INTEGER NOT NULL, need INTEGER NOT NULL,         -- need: the drive-gated part of instinct
  learned INTEGER NOT NULL, social INTEGER NOT NULL, culture INTEGER NOT NULL, personality INTEGER NOT NULL, cost INTEGER NOT NULL, explore INTEGER NOT NULL,
  candidates INTEGER NOT NULL, learned_changed_winner INTEGER NOT NULL, explore_won INTEGER NOT NULL, continued INTEGER NOT NULL,
  second_verb INTEGER NOT NULL, second_total INTEGER NOT NULL,
  sample_weight INTEGER NOT NULL DEFAULT 1);
