# Data warehouse design (stage 4)

## 1. Purpose and scope
This document owns the **analytical database**: how the sim's domain events and snapshots become a relational, dimensional warehouse that can be queried with plain SQL outside the game. It covers:
- the event schema (versions, ids, ordering);
- recording levels and per-domain switches;
- dimensions, with slowly-changing-dimension (SCD) types;
- fact tables: grain, keys, time semantics, sources and relationships;
- how history is preserved;
- SQLite first, then Parquet and DuckDB;
- the ETL;
- size budgets and tests.

It links to, and does not repeat:
- the runner, scenarios, replays and hashes: [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md);
- KPI definitions (each names its source table here): [KPI_CATALOGUE.md](KPI_CATALOGUE.md);
- information layers, institutional records and how reports reach the player: [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md);
- experiment result tables (`experiment`, `experiment_run`, `experiment_metric`, `experiment_result`): [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md);
- dataset export for training: [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md);
- measuring strategies after the fact: [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md).

### 1.1 The published table list
Other documents refer to these names. A name changes only by editing this list.

**Dimensions**

| Table | SCD | Scope | Milestone |
|---|---|---|---|
| `dim_run` | static | one row per run | M13 core |
| `dim_date` | static | per run (sim day → year, day of year) | M13 core |
| `dim_person` | type 1 (fixed attributes; death fields filled once) | per run | M13 core |
| `dim_person_history` | **type 2** (stage, citizenship, household, occupation, role) | per run | M13 extended |
| `dim_household` | type 1 | per run | M13 core |
| `dim_faction` | type 1 | per run | M13 core |
| `dim_faction_history` | type 2 (age, controlling policy, eliminated) | per run | M13 extended |
| `dim_institution` | type 1 | per run | M13 extended (M12 sources) |
| `dim_institution_history` | type 2 (head, policy, status) | per run | M13 extended |
| `dim_enterprise` | type 1 | per run | M13 extended (M8 sources) |
| `dim_party` | type 1 (one row per economic actor of any kind) | per run | M13 core (plumbing) |
| `dim_building` | type 1 | per run | M13 core |
| `dim_location` | static (hex, node, region) | per run (the board depends on the seed) | M13 extended |
| `dim_occupation` | static | global | M13 extended |
| `dim_ware` | static | global | M13 core |
| `dim_policy` | static (state policy instruments) | global | M13 extended |
| `dim_agent_policy` | static (registry policies and model hashes) | global | M13 extended |
| `dim_action` | static (intents, commands, gestures) | global | later (stage 5) |
| `dim_relationship` | static junk dimension (kin band, same household, same faction, trust band) | global | M13 extended |
| `dim_strategy` | type 2 by window (behaviour fingerprints, computed by analysis) | per run | after M13 (analysis job) |
| `dim_kpi` | static (mirrors [KPI_CATALOGUE.md](KPI_CATALOGUE.md)) | global | M13 core |
| `dim_code` | static (small enums: death cause, end reason, verdict, age band, household type…) | global | M13 core (plumbing) |
| `dim_class` | static (class positions, from [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)) | global | M13 core |
| `dim_trait` | static (cultural trait ids, from [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)) | global | M13 extended (M11 sources) |
| `dim_outlet` | type 1 (media outlets, owner party and kind) | per run | stage 7 |

**Facts**

