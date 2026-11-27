-- Warehouse schema v16 (C3a, SHARED_ARCHITECTURAL_CONTRACTS 18.2 0016): employment between individuals. Grains:
-- fact_employment one row per contract spell (EmploymentStarted opens it with the employer party, the worker, the asset
-- and the pay terms; EmploymentEnded closes it with the end reason and the arrears left; to_tick null while open, active
-- or suspended); fact_job_day one row per contract open on a day (JobDay: work ticks, output, paid and unpaid units);
-- fact_employment_event one row per WagePaid, ContractSuspended, ContractResumed and WageUnpaid (a wage line also appears
-- in fact_ownership as an OwnershipChanged with basis 16 Wage, which it reconciles to). Employment relationships are
-- written by C3's mapper into C1's fact_economic_relationship (kind 2, keys from 2^40). Enterprise columns on dim_org
-- wait for 0025 (needs 0018). References only 0001-0015. Integers only.

CREATE TABLE fact_employment (                               -- S (spells): one employment contract from acceptance to end
  run_key INTEGER NOT NULL, contract_id INTEGER NOT NULL,
  employer_party_key INTEGER REFERENCES dim_party (party_key), employer_kind_code INTEGER NOT NULL, employer_id INTEGER NOT NULL,
  agent_person_key INTEGER REFERENCES dim_person (person_key), worker_person_key INTEGER REFERENCES dim_person (person_key),
  asset_id INTEGER NOT NULL,
  pay_unit_kind_code INTEGER NOT NULL,                       -- 1 a ware (pay_unit_ref = ware key); 2 money (K2, later)
  pay_unit_ref INTEGER NOT NULL, pay_amount INTEGER NOT NULL,
  per_code INTEGER NOT NULL,                                 -- dim_code 'pay_per': 1 shift, 2 output unit, 3 task
  shift_ticks INTEGER, control_code INTEGER,
  from_tick INTEGER NOT NULL, from_date_key INTEGER REFERENCES dim_date (date_key),
  to_tick INTEGER, to_date_key INTEGER REFERENCES dim_date (date_key),
  end_reason_code INTEGER,                                   -- dim_code 'employment_end_reason'
  ended_by_code INTEGER, arrears_units INTEGER, units_earned INTEGER, units_paid INTEGER,
  PRIMARY KEY (run_key, contract_id));
CREATE INDEX ix_femp_worker ON fact_employment (worker_person_key, from_tick);

CREATE TABLE fact_job_day (                                  -- one open contract on one day
  run_key INTEGER NOT NULL, contract_id INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  worker_person_key INTEGER REFERENCES dim_person (person_key),
  work_ticks INTEGER NOT NULL, output_count INTEGER NOT NULL, paid_units INTEGER NOT NULL, unpaid_units INTEGER NOT NULL,
  status_code INTEGER NOT NULL,                              -- the status at the day's end: 2 active, 3 suspended, 4 ended
  PRIMARY KEY (run_key, contract_id, date_key));

CREATE TABLE fact_employment_event (                         -- E: wage paid, suspended, resumed, wage unpaid
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  contract_id INTEGER NOT NULL,
  kind_code INTEGER NOT NULL,                                -- the event code: 251 wage paid, 254 suspended, 255 resumed, 256 wage unpaid
  payer_person_key INTEGER REFERENCES dim_person (person_key), worker_person_key INTEGER REFERENCES dim_person (person_key),
  ware_key INTEGER REFERENCES dim_ware (ware_key), quantity INTEGER, units INTEGER, arrears_units INTEGER,
  by_code INTEGER, reason_code INTEGER, obligation_id INTEGER, lot_id INTEGER, control_code INTEGER);
CREATE INDEX ix_feev_contract ON fact_employment_event (contract_id, tick);

INSERT INTO dim_code (code_set, code, name, description) VALUES
  ('pay_per', 1, 'shift', 'one pay unit per shift of work ticks at the asset'),
  ('pay_per', 2, 'unit', 'one pay unit per output unit (a recipe cycle)'),
  ('pay_per', 3, 'task', 'one pay unit per task'),
  ('employment_end_reason', 1, 'resigned', 'the worker gave the work up'),
  ('employment_end_reason', 2, 'dismissed', 'the employer ended it'),
  ('employment_end_reason', 3, 'died', 'a party died (never restored by a resurrection)'),
  ('employment_end_reason', 4, 'asset_gone', 'the asset ceased to exist'),
  ('employment_end_reason', 5, 'abandoned', 'an active contract without work or pay progress for Civ.C3.AbandonTicks (never a suspended one)'),
  ('employment_by', 1, 'worker', 'the worker'),
  ('employment_by', 2, 'employer', 'the employer'),
  ('employment_by', 3, 'sim', 'no party: a timer or a fact'),
  ('economic_relationship', 2, 'employs', 'the employer party employs the worker on an asset');
