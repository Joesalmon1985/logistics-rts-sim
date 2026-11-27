-- Warehouse schema v9 (D2, SHARED_ARCHITECTURAL_CONTRACTS 18.2): divine suggestions, commands and compulsions. References only
-- 0001-0008. Grains: fact_divine_command one row per accepted suggestion or command (DivineSuggestionIssued 810,
-- DivineCommandIssued 811, InterventionId); fact_divine_command_target one row per recipient an organisation-scope
-- instruction was relayed to (InstructionRelayed 812); fact_command_perception one row per perceiving recipient
-- (CommandPerceived 813, kind_code 1) or witness of a command's sign (DivineSignPerceived 819, kind_code 2); fact_command_response
-- exactly one row per perceiver (CommandResponded 814); fact_compulsion one row per compulsion event: started (815, kind 1),
-- ended (816, kind 2, with the end reason) or blocked (817, kind 3), so every spell opens and closes exactly once. Analytics
-- truth (mismatch) is written at Full only. Every table is empty while Divinity is off. Integers only.

CREATE TABLE fact_divine_command (                           -- E: one accepted suggestion (kind 1) or command (kind 2)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, god_key INTEGER REFERENCES dim_god (god_key), kind_code INTEGER NOT NULL,
  act_code INTEGER NOT NULL, target_kind_code INTEGER, target_id INTEGER, target_node INTEGER, target_radius INTEGER,
  scope_code INTEGER NOT NULL, recipients INTEGER NOT NULL, not_reached INTEGER NOT NULL, expires_tick INTEGER NOT NULL);
CREATE UNIQUE INDEX ux_fdc_intervention ON fact_divine_command (run_key, intervention_id);

CREATE TABLE fact_divine_command_target (                    -- E: one relayed recipient of an organisation-scope instruction
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, org_id INTEGER, person_key INTEGER REFERENCES dim_person (person_key));
CREATE INDEX ix_fdct_intervention ON fact_divine_command_target (intervention_id);

CREATE TABLE fact_command_perception (                       -- E: a recipient perceived (1) or a witness saw the sign (2)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, kind_code INTEGER NOT NULL, person_key INTEGER REFERENCES dim_person (person_key),
  recipient_person_key INTEGER REFERENCES dim_person (person_key), instruction_kind_code INTEGER, decoded_act_code INTEGER,
  topic_kind_code INTEGER, topic_id INTEGER, clarity INTEGER, on_waking INTEGER, evicted_intervention_id INTEGER,
  location_key INTEGER REFERENCES dim_location (location_key), mismatch INTEGER);   -- mismatch: analytics truth, Full only
CREATE INDEX ix_fcp_intervention ON fact_command_perception (intervention_id);

CREATE TABLE fact_command_response (                         -- E: exactly one per perceiver
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, person_key INTEGER REFERENCES dim_person (person_key), god_key INTEGER REFERENCES dim_god (god_key),
  instruction_kind_code INTEGER NOT NULL, response_code INTEGER NOT NULL,   -- 1 followed 2 ignored 3 refused 4 lapsed 5 misread
  verb INTEGER, divine_term INTEGER);
CREATE UNIQUE INDEX ux_fcr_once ON fact_command_response (run_key, intervention_id, person_key);

CREATE TABLE fact_compulsion (                               -- E: a compulsion started (1), ended (2) or was blocked (3)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, god_key INTEGER REFERENCES dim_god (god_key), person_key INTEGER REFERENCES dim_person (person_key),
  kind_code INTEGER NOT NULL, act_code INTEGER, target_kind_code INTEGER, target_id INTEGER, target_node INTEGER, target_radius INTEGER,
  expires_tick INTEGER, end_reason_code INTEGER, started INTEGER, since_tick INTEGER);
CREATE INDEX ix_fco_intervention ON fact_compulsion (intervention_id);

INSERT INTO dim_code (code_set, code, name, description) VALUES
  ('command_response', 1, 'followed', 'Followed'), ('command_response', 2, 'ignored', 'Ignored'), ('command_response', 3, 'refused', 'Refused'),
  ('command_response', 4, 'lapsed', 'Lapsed'), ('command_response', 5, 'misread', 'Misread'),
  ('control_end_reason', 1, 'completed', 'Completed'), ('control_end_reason', 2, 'timeout', 'Timeout'), ('control_end_reason', 3, 'cancelled', 'Cancelled'),
  ('control_end_reason', 4, 'death', 'Death'), ('control_end_reason', 5, 'mythic_exhausted', 'MythicExhausted'), ('control_end_reason', 6, 'superseded', 'Superseded'),
  ('control_end_reason', 7, 'destroyed', 'Destroyed');