| Table | Kind | Milestone |
|---|---|---|
| `fact_person_day` | periodic snapshot | M13 core |
| `fact_person_day_ware` | periodic snapshot (holdings per person × ware, so wealth needs no value weights) | M13 extended |
| `fact_person_day_class` | periodic snapshot (class measures per person-day) | M13 core |
| `fact_household_day` | periodic snapshot | M13 extended (M4 sources) |
| `fact_hex_day` | periodic snapshot (resource stock, capacity, extraction, regrowth per hex) | M13 extended |
| `fact_birth`, `fact_death` | event | M13 core |
| `fact_household_membership` | history (spells) | M13 core |
| `fact_citizenship` | history (spells) | M13 core |
| `fact_migration` | event | M13 extended (M10) |
| `fact_transaction`, `fact_transaction_line` | accumulating header, event lines | M13 core |
| `fact_exchange_day` | daily aggregate (Summary level) | M13 core |
| `fact_production` (event), `fact_production_day` (aggregate) | event / aggregate | M13 core |
| `fact_employment` | accumulating (spells) | M13 core |
| `fact_job_day` | periodic snapshot (one contract per day: wage, output) | M13 extended (M6 sources) |
| `fact_tax` | event | M13 extended |
| `fact_ownership` | history (spells) | M13 core |
| `fact_inheritance` | event (one line per transfer of an estate) | M13 extended (M4) |
| `fact_health_encounter`, `fact_referral` | event | later (M12 healer; stage 6 healthcare) |
| `fact_education_activity` | event | M13 extended (M11 teaching) |
| `fact_learning` | event, sampled, plus daily aggregate (association-store changes) | M13 extended (M3 sources) |
| `fact_decision` | event (the optional decision trace; off unless `--record-decisions`) | M13 extended (M3 sources) |
| `fact_trait_census_daily`, `fact_trait_transmission` | snapshot / event | M13 extended (M11 sources) |
| `fact_crime`, `fact_crime_report`, `fact_investigation` | event / event / accumulating | M13 extended (M8 theft); stage 6 police |
| `fact_court_case` | accumulating (cases before any forum: elders' mediation first, courts later) | M13 extended (M12 mediation); stage 5–6 courts |
| `fact_grievance` | event (one wrong believed by a person, and whether and where it was brought) | M13 extended (M10 sources) |
| `fact_detention` | history (spells) | stage 6 |
| `fact_military_service` | history (spells) | M13 extended (M9) |
| `fact_service_day` | periodic snapshot (one soldier per day: fit, kitted, fed, assigned, morale, arrears) | M13 extended (M9) |
| `fact_combat` | event | M13 extended |
| `fact_contest` | accumulating (one civilian contest, [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)) | M13 extended (M5 sources) |
| `fact_social_interaction` | event | M13 core |
| `fact_social_day` | daily aggregate (Summary level) | M13 core |
| `fact_relation_edge_day` | periodic snapshot of relationship edges (optional, Full level only) | M13 extended |
| `fact_promise` | accumulating | M13 extended (M7) |
| `fact_loan`, `fact_loan_repayment` | accumulating / event | M13 extended (M7, M12) |
| `fact_record`, `fact_record_audit` | event (layer 4 record written; audit finding against truth) | M13 extended (M12 sources) |
| `fact_return` | event (a manager's filed return, `ReturnFiled`; can be gamed) | M13 extended (M12 sources) |
| `fact_belief_sample` | sampled snapshot (believed vs true value) | M13 extended |
| `fact_collective_action` | event (gatherings and votes from M12; strikes, protests and assemblies from stage 5) | M13 extended (M12); stage 5 |
| `fact_media_publication` | event | stage 7 |
| `fact_exposure`, `fact_outlet_day` | event / periodic snapshot | stage 7 |
| `fact_policy_change` | event plus history (valid from/to) | M13 core |
| `fact_institution_day` | periodic snapshot | M13 extended |
| `fact_faction_day` | periodic snapshot | M13 extended |
| `fact_kpi` | periodic snapshot, actual vs reported | M13 core |
| `fact_command` | event (external and AI commands applied or rejected) | M13 extended |
| `fact_event` | event (generic row for any mapped event with no dedicated table: code, tick, ids, small payload) | M13 core (plumbing) |
| `fact_run_metric`, `fact_metric_day` | run-level and daily metrics (from the Metrics level) | M13 core (plumbing) |
| `fact_lab_match`, `fact_lab_population_step` | lab results | M13 (M2 writes CSV) |

**Metadata:** `meta_schema_version`, `meta_event_type`, `meta_event_map`, `meta_load`.

**Owned elsewhere, stored alongside:** `experiment`, `experiment_run`, `experiment_metric`, `experiment_result` ([POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)). `experiment_run.run_id` references `dim_run.run_id`.

## 2. Principles
1. **The sim is the authority.** The authoritative sim stays pure, deterministic, in-memory C#. The warehouse is a copy for analysis. Database operations never feed back into the sim, never block a decision and never add nondeterminism.
2. **One stream, two sources.** The sim's `Telemetry` module emits typed **domain events** and offers read-only **snapshot builders**. Recorders outside the sim (`God.Recording`, and the SQLite writer in `God.Runner`) are the only writers. The view's animation events are the same events ([DESIGN_CONVENTIONS.md §8](DESIGN_CONVENTIONS.md#8-domain-events-and-snapshots)).
3. **Recording never changes outcomes.** Every recording level, every domain switch and every sampling rate gives the same state hashes. A test proves it (section 10).
4. **Truth and records are both kept, and labelled.**
   - The warehouse stores true state (layer 1), institutional records (layer 4) and published information (layer 5) in separate columns or tables, each with an `info_layer` value.
   - Goodhart effects are measurable only because the reported KPI and the actual one sit side by side in `fact_kpi`, and managers' filed returns sit beside the truth in `fact_return`. Under `FullInformation`, `KpiReported` is the faction's measured value and equals the actual value; returns are separate and can be gamed ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)).
   - **Truth flags.** Events may carry analytics-only truth fields (whether a message was a lie, a record falsified, a return inflated, a beggar truly hungry). No policy or sim rule reads them; recorders store them only at the Full level (including a domain switched to Full) and drop them at lower levels. Truth computed by snapshot builders (actual KPI values, sampled belief error, daily layer 1 counts) is stored at every level that writes the table.
5. **History is never overwritten.**
   - Citizenship, household membership, employment, ownership, policy and the type 2 dimensions keep every version, with valid-from and valid-to as both tick and day.
   - Intervals are half-open: `[from, to)`. An open interval has `valid_to_tick` NULL and `is_current = 1`.
6. **Integers everywhere.** Values are stored as the sim holds them: counts, permille, ticks, ware units. `value_ref` (wealth in reference units, the FE unit) uses `WareRefValue`, owned by [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) and copied into `dim_ware.ref_value`. It is a measurement index for comparisons such as the Gini, not a price: NPCs and sim rules never read it. Because holdings are also stored per ware (`fact_person_day_ware`), no value theory is built in, and an analysis can choose its own weights.
7. **Surrogate keys are separate from sim ids** ([DESIGN_CONVENTIONS.md §5](DESIGN_CONVENTIONS.md#5-modules-and-names-assetsgodsim)). Sim ids stay as natural-key columns (`npc_id`, `building_id`…), unique within a run.
8. **Never presented as designed in.** The warehouse measures what happened. No table, view or query is used to steer the sim towards an outcome.

## 3. Data structures

### 3.1 Domain event schema (`Sim/Telemetry`)
```text
abstract class DomainEvent : SimEvent            // SimEvent already carries Tick
  ushort Code      // stable numeric type code, never reused (table meta_event_type)
  byte   Version   // schema version of this event type
  int    Seq       // order within the tick, stamped by EventBuffer.Add (0, 1, 2 …), reset every tick
  TelemetryDomain Domain   // population, household, citizenship, economy, production, employment, ownership,
                           // estate, state, social, contest, comms, learning, culture, military, crime, justice,
                           // health, education, finance, politics, records, media, policy, institutions, kpi,
                           // commands, lab
```
- **Pure data.** Events hold only ints, longs, enums, ids and small fixed arrays, never references into `GodState`. They are immutable once added, so a recorder may read them on another thread.
- **Existing stage 1–3 events** (`WarDeclared`, `HqCaptured`, `TradeOffered`…) become `DomainEvent`s with codes in the 1–99 range. `CommandApplied` and `CommandRejected` hold a `Command` reference: the recorder serialises the command on the sim thread at drain time, before the next tick.
- **Event identity** is `(run_id, tick, seq)`. It is unique and totally ordered.
- **Order within a tick:**
  1. `CommandApplied` and `CommandRejected`, in submission order;
  2. then systems in ascending `SystemOrder`;
  3. within a system, ascending entity id.

  This is the order the sim already guarantees, so seq is deterministic.
- **Versioning:**
  - adding a field at the end raises `Version` and gives older rows NULL;
  - renaming or changing the meaning of a field creates a **new code**;
  - the old code is kept and marked `deprecated` in `meta_event_type`;
  - JSONL lines carry `"e"` (name), `"c"` (code) and `"v"` (version).
- **Guarded emission** (for speed at low levels):
  - the event buffer carries a `TelemetryMask` set by the host;
  - emission sites may skip *building* an event whose domain is masked off (`if (ev.Wants(Domain.Social)) ev.Add(new GestureMade{…})`);
  - the guard may cover only the construction of the event: no state change, no `Rng` draw and no cached computation may sit inside it;
  - a code-review rule and the hash matrix test (section 10) enforce this.

### 3.2 Snapshots
- **Builders:** `SnapshotBuilder.PersonDay(GodState)`, `.InstitutionDay`, `.FactionDay` and `.KpiActuals` are pure functions. They return arrays of row structs.
- **When they are called:** the host calls them after the last tick of each sim day (`SimCalendar.IsDayEnd`). The sim never calls them, and nothing in the sim reads their results.
- **Actual KPI values** (computed from true state, layer 1) come only from these builders. Reported values are sim state (institutional records), carried by the `KpiReported` event.

### 3.3 Keys and common columns
- **Run-scoped tables** carry `run_key`. In the per-run SQLite file every row has the same `run_key`, and each table's own key is `INTEGER PRIMARY KEY` (the rowid, fastest for SQLite). The logical key is `(run_key, x_key)`, which DuckDB and Parquet use when runs are combined.
- **`date_key`** equals the sim day number (day 0 is the first). `dim_date` is per run because `TicksPerDay` and `DaysPerYear` are scenario parameters.
- **Every event fact** has `tick` and `date_key`. Every spell has `valid_from_tick`, `valid_to_tick`, `valid_from_day`, `valid_to_day` and `is_current`.
- **Parties.** Economic actors can be persons, households, enterprises, institutions or factions (the state). `dim_party(party_key, party_kind, person_key, household_key, enterprise_key, institution_key, faction_key)` has exactly one non-null reference. This keeps transactions, ownership and loans to one foreign key per side.
- **`info_layer`** (1 true, 4 record, 5 published, 7 faction view) appears wherever a row could be either truth or a record.

### 3.4 Core DDL sketch (SQLite, M13)
```sql
CREATE TABLE dim_run (
  run_key INTEGER PRIMARY KEY, run_id TEXT NOT NULL UNIQUE, experiment_id TEXT, arm TEXT,
  scenario_name TEXT NOT NULL, scenario_hash TEXT NOT NULL, seed INTEGER NOT NULL, replicate INTEGER NOT NULL,
  git_commit TEXT NOT NULL, sim_version TEXT, save_version INTEGER, schema_version INTEGER NOT NULL,
  life_scale INTEGER, ticks_per_day INTEGER, days_per_year INTEGER, faction_count INTEGER,
  victory TEXT, recorder_level TEXT, record_domains TEXT, sample_every INTEGER,
  host TEXT, final_tick INTEGER, final_hash TEXT, stop_reason TEXT, exit_code INTEGER,
  wall_started TEXT, wall_seconds REAL);                      -- wall clock: here and meta_load only

CREATE TABLE dim_date (
  date_key INTEGER PRIMARY KEY,           -- sim day
  run_key INTEGER NOT NULL REFERENCES dim_run,
  sim_year INTEGER NOT NULL, day_of_year INTEGER NOT NULL,
  tick_start INTEGER NOT NULL, tick_end INTEGER NOT NULL, is_year_start INTEGER NOT NULL);

CREATE TABLE dim_person (
  person_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, npc_id INTEGER NOT NULL UNIQUE,
  origin_code INTEGER NOT NULL,           -- dim_code 'origin': initial, born, immigrant
  sex INTEGER, birth_tick INTEGER, birth_date_key INTEGER REFERENCES dim_date, cohort_year INTEGER,
  mother_person_key INTEGER REFERENCES dim_person, father_person_key INTEGER REFERENCES dim_person,
  birth_faction_key INTEGER REFERENCES dim_faction, birth_location_key INTEGER,
  genome_hash INTEGER,                    -- link to the genome, not the genome
  death_tick INTEGER, death_date_key INTEGER, death_cause_code INTEGER);   -- filled once (type 1)

CREATE TABLE dim_person_history (
  person_hist_key INTEGER PRIMARY KEY, person_key INTEGER NOT NULL REFERENCES dim_person,
  valid_from_tick INTEGER NOT NULL, valid_to_tick INTEGER, valid_from_day INTEGER NOT NULL, valid_to_day INTEGER,
  is_current INTEGER NOT NULL, life_stage_code INTEGER, faction_key INTEGER, household_key INTEGER,
  occupation_key INTEGER, role_code INTEGER, change_reason_code INTEGER);

CREATE TABLE fact_person_day (
  run_key INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date,
  person_key INTEGER NOT NULL REFERENCES dim_person, person_hist_key INTEGER REFERENCES dim_person_history,
  faction_key INTEGER, household_key INTEGER, location_key INTEGER, occupation_key INTEGER,
  age_days INTEGER, health INTEGER, nutrition INTEGER, energy INTEGER, warmth INTEGER,  -- permille
  hunger INTEGER, fatigue INTEGER, fear INTEGER,                                    -- drives, permille
  carried_value_ref INTEGER, owned_value_ref INTEGER, wealth_value_ref INTEGER,     -- reference units
  productive_assets_ref INTEGER, work_ticks INTEGER, wage_received_ref INTEGER,
  labour_sold_ticks INTEGER, labour_hired_ticks INTEGER,                            -- class measures' inputs
  children_alive INTEGER, friends INTEGER, mean_trust INTEGER, loyalty INTEGER,
  lod_tier INTEGER, info_layer INTEGER NOT NULL DEFAULT 1,
  PRIMARY KEY (date_key, person_key)) WITHOUT ROWID;

CREATE TABLE fact_transaction (           -- one exchange, from proposal to its end (accumulating)
  transaction_key INTEGER PRIMARY KEY, run_key INTEGER NOT NULL, exchange_id INTEGER NOT NULL UNIQUE,
  kind_code INTEGER NOT NULL,             -- barter, gift, wage, tax, purchase, loan_disbursement, theft…
  party_a_key INTEGER NOT NULL REFERENCES dim_party, party_b_key INTEGER REFERENCES dim_party,
  location_key INTEGER, proposed_tick INTEGER, first_handover_tick INTEGER, closed_tick INTEGER,
  proposed_date_key INTEGER, closed_date_key INTEGER,
  outcome_code INTEGER,                   -- completed, a_reneged, b_reneged, refused, timed_out, enforced
  enforced_by_institution_key INTEGER, value_a_to_b_ref INTEGER, value_b_to_a_ref INTEGER,
  promise_key INTEGER, sample_weight INTEGER NOT NULL DEFAULT 1);

CREATE TABLE fact_transaction_line (      -- one hand-over (ExchangeStep): never atomic between NPCs
  transaction_key INTEGER NOT NULL REFERENCES fact_transaction, line_no INTEGER NOT NULL,
  tick INTEGER NOT NULL, date_key INTEGER NOT NULL, from_party_key INTEGER NOT NULL, to_party_key INTEGER NOT NULL,
  ware_key INTEGER REFERENCES dim_ware, quantity INTEGER, labour_ticks INTEGER, value_ref INTEGER,
  PRIMARY KEY (transaction_key, line_no)) WITHOUT ROWID;

CREATE TABLE fact_citizenship (           -- history: one row per spell of citizenship
  citizenship_key INTEGER PRIMARY KEY, person_key INTEGER NOT NULL, faction_key INTEGER NOT NULL,
  valid_from_tick INTEGER NOT NULL, valid_to_tick INTEGER, valid_from_day INTEGER NOT NULL, valid_to_day INTEGER,
  is_current INTEGER NOT NULL, start_reason_code INTEGER, end_reason_code INTEGER);  -- birth, initial, migration, defection, capture, death

CREATE TABLE fact_kpi (
  run_key INTEGER NOT NULL, kpi_key INTEGER NOT NULL REFERENCES dim_kpi, faction_key INTEGER,
  institution_key INTEGER, period_from_day INTEGER NOT NULL, period_to_day INTEGER NOT NULL,
  actual_value INTEGER,                   -- layer 1, from SnapshotBuilder.KpiActuals
  reported_value INTEGER,                 -- layer 4/7, from the KpiReported event (NULL if not reported)
  report_tick INTEGER, reporter_institution_key INTEGER, method_code INTEGER, kpi_version INTEGER,
  lo_value INTEGER, hi_value INTEGER, sample_n INTEGER, coverage INTEGER,
  PRIMARY KEY (kpi_key, faction_key, institution_key, period_from_day));
```
The other tables follow the same patterns; their columns are listed in sections 3.5 and 3.6.

### 3.5 Dimensions
| Dimension | Natural key | Main attributes | History |
|---|---|---|---|
| `dim_run` | `run_id` | experiment, arm, scenario name and hash, seed, replicate, git commit, versions, calendar settings, recorder settings, stop reason, final hash | static |
| `dim_date` | sim day | year, day of year, first and last tick | static |
| `dim_person` | `npc_id` | origin, sex, birth tick and day, cohort year, mother, father, birth faction and place, genome hash, death fields | type 1; death fields filled once |
| `dim_person_history` | (person, valid from) | life stage, citizenship faction, household, occupation, role, change reason | **type 2**: a new row on each change of any tracked attribute |
| `dim_household` | `household_id` | formed and dissolved tick, founder, home building | type 1; membership is in `fact_household_membership` |
| `dim_faction` / `_history` | `faction_id` | name, colour, human or AI / age, controlling policy, eliminated | type 1 / **type 2** |
| `dim_institution` / `_history` | `institution_id` | faction, type, age, provision model (public, private, cooperative, communal), founded and dissolved / head person, policy, status (active, failing, suspended) | type 1 / **type 2** |
| `dim_enterprise` | `enterprise_id` | founder, form (private, cooperative, communal), founded and closed, sector | type 1; ownership in `fact_ownership` |
| `dim_party` | (kind, id) | exactly one of person, household, enterprise, institution, faction | type 1 |
| `dim_building` | `building_id` | type (kind string), size, node, hex, built and destroyed tick, initial owner party | type 1; level and ownership changes are facts |
| `dim_location` | (kind, index) | kind (hex, node, region), hex index, node index, parent hex, region id, terrain, dice number, x and z in mm | static per run |
| `dim_occupation` | code | name, sector (subsistence, production, transport, construction, military, institution, care, none) | static |
| `dim_ware` | `Ware` enum value | name, category (raw, processed, food, gear, tool), `ref_value` | static per schema version |
| `dim_policy` | policy code | instrument (tax rate, wage level, welfare payment, law…), domain, parameter name, unit | static |
| `dim_agent_policy` | registry name + version | level (civilian, military, institution, faction), kind (scripted, benchmark, trained, human), model hash | static |
| `dim_action` | action code | category (intent, command, gesture), level, name | static |
| `dim_relationship` | combination | kin band (self, parent/child, sibling, other kin, none), same household, same faction, partner, trust band (5 bands) | static junk dimension; the key is computed when the event is recorded, from the relationship at that moment |
| `dim_strategy` | (person, window) | features over the window: first-move cooperation with strangers, reciprocation, retaliation after defection, forgiveness latency, unprovoked defection, promise keeping; nearest benchmark and distance; method version | type 2 by window. Computed after the run by the `strategy-fingerprint` analysis job from `fact_social_interaction` and `fact_promise`. Never read by the sim. |
| `dim_kpi` | KPI code + version | name, unit, numerator and denominator text, source table, owner institution type | static ([KPI_CATALOGUE.md](KPI_CATALOGUE.md)) |
| `dim_code` | (code set, code) | name, description | static |

### 3.6 Facts
Kinds:
- **E** is an event row;
- **S** is a periodic snapshot;
- **A** is an accumulating row (one row per thing, updated as its milestones happen; the recorder holds it in memory and writes it when it closes, or at run end as open);
- **H** is a history spell.

| Fact | Kind | Grain | Keys | Source events / snapshot | Relationships |
|---|---|---|---|---|---|
| `fact_person_day` | S | one living person × sim day | date, person, person_hist, faction, household, location, occupation | `SnapshotBuilder.PersonDay` | exactly one row per person alive at day end |
| `fact_birth` | E | one birth | child, mother, father, household, faction, location, date | `NpcBorn` | child's `dim_person.origin` = born |
| `fact_death` | E | one death | person, faction, location, date, cause; killer (nullable) | `NpcDied` | equals `dim_person.death_*`; starts an estate |
| `fact_household_membership` | H | one person × household spell | person, household, role (head, partner, child, dependant, lodger) | `HouseholdJoined`, `HouseholdLeft` | at most one open spell per person |
| `fact_citizenship` | H | one person × faction spell | person, faction, start and end reason | `CitizenshipChanged`, `NpcBorn`, `NpcDied` | exactly one open spell per living person |
| `fact_migration` | E | one move between factions or homes | person, from and to faction, from and to location, reason (migration, defection, capture, asked to join) | `NpcMigrated`, `NpcDefected`, `NpcCaptured` | closes and opens citizenship spells in the same tick |
| `fact_transaction` | A | one exchange | parties, kind, outcome, enforcer | `ExchangeProposed`, `ExchangeStep`, `ExchangeClosed`, `GiftGiven`, `WagePaid`, `TaxCollected` | lines in `fact_transaction_line`; optional promise |
| `fact_transaction_line` | E | one hand-over | transaction, from and to party, ware, quantity or labour ticks | `ExchangeStep` | 0..n per transaction (0 = refused or reneged before any hand-over) |
| `fact_exchange_day` | S (aggregate) | day × kind × faction pair × ware | | aggregated by the recorder from the same events | the sums equal the event rows at Full |
| `fact_production` | E | one completed work cycle (building recipe, gathering trip, craft) | building (nullable), worker person, enterprise, ware out, date | `ProductionCompleted`, `Gathered` | inputs as negative lines in the same row set |
| `fact_production_day` | S (aggregate) | day × building (or gatherer faction) × ware | | aggregated | equals the event sums |
| `fact_employment` | A / H | one employment spell (a contract with an employer, not a single task) | person, employer party, building, occupation, contract id, wage terms; start, end, end reason (quit, fired, contract done, death, enlisted, migrated) | `JobTaken`, `JobLeft`, `ContractEnded` | wages through `fact_transaction` (kind wage) |
| `fact_tax` | E | one assessment or collection | payer party, collecting faction or institution, policy, ware, assessed, collected, evaded | `TaxAssessed`, `TaxCollected`, `TaxEvaded` | collection also appears as a transaction |
| `fact_ownership` | H | one asset × owner spell | asset kind (building, land hex, tool lot, enterprise share), asset id, owner party, form (state, private, communal, cooperative), acquired how (built, bought, inherited, founded, seized, gift, stolen) | `OwnershipChanged` | at most one open spell per asset (shares: per share unit) |
| `fact_inheritance` | E | one transfer from an estate to an heir | estate id, deceased, heir party, asset or ware, quantity, value, rule (custom, will, law) | `EstateSettled`, `EstateTransfer` | closes and opens ownership spells |
| `fact_health_encounter` | E | one contact with a healer or clinic | patient, provider person, institution, condition, treatment, outcome, wait ticks | `CareGiven` (M12), later clinic events | |
| `fact_referral` | E | one referral | encounter, from and to institution, outcome | `Referred` | |
| `fact_education_activity` | E | one teaching or trait-imitation episode | teacher or model, learner, kind (teach, observe, apprentice, imitate), skill or trait, ticks, gain (permille), setting (family, school) | `TeachingGiven`, `TraitImitated` | |
| `fact_learning` | E (sampled) + daily aggregate | one association-store change | person, feature, verb group, origin (`Own`, `Imitated`, `Instinct`), old and new weight, kind (update, recruit, free, episode close, imitate) | `AssociationUpdated`, `ConjunctionRecruited`, `ConjunctionFreed`, `EpisodeClosed`, `AssociationsImitated` | [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) |
| `fact_decision` | E | one sampled decision | person, top candidate scores, chosen intent, policy | `DecisionMade` | optional trace ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)) |
| `fact_trait_census_daily` | S | faction × generation × trait × day | trait, holders, mean strength | the daily culture census | [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) |
| `fact_trait_transmission` | E | one trait gained, lost or changed | person, trait, source person, route (parent, teacher, peer, innovation, mutation) | `TraitAcquired`, `TraitReinforced`, `TraitLost`, `TraitInnovated`, `TraitMutated`, `TraditionHeld` | |
| `fact_crime` | E (layer 1) | one true offence | offender, victim party, type, location, witnesses count | `CrimeCommitted` | 0..1 report, 0..n investigations |
| `fact_crime_report` | E (layer 4) | one crime recorded by an institution | institution, reported type, reported offender (may be wrong), crime (nullable: false reports exist) | `CrimeRecorded` | recorded vs true crime is the measurement |
| `fact_investigation` | A | one investigation | institution, investigator, report, suspect, outcome | `InvestigationOpened`, `…Closed` | may lead to a court case |
| `fact_court_case` | A | one case before any forum (elders' mediation from M12, courts later) | institution, judge or mediator, plaintiff and defendant parties, crime or contract, opened, decided, outcome, appealed, reversed, cost | `CaseOpened`, `CaseClosed`, `CaseDropped`, `CaseRefused`; later `CaseFiled`, `CaseDecided` | |
| `fact_grievance` | E | one wrong believed by a person | person, kind, issue, forum (0 if not brought), shared count, resolved | `GrievanceFormed`, `GrievanceShared`, `PetitionReceived`, `PetitionAnswered` | [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md) |
| `fact_detention` | H | one detention spell | person, facility institution, case, release reason | `Detained`, `Released` | |
| `fact_military_service` | H | one service spell | person, faction, assignment, pay terms, end reason (discharge, desertion, death, capture, surrender) | `Enlisted`, `ServiceEnded` | |
| `fact_service_day` | S | soldier × day | person, faction, assignment, fit, kitted, fed, assigned, morale, pay arrears | `SnapshotBuilder.PersonDay` (service part) | |
| `fact_combat` | E | one duel resolution (a battle id groups duels) | attacker, defender, location, weapons, damage dealt, outcome | `DuelResolved`, `AttackResolved`, `BuildingStruck` | |
| `fact_contest` | A | one civilian contest | parties, resource, turns, postures, escalated to `Duel` (flag), outcome | `ContestStarted`, `ContestMove`, `ContestEnded` | per-pair history lives in sim social memory ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)) |
| `fact_social_interaction` | E | one act between two NPCs | actor, target, relationship, kind (gesture, help, gift, request, threat, insult, gossip told, promise made, kept, broken), message code, `about_person_key`, honest (truth flag), understood, response | `GestureMade`, `MessageDecoded`, `GossipPassed`, `HelpGiven`, `PromiseMade`, `PromiseKept`, `PromiseBroken` | |
| `fact_social_day` | S (aggregate) | day × faction × kind × relationship | | aggregated | |
| `fact_promise` | A | one promise | promiser, promisee, relationship at time made, what (ware, labour, repayment), made, due, resolved, outcome (kept, broken, released, expired, enforced) | `PromiseMade`, `PromiseKept`, `PromiseBroken`, `PromiseReleased` | links to the transaction and loan |
| `fact_loan` | A | one loan | lender and borrower parties, institution (informal or lending), principal, terms, made, due, repaid, defaulted tick | `LoanMade`, `LoanRepaid`, `LoanDefaulted` | repayments in `fact_loan_repayment` |
| `fact_loan_repayment` | E | one repayment | loan, ware, quantity, on-time flag | `LoanRepayment` | |
| `fact_record` | E (layer 4) | one institutional record written | institution, writer, subject, recorded value, true value (truth field), falsified flag (truth field) | `RecordWritten`, `RecordFalsified` | [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md) |
| `fact_record_audit` | E | one audit or collection finding | institution, auditor, record, finding | `AuditFinding`, `CollectionRun` | |
| `fact_return` | E (layer 4) | one manager's return | institution, manager, KPI or target, period, returned value, actual value (truth field) | `ReturnFiled` (from the `FileReturn` command), `IncentivePaid` | incentives are paid on returns only ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)) |
| `fact_belief_sample` | S (sampled) | person × subject, on a deterministic sample of persons | person, subject, believed value, true value, source kind, outlet | `SnapshotBuilder.BeliefSample` | |
| `fact_collective_action` | E | one gathering, vote, strike, protest or assembly | faction, kind, participants, eligible | M12 gatherings; stage 5 `AssociationFounded`, `RallyCalled`, `WithholdStarted`, `WithholdEnded`, `Conceded`, `DispersalPosted` | |
| `fact_media_publication` | E | one publication | publisher institution, owner party, topic, subject, claimed value, true value, reach, credibility | `PublicationIssued` | the truth gap is measured here |
| `fact_exposure`, `fact_outlet_day` | E / S | one exposure / outlet × day | person, outlet / audience, perceived credibility, accuracy | `MediaExposure` / snapshot | stage 7 |
| `fact_policy_change` | E + H | one change of a state policy | faction, policy, old and new value, decided by (agent policy or human), valid from and to | `PolicyChanged` | `valid_to` is filled by the next change of the same policy |
| `fact_institution_day` | S | institution × day | institution, faction, head | `SnapshotBuilder.InstitutionDay`: staff, staff left today, vacancies, budget, spending, open cases or backlog, records written, records falsified (layer 1 count) | |
| `fact_faction_day` | S | faction × day | faction, age | `SnapshotBuilder.FactionDay`: population, citizens, treasury per main ware, hexes, HQs, VP, wars, soldiers | |
| `fact_person_day_ware` | S | person × ware × day (non-zero holdings only) | person, ware | `SnapshotBuilder.PersonDay` | wealth without value weights |
| `fact_person_day_class` | S | person × day | person, class, class measures (labour sold and hired, assets, surplus) | `SnapshotBuilder.PersonDay` (class part) | [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md) |
| `fact_household_day` | S | household × day | household, faction, hut | `SnapshotBuilder.HouseholdDay`: members, children, elders, store value, food days | [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md) |
| `fact_hex_day` | S | hex × day | location | resource stock, capacity, extracted, regrown per resource | |
| `fact_job_day` | S | contract × day | contract, employer party, worker | wage paid (ref), output value (ref) | |
| `fact_relation_edge_day` | S | relationship edge × day (Full only) | person, other person, relationship | trust, debt balance | [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md) |
| `fact_kpi` | S | KPI × subject × period | KPI, faction, institution; also lo, hi, sample n, coverage, method, producer, published tick | `SnapshotBuilder.KpiActuals` and `KpiReported` | actual vs reported (the faction's measured value) |
| `fact_command` | E | one command applied or rejected | faction, command type, origin (human, AI, schedule, external), reason code | `CommandApplied`, `CommandRejected` | |
| `fact_event` | E | one event with no dedicated table | code, tick, up to 4 ids, small integer payload | any event mapped `generic` in `meta_event_map` | low-volume events only |
| `fact_run_metric`, `fact_metric_day` | S | run × metric / day × metric | metric code | the Metrics recorder | `fact_run_metric` exists at every level except Off |
| `fact_lab_match`, `fact_lab_population_step` | E | lab match / lab generation step | preset, strategies, payoffs, shares | `lab` command | lab only, no island data |

### 3.7 Event catalogue: event to table map
This is the **full catalogue** of domain events (the conventions, section 8, name only the families). The owning document defines each event's fields; this table says where it lands. `generic` means a row in `fact_event`. An event added by a module must be added here (test 10.1).

| Family (code range) | Events (owning document) | Tables |
|---|---|---|
| Life cycle (100–199) | `NpcBorn`, `NpcMatured`, `NpcDied`, `NpcImmigrated`, `NpcEmigrated`, `PregnancyStarted`, `PregnancyEnded`, `IllnessStarted`, `IllnessEnded`, `InjuryTaken` ([POPULATION_DESIGN.md](POPULATION_DESIGN.md)); `GeneMutated` (Full only), the `GeneticsYearly` snapshot ([BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)) | `fact_birth`, `fact_death`, `fact_migration`, `dim_person`, `dim_person_history`; others `generic` |
| Household (100–199) | `HouseholdFormed`, `HouseholdJoined`, `HouseholdLeft`, `HouseholdDissolved`, `CourtshipAct`, `PartnershipFormed`, `PartnershipEnded`, `TryForChildChosen`, `TryForChildStopped`, `ChildFed`, `BeggingMade`, `CareGiven`, `ChildOrphaned`, `ChildFostered`, `ChildUnattendedHazard`, `KinBeliefFormed` (Full only), `ActivityObservable` ([HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md)) | `dim_household`, `fact_household_membership`, `fact_household_day`; `ChildFed` and `CareGiven` as daily aggregates in `fact_social_day`; others `generic` |
| Economy (200–299) | `Produced`, `Consumed`, `Gathered`, `ProductionCompleted`, `ItemMoved`, `ExchangeProposed`, `ExchangeStep`, `ExchangeClosed`, `GiftGiven`, `LabourHelped`, `JobPosted`, `JobTaken`, `JobWithdrawn`, `JobAbandoned`, `JobDone`, `WagePaid`, `WageUnpaid`, `OwnershipChanged`, `UseRightGranted`, `UseRightEnded`, `TheftCommitted`, `EnterpriseFounded`, `EnterpriseDissolved`, `CoopDecision`, `Contributed`, `CommonsTake`, `FakeSold`, `FakeDetected`, `HutBuilt`, `HutEntered`, `ConventionObserved`, `MediumOfExchangeObserved`, `WorkDone`, `SurplusDistributed` ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md), [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)) | `fact_production`, `fact_transaction`, `fact_transaction_line`, `fact_employment`, `fact_ownership`, `dim_enterprise`, `fact_crime` (theft); others `generic` |
| Estate (200–299) | `EstateOpened`, `EstateTransfer`, `EstateSettled`, `EstateDisputed`, `EstateDisputeResolved`, `EstateAssetClaimed`, `WillMade`, `WillHonoured`, `WillIgnored` ([MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)) | `fact_inheritance`; others `generic` |
| State (200–299) | `SiteDesignated`, `SiteCancelled`, `SiteDelivered`, `SiteWorked`, `SiteCompleted`, `GoodsCommitted`, `GoodsReleased`, `ContractPosted`, `ContractCompleted`, `ContractAbandoned`, `ContractWithdrawn`, `DebtWrittenOff`, `ArrearsPaid`, `PurchaseMade`, `TaxAssessed`, `TaxCollected`, `TaxEvaded`, `HandoutGiven`, `HandoutRefused`, `LawEnacted`, `LawRepealed`, `CrimeCommitted`, `CrimeObserved`, `FineLevied`, `FineRefused`, `CitizenshipChanged`, `MigrationStarted`, `ImmigrationRefused`, `GameOver` ([STATE_DESIGN.md](STATE_DESIGN.md)) | `fact_tax`, `fact_transaction`, `fact_crime`, `fact_citizenship`, `fact_migration`, `fact_policy_change` (laws); others `generic` |
| Social (300–399) | `PromiseMade`, `PromiseKept`, `PromiseBroken`, `PromiseReleased`, `Accused`, `Praised`, `Apologised`, `Forgave`, `BetrayalRecorded`, `RecognitionError`, `TrustChanged`, `RelationEvicted`, `GossipPassed`, `HelpGiven`, `LoanMade`, `LoanRepaid`, `LoanDefaulted`, `LoanRepayment` ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md), [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)); `ContestStarted`, `ContestMove`, `ContestEnded` ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)) | `fact_social_interaction`, `fact_promise`, `fact_loan`, `fact_loan_repayment`, `fact_contest`; `TrustChanged` and `RelationEvicted` as daily aggregates; edges in `fact_relation_edge_day` (Full) |
| Comms (300–399) | `GestureMade`, `MessageDecoded` ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)) | `fact_social_interaction` (truth fields `honest`, `mismatch` at Full only) |
| Learning (300–399) | `AssociationUpdated`, `ConjunctionRecruited`, `ConjunctionFreed`, `EpisodeClosed`, `AssociationsImitated`, `DecisionMade` (optional trace) ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)) | `fact_learning`, `fact_decision` |
| Culture (300–399) | `TraitAcquired`, `TraitReinforced`, `TraitLost`, `TeachingGiven`, `TraitImitated`, `TraitInnovated`, `TraitMutated`, `TraditionHeld`, the daily culture census ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)) | `fact_trait_transmission`, `fact_education_activity`, `fact_trait_census_daily` |
| Military (400–499) | `Enlisted`, `Discharged`, `Deserted`, `KitIssued`, `KitReturned`, `RationGiven`, `RationMissed`, `AssignmentPosted`, `AssignmentTaken`, `AssignmentLeft`, `AssignmentWithdrawn`, `BountyPaid`, `AttackResolved`, `DuelResolved`, `BuildingStruck`, `WarningShot`, `SoldierWounded`, `MoraleShock`, `Retreated`, `Fled`, `SurrenderOffered`, `SurrenderAccepted`, `SurrenderIgnored`, `SurrenderedKilled`, `PrisonerTaken`, `PrisonerEscaped`, `PrisonerReleased`, `TruceFormed`, `TruceBroken`, `RaidStarted`, `RaidEnded`, `AlarmCalled` ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)) | `fact_military_service`, `fact_combat`, `fact_service_day`, `fact_transaction` (kit, rations, pay, bounties); others `generic` |
| Institutions (500–599) | `InstitutionFounded`, `InstitutionDissolved`, `InstitutionStatusChanged`, `UnlockGained`, `UnlockLapsed`, `PostOffered`, `StaffHired`, `StaffLeft`, `ManagerAppointed`, `DirectiveSet`, `BudgetAllocated`, `BudgetSpent`, `CaseOpened`, `CaseClosed`, `CaseDropped`, `CaseRefused`, `ReturnFiled`, `IncentivePaid`, `Embezzled`, `BribeOffered`, `BribeTaken`, `InspectionDone`, `ComplaintMade` ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)) | `dim_institution`, `dim_institution_history`, `fact_court_case`, `fact_return`, `fact_institution_day`; others `generic` |
| Politics (500–599) | `InterestUpdated`, `GrievanceFormed`, `GrievanceShared`, `PetitionReceived`, `PetitionAnswered`; stage 5: `PledgeMade`, `RallyCalled`, `EndorsementGiven`, `DemandMade`, `WithholdStarted`, `WithholdEnded`, `Conceded`, `AssociationFounded`, `AssociationJoined`, `AssociationLeft`, `DispersalPosted` ([POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)) | `fact_grievance`, `fact_collective_action`; `InterestUpdated` as a daily aggregate |
| Information (600–699) | `RecordWritten`, `RecordFalsified`, `CollectionRun`, `AuditFinding`; stage 7: `PublicationIssued`, `MediaExposure` ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)) | `fact_record`, `fact_record_audit`, `fact_media_publication`, `fact_exposure` |
| Policy and KPI (700–799) | `PolicyChanged`, `KpiReported` | `fact_policy_change`, `fact_kpi` |
| Commands (1–99) | `CommandApplied`, `CommandRejected` and the stage 1–3 events | `fact_command`; stage 1–3 events `generic` |
| Lab (900–999) | `LabRunStarted`, `LabGeneration`, `LabCheck`, `LabRunEnded` (outside the sim stream; [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md)) | `fact_lab_match`, `fact_lab_population_step` |

