-- Warehouse schema v6 (M5, SHARED_CONTRACTS 8): social memory, the gesture language, barter and gifts. Grains (also in
-- meta_table_grain): fact_social_interaction one row per GestureMade, MessageDecoded, ResponseChosen, TrustChanged,
-- RelationEvicted, RecognitionError or BetrayalRecorded (kind_code says which; high-volume kinds sampled below Full with
-- sample_weight); fact_transaction one row per deal (DealId): opened by ExchangeOpened, closed by ExchangeClosed;
-- fact_transaction_line one row per hand-over of a deal (ExchangeStep or GiftGiven); fact_person_day gains the soc_ columns
-- of PersonDaySocial (one living person x sim day). GiftGiven also adds to fact_social_day (kind 213, by believed kin band).
-- Analytics-only truth (Honest, Mismatch, TrueR) is written at Full only. Integers only. Applied after 0004 (and 0005 when
-- M4 is merged); the writer applies whatever numbers exist, in order.

CREATE TABLE fact_social_interaction (                      -- E: one social or communication event
  interaction_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL,
  date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  kind_code INTEGER NOT NULL,     -- 1 gesture, 2 decoded, 3 response, 4 trust changed, 5 relation evicted, 6 recognition error, 7 betrayal
  a_person_key INTEGER REFERENCES dim_person (person_key),  -- gesture: speaker; decoded, response: listener; trust, evicted, recognition: the mind; betrayal: victim
  b_person_key INTEGER REFERENCES dim_person (person_key),  -- gesture: addressee; decoded, response: the speaker as recognised; trust: the other; evicted: the forgotten; recognition: the missed; betrayal: offender
  message_id INTEGER, deal_id INTEGER,
  act_code INTEGER,               -- GestureAct
  topic_kind_code INTEGER, topic_id INTEGER, topic_sub INTEGER, quantity INTEGER,
  counter_kind_code INTEGER, counter_id INTEGER, counter_sub INTEGER, time_code INTEGER,
  location_key INTEGER REFERENCES dim_location (location_key),
  listeners INTEGER, dist INTEGER,
  value_before INTEGER, value_after INTEGER, fam_before INTEGER, fam_after INTEGER,  -- trust and familiarity (kind 4); importance, trust (5); severity, value (7)
  cause_code INTEGER, cause_ref INTEGER,                    -- SocialCause and the deal, message or lot it came from; gesture: presented 1/0; decoded: the addressee
  verb INTEGER, ignored INTEGER,                            -- response: the learning verb chosen, or ignored
  truth_flag INTEGER,                                       -- Full only: Honest (gesture) or Mismatch (decoded); null below Full
  sample_weight INTEGER NOT NULL DEFAULT 1);
CREATE INDEX ix_fsi_message ON fact_social_interaction (message_id);
CREATE INDEX ix_fsi_kind ON fact_social_interaction (kind_code, date_key);

CREATE TABLE fact_transaction (                             -- one deal (DealId), opened and closed
  run_key INTEGER NOT NULL, deal_id INTEGER NOT NULL,
  opened_tick INTEGER NOT NULL, opened_date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  kind_code INTEGER NOT NULL,                               -- DealKind: 1 spot exchange, 2 gift, 3 help
  a_person_key INTEGER REFERENCES dim_person (person_key),  -- who opened it (the first offer, the giver, the helper)
  b_person_key INTEGER REFERENCES dim_person (person_key),  -- the other side (null while an offer to anyone watching is unanswered)
  message_id INTEGER,                                       -- the request a gift or help answers
  a_ware_key INTEGER REFERENCES dim_ware (ware_key), a_count INTEGER, -- the terms as A meant them (A gives, A wants)
  b_ware_key INTEGER REFERENCES dim_ware (ware_key), b_count INTEGER,
  same_household INTEGER NOT NULL,
  closed_tick INTEGER, closed_date_key INTEGER REFERENCES dim_date (date_key),
  outcome_code INTEGER,                                     -- DealOutcome: 1 completed, 2 defected, 3 partial, 4 abandoned, 5 refused
  a_given INTEGER, b_given INTEGER, a_gain INTEGER, b_gain INTEGER,  -- what passed each way; each side's believed worth gain
  PRIMARY KEY (run_key, deal_id));

CREATE TABLE fact_transaction_line (                        -- E: one hand-over of a deal, by its giver
  line_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL,
  date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  deal_id INTEGER NOT NULL, step INTEGER NOT NULL,
  kind_code INTEGER NOT NULL,                               -- the event code: 211 exchange step, 213 gift
  giver_person_key INTEGER REFERENCES dim_person (person_key), receiver_person_key INTEGER REFERENCES dim_person (person_key),
  lot_id INTEGER NOT NULL, ware_key INTEGER REFERENCES dim_ware (ware_key), quantity INTEGER NOT NULL,
  location_key INTEGER REFERENCES dim_location (location_key),
  kin_band_code INTEGER, same_household INTEGER, answers_message_id INTEGER,
  true_r INTEGER,                                           -- Full only
  sample_weight INTEGER NOT NULL DEFAULT 1);
CREATE INDEX ix_ftl_deal ON fact_transaction_line (deal_id);

-- fact_person_day: M5's columns (prefix soc_, SHARED_CONTRACTS 8), from PersonDaySocial
ALTER TABLE fact_person_day ADD COLUMN soc_rel_count INTEGER;
ALTER TABLE fact_person_day ADD COLUMN soc_mean_trust INTEGER;
ALTER TABLE fact_person_day ADD COLUMN soc_friends INTEGER;
ALTER TABLE fact_person_day ADD COLUMN soc_enemies INTEGER;
ALTER TABLE fact_person_day ADD COLUMN soc_grudge_sum INTEGER;
ALTER TABLE fact_person_day ADD COLUMN soc_sent INTEGER;
ALTER TABLE fact_person_day ADD COLUMN soc_received INTEGER;
ALTER TABLE fact_person_day ADD COLUMN soc_misread INTEGER;
ALTER TABLE fact_person_day ADD COLUMN soc_gifts_given INTEGER;
ALTER TABLE fact_person_day ADD COLUMN soc_gifts_received INTEGER;
ALTER TABLE fact_person_day ADD COLUMN soc_deals INTEGER;
ALTER TABLE fact_person_day ADD COLUMN soc_cheated INTEGER;
