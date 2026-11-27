-- Warehouse schema v18 (C6, SHARED_ARCHITECTURAL_CONTRACTS 18.2 0018; LEAD_RULINGS R49: fact_org_decision and, with 0020
-- reserved and unused, the welfare fact here too): organisations, their status history, membership and office spells, spending,
-- the daily snapshot, service requests (accumulating), deliveries, recorded outcomes, decisions and welfare grants.
-- Grains: dim_org one row per organisation (OrgFounded; OrgDissolved fills the end; org_key = the OrgId, F1.11);
-- dim_org_history one row per status spell (type 2: OrgFounded opens Operating, OrgStatusChanged closes and opens);
-- fact_org_membership one row per spell of one role or one office of one person (MemberJoined / MemberLeft for roles,
-- OrgHeadChanged To / From for offices; half-open from_tick to to_tick, to_tick null while open);
-- fact_org_expenditure one row per OrgSpent; fact_org_day one row per OrgDay (organisation x day) and fact_org_day_store one
-- row per organisation x day x ware held; fact_service_request one row per request (ServiceRequested inserts, ServiceAccepted,
-- ServiceDelivered, ServiceRefused, ServiceExpired and ServiceOutcomeRecorded update); fact_service_delivery one row per
-- ServiceDelivered; fact_service_outcome one row per ServiceOutcomeRecorded (true_outcome: truth, Full only);
-- fact_org_decision one row per OrgDecisionTaken; fact_welfare one row per WelfareGranted. References only 0001-0007.

CREATE TABLE dim_org (                                       -- D: one organisation (natural key OrgId)
  org_key INTEGER NOT NULL, run_key INTEGER NOT NULL,
  template_code INTEGER, type_code INTEGER NOT NULL, ownership_code INTEGER NOT NULL,
  faction_key INTEGER REFERENCES dim_faction (faction_key), place_location_key INTEGER REFERENCES dim_location (location_key),
  store_bin_id INTEGER, founders_n INTEGER NOT NULL, origin_code INTEGER,
  founded_tick INTEGER NOT NULL, founded_date_key INTEGER REFERENCES dim_date (date_key),
  dissolved_tick INTEGER, dissolved_date_key INTEGER REFERENCES dim_date (date_key), dissolve_reason_code INTEGER, dissolve_rule_code INTEGER);
CREATE UNIQUE INDEX ux_dim_org ON dim_org (run_key, org_key);

CREATE TABLE dim_org_history (                               -- D type 2: one status spell of one organisation
  org_hist_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, org_key INTEGER NOT NULL,
  status_code INTEGER NOT NULL, from_tick INTEGER NOT NULL, to_tick INTEGER);
CREATE INDEX ix_doh_org ON dim_org_history (org_key, from_tick);

CREATE TABLE fact_org_membership (                           -- S (spells): one role, or one office, of one person
  membership_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, org_key INTEGER NOT NULL,
  person_key INTEGER REFERENCES dim_person (person_key),
  role_code INTEGER NOT NULL,                               -- RoleKind; 4 Officer for an office spell
  office_index INTEGER, office_kind_code INTEGER, basis_code INTEGER,
  from_tick INTEGER NOT NULL, from_date_key INTEGER REFERENCES dim_date (date_key),
  to_tick INTEGER, to_date_key INTEGER REFERENCES dim_date (date_key), end_reason_code INTEGER);   -- 1 left, 2 dismissed, 3 died, 4 dissolved, 5 office changed
CREATE INDEX ix_fom_org ON fact_org_membership (org_key, from_tick);

CREATE TABLE fact_org_expenditure (                          -- E: one spend of the organisation's goods by an actor within reach
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  org_key INTEGER NOT NULL, actor_person_key INTEGER REFERENCES dim_person (person_key), category_code INTEGER NOT NULL,
  ware_key INTEGER REFERENCES dim_ware (ware_key), quantity INTEGER NOT NULL, payee_person_key INTEGER REFERENCES dim_person (person_key),
  request_id INTEGER, control_code INTEGER);
CREATE INDEX ix_foe_org ON fact_org_expenditure (org_key, tick);

CREATE TABLE fact_org_day (                                  -- P: one organisation x day (OrgDay)
  run_key INTEGER NOT NULL, org_key INTEGER NOT NULL, day INTEGER NOT NULL, date_key INTEGER REFERENCES dim_date (date_key),
  status_code INTEGER NOT NULL, members INTEGER NOT NULL, staff INTEGER NOT NULL, offices_vacant INTEGER NOT NULL, store_units INTEGER NOT NULL,
  requests_open INTEGER NOT NULL, served INTEGER NOT NULL, refused INTEGER NOT NULL, expired INTEGER NOT NULL,
  deposits INTEGER NOT NULL, records INTEGER NOT NULL, decisions INTEGER NOT NULL,
  true_unmet INTEGER);                                      -- truth (analytics only): hungry members with no food