## 4. Mechanics

### 4.1 Recording levels
| Level | Writes | Typical use |
|---|---|---|
| **Off** | `run.json` and the replay only | training, very long sweeps |
| **Metrics** | in-memory aggregates only: `metrics.jsonl`, `summary.json`, and `dim_run` + `fact_run_metric` + `fact_metric_day` | batches that need only outcome numbers and stop conditions |
| **Experiment** | Metrics, plus the dimensions, the daily snapshots and the events that the plan's KPIs need, with the heavy event tables off (the preset named by [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)) | policy experiments |
| **Summary** | all dimensions; all spells (H) and accumulating (A) facts; all low-volume events (births, deaths, migration, tax, ownership, inheritance, policy, KPI, commands, crime and justice, military service); daily snapshots; high-volume events (transaction lines, social interactions, production, combat) as **daily aggregates plus a 1-in-`sampleEvery` sample** | normal analysis, 200-year runs |
| **Full** | every event in every table, the analytics-only truth fields, plus `events.jsonl.gz` if asked | short runs, debugging, re-recording a replay |

The **decision trace** (`fact_decision`, [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)) is an optional add-on to any level (`--record-decisions`), off by default.

- **Per-domain switches:**
  - `record.domains` sets any domain (section 3.1 list) to `off`, `metrics`, `summary` or `full`, overriding the level;
  - the runner turns these into the event buffer's `TelemetryMask` (off domains are not even built) and the recorder's table filter.
