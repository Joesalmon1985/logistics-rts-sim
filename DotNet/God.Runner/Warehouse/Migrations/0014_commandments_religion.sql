-- Warehouse schema v14 (package D6, SHARED_ARCHITECTURAL_CONTRACTS 18.2): commandments, people's knowledge of them, religions,
-- affiliation spells and beliefs about gods. References only 0001-0013. Grains: fact_commandment one row per revealed
-- commandment (CommandmentIssued 860); fact_commandment_revision one row per revision (861, kind 1) or revocation (862, kind 2);
-- fact_commandment_knowledge one row per knowledge event: learned (863, kind 1: a new entry or a newer revision, is_update = 1),
-- stance changed (864, kind 2) or forgotten (865, kind 3), so a knowledge spell opens at its first 863 and closes at its 865;
-- fact_commandment_choice one row per sampled choice a held rule weighed (866, high volume); fact_religion one row per religion
-- (867); fact_affiliation one row per affiliation spell change (868: opened 1 / closed 0, with the person's community and
-- citizenship stand-in, unchanged by it); fact_god_belief one row per sampled change of a person's five fields (869, high volume).
-- Prayer and worship (870, 871) get their tables when those events land. Analytics truth (variant_mask) at Full only. Every
-- table is empty while no commandment exists and Tier 2 is off. Integers only.

CREATE TABLE fact_commandment (                              -- E: one revealed commandment
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, commandment_id INTEGER NOT NULL, god_key INTEGER REFERENCES dim_god (god_key),
  template_id INTEGER NOT NULL, importance INTEGER NOT NULL, rule_count INTEGER NOT NULL, target_kind_code INTEGER, target_id INTEGER,
  target_node INTEGER, target_radius INTEGER, recipients INTEGER NOT NULL);
CREATE UNIQUE INDEX ux_fcm_commandment ON fact_commandment (run_key, commandment_id);

CREATE TABLE fact_commandment_revision (                     -- E: a revision (1) or the revocation (2)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, commandment_id INTEGER NOT NULL, god_key INTEGER REFERENCES dim_god (god_key), kind_code INTEGER NOT NULL,
  revision INTEGER NOT NULL, importance INTEGER, rule_count INTEGER, recipients INTEGER NOT NULL);
CREATE INDEX ix_fcmr_commandment ON fact_commandment_revision (commandment_id);

CREATE TABLE fact_commandment_knowledge (                    -- E: learned (1), stance changed (2), forgotten (3)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  person_key INTEGER REFERENCES dim_person (person_key), commandment_id INTEGER NOT NULL, kind_code INTEGER NOT NULL, revision INTEGER,
  source_code INTEGER, from_person_key INTEGER REFERENCES dim_person (person_key), intervention_id INTEGER, salience INTEGER, adoption INTEGER,
  stance_from_code INTEGER, stance_to_code INTEGER, cause_code INTEGER, is_update INTEGER, evicted INTEGER,
  variant_mask INTEGER);                                     -- variant_mask: analytics truth, Full only
CREATE INDEX ix_fcmk_person ON fact_commandment_knowledge (person_key, commandment_id);

CREATE TABLE fact_commandment_choice (                       -- E (sampled): a choice a held rule weighed
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  person_key INTEGER REFERENCES dim_person (person_key), commandment_id INTEGER NOT NULL, modality_code INTEGER NOT NULL, verb INTEGER NOT NULL,
  target_id INTEGER, term INTEGER NOT NULL);

CREATE TABLE fact_religion (                                 -- E: one religion founded
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  religion_id INTEGER NOT NULL, god_key INTEGER REFERENCES dim_god (god_key), parent_religion_id INTEGER);
CREATE UNIQUE INDEX ux_frel_religion ON fact_religion (run_key, religion_id);

CREATE TABLE fact_affiliation (                              -- E: an affiliation spell opened (1) or closed (0)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  person_key INTEGER REFERENCES dim_person (person_key), religion_id INTEGER NOT NULL, god_key INTEGER REFERENCES dim_god (god_key),
  opened INTEGER NOT NULL, reason_code INTEGER NOT NULL, community_code INTEGER NOT NULL, citizenship_code INTEGER NOT NULL);
CREATE INDEX ix_faff_person ON fact_affiliation (person_key);

CREATE TABLE fact_god_belief (                               -- E (sampled): a change of one person's five fields about one god
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  person_key INTEGER REFERENCES dim_person (person_key), god_key INTEGER REFERENCES dim_god (god_key), cause_code INTEGER NOT NULL,
  cause_ref INTEGER, d_belief INTEGER, d_faith INTEGER, d_trust INTEGER, d_fear INTEGER, d_loyalty INTEGER,
  belief INTEGER NOT NULL, faith INTEGER NOT NULL, trust INTEGER NOT NULL, fear INTEGER NOT NULL, loyalty INTEGER NOT NULL);
CREATE INDEX ix_fgb_person ON fact_god_belief (person_key, god_key);
