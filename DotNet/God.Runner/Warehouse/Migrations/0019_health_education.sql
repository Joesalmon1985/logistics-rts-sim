-- Warehouse schema v19 (C7, SHARED_ARCHITECTURAL_CONTRACTS 18.2 0019; needs 0018): health encounters, teaching sessions and
-- assessments (skill and knowledge changes measured on the learner itself).
-- Grains: fact_health_encounter one row per TreatmentStarted (TreatmentEnded fills the end; request_id joins 1:1 to
-- fact_service_request rows of kind 1 Treat; truth columns *_before/*_after at Full only);
-- fact_education_enrolment one row per TeachingSession (a teacher and one learner in one session: Teach, Demonstrate, Apprentice);
-- fact_education_assessment one row per KnowledgeTransferred (assessment_kind 1: associations copied, skill before/after exact)
-- and per HealerSkillChanged (assessment_kind 2: one skill change with its cause; after - before = the gain).

CREATE TABLE fact_health_encounter (                         -- A (accumulating): one treatment from start to end
  run_key INTEGER NOT NULL, request_id INTEGER NOT NULL, org_key INTEGER,
  healer_person_key INTEGER REFERENCES dim_person (person_key), patient_person_key INTEGER REFERENCES dim_person (person_key),
  skill INTEGER, remedy_lot INTEGER, seen_need INTEGER, control_code INTEGER,
  start_tick INTEGER NOT NULL, start_date_key INTEGER REFERENCES dim_date (date_key),
  end_tick INTEGER, ticks INTEGER, outcome_code INTEGER,    -- outcome: 1 completed, 2 not needed, 3 patient gone, 4 stopped
  severity_permille INTEGER, injury_permille INTEGER, remedy_used INTEGER,
  severity_before INTEGER, severity_after INTEGER, injury_before INTEGER, injury_after INTEGER,   -- truth (Full only)
  health_before INTEGER, health_after INTEGER);                                                   -- truth (Full only)
CREATE INDEX ix_fhe_req ON fact_health_encounter (run_key, request_id);

CREATE TABLE fact_education_enrolment (                      -- E (spell as one row): one learner in one session
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  teacher_person_key INTEGER REFERENCES dim_person (person_key), learner_person_key INTEGER REFERENCES dim_person (person_key),
  topic_code INTEGER NOT NULL, kind_code INTEGER NOT NULL, ticks INTEGER, org_key INTEGER, control_code INTEGER);

CREATE TABLE fact_education_assessment (                     -- E: one measured change on one person
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  assessment_kind INTEGER NOT NULL,                          -- 1 knowledge transferred, 2 skill changed
  person_key INTEGER REFERENCES dim_person (person_key), teacher_person_key INTEGER REFERENCES dim_person (person_key),
  topic_code INTEGER, entries_copied INTEGER, fidelity INTEGER, cause_code INTEGER,
  skill_before INTEGER NOT NULL, skill_after INTEGER NOT NULL);