- **Deterministic sampling:**
  - an event is kept when `hash64(run_seed, tick, seq) % sampleEvery == 0`;
  - this uses no `Rng`, so it never touches sim streams;
  - the same run always gives the same sample;
  - sampled rows carry `sample_weight = sampleEvery`;
  - accumulating rows (a transaction and its lines, a promise) are sampled by their id, so a sampled transaction keeps all its lines.
- **Training runs** use Off or Metrics with only the required metrics. By principle 3 their outcomes are identical to fully recorded runs, and `replay --record full` can recover any one of them in full later.

### 4.2 Recording pipeline
```text
sim thread:  Step → DrainInto(events) → serialise commands → mask/sample → enqueue(batch for tick)
             day end → SnapshotBuilder.* → enqueue(snapshot rows)
writer thread (one per run): map ids → surrogate keys → append rows to per-table buffers
             on day boundary → one SQLite transaction for the whole day → commit
```
- **Thread handoff:** the queue between the threads is bounded (default 8 sim days). When it is full the sim thread waits. That changes speed, never results.
- **Surrogate keys:**
  - keys are assigned by the writer in the order of first appearance, which is deterministic;
  - the maps from sim id to key are in memory (500 NPCs over 200 years is about 50,000 persons, a few MB);
  - a dimension row is written before any fact that refers to it, in the same transaction.