CREATE INDEX ix_fod_org ON fact_org_day (org_key, day);

CREATE TABLE fact_org_day_store (                            -- P: one organisation x day x ware in its stores
  run_key INTEGER NOT NULL, org_key INTEGER NOT NULL, day INTEGER NOT NULL, ware_key INTEGER NOT NULL REFERENCES dim_ware (ware_key), quantity INTEGER NOT NULL);
CREATE INDEX ix_fods_org ON fact_org_day_store (org_key, day);

CREATE TABLE fact_service_request (                          -- A (accumulating): one request through its steps
  request_id INTEGER NOT NULL, run_key INTEGER NOT NULL, org_key INTEGER, kind_code INTEGER NOT NULL,
  requester_person_key INTEGER REFERENCES dim_person (person_key), subject_kind_code INTEGER, subject_id INTEGER,
  place_location_key INTEGER REFERENCES dim_location (location_key), received_by_person_key INTEGER REFERENCES dim_person (person_key),
  control_code INTEGER, true_need INTEGER,                  -- true_need: truth (Full only)
  opened_tick INTEGER NOT NULL, opened_date_key INTEGER REFERENCES dim_date (date_key),
  accepted_tick INTEGER, provider_person_key INTEGER REFERENCES dim_person (person_key),
  delivered_tick INTEGER, refused_tick INTEGER, refused_reason_code INTEGER, expired_tick INTEGER, recorded_outcome INTEGER,
  status_code INTEGER NOT NULL);                            -- RequestStatus: 1 waiting, 2 active, 3 done, 4 refused, 6 expired
CREATE UNIQUE INDEX ux_fsr ON fact_service_request (run_key, request_id);

CREATE TABLE fact_service_delivery (                         -- E: one delivered service (the provider's activity changed real state)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  request_id INTEGER NOT NULL, org_key INTEGER, provider_person_key INTEGER REFERENCES dim_person (person_key),
  ticks INTEGER, goods_used INTEGER, outcome_event_code INTEGER, control_code INTEGER);

CREATE TABLE fact_service_outcome (                          -- E: one outcome written by a recorder
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  request_id INTEGER NOT NULL, org_key INTEGER, recorder_person_key INTEGER REFERENCES dim_person (person_key),
  recorded_outcome INTEGER, true_outcome INTEGER);          -- true_outcome: truth (Full only)

CREATE TABLE fact_org_decision (                             -- E: one decision (R49), counts as the counter saw them
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  org_key INTEGER NOT NULL, decision_id INTEGER NOT NULL, proposer_person_key INTEGER REFERENCES dim_person (person_key),
  source_code INTEGER, source_ref INTEGER, what_code INTEGER NOT NULL, a0 INTEGER, a1 INTEGER,
  outcome_code INTEGER NOT NULL, rule_code INTEGER, accept_n INTEGER, refuse_n INTEGER, absent_n INTEGER,
  counter_person_key INTEGER REFERENCES dim_person (person_key));
CREATE INDEX ix_fodec_org ON fact_org_decision (org_key, tick);

CREATE TABLE fact_welfare (                                  -- E: one welfare grant (a keeper's hand-over of rationed goods)
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  org_key INTEGER NOT NULL, keeper_person_key INTEGER REFERENCES dim_person (person_key), recipient_person_key INTEGER REFERENCES dim_person (person_key),
  ware_key INTEGER REFERENCES dim_ware (ware_key), quantity INTEGER NOT NULL, request_id INTEGER, control_code INTEGER);
CREATE INDEX ix_fw_org ON fact_welfare (org_key, tick);

INSERT INTO dim_code (code_set, code, name, description) VALUES
  ('org_type', 3, 'service_body', 'provides a service (shared store, healer, school, court)'),
  ('org_ownership', 3, 'communal', 'owned by its member households'),
  ('org_status', 2, 'operating', 'has a living decider'),
  ('org_status', 3, 'dormant', 'no living office-holder with Propose for DormantDays'),
  ('org_status', 5, 'dissolved', 'dissolved by its rule'),
  ('org_role', 1, 'member', 'a member'),
  ('org_role', 4, 'officer', 'holds an office (office_index)'),
  ('org_decision_outcome', 1, 'adopted', 'adopted by the rule'),
  ('org_decision_outcome', 2, 'rejected', 'refused, or a tie (the old state kept)'),
  ('org_decision_outcome', 3, 'lapsed', 'below quorum or never put'),
  ('service_kind', 4, 'ration', 'food handed to a member by the keeper'),
  ('service_status', 1, 'waiting', ''), ('service_status', 2, 'active', ''), ('service_status', 3, 'done', ''),
  ('service_status', 4, 'refused', ''), ('service_status', 6, 'expired', ''),
  ('org_spend_category', 8, 'ration', 'welfare hand-over');
