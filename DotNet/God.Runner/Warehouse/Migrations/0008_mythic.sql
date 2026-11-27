-- Warehouse schema v8 (D1, SHARED_ARCHITECTURAL_CONTRACTS 18.2): Mythic and divine actions. References only 0001-0007.
-- Grains: fact_mythic_balance_day one row per god x finished sim day (MythicBalanceDay); fact_mythic_ledger one row per
-- movement of a god's Mythic: the opening balance (GodCreated, event_code 800), a credit (MythicCredited 804, amount > 0),
-- a spend (MythicSpent 805, amount <= 0, nominal cost and waived flag kept) or one day's upkeep of one spell kind
-- (MythicUpkeepCharged 806, amount <= 0), so SUM(amount) per god is its balance (invariant M1); fact_divine_action one row
-- per accepted intervention (DivineActionAccepted, InterventionId); fact_divine_observation one row per non-omniscient
-- observation answer (DivineObservationAnswered; empty in this run). Every table is empty while Divinity is off. Integers only.

CREATE TABLE fact_mythic_balance_day (                       -- P (periodic): one god's Mythic over one day
  run_key INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key), god_key INTEGER NOT NULL REFERENCES dim_god (god_key),
  balance INTEGER NOT NULL, max_balance INTEGER NOT NULL, credited INTEGER NOT NULL, capped INTEGER NOT NULL, spent INTEGER NOT NULL,
  waived INTEGER NOT NULL, upkeep INTEGER NOT NULL, actions INTEGER NOT NULL, mythic_mode_code INTEGER NOT NULL);   -- milli-Mythic
CREATE INDEX ix_fmbd_god ON fact_mythic_balance_day (god_key, date_key);

CREATE TABLE fact_mythic_ledger (                            -- E: one movement of a god's Mythic (milli-Mythic, signed)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  god_key INTEGER NOT NULL REFERENCES dim_god (god_key),
  event_code INTEGER NOT NULL,                              -- 800 opening, 804 credit, 805 spend, 806 upkeep (day)
  kind_code INTEGER,                                        -- InterventionKind of a spend or upkeep
  intervention_id INTEGER, amount INTEGER NOT NULL, nominal INTEGER, waived INTEGER NOT NULL DEFAULT 0, capped INTEGER,
  balance_after INTEGER, upkeep_ticks INTEGER, source_code INTEGER);
CREATE INDEX ix_fml_god ON fact_mythic_ledger (god_key, tick);

CREATE TABLE fact_divine_action (                            -- E: one accepted divine action (one per InterventionId)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, god_key INTEGER NOT NULL REFERENCES dim_god (god_key),
  kind_code INTEGER NOT NULL, act_code INTEGER, target_kind_code INTEGER, target_id INTEGER, target_node INTEGER, target_radius INTEGER,
  scope_code INTEGER, cost INTEGER NOT NULL, charged INTEGER NOT NULL);
CREATE UNIQUE INDEX ux_fda_intervention ON fact_divine_action (run_key, intervention_id);

CREATE TABLE fact_divine_observation (                       -- E (Full): a Perceived/Reported answer (Tier 3; empty in this run)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  god_key INTEGER NOT NULL REFERENCES dim_god (god_key), domain_code INTEGER, channel_code INTEGER, subject_id INTEGER);

INSERT INTO dim_code (code_set, code, name, description) VALUES
  ('intervention_kind', 1, 'suggestion', 'a divine suggestion'),
  ('intervention_kind', 2, 'command', 'a divine command'),
  ('intervention_kind', 3, 'compulsion', 'a compulsion'),
  ('intervention_kind', 4, 'possession', 'a possession'),
  ('intervention_kind', 5, 'commandment', 'a commandment'),
  ('intervention_kind', 6, 'miracle', 'a miracle'),
  ('intervention_kind', 7, 'avatar', 'an avatar manifestation'),
  ('mythic_mode', 0, 'standard', 'costs, regeneration, upkeep and cooldowns'),
  ('mythic_mode', 1, 'unlimited', 'nominal costs recorded, nothing charged'),
  ('mythic_mode', 2, 'disabled', 'every divine action refused'),
  ('god_seat', 0, 'none', 'nobody drives the god'),
  ('god_seat', 1, 'human', 'the player'),
  ('god_seat', 2, 'ai', 'a god policy');