- **Accumulating rows** are held in memory until they close, then written. At run end, open ones are written with NULL end fields and `is_current = 1`.
- **Type 2 maintenance:**
  - a change event closes the person's current history row (`valid_to_* = now`, `is_current = 0`) and inserts a new row;
  - both writes happen in the same day transaction;
  - several changes in one tick collapse into one new row.

### 4.3 History rules
- **Spells are opened and closed by events, never inferred from snapshots.** Snapshots are used only to cross-check them (section 10).
- **Same-tick changes:**
  - a spell that closes and a spell that opens in the same tick share the tick (`to` of the old row = `from` of the new row);
  - zero-length spells are dropped by the writer and counted in `meta_load.dropped_zero_spells`.
- **Day columns** are derived from tick columns through `dim_date`, so tick and day always agree.
- **The tracked histories:**
  - **Citizenship, household membership, employment, ownership and policy** are history tables (H) or carry valid-from and valid-to.
  - **Person attributes** that change (life stage, faction, household, occupation, role) are type 2 in `dim_person_history`.
  - Every `fact_person_day` row points to the history row valid at that day's end.

### 4.4 Storage choice: SQLite first
SQLite is the first store, for these reasons:

| Need | How SQLite meets it |
|---|---|
| A real relational schema with foreign keys, views and window functions (Gini, ranks, cohorts) | standard SQL; window functions since SQLite 3.25; `PRAGMA foreign_keys` |
| No server, one file per run, easy to copy | an embedded library (`Microsoft.Data.Sqlite`); a run directory is self-contained |
| Fast bulk writes from a headless run | WAL journal, `synchronous=NORMAL` (or `OFF` for throwaway runs), one transaction per sim day, prepared statements reused, `WITHOUT ROWID` for composite-key facts. 300,000–1,000,000 rows per second is typical, so a Summary 200-year run (about 8 million rows) writes in under a minute of the writer thread's time. |
| Indexes that do not slow the load | indexes are created at run end for batch runs (`--index-after`, the default for `batch`), or up front for runs read live |
| Parallel runs | one file per run, so there is no write contention. `batch.sqlite` holds only `dim_run`, `fact_run_metric` and the experiment tables, merged at the end |

**Pragmas at open:**
```text
journal_mode=WAL
synchronous=NORMAL
temp_store=MEMORY
cache_size=-65536
page_size=8192 (before the first table)
foreign_keys=ON in tests; OFF in production loads, followed by foreign_key_check at the end
```

**Standard indexes:**
- `fact_person_day(person_key, date_key)`;
- `fact_birth(mother_person_key)`;
- each spell table on `(person_key, valid_from_tick)`;
- `fact_transaction(party_a_key)` and `(party_b_key)`;
- `fact_social_interaction(actor_person_key, target_person_key)`;
- `fact_promise(promiser_person_key)`;
- `fact_kpi(kpi_key, period_from_day)`.

At the end of the load: `ANALYZE`, then `PRAGMA optimize`.

### 4.5 Schema migrations
- **Version record:** `meta_schema_version(version, applied_at, description)`. Migrations are numbered SQL files in `DotNet/God.Runner/Warehouse/Migrations/0001_core.sql`, `0002_…`.
- **Writer:** it creates new files at the current version and refuses to append to a file of another major version (exit code 3).
- **Readers:** they use the stable views `v_person_day`, `v_life`, `v_spells_*`, which are redefined by each migration, so analysis SQL survives column additions.
- **Event mapping:** `meta_event_type(code, name, version, domain, status)` and `meta_event_map(code, version, target_table, mapper)` record which mapper handles each event version. Old JSONL streams can therefore be loaded into the current schema (`export --to sqlite`).

### 4.6 ETL
- **In-run (the normal path):** the pipeline of 4.2, with one transaction per sim day (`commitEveryDays`, default 1; Full runs with very busy days may set a tick-count limit per transaction). `meta_load(load_id, run_key, first_day, last_day, rows_by_table, dropped_zero_spells, wall_seconds)` records each committed day range.
- **Crash safety:** the file is consistent up to the last committed day. `run --from-save` at that day, with the same recorder settings, appends from the next day.
- **Offline:**
  - `export --to sqlite` loads `events.jsonl.gz` plus snapshots, which a Full JSONL stream includes as `"snapshot"` lines;
  - `replay --record LEVEL` re-runs the sim to rebuild any level;
  - both go through the same mapper code as the in-run path.
- **Post-load:** the writer computes no analysis inside the load. `dim_strategy` and other derived tables are built by analysis jobs (`god-runner analyse strategy-fingerprint --db FILE`, stage 4 after M13) that write their own tables and record their method version.

### 4.7 Parquet and DuckDB
- **`export --format parquet`** writes a hive-partitioned lake:

  ```text
  lake/<table>/experiment=<id>/scenario=<hash8>/seed=<n>/run=<run_id>.parquet
  lake/_global/<dim_ware|dim_occupation|dim_code|dim_kpi|...>/schema=<version>.parquet
  ```
- **What gets exported:**
  - run-scoped dimensions are exported per run like facts;
  - global dimensions are exported once per schema version;
  - every file carries `run_key` and `run_id`, and joins on combined data use `(run_id, x_key)`.
- **DuckDB use:**
  - query the lake with `read_parquet('lake/fact_person_day/**/*.parquet', hive_partitioning = true)`;
  - partition pruning by experiment, scenario and seed keeps queries over thousands of runs fast;
  - for small batches, DuckDB can also `ATTACH` the per-run SQLite files directly (the sqlite extension) with no export.
- **When to use which:**
  - SQLite for one to a few dozen runs and for tests;
  - Parquet and DuckDB for experiments with hundreds of runs or more, and for training datasets ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)).

### 4.8 Example queries (SQLite)
**Life expectancy at birth by cohort.** Living people are right-censored: the query reports the censored share, and a proper survival estimate is a later analysis view.
```sql
SELECT p.cohort_year,
       COUNT(*) AS born,
       AVG(CASE WHEN p.death_tick IS NOT NULL
                THEN (p.death_tick - p.birth_tick) * 1.0 / (r.ticks_per_day * r.days_per_year) END) AS mean_age_at_death_years,
       AVG(p.death_tick IS NULL) AS share_still_alive
FROM dim_person p JOIN dim_run r ON r.run_key = p.run_key
WHERE p.origin_code = (SELECT code FROM dim_code WHERE code_set='origin' AND name='born')
GROUP BY p.cohort_year ORDER BY p.cohort_year;
```

**Gini of wealth by sim year** (last day of each year, reference units):
```sql
WITH last_day AS (SELECT sim_year, MAX(date_key) AS d FROM dim_date GROUP BY sim_year),
w AS (SELECT l.sim_year, f.wealth_value_ref AS x,
             ROW_NUMBER() OVER (PARTITION BY l.sim_year ORDER BY f.wealth_value_ref) AS i,
             COUNT(*)     OVER (PARTITION BY l.sim_year) AS n
      FROM fact_person_day f JOIN last_day l ON f.date_key = l.d)
SELECT sim_year, SUM((2.0 * i - n - 1) * x) / (MAX(n) * SUM(x)) AS gini
FROM w GROUP BY sim_year ORDER BY sim_year;
```

**Intergenerational mobility.** This is the rank-rank table between the parent's wealth quintile and the child's wealth quintile, each measured at the same age, the start of adulthood (`age_days` = 10 sim days at LifeScale 100):
```sql
WITH adult AS (
  SELECT f.person_key, f.date_key, f.wealth_value_ref,
         NTILE(5) OVER (PARTITION BY f.date_key / 10 ORDER BY f.wealth_value_ref) AS q   -- quintile within the same year
  FROM fact_person_day f WHERE f.age_days = 10)
SELECT pa.q AS parent_quintile, ch.q AS child_quintile, COUNT(*) AS n
FROM dim_person c
JOIN adult ch ON ch.person_key = c.person_key
JOIN adult pa ON pa.person_key = COALESCE(c.mother_person_key, c.father_person_key)
GROUP BY pa.q, ch.q ORDER BY pa.q, ch.q;
```

**Employment spells by occupation:**
```sql
SELECT o.name AS occupation, COUNT(*) AS spells,
       AVG((COALESCE(e.valid_to_tick, r.final_tick) - e.valid_from_tick) * 1.0 / r.ticks_per_day) AS mean_days,
       SUM(e.end_reason_code = (SELECT code FROM dim_code WHERE code_set='job_end' AND name='quit')) * 1.0 / COUNT(*) AS quit_share
FROM fact_employment e JOIN dim_occupation o ON o.occupation_key = e.occupation_key
JOIN dim_run r ON r.run_key = e.run_key
GROUP BY o.name ORDER BY spells DESC;
```

**Promise-keeping rate by relationship type:**
```sql
SELECT rel.kin_band, rel.same_household, rel.trust_band,
       COUNT(*) AS promises,
       AVG(pr.outcome_code = (SELECT code FROM dim_code WHERE code_set='promise_outcome' AND name='kept')) AS kept_rate
FROM fact_promise pr JOIN dim_relationship rel ON rel.relationship_key = pr.relationship_key
WHERE pr.resolved_tick IS NOT NULL
GROUP BY rel.kin_band, rel.same_household, rel.trust_band
ORDER BY rel.kin_band, rel.trust_band;
```

## 5. Interactions with other systems
- **Every sim module** emits its domain events through `Telemetry` and adds its snapshot fields. Each owning document's Telemetry section lists them, and this document maps them to tables (`meta_event_map`).
- **KPIs:** each KPI in [KPI_CATALOGUE.md](KPI_CATALOGUE.md) names its source table here and has a reference SQL. The `actual_value` in `fact_kpi` must equal that SQL on the warehouse (test 10.6).
- **Information layers:** institutional records and reports are sim state in `Sim/Information` ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)). The warehouse stores copies of them (`fact_kpi.reported_value`, `fact_crime_report`, `fact_media_publication`) beside the truth.
- **Experiments** compute their metrics from these tables ([POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)).
- **Training** reads `dim_run` attributes (policy ids, model hashes) and usually records nothing ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)).
- **Game-theory measurement** of strategies on the island uses `fact_social_interaction`, `fact_promise` and `dim_strategy` ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)).

## 6. Player and view
- **The analytical-reporting path to the player goes through the sim, not the warehouse.**
  1. Institutions keep records (layer 4).
  2. Records become reports in `FactionKnowledgeView` (layer 7).
  3. The HUD reports panel (M13) and faction AIs read only that view.
- **Normal play never queries the warehouse.** The Unity game does not reference the SQLite writer, and a test checks the assembly references.
- **In stage 4, `FullInformation`** fills the view almost completely, so the reports the player sees match the warehouse's `reported_value` columns. Later ages make records partial, late or biased, and the gap becomes visible only in the warehouse.
- **Developer tools:** a developer-only analytics window may open a run's SQLite file in the editor, behind a define (`GOD_DEV_ANALYTICS`). It is never compiled into the player.

## 7. Telemetry
- **Events:** this document defines no domain events of its own. It owns the base schema (section 3.1), the code ranges, the domains and the mapping.
- **Code ranges** (`ushort`):

  | Codes | Module |
  |---|---|
  | 1–99 | stage 1–3 events |
  | 100–199 | Population and households |
  | 200–299 | Economy, Estate and State |
  | 300–399 | Social (with contests), Comms, Learning and Culture |
  | 400–499 | Military |
  | 500–599 | Institutions, justice, health, education, finance and politics |
  | 600–699 | Information and media |
  | 700–799 | Policy and KPI |
  | 900–999 | runner and lab |
- **Recorder counters** go into `fact_run_metric`:
  - `rows_<table>`;
  - `bytes_db`;
  - `writer_seconds`;
  - `queue_full_waits`;
  - `sampled_out_<domain>`.

## 8. Stage 4 minimum vs later stages
| Milestone | Warehouse work |
|---|---|
| **M0** | `DomainEvent` base with code, version, seq and domain; `meta_event_type` generated from the code; the **JSONL event stream** (`events.jsonl.gz`: one line per event with run id, tick, seq, code, name, version, payload); recording levels Off, Metrics and Full (JSONL); the Metrics recorder; the hash-equality test across levels |
| M1–M12 | each module adds its events and snapshot fields with codes in its range, and its JSONL mapping |
| **M13 core** (SQLite) | `dim_run`, `dim_date`, `dim_person`, `dim_household`, `dim_faction`, `dim_ware`, `dim_building`, `dim_class`, plus the plumbing `dim_party`, `dim_code`, `dim_kpi`, `meta_*`, `fact_event`; `fact_person_day`, `fact_person_day_class`, `fact_birth`, `fact_death`, `fact_household_membership`, `fact_citizenship`, `fact_transaction` and `fact_transaction_line` (with `fact_exchange_day`), `fact_production` (with `fact_production_day`), `fact_employment`, `fact_ownership`, `fact_social_interaction` (with `fact_social_day`), `fact_policy_change`, `fact_kpi`, `fact_run_metric`, `fact_metric_day`; Summary and Experiment levels; per-domain switches; deterministic sampling; the migrations framework; `export` to Parquet; the size-budget test |
| **M13 extended** (same milestone, where the source events exist by then) | `dim_person_history`, `dim_faction_history`, `dim_institution` (+ history), `dim_enterprise`, `dim_location`, `dim_occupation`, `dim_policy`, `dim_agent_policy`, `dim_relationship`; `fact_migration`, `fact_tax`, `fact_inheritance`, `fact_education_activity`, `fact_crime` and `fact_crime_report` (theft), `fact_military_service`, `fact_service_day`, `fact_combat`, `fact_contest`, `fact_promise`, `fact_loan` and repayments, `fact_institution_day`, `fact_faction_day`, `fact_command`, `fact_person_day_ware`, `fact_household_day`, `fact_hex_day`, `fact_job_day`, `fact_learning`, `fact_decision`, `dim_trait` with `fact_trait_census_daily` and `fact_trait_transmission`, `fact_grievance`, `fact_court_case` (mediation), `fact_record`, `fact_record_audit`, `fact_return`, `fact_belief_sample`, `fact_collective_action` (gatherings), `fact_relation_edge_day`; lab tables |
| After M13 in stage 4 | `dim_strategy` analysis job; DuckDB example notebooks |
| Stage 5 | `fact_court_case` for the headman's court, `dim_action`, strikes and assemblies in `fact_collective_action` ([POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)) |
| Stage 6 | `fact_health_encounter` and `fact_referral` at clinic scale, `fact_investigation`, `fact_detention`, currency columns on transactions |
| Stage 7 | `fact_media_publication`, `fact_exposure`, `fact_outlet_day`, `dim_outlet`, statistics-office records with limited information, very large DuckDB lakes |

## 9. Migration from the existing code
- **Events:** `SimEvent` and `EventBuffer` stay. `EventBuffer.Add` also stamps `Seq`. Existing event classes gain codes and domains.
- **Counters:** `SmokeRun`'s event counting becomes the first Metrics recorder's counters ([HEADLESS_SIMULATION.md §9](HEADLESS_SIMULATION.md#9-migration-from-the-existing-code-smokerun)).
- **Current stores:** there is no database today. `SaveFile` stays the only persistence of sim state. The warehouse never stores state for reloading.

## 10. Tests and acceptance criteria
| # | Test | What must hold |
|---|---|---|
| 10.1 | `EveryEventType_MapsToATable` | Reflection over all `DomainEvent` subclasses: each (code, version) has a `meta_event_map` row with a target table (`generic` = `fact_event`), or status `analysis-none` with a written reason, and appears in section 3.7. Codes are unique and in their module's range. |
| 10.2 | `ForeignKeys_Hold` | After a 5-sim-year Full run on `band_village_100`: `PRAGMA foreign_key_check` returns no rows. Polymorphic `dim_party` rows have exactly one non-null reference. |
| 10.3 | `Counts_Reconcile_WithMetrics` | At Full: row counts of births, deaths, transactions, lines, tax and promises equal the Metrics recorder's counters exactly. Sums of ware quantities per ware per faction in lines equal the counters exactly. At Summary: daily aggregates equal exactly, and the sampled row count × weight is within ±10% for any series with at least 1000 events. |
| 10.4 | `Snapshots_Reconcile_WithState` | On the last day: `fact_person_day` rows = living NPCs in state. Each living person has exactly one open citizenship spell and at most one open household spell. Open ownership spells = current owners in state. |
| 10.5 | `Recording_DoesNotChangeHash` | Seeds 1–4, 2 sim years (CI) and seed 1 for 200 sim years (nightly): Off, Metrics, Experiment, Summary, Full, every domain alone switched to Full, and `sampleEvery` 1, 16 and 64 all give identical checkpoint hashes. |
| 10.6 | `KpiActual_EqualsReferenceSql` | For every KPI with a reference SQL in [KPI_CATALOGUE.md](KPI_CATALOGUE.md): the SQL over the warehouse equals `fact_kpi.actual_value` exactly (integers) for every period of a 2-year run |
| 10.7 | `Spells_AreConsistent` | No overlapping spells per person in citizenship, household and military service. `valid_to ≥ valid_from`. Type 2 rows are contiguous (each `valid_to` = the next row's `valid_from`). Day columns agree with ticks through `dim_date`. |
| 10.8 | `Warehouse_IsDeterministic` | Two identical runs produce identical content: an ordered dump of every table, hashed, excluding the wall-clock columns of `dim_run` and `meta_load` |
| 10.9 | `Size_Budget_Summary` | A 20-sim-year Summary run of `band_village_500`: bytes per sim day × 2000 days ≤ **1 GB** (target 600 MB). Nightly, the real 200-year run (`LongRun_200Years`) asserts the file ≤ 1 GB. |
| 10.10 | `Writer_Overhead` | On `bench_npc_500`: Summary recording costs at most 15% of sim time; Full at most 60%; Metrics at most 3% |
| 10.11 | `Parquet_RoundTrip` | `export` then DuckDB: row counts and sums per table equal SQLite |
| 10.12 | `Migration_FromFixture` | A schema-v1 fixture DB migrates to the current version and the example queries (4.8) return non-empty results of the right shape (life expectancy between 0 and the maximum age; Gini between 0 and 1) |
| 10.13 | `Game_DoesNotReference_Warehouse` | The `God.Game` and `God.Sim` assemblies do not reference `Microsoft.Data.Sqlite` or the SQLite writer |
| 10.14 | `Replay_ReRecord_EqualsLiveRecord` | A run recorded live at Full and the same run recorded Off and then `replay --record full` give identical warehouse content (10.8 method) |
| 10.15 | `TruthFields_OnlyAtFull` | At Summary and below, every event-borne truth-field column (`honest`, `mismatch`, `falsified`, the return's actual value, a record's true value) is NULL; at Full it is filled. Snapshot-built truth (`fact_kpi.actual_value`, `fact_belief_sample.true_value`, layer 1 counts in `fact_institution_day`) is stored at every level that writes the table. No policy references a truth field (reflection, with the conventions' policy test). |

**Size estimate behind 10.9** (500 living NPCs on average, 10 days per year, 200 years = 2000 days, about 50,000 persons ever):

| Table | Rows | About |
|---|---|---|
| `fact_person_day` | 1.0 M | 90 MB |
| `dim_person` + `dim_person_history` | 50 k + 400 k | 30 MB |
| spells (household, citizenship, employment, ownership, service) | 1.5 M | 90 MB |
| promises (all) | 1.0 M | 60 MB |
| transactions + lines (1 in 16 sample) | 0.6 M | 40 MB |
| social interactions (1 in 16) + `fact_social_day` | 1.3 M + 0.3 M | 60 MB |
| production, exchange, KPI, institution and faction daily tables | 1.2 M | 50 MB |
| low-volume events (births, deaths, migration, tax, policy, commands) | 0.5 M | 30 MB |
| indexes | | about 130 MB |
| **Total** | | **about 580 MB** |

Full at the same scale is about 5–8 GB, so Full is meant for runs of up to about 20 sim years.

## 11. Open questions
None for the owner. Settled:
- **`ref_value` for wealth:** `WareRefValue` is owned by [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) as a measurement index only, and holdings are also stored per ware in `fact_person_day_ware`, so analyses can choose their own weights.
- **Employment grain:** a job is a lasting contract (a spell in `fact_employment`); single tasks are production rows, as [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) describes.
- **The `experiment` level:** adopted as a preset between Metrics and Summary; its domain list is fixed per plan by [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md).
- **Experiment table names** keep their unprefixed names as tables owned by POLICY_EXPERIMENTS.
