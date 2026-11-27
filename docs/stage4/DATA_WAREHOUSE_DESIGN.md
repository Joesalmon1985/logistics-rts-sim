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
Other documents refer to these names. A name changes only by editing this list. **Milestone names (27 Nov):** M13 is split into **M13a** and **M13b** ([conventions §10](DESIGN_CONVENTIONS.md#10-roadmap-and-stage-4-milestones-the-official-list)). "M13 core" below means **M13a**, except `dim_kpi` and `fact_kpi`, which come with the KPI engine in **M13b**. An "M13 extended" table comes in **M13a** when its sources are person, household, economy, citizenship, learning, culture or military events of milestones already done, and in **M13b** when it records institutions, records, returns or KPIs (`dim_institution`, `fact_institution_day`, `fact_record`, `fact_record_audit`, `fact_return`, `fact_court_case`, `fact_grievance` reports). M13a needs only M0 and the event types of the milestones done, so it may be pulled forward after M6; it then grows with each later milestone's events.

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
- **`date_key`** equals the sim day number (day 0 is the first; as built, an event at tick t belongs to day (t−1)/TicksPerDay, see [8.1](#81-as-built-m13a-27-nov-2026)). `dim_date` is per run because `TicksPerDay` and `DaysPerYear` are scenario parameters.
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

The **decision trace** (`fact_decision`, [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)) is an optional add-on to any level (`--record-decisions`), off by default. **As built (M3b, 26 Nov 2026):** the event `DecisionMade` (code 334, one per chosen intent with the term split) exists and is in the catalogue mapped to `fact_decision`; the runner builds it only when the learning domain records at Full. The `--record-decisions` switch for lower levels is not built yet.

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
  - *As built (M13a, 27 Nov 2026):* only `replay --record full --sqlite` is built; `export --to sqlite` returns exit 9 (see [8.1](#81-as-built-m13a-27-nov-2026)).
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

**One NPC's whole life** (owner requirement, 27 Nov: it must be possible to inspect an NPC's entire life: family, education, employment, ownership, transactions, relationships, estate). The runner's `life NPC_ID` report and the M13a dev-mode view run this union over the person's spells and events, in time order:
```sql
-- :run and :npc are parameters; each branch returns (tick, kind, detail)
SELECT birth_tick, 'born', 'mother ' || mother_npc_id || ', father ' || bio_father_npc_id FROM dim_person WHERE run_key=:run AND npc_id=:npc
UNION ALL SELECT from_tick, 'household', household_key || ' ' || join_reason FROM fact_household_membership WHERE run_key=:run AND npc_id=:npc
UNION ALL SELECT from_tick, 'employment', employer_party_key || ' ' || job_kind FROM fact_employment WHERE run_key=:run AND npc_id=:npc
UNION ALL SELECT from_tick, 'citizenship', faction_key FROM fact_citizenship WHERE run_key=:run AND npc_id=:npc
UNION ALL SELECT from_tick, 'owns', asset_key FROM fact_ownership WHERE run_key=:run AND owner_npc_id=:npc
UNION ALL SELECT tick, 'transaction', transaction_key FROM fact_transaction WHERE run_key=:run AND (party_a_npc_id=:npc OR party_b_npc_id=:npc)
UNION ALL SELECT tick, 'teaching', skill_code FROM fact_education_activity WHERE run_key=:run AND learner_npc_id=:npc
UNION ALL SELECT tick, 'inherited', estate_key FROM fact_inheritance WHERE run_key=:run AND heir_npc_id=:npc
UNION ALL SELECT death_tick, 'died', death_cause_code FROM dim_person WHERE run_key=:run AND npc_id=:npc AND death_tick IS NOT NULL
ORDER BY 1;
```
Relationships come from `fact_social_interaction` and `fact_relation_edge_day` for the same NPC. The column names here are indicative and are fixed against the DDL (3.4) when M13a is built; the test `Life_History_Complete` (10.16) checks that every event naming the NPC appears.

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
| **M13a = M13 core** (SQLite; may be pulled forward after M6) | `dim_run`, `dim_date`, `dim_person`, `dim_household`, `dim_faction`, `dim_ware`, `dim_building`, `dim_class`, plus the plumbing `dim_party`, `dim_code`, `dim_kpi`, `meta_*`, `fact_event`; `fact_person_day`, `fact_person_day_class`, `fact_birth`, `fact_death`, `fact_household_membership`, `fact_citizenship`, `fact_transaction` and `fact_transaction_line` (with `fact_exchange_day`), `fact_production` (with `fact_production_day`), `fact_employment`, `fact_ownership`, `fact_social_interaction` (with `fact_social_day`), `fact_policy_change`, `fact_kpi`, `fact_run_metric`, `fact_metric_day`; Summary and Experiment levels; per-domain switches; deterministic sampling; the migrations framework; `export` to Parquet; the size-budget test |
| **M13a/M13b extended** (M13a where the sources are person, household, economy, citizenship and other non-institution events; M13b for institutions, records, returns and KPIs; see 1.1) | `dim_person_history`, `dim_faction_history`, `dim_institution` (+ history), `dim_enterprise`, `dim_location`, `dim_occupation`, `dim_policy`, `dim_agent_policy`, `dim_relationship`; `fact_migration`, `fact_tax`, `fact_inheritance`, `fact_education_activity`, `fact_crime` and `fact_crime_report` (theft), `fact_military_service`, `fact_service_day`, `fact_combat`, `fact_contest`, `fact_promise`, `fact_loan` and repayments, `fact_institution_day`, `fact_faction_day`, `fact_command`, `fact_person_day_ware`, `fact_household_day`, `fact_hex_day`, `fact_job_day`, `fact_learning`, `fact_decision`, `dim_trait` with `fact_trait_census_daily` and `fact_trait_transmission`, `fact_grievance`, `fact_court_case` (mediation), `fact_record`, `fact_record_audit`, `fact_return`, `fact_belief_sample`, `fact_collective_action` (gatherings), `fact_relation_edge_day`; lab tables |
| After M13 in stage 4 | `dim_strategy` analysis job; DuckDB example notebooks |
| Stage 5 | `fact_court_case` for the headman's court, `dim_action`, strikes and assemblies in `fact_collective_action` ([POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)) |
| Stage 6 | `fact_health_encounter` and `fact_referral` at clinic scale, `fact_investigation`, `fact_detention`, currency columns on transactions |
| Stage 7 | `fact_media_publication`, `fact_exposure`, `fact_outlet_day`, `dim_outlet`, statistics-office records with limited information, very large DuckDB lakes |

### 8.1 As built (M13a, 27 Nov 2026)
The M13a core is merged in `unity/poc`: code in `DotNet/God.Runner/Warehouse/`, tests in `DotNet/God.Runner.Tests/WarehouseTests.cs`. The design above stands. It was built for the **stage 3 event set** (factions, production, combat, commands), so the person, household and NPC tables are not built yet. The notes below record what settled.

- **Where it lives.** SQLite is in `God.Runner` (net10) behind `IRunRecorder`; `God.Sim` and `God.Game` do not reference it (test `SimAndRecordingAssemblies_DoNotReferenceSqlite`). Unity stays on JSONL. SQLite is opt-in (`--sqlite`), and the scenario schema is unchanged. CLI: [HEADLESS_SIMULATION.md §4.11](HEADLESS_SIMULATION.md#411-the-cli).
- **Tables in schema v1** (`Warehouse/Migrations/0001_core.sql`):

  | Group | Tables |
  |---|---|
  | meta | `meta_schema_version`, `meta_event_type`, `meta_event_map`, `meta_load` |
  | dimensions | `dim_run`, `dim_date`, `dim_code`, `dim_ware`, `dim_faction`, `dim_faction_history`, `dim_location`, `dim_building`, `dim_kpi` |
  | facts | `fact_faction_day`, `fact_faction_day_stock`, `fact_production`, `fact_production_day` (faction is part of the key), `fact_combat`, `fact_combat_day`, `fact_command`, `fact_event`, `fact_event_day`, `fact_run_metric` (`run_key`, `metric`, `source`, `value`, `deterministic`), `fact_metric_day` |
  | structure only | `fact_policy_change`, `fact_kpi` (no rows until their sources exist) |
  | views | `v_faction_day`, `v_events` |

  `fact_combat` is one table with a `kind_code` (attack, unit_died, building_destroyed), not three tables.
- **Day convention.** An event at tick t belongs to day (t−1)/TicksPerDay. `dim_date.tick_start` and `tick_end` are inclusive.
- **Dimensions.** `dim_building` is type 1: an upsert on the day of change, with no history table. Setup HQs are inserted at Begin. An event that drives a dimension also writes a `fact_event` row, so the event log stays complete.
- **Adding tables (registration pattern).** A later milestone writes a migration (`0002_…sql`), the `TableDef`s, and `EventMapRegistry.Register<T>(table, mapper, handler)` calls. The recorder is not edited (test `ARegisteredMapper_ChangesTheMapAndTheRows_WithoutTouchingTheRecorder`). An event with no registered mapper lands in `fact_event`.
- **Versions and pins.** The writer refuses a file of a newer schema version (exit 3). `SQLitePCLRaw.lib.e_sqlite3` is pinned at 2.1.14, because 2.1.11 has a known high-severity advisory.
- **Offline routes.** `export` to CSV and to Parquet (hive-partitioned, with `_global`) is built. `export --to sqlite` from JSONL is not built and returns exit 9; the route is `replay --record full --sqlite` ([4.6](#46-etl)).
- **Overhead bench** (`bench --cases recording`; stage 3 scenario, no NPCs, 30 sim days, seed 1). Every mode gives the same final hash.

  | Mode | Ticks/s | Overhead vs off |
  |---|---|---|
  | off | 11,582 | — |
  | in-memory metrics | 11,423 | 1.4% |
  | SQLite metrics | 11,208 | 3.3% |
  | JSONL summary | 11,205 | 3.4% |
  | SQLite summary | 10,784 | 7.4% |
  | JSONL full | 11,141 | 3.9% |
  | SQLite full | 10,899 | 6.3% |

  SQLite size per sim day: 12.6 KB at metrics, 34.7 KB at summary, 54.9 KB at full. These are stage 3 numbers; the NPC-scale bench (`bench_npc_500`, criteria 10.9 and 10.10) comes after M3.
- **Not built yet:**
  - the household, spell, transaction and other NPC-era tables beyond the M3 set ([8.2](#82-as-built-population-tables-m3-schema-v2));
  - the Experiment level;
  - `--record-decisions`;
  - the size-budget test (10.9) and the 200-year nightly run;
  - appending after `--from-save` (4.6, crash safety);
  - a DuckDB check of the Parquet lake (10.11 is checked by reading the Parquet files back and comparing row counts and integer sums with SQLite);
  - the migration-fixture test (10.12), since only schema v1 exists;
  - the life-history report (10.16).

### 8.2 As built: population tables (M3, schema v2)
`Warehouse/Migrations/0002_population.sql`, mappers in `Warehouse/PopulationMapping.cs`, tests in `PopulationWarehouseTests.cs`. Each grain is also a row in `meta_table_grain` (global table, exported under `_global`).

| Table | Grain | Source events | Notes |
|---|---|---|---|
| `dim_person` | one row per person ever alive in the run | `NpcSpawned`, `NpcDied` | Type 1: key by first appearance; `npc_id` is the sim's persistent id. `is_founder`, `spawn_tick`, `spawn_date_key`, `spawn_location_key`, faction; death fields filled once. No genome or trait columns: `NpcSpawned` carries none (M4 adds birth and genome). A person first seen in another event (a recorder that starts late) gets a row with a NULL spawn. |
| `fact_death` | one row per death | `NpcDied` | Cause is `dim_code 'death_cause'`; `age_ticks`; equals `dim_person.death_*`. |
| `fact_person_day` | one row per living person x sim day | `NpcDay` | `date_key` is the day the row describes, not the day it was emitted. All `NpcDay` fields: drives at day end and day means, eleven `ticks_*` columns (`dim_code 'activity'`; their sum is the ticks lived), meals, bed, carry, decisions, learned and explore share, reward and punishment, store size, mode. Written at Summary and Full (the event is not high volume). |
| `fact_production` | one row per `WaresProduced`, `Produced` or `Consumed` | same | The stage 3 table extended: `person_key`, `direction` (1 in, -1 out), `reason_code` (`produce_reason` or `consume_reason`), `source_id`, `hut_id`, `with_tool`. Stage 3 rows keep a building and a NULL person. Personal-goods rows are sampled below Full (`sample_weight`); no daily aggregate yet. |
| `fact_learning` | one row per learning event | `AssociationUpdated`, `ConjunctionRecruited`, `ConjunctionFreed`, `EpisodeClosed` | `kind_code` (`dim_code 'learning_kind'`): 1 update, 2 recruit, 3 free, 4 episode close. Columns not used by a kind are NULL. Kind 1 is sampled below Full. |

`dim_ware` lists every ware id (`Wares.PersonalCount`, 32), the personal wares included. An event whose catalogue table has no mapper yet still lands in `fact_event`.

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
| 10.16 | `Life_History_Complete` | (M13a) For 20 NPCs sampled by id over a 10-sim-year Full run, the life-history report contains every domain event that names the NPC (birth, household and citizenship spells, employment, ownership, transactions, teaching, relationships, inheritance, death), in tick order, and nothing that does not; spells are half-open and do not overlap |

**As built (M13a, 27 Nov 2026), 10.8 and 10.14:** the content hash excludes the wall-clock columns of `dim_run` and the timestamps of `meta_load`. `dim_run.stop_reason` legitimately differs between a live run (`maxTicks`) and a replay (`toTick`), so 10.14 compares everything else. The tests are `Warehouse_IsDeterministic` and `ReRecording_AReplay_GivesTheSameContentAsTheLiveRecord` in `WarehouseTests.cs`.

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


---

## 12. Owner D5: Civilisation and Divinity (26 Nov 2026)
**Status: plan only. Nothing in this section is built.** Source: [OWNER_BRIEF_2026-11-26.md](../civdiv/OWNER_BRIEF_2026-11-26.md) §§33–36, decisions D5.1–D5.33. Where this section and sections 1–11 differ about the player (a god, not a government, D5.1–D5.2) or about the tables below, this section wins. Sections 1–11 and the as-built notes in 8.1–8.2 stand; the schema is at v6 (`0006_social.sql`) and this section plans v7 onwards.

### 12.1 What stays, and the rules every table below follows
- **Retained (brief §33):** the SQLite warehouse, CSV and Parquet export, pure deterministic C# as the authority, recording that never changes a hash (test 10.5), explicit grains in `meta_table_grain`, persistent ids, deterministic event ids `(run_id, tick, seq)`, half-open effective dates, the five recording levels (Off, Metrics, Experiment, Summary, Full) with per-domain switches and deterministic sampling.
- **No parallel system.** New tables are `dim_*` and `fact_*` registered the way 8.1 describes: a numbered migration, `TableDef`s, `EventMapRegistry.Register<T>` calls, rows in `meta_table_grain` and `meta_event_map`. An event with no mapper still lands in `fact_event`. `fact_command` stays the one table of external commands applied or rejected; divine tables link to it by `command_key`.
- **Event names, not codes.** The event code ranges for the new families are allocated by `docs/civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md` (a contracts agent is writing it; it did not exist when this section was written). This section names events only. The existing ranges (100–599, 700–799, 900–999) are untouched. Every new event must appear in 3.7 and have a `meta_event_map` row (test 10.1); the new rows of the catalogue are in 12.5.
- **Persistent ids** (sim ids, never surrogate keys; `*_id` columns are the natural keys, unique within a run): `god_id`, `faction_id`, `org_id`, `npc_id`, `commandment_id`, `religion_id`, `intervention_id` (one per divine action, minted when the action is accepted and carried by every effect row), `command_id`, `compulsion_id`, `possession_id`, `avatar_id`, `miracle_id`, `ledger_id`, `request_id`, `case_id`, `loan_id`. Surrogate keys are assigned by the writer in order of first appearance as today.
- **Effective dates** (history tables, marked **H**): `valid_from_tick` inclusive, `valid_to_tick` exclusive, `valid_from_day`, `valid_to_day`, `is_current`; opened and closed by events, never inferred from snapshots (4.3); same-tick close and open share the tick; zero-length spells are dropped and counted in `meta_load.dropped_zero_spells`. Type 2 dimensions carry the same columns.
- **Causality columns** (brief §14: "record the causal information needed to investigate"): every effect row can carry `cause_kind_code` and `cause_ref` (the convention of `fact_social_interaction.cause_code`) plus `intervention_id` when a divine action is upstream. `dim_code 'cause_kind'` gains: divine suggestion, divine command, compulsion, possession, avatar act, miracle, commandment, own decision, institution.
- **Who acted.** Because possession and compulsion must be visibly "divinely controlled rather than voluntarily chosen" (brief §6 Mode C), an `ALTER TABLE ... ADD COLUMN control_code` (`dim_code 'control_source'`: 0 own decision, 1 compelled, 2 possessed, 3 avatar) goes onto the person-action tables that exist by then: `fact_decision`, `fact_production` (person rows), `fact_social_interaction`, `fact_transaction`, `fact_transaction_line`, `fact_ownership`. Default 0, so older rows and Enabled=0 runs are unchanged.
- **Truth fields** (Full only, section 2 principle 4 and test 10.15) added by this section: `misunderstood` and `witness_attributed_to_god` (what the NPC wrongly or rightly blames, truth is the intervention), `record_true_value` on institutional rows, `prayer_answered_by_intervention`. At Summary and below these are NULL. No policy, institution manager or AI god reads them.
- **Divine and institutional knowledge are separate (D5.28).** Rows say which layer they describe: `info_layer` 1 true, 4 institutional record, 8 divine observation (new value, see [INFORMATION_AND_MEDIA_DESIGN.md §12](INFORMATION_AND_MEDIA_DESIGN.md#12-owner-d5-civilisation-and-divinity-26-nov-2026)). The warehouse is layer 9 and is read by nobody inside the sim.
- **Unlimited sandbox and disabled modes (brief §7).** Mythic rows record both the nominal cost and the charged cost, so an unlimited-power run still measures what it would have cost; Disabled mode records every divine command as a `fact_command` rejection with its reason code and writes nothing else.

### 12.2 Recording levels for the new tables
| Mark | Meaning | Used for |
|---|---|---|
| **S** | every row at Summary and Full; Experiment writes it only if the plan's KPIs name it | low-volume events, spells, accumulating rows, dimensions |
| **HV** | high volume: below Full a daily aggregate plus a 1-in-`sampleEvery` sample (`sample_weight`), accumulating rows sampled by their id | perceptions by bystanders, prayers, worship, service requests and deliveries, relevant-choice rows, divine observation |
| **F** | Full only | truth fields, per-edge or per-item detail |
| **D** | daily snapshot at Summary and Full, in-memory aggregate at Metrics | balances, institution-day |

New `TelemetryDomain` values (names; the contracts document fixes the numbers): `divine`, `possession`, `avatar`, `miracle`, `ledger`, `commandment`, `religion`, `organisation`, `service`, `lending`, `justice`, plus the existing `employment`, `ownership`, `health`, `education`, `finance`, `crime`, `military`, `institutions`. `record.domains` can switch each alone, as today.

### 12.3 Planned tables
Column "Level" uses the marks above. "Reconciles to" is the rule of acceptance test W-D5-1 (section 12.8): the table is checked against the sim's own events or counters and the check must be exact (integers) unless it says "sample". "Mig" is the planned migration block (12.4). Package and tier use the IDs of brief §39 and §42.

**A. Divine identity, Mythic and divine actions**

| Table | Grain and persistent keys | Time semantics | Level | Reconciles to | Mig / Pkg / Tier |
|---|---|---|---|---|---|
| `dim_god` | one god ever in the run; `god_id` | type 1; created at setup (every initial faction has a god; gods persist independently of factions, D5.4–D5.5); never closed, because a god cannot cease to exist | S | count = gods created by the scenario (`GodCreated`); never decreases | 0007 / F1, D1 / 1 |
| `dim_god_history` | one row per change of a god's controlling policy (human, a named AI god policy and its model hash), associated faction, Mythic mode (standard, unlimited sandbox, disabled) and configured maximum, regeneration and cost table version | type 2, **H** | S | `GodPolicyChanged`, `GodAssociated`, `MythicConfigChanged`; rows contiguous per god | 0007 / D1 / 1 |
| `fact_god_faction` | one spell per (god, faction) association; the god outlives the faction | **H** | S | open spells = associations in state; a collapsed faction closes its spell and not the god | 0007 / D1 / 1 |
| `fact_mythic_balance_day` | one god × sim day: balance at start and end, maximum, regeneration credited, upkeep charged, spent by category (the `fact_mythic_ledger` categories), nominal and charged | day-end snapshot | D | `end = start + regen − charged` exactly; equals the in-sim balance on the last day; sums equal the ledger | 0008 / D1 / 1 |
| `fact_mythic_ledger` | one row per credit or debit of Mythic: `ledger_id`, god, category (suggestion, command, compulsion, possession, avatar manifest, avatar upkeep, blessing, healing, smiting, resource creation, commandment, resurrection, other miracle, regeneration, grant), `nominal_cost`, `charged_cost`, `balance_after`, `intervention_id` | event | S (continuous upkeep is rolled into daily rows, kept per tick only at Full) | Σ per god = balance change; every accepted divine action has ≥ 1 row; unlimited mode has `charged_cost = 0` and `nominal_cost` > 0 | 0008 / D1 / 1 |
| `fact_divine_action` | one accepted divine action: `intervention_id`, god, kind (suggestion, command, compulsion, possession start or end, avatar manifest or dismiss, miracle kind, commandment reveal, revise or revoke, resurrection), target kind and id, scope, tick, `command_key` link to `fact_command`, Mythic cost, status | event (one row, status updated at close) | S | one row per `CommandApplied` whose command is a divine type; rejected ones stay in `fact_command` only | 0008 / D1 / 1 |

**B. Commands, compulsion, possession, avatars, miracles**

| Table | Grain and persistent keys | Time semantics | Level | Reconciles to | Mig / Pkg / Tier |
|---|---|---|---|---|---|
| `fact_divine_command` | one issued suggestion, command or compulsion: `command_id`, god, kind, action code (the brief §9 vocabulary, mapped to the existing action primitives), target (individual, group, household, institution, location), scope, expiry tick, cost, final status (open, completed, expired, cancelled, refused by all, target died) | accumulating, closed by its end event | S | one row per issued instruction; the sum of target rows equals the fan-out in the sim | 0009 / D2 / 1 |
| `fact_divine_command_target` | one row per individual a command reaches (an institution-level instruction fans out to its office holders): `command_id`, `person_key`, route (direct, via institution, via household) | event | S | count = `CommandFanOut` events | 0009 / D2 / 1 |
| `fact_command_perception` | one row per person who perceives a command or suggestion: channel (voice, vision, dream, via another person), clarity, tick; the target and any bystander | event | HV (the target's own perception always S) | per target: exactly one row if perceived, none if not; perceived ≤ issued × fan-out | 0009 / D2 / 1 |
| `fact_command_response` | one row per response: considered, accepted, refused, deferred, partly followed, completed, expired unheard; reason code, delay in ticks, `decision_key` when `fact_decision` is on | event | S for targets, HV for others | every target has a final response or an expiry row by `expiry tick`; institutions' accept, modify or refuse (scenario 15) is a response with `org_key` | 0009 / D2 / 1 |
| `fact_compulsion` | one temporary compulsion: `compulsion_id`, person, `command_id`, action, the physical-constraint outcome (attempted, blocked: no path, no material, no action available), Mythic charged | **H** (`valid_from_tick` to end by completion, timeout, cancel, death) | S | spells never overlap per person; every spell has an end reason; no spell outlives the person | 0009 / D2 / 1 |
| `fact_possession` | one possession period: `possession_id`, god, person, start and end, end reason (player ended, Mythic exhausted, died, forced), Mythic charged, ticks, count of actions taken, injury taken (health at start and end) | **H** | S | at most one open spell per run in the first release (brief §6); `ticks` = Σ of `control_code = 2` rows in `fact_person_day`; Mythic = ledger | 0010 / D4 / 2 |
| `fact_avatar_manifestation` | one manifestation: `avatar_id`, god, start and end tick, start and end location, end reason (dismissed, Mythic exhausted, destroyed), upkeep charged, damage taken; the god persists after destruction | **H** | S | at most one open spell per god; `destroyed` rows equal `AvatarDestroyed` events; the god's `dim_god` row is never touched | 0011 / D5 / 2 |
| `fact_miracle` | one miracle: `miracle_id`, god, kind (heal injury, heal disease, bless, protect, smite person, building or resource source, create or destroy resources, reveal knowledge or technology, consecrate, resurrect, area, weather, terrain), target kind and id, area, magnitude, cost, validated or refused, `intervention_id` | event | S | one row per accepted miracle command; refused ones in `fact_command` | 0012 / D3 / 1 |
| `fact_miracle_effect` | one genuine state change caused by a miracle: kind (health delta, disease cured, ware created or destroyed, building damaged, tree removed, knowledge revealed, flag set), before and after, `ledger_id` where goods moved | event | S | a miracle row with no effect row is a no-op and must say why; health deltas equal the change in `fact_person_day.health` across the tick | 0012 / D3 / 1 |
| `fact_resource_ledger` | one creation or destruction of material resources that is not a recipe or consumption: `ledger_id`, ware, quantity, direction, cause (divine create, divine destroy, smite, scenario spawn, ecology event), holder kind and id before and after, `lot_id`, `intervention_id` | event | S | joins 1:1 to `fact_production` rows with reasons divine create and divine destroy (the conservation law C1 of SHARED_CONTRACTS 2.4 is checked with the ledger included): `Σ Produced − Σ Consumed` per ware equals `Σ holdings` exactly | 0007 / F1, C1, D3 / 1 |
| `fact_divine_observation` | one answer the god's observation facility gave (what was asked, which layer answered, confidence), for human and AI gods alike | event | HV; Tier 3 | none to truth (it records what was shown); links to `fact_record` by `record_id` for institutional answers | 0008 / D1, U5 / 3 |

**C. Resurrection and archive**

| Table | Grain and persistent keys | Time semantics | Level | Reconciles to | Mig / Pkg / Tier |
|---|---|---|---|---|---|
| `fact_resurrection` | one resurrection: person, variant (lesser, full, restoration, rejuvenation), years since death, health and energy given, age change (rejuvenation only), god, `intervention_id` | event | S | one row per `NpcResurrected`; the same `dim_person` row (the identity is kept, brief §11) | 0013 / D7 / 2 |
| `fact_life_spell` | one life of a person: `life_no` (1 at birth or founding, +1 per resurrection), start tick, end tick (death), start cause (born, founded, immigrated, resurrected) | **H** | S | spells contiguous with no overlap; deaths = closed spells; `dim_person.death_*` keeps the first death, and `fact_death` gains `life_no` so every later death is a row | 0013 / D7 / 2 |
| `fact_npc_archive` | one archival of a dead individual out of active processing and any restoration | **H** | S | `archived` count = deaths archived by the sim; a resurrection of an archived person closes the spell | 0013 / D7 / 2 |

A resurrection never rewrites other tables: estates already settled stay in `fact_inheritance` and `fact_ownership` with their original dates, partnerships and employment spells closed by the death stay closed, and new spells open only on new events (acceptance test W-D5-5).

**D. Commandments, belief, religion**

| Table | Grain and persistent keys | Time semantics | Level | Reconciles to | Mig / Pkg / Tier |
|---|---|---|---|---|---|
| `dim_commandment` | one commandment: `commandment_id`, issuing god, template code, kind (required, prohibited, encouraged), action and object classes, scope (people, geography), conditions, importance, introduced tick | type 1 | S | = `CommandmentIssued` | 0014 / D6 / 1 |
| `dim_commandment_revision` | one revision of a commandment: revision number, the fields above as they stand in that revision, revoked flag | type 2, **H** | S | contiguous revisions; the current row equals the sim's commandment; revoked rows close the commandment | 0014 / D6 / 1 |
| `fact_commandment_known` | one spell per (person, commandment, revision known): learned from tick, channel (revealed, taught, observed, read), stance (adopted, rejected, misunderstood, selective), until forgotten, replaced by a newer revision, or death | **H** | S | open spells = the sim's known-commandment lists (the brief's "known commandments per person"); `misunderstood` stance is truth (Full only) | 0014 / D6 / 1 |
| `fact_commandment_choice` | one choice that matched a commandment's action class: person, commandment, followed or violated or unaware, the alternative chosen, `control_code` | event | HV | followed + violated + unaware = matched choices counted by the sim's per-day counters (exact at Full, sample at Summary) | 0014 / D6 / 2 |
| `dim_religion` | one religion: `religion_id`, founding god or gods, founding tick, linked `org_key` (religious institutions use the same organisation tables, D5.19) | type 1 | S | = `ReligionFounded` | 0014 / D6 / 2 |
| `fact_religious_affiliation` | one spell per (person, god or religion): kind (follower, convert, lapsed, secret), source (born into, taught, miracle witnessed, conversion), until changed. Several open spells per person are legal (worshipping more than one god). Never touches `fact_citizenship` (D5.16) | **H** | S | open spells = the sim's affiliation lists; a change closes and opens spells without any `fact_citizenship` row | 0014 / D6 / 2 |
| `fact_belief_change` | one change of one belief dimension: belief that the god exists, faith, trust, fear, loyalty (each its own column or row; there is no obedience score), before, after, cause kind and ref | event | S above a threshold, HV for small drifts | each `BeliefChanged` event; snapshot `fact_belief_sample` agrees on sampled persons | 0014 / D6 / 2 |
| `fact_prayer` | one prayer: person, god, request kind (food, healing, protection, forgiveness, other), the need it names, tick; Full only: whether an intervention followed (`prayer_answered_by_intervention`) | event | HV | `PrayerMade` events; daily aggregate exact | 0014 / D6 / 2 |
| `fact_worship` | one worship act or gathering: persons present (aggregate), site or organisation, god, act kind (rite, sacrifice, communal meal), goods given (links to `fact_transaction`) | event plus `fact_worship_day` aggregate | HV | `WorshipHeld`; goods given appear once in the ledger of goods | 0014 / D6 / 3 |

**E. Organisations, ownership, work, services**

| Table | Grain and persistent keys | Time semantics | Level | Reconciles to | Mig / Pkg / Tier |
|---|---|---|---|---|---|
| `dim_org` | one organisation of any kind (government, enterprise, cooperative, household business, school, clinic, temple, militia, bank, court, communal store): `org_id`, kind, founding event, how it arose (scenario seed, emerged from NPC action, never "created by the player", D5.18–D5.19), ownership model (private, shared, cooperative, communal, state, religious), jurisdiction | type 1; it extends today's `dim_institution` and `dim_enterprise` rather than adding a third table (`org_kind` separates them; the old names stay as views) | S | = `OrgFounded`; `dissolved` = `OrgDissolved` | 0018 / C6 / 2 |
| `dim_org_history` | head, charter or rules, funding source, ownership model, status | type 2, **H** | S | contiguous; head equals the sim's current head | 0018 / C6 / 2 |
| `fact_ownership_spell` | one spell per (asset, owner): asset kind (item lot, building, productive asset, land or site right, share), owner party, share in permille, basis (acquired, built, inherited, gift, theft, divine grant, confiscated) | **H**, written by the writer from `OwnershipChanged` (lots keep their event rows in `fact_ownership`, which is the event stream) | S | every asset has spells summing to 1000‰ at every tick (ownership integrity, brief §46); open spells = current owners in state (extends test 10.4) | 0015 / C1 / 1 |
| `fact_use_right` | one spell per (asset, user): lease, loan, tenancy, licence, communal access, granted by whom | **H** | S | `UseRightGranted` and `UseRightEnded` | 0015 / C1 / 2 |
| `fact_economic_relationship` | one spell per (party, party, kind): supplier, customer, tenant, partner, co-op member, patron; exposure as trade volume to date; the "production and economic relationships" of brief §34 | **H** | S | derived from events (`EnterpriseFounded`, `JobTaken`, use-right and exchange events); spells never overlap for the same (a, b, kind) | 0015 / C1, C3 / 2 |
| `fact_org_membership` | one spell per (person, org, role): member, officer, head, trustee, priest, soldier, patient-registered, student-enrolled | **H** | S | open spells = the sim's rolls; the head in `dim_org_history` has an open spell | 0018 / C6 / 2 |
| `fact_employment` | one spell per contract (the M13 core plan): employer party, worker, role, pay terms (ware, amount, interval), end reason | **H** | S | `JobTaken` opens, `JobAbandoned` or end closes; `WagePaid` rows reconcile to `fact_transaction` lines | 0016 / C3 / 2 |
| `fact_org_expenditure` | one spending act of an organisation: org, category (staff pay, supplies, capital, transfer, loan, service delivery), payee party, wares and quantity, `request_id` if a service caused it | event plus `fact_org_day` aggregate | S | equals the org's store and treasury movements in `fact_transaction` and `fact_production` (consume side); Σ per org per day equals the daily snapshot delta | 0018 / C6 / 2 |
| `fact_org_day` | one org × day: staff, store stock per ware, budget, requests open and served, queue, records held | day-end snapshot (this is the existing `fact_institution_day`, extended; the old name stays a view) | D | stock equals state exactly; staff equals open employment and membership spells | 0018 / C6 / 2 |
| `fact_service_request` | one request: `request_id`, requester (person or org), service kind (healthcare, education, justice, defence, relief, credit, worship, other), the need (injury id, illness id, skill sought, grievance id), channel, tick, status (open, accepted, served, refused, expired, withdrawn, requester died), close tick, reason | accumulating | HV (Tier 1 kinds healthcare and relief are S) | `ServiceRequested` opens, a close event closes; every open row at run end is listed; `unmet` KPIs read it | 0018 / C6, C7 / 2 |
| `fact_service_delivery` | one delivery: `request_id`, provider person, org, start and end tick, labour ticks, wares used (line rows or a join to `fact_production` consume side), `control_code` of the provider | event | HV | provider labour ticks equal `fact_person_day.ticks_*` for the activity; wares used equal consume rows | 0018 / C6, C7 / 2 |
| `fact_service_outcome` | one outcome per delivery: measured change (health before and after, skill before and after, case closed), the institution's recorded outcome and, at Full, the true outcome (`record_true_value`) | event | S for healthcare and education, HV otherwise | health outcome equals the delta in person-day rows; recorded vs true gap feeds the information KPIs | 0018 / C6, C7 / 2 |
| `fact_health_encounter` | one treatment episode (the planned table): patient, healer, org, injury or illness ref, treatment, start and end, health before and after, request link | accumulating | S | `fact_service_*` rows of kind healthcare join 1:1; patient's `health` before and after equal person-day rows | 0019 / C7 / 2 |
| `fact_education_enrolment` | one spell per (learner, school or teacher, subject), until completed, dropped out or taught elsewhere | **H** | S | `fact_org_membership` role student-enrolled agrees | 0019 / C7 / 2 |
| `fact_education_assessment` | one assessment of a skill or trait: learner, subject, level before and after (true, from the mind), the level the school recorded | event | S | skill after − before equals `fact_education_activity.gain` summed over the window (exact for teaching, see 12.6 limits) | 0019 / C7 / 2 |

**F. Finance, justice, military**

| Table | Grain and persistent keys | Time semantics | Level | Reconciles to | Mig / Pkg / Tier |
|---|---|---|---|---|---|
| `fact_loan` | one loan: `loan_id`, lender and borrower parties, principal in wares (and later currency), due terms, collateral, status (open, repaid, overdue, restructured, written off, defaulted) | accumulating | S | `LoanMade`, `LoanRepaid`, `LoanDefaulted`; `lent − repaid − written off = outstanding` exactly | 0017 / C2, C8 / 2 |
| `fact_loan_repayment` | one repayment: loan, amount, tick, on time or late, source | event | S | each is also a `fact_transaction` line; the physical goods moved (scenario 7) | 0017 / C2 / 2 |
| `fact_loan_default` | one default, restructure, write-off or collateral seizure: loan, kind, amount, cause code, consequence (trust change, ownership transfer) | event | S | links to `fact_social_interaction` (betrayal, trust) and `fact_ownership` | 0017 / C2, C8 / 3 |
| `fact_crime`, `fact_crime_report` | as planned in 1.1, extended with `control_code` (a compelled act is recorded as such) | event | S | `CrimeCommitted`, `CrimeObserved` | 0021 / C9 / 3 |
| `fact_investigation` | one investigation: case, investigator, org, start, evidence items (including evidence revealed by a miracle, tagged divine), end, finding | accumulating | S | `InvestigationOpened`, `InvestigationClosed` | 0021 / C9 / 3 |
| `fact_court_decision` | one decision of a forum: case, forum org, decider, verdict, sanction, appeal link, grounds code (evidence, law, commandment adopted as law, custom, instruction refused) | event | S | extends `fact_court_case`; one row per `CaseDecided`; religious rules and legal rules stay distinct by grounds code | 0021 / C9 / 3 |
| `fact_detention` | one custody spell: person, org, reason, release reason | **H** | S | open spells = persons held in state; capacity respected | 0021 / C9 / 3 |
| `fact_military_service` | one spell of service per person and unit; `fact_service_day` as planned | **H** | S | `Enlisted`, `Discharged`, `Deserted`; extends the planned M9 table | 0022 / C4 / 3 |

### 12.4 Migration numbering by package (proposal, to be confirmed by the contracts document)
The writer applies the numbers that exist, in order, and a migration may reference only tables in lower-numbered files. Blocks therefore follow dependency order; a package that merges earlier or later than planned takes the next free number at merge and keeps its file name suffix, so the order of numbers is the order of merge (a merged file is never edited, as today). Unity branches `unity/demog` (adds no migration; its `demog report` reads the sim and the recorder) and `unity/ibs` (changes `PopulationMapping.cs` only) merge first and need no number.

| Migration | Package | Contents |
|---|---|---|
| `0007_divine_core.sql` | F1, D1 | `dim_god`, `dim_god_history`, `fact_god_faction`, `fact_resource_ledger`, new `dim_code` sets (`control_source`, `cause_kind`, mythic category), `control_code` columns, `intervention_id` columns |
| `0008_mythic.sql` | D1 | `fact_mythic_balance_day`, `fact_mythic_ledger`, `fact_divine_action`, `fact_divine_observation` |
| `0009_commands.sql` | D2 | `fact_divine_command`, `_target`, `fact_command_perception`, `fact_command_response`, `fact_compulsion` |
| `0010_possession.sql` | D4a | `fact_possession` |
| `0011_avatar.sql` | D4b (absorbs D5) | `fact_avatar_manifestation` |
| `0012_miracles.sql` | D3 | `fact_miracle`, `fact_miracle_effect` |
| `0013_resurrection.sql` | D7 | `fact_resurrection`, `fact_life_spell`, `fact_npc_archive`, `life_no` on `fact_death` |
| `0014_commandments_religion.sql` | D6 | commandment, religion, affiliation, belief, prayer and worship tables |
| `0015_ownership_work.sql` | C1, C3 | `fact_ownership_spell`, `fact_use_right`, `fact_economic_relationship` |
| `0016_employment.sql` | C3 | `dim_enterprise` (as `dim_org` view), `fact_employment`, `fact_job_day`, `fact_org_day` columns |
| `0017_lending.sql` | C2 (C8 banking columns deferred) | `fact_loan`, `fact_loan_repayment`, `fact_loan_default`, `fact_promise` columns |
| `0018_organisations.sql` | C6 | `dim_org`, `dim_org_history`, `fact_org_membership`, `fact_org_expenditure`, `fact_org_day`, `fact_service_request`, `_delivery`, `_outcome` |
| `0019_health_education.sql` | C7 | `fact_health_encounter`, `fact_education_enrolment`, `fact_education_assessment` |
| `0021_justice.sql` | C9 (deferred, Tier 3) | `fact_investigation`, `fact_court_decision`, `fact_detention`, crime columns |
| `0022_military.sql` | C4 (deferred, Tier 3) | `fact_military_service`, `fact_service_day` |
| `0023_citizenship_politics.sql` | C10 (absorbs C5's minimal citizenship) | citizenship spells and migration extensions (affiliation stays separate from citizenship) |
| `0024_culture_religion_orgs.sql` | C10 | religious-institution columns on `dim_org`, trait and tradition links |
| `0030_kpi_views.sql` | Q1 | `dim_kpi` rows for the new KPI IDs, the reference-SQL views `v_kpi_*`, the stable analysis views (`v_person_life`, `v_god_day`, `v_intervention_chain`, `v_org_history`), `ANALYZE` hints |

`0020` is left free for a package found late. If the contracts document changes the package split, the table list stays and only the file grouping moves.

### 12.5 Events to add to the catalogue (3.7); codes come from the contracts document
| Family | Events | Tables |
|---|---|---|
| Divine | `GodCreated`, `GodAssociated`, `GodPolicyChanged`, `MythicConfigChanged`, `MythicCredited`, `MythicSpent`, `MythicUpkeepCharged`, `DivineActionAccepted`, `DivineObservationAnswered` | `dim_god*`, `fact_god_faction`, `fact_mythic_*`, `fact_divine_action`, `fact_divine_observation` |
| Commands | `DivineSuggestionIssued`, `DivineCommandIssued`, `CommandFanOut`, `CommandPerceived`, `CommandResponded`, `CompulsionStarted`, `CompulsionEnded` | `fact_divine_command*`, `fact_command_perception`, `fact_command_response`, `fact_compulsion` |
| Control | `PossessionStarted`, `PossessionEnded`, `AvatarManifested`, `AvatarDismissed`, `AvatarDestroyed` | `fact_possession`, `fact_avatar_manifestation` |
| Miracles | `MiracleCast`, `MiracleEffectApplied`, `ResourceCreated`, `ResourceDestroyed` | `fact_miracle*`, `fact_resource_ledger` (and `Produced` or `Consumed` with a divine reason, so C1 holds) |
| Life | `NpcResurrected`, `NpcArchived`, `NpcRestoredFromArchive` | `fact_resurrection`, `fact_life_spell`, `fact_npc_archive`, `fact_death` |
| Belief | `CommandmentIssued`, `CommandmentRevised`, `CommandmentRevoked`, `CommandmentLearned`, `CommandmentStanceChanged`, `CommandmentForgotten`, `CommandmentChoiceMade`, `ReligionFounded`, `AffiliationChanged`, `BeliefChanged`, `PrayerMade`, `WorshipHeld` | the D table group |
| Organisations | `OrgFounded`, `OrgDissolved`, `OrgHeadChanged`, `MemberJoined`, `MemberLeft`, `OrgSpent`, `ServiceRequested`, `ServiceAccepted`, `ServiceDelivered`, `ServiceOutcomeRecorded`, `ServiceRefused` | E group; existing `Institution*` events (500–599) map to the same tables |
| Finance and justice | `LoanDefaulted` (exists), `LoanWrittenOff`, `CollateralSeized`, `InvestigationOpened`, `InvestigationClosed`, `CaseDecided`, `Detained`, `Released` | F group |

### 12.6 Worked queries (SQLite, planned views)
```sql
-- What happened after each divine command: perception, response, and the work done (scenario 8)
SELECT c.command_id, c.kind_code, p.person_key, r.response_code, r.delay_ticks
FROM fact_divine_command c
JOIN fact_divine_command_target t ON t.command_id = c.command_id
LEFT JOIN fact_command_perception p ON p.command_id = c.command_id AND p.person_key = t.person_key
LEFT JOIN fact_command_response  r ON r.command_id = c.command_id AND r.person_key = t.person_key;

-- Mythic spent by category per sim year, nominal against charged
SELECT d.year, l.category_code, SUM(l.nominal_cost) nominal, SUM(l.charged_cost) charged
FROM fact_mythic_ledger l JOIN dim_date d USING (date_key) GROUP BY 1, 2;

-- Who owned the dead person's goods after a resurrection (scenario 14): spells overlapping the life gap
SELECT o.* FROM fact_ownership_spell o JOIN fact_resurrection r ON o.valid_from_tick < r.tick
WHERE (o.valid_to_tick IS NULL OR o.valid_to_tick > r.tick) AND o.owner_person_key <> r.person_key;
```
Limits: sampled rows (HV) carry `sample_weight`; analyses must weight them. Truth columns are NULL below Full. Read-only SQL access by a player is described in [INFORMATION_AND_MEDIA_DESIGN.md §12](INFORMATION_AND_MEDIA_DESIGN.md#12-owner-d5-civilisation-and-divinity-26-nov-2026).

### 12.7 Tier and package mapping

Package ids follow the final [package index](../civdiv/DEVELOPMENT_PACKAGE_INDEX.md) (review 27 Nov 2026): D5 is absorbed into D4 (D4a possession, D4b avatar); U4 into U3 (U3a world, U3b characters and effects); Q2 into Q1; C8's Tier 2 scope into C6 and C5's minimal citizenship into C10; C4, C9, C8 banking and C5 beyond citizenship are deferred to Tier 3; U6 is deferred apart from the scenario files, which belong to Q3.
| Group | Tables | Package (brief §39) | Tier | Warehouse package |
|---|---|---|---|---|
| Divine identity, Mythic, action ledger | `dim_god*`, `fact_mythic_*`, `fact_divine_action`, `fact_resource_ledger` | F1, D1 | 1 | Q1 |
| Suggestions, commands, compulsion | `fact_divine_command*`, `fact_command_*`, `fact_compulsion` | D2 | 1 | Q1 |
| Miracles | `fact_miracle*` | D3 | 1 | Q1 |
| Commandments and known commandments | `dim_commandment*`, `fact_commandment_known` | D6 | 1 | Q1 |
| Physical ownership and labour | `fact_ownership_spell`, `fact_use_right`, `fact_economic_relationship` | C1 | 1 | Q1 |
| Possession, avatar, resurrection | `fact_possession`, `fact_avatar_manifestation`, `fact_resurrection`, `fact_life_spell`, `fact_npc_archive` | D4 (D4a, D4b), D7 | 2 | Q1 |
| Religion, belief, prayer | `dim_religion`, `fact_religious_affiliation`, `fact_belief_change`, `fact_prayer`, `fact_commandment_choice` | D6 | 2 | Q1 |
| Employment, enterprises | `fact_employment`, `fact_job_day` | C3 | 2 | Q1 |
| Organisations and services | `dim_org*`, `fact_org_*`, `fact_service_*` | C6 | 2 | Q1 |
| Healthcare, education | `fact_health_encounter`, `fact_education_*` | C7 | 2 | Q1 |
| Lending | `fact_loan*` | C2 (C8 banking deferred) | 2 (default detail 3) | Q1 |
| Justice, detention, military | `fact_investigation`, `fact_court_decision`, `fact_detention`, `fact_military_service` | C9, C4 (deferred) | 3 | Q1 |
| Worship, divine observation answers | `fact_worship*`, `fact_divine_observation` | D6, U5 | 3 | Q1 |
| Views, KPI reference SQL, reconciliation suite | `0030_kpi_views.sql`, W-D5 tests | Q1, Q3 | 1 | Q1, Q3 |

Tier 1 means the table is written by the first demonstrable build (checkpoint B, divine sandbox, brief §44). Tier 2 and 3 tables keep their schema and mappers planned so the sim packages can emit their events from the start; a table with no source package merged yet is simply empty.

### 12.8 Acceptance hooks
New tests, numbered like section 10 and run by Q1 with Q3. They add to, and do not weaken, 10.1–10.16.

| # | Test | What must hold |
|---|---|---|
| W-D5-1 | `Reconciliation_AnalyticalEqualsSim` (scenario 17) | On a Full run of the integrated scenario (divine commands, a compulsion, a possession, a miracle, a resurrection, an institution delivering a service, a loan): every identity in the "Reconciles to" column holds exactly: Mythic balance equation per god-day; ledger Σ = balance delta; goods conservation C1 with the resource ledger included; row counts of each new event family = the sim's own counters; open spells = state (affiliations, known commandments, memberships, employment, ownership, detentions); population identity births + immigration + resurrections − deaths − emigration = Δ alive |
| W-D5-2 | `Replay_ReRecord_IncludesDivine` (scenario 18) | A complete scenario with ordinary NPC actions and divine inputs: live Full record, Off run then `replay --record full`, and a save-and-continue run give the same warehouse content (10.8 and 10.14 methods), and the same final hash. The replay file holds every divine input (see [HEADLESS_SIMULATION.md §12](HEADLESS_SIMULATION.md#12-owner-d5-civilisation-and-divinity-26-nov-2026)) |
| W-D5-3 | `DivineRecording_DoesNotChangeHash` | Off, Metrics, Experiment, Summary, Full, each new domain alone at Full, `sampleEvery` 1, 16, 64: identical hashes (extends 10.5) |
| W-D5-4 | `NewTruthFields_OnlyAtFull` | the truth columns of 12.1 are NULL below Full and filled at Full (extends 10.15); no policy, manager or AI god type references them (reflection) |
| W-D5-5 | `Resurrection_KeepsHistory` | after a resurrection, `dim_person` has one row for the person, `fact_death` has both deaths, previously closed spells stay closed, estates stay with their owners, `fact_life_spell` has two contiguous lives |
| W-D5-6 | `Spells_Half_Open_Divine` | extends 10.7 to compulsion, possession, avatar, affiliation, known commandment, ownership, membership, employment, detention and loan spells: no overlap where the rule forbids it, `valid_to ≥ valid_from`, day columns agree with ticks |
| W-D5-7 | `Control_Source_Marked` | every person-action row produced during a compulsion, possession or avatar spell has the matching `control_code`; none outside one |
| W-D5-8 | `Migration_0007_To_Current` | fixtures at v6 and at each new version migrate forward and the example queries of 12.6 return rows of the right shape |
| W-D5-9 | `Size_Budget_Civilisation` | a 20-sim-year Summary run of the integrated 500-person scenario stays within 1.3 × the 10.9 budget (still ≤ 1 GB for 200 years); the biggest new tables are the HV ones, which are sampled |
| W-D5-10 | `Warehouse_NeverFeedsSim` | the existing `SimAndRecordingAssemblies_DoNotReferenceSqlite` test, plus a reflection test that no type implementing `IFactionPolicy`, `IInstitutionPolicy`, `INpcPolicy` or `IGodPolicy` references `God.Runner.Warehouse` or `Microsoft.Data.Sqlite` |

---

## 13. Owner D6: industry, energy, money, claims and knowledge (27 Nov 2026)
**Status: plan only. Nothing in this section is built.** Source: [OWNER_BRIEF_2026-11-27_INDUSTRY.md](../civdiv/OWNER_BRIEF_2026-11-27_INDUSTRY.md) "additional owner decisions" §§1, 5–9, 12, decisions **D6.2, D6.9–D6.13, D6.16, D6.18**. The nine ontological categories and their one-line reconciliation rules are in [WORLD_ONTOLOGY.md §11](WORLD_ONTOLOGY.md); this section lists the tables that carry them, their grains, levels and exact reconciliation, and the migration numbers by package. Industrial table names are those proposed by their owning documents ([INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md](INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md) §7, [VEHICLES_AND_HARVESTING_DESIGN.md](VEHICLES_AND_HARVESTING_DESIGN.md) §7, [ELECTRICITY_DESIGN.md](ELECTRICITY_DESIGN.md) §7, [INDUSTRIAL_MILITARY_DESIGN.md](INDUSTRIAL_MILITARY_DESIGN.md), [WASTE_AND_CIRCULAR_DESIGN.md](WASTE_AND_CIRCULAR_DESIGN.md)); they are gathered here so the warehouse has one table of contents. Money, claims, knowledge, artefacts, rights and auctions are owned here with [MONEY_AND_ACCOUNTING_DESIGN.md](MONEY_AND_ACCOUNTING_DESIGN.md) and [KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md](KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md). Package ids: **I1–I6** (industrial, provisional), **K1** (knowledge, designs, artefacts, rights), **K2** (money and accounting; K2a Tier 2, K2b Tier 3).

### 13.1 Rules (in addition to 12.1)
- **Units, never prices.** Money and claim columns carry `unit_kind` (ware or money) and `unit_id` with an integer `amount`. No column stores a price the sim did not record. Reference values (`dim_ware.ref_value`, FE) are joined only in views, and every view that uses them names it (`*_fe`).
- **Energy is its own unit** (`eu`), in its own tables. No energy column appears in a money table; the only path between them is `fact_meter_bill` → obligation → payment.
- **Truth fields (Full only, †)** added by D6: `fact_artefact_copy.authorised`, `fact_infringement` (whole table), `fact_knowledge_transfer.trespass`, `fact_court_decision.true_infringement`, `fact_obligation.records_differ` (the org's record differs from the registry), `fact_payment.recorded_by_payer`/`_payee` (whether each side's recorder wrote it). No policy, manager or AI god reads them (W-D5-4 extended).
- **Information layers.** Org book rows are truth (`info_layer` 1); the organisation's own financial records are `fact_record` rows (layer 4). Reports show both, never mixed (D5.28).
- **Causality.** Every money, claim, knowledge and rights row carries `cause_kind_code`, `cause_ref` and `intervention_id` (12.1). New `dim_code 'cause_kind'` values: payment, billing act, assessment, ruling, licence grant, auction close, supply-agreement unload, research, teaching, reading, revelation.

### 13.2 Recording levels
Marks as 12.2. New `TelemetryDomain` names (numbers by the SAC Industry addendum): `industry`, `vehicle`, `energy`, `waste`, `environment`, `industrial_military` (I-packages, codes 1000–1099), `knowledge` 76, `rights` 77, `auction` 78, `money` 79 (K1, K2, codes 1100–1199). `record.domains` switches each alone.

### 13.3 Planned tables

**G. Money and claims (K2; migration `0038_money_claims.sql`, Tier 2)**

| Table | Grain and persistent keys | Time | Level | Reconciles to | Pkg / Tier |
|---|---|---|---|---|---|
| `fact_payment` | one payment: `payment_id`, payer and payee parties, `unit_kind`, `unit_id`, amount agreed, amount paid, purpose, `obligation_id`, `deal_id`, ref (agreement, right, auction, request, contract), authoriser and receiver persons, node, status, opened and closed tick, `control_code` | accumulating | S | one row per `PaymentMade`/`PaymentFailed`/`PaymentAbandoned`; `amount_paid` = Σ `fact_payment_lot.count` | K2a / 2 |
| `fact_payment_lot` | one lot movement of a commodity payment: `payment_id`, `lot_id`, ware, count, from and to holder, step | event | S (HV above 10 per payment) | = `ItemMoved` rows whose `PurposeRef` is the payment (MON-I2) | K2a / 2 |
| `fact_journal_line` | one line of a double-entry posting for any party (orgs from `OrgBook` postings; persons and households derived): party, account class, unit, purpose, debit, credit, basis (cash, accrual), source (payment, obligation event) | event | S (persons: Summary aggregates by day, Full per line) | per party per unit Σ debit = Σ credit (BK-I1); per org equals `fact_org_book_day` deltas | K2a / 2 |
| `fact_org_book_day` | one org × day × account class × unit × purpose: debit, credit, balance | day-end snapshot | D | equals the sim's `OrgBook` (exact) on the last day; Receivable and Payable equal open `fact_obligation` rows (MON-I3) | K2a / 2 |
| `fact_unit_of_account_spell` | one spell per (org, unit of account) and per accepted unit | **H** | S | `UnitOfAccountChanged` | K2a / 2 |
| `fact_obligation` | one formal obligation of any source (wage, loan, tax, fine, fee, bill, licence fee, royalty, auction price, supply, damages, deposit): `obligation_id`, debtor, creditor, unit, principal, settled, written off, due day, source, ref, status | accumulating (opened to closed) | S | open rows = the C2 registry; `principal − settled − written_off = outstanding`; `fact_loan` (C2, 0017) joins 1:1 for loans | K2a with C2 / 2 |
| `fact_obligation_event` | one change: opened, adjusted, settled (part or whole, with `payment_id` or delivery `lot_id`), defaulted, written off, forfeited, assigned | event | S | Σ settled per obligation = linked payments + deliveries (MON-I8) | K2a with C2 / 2 |
| `fact_claim_balance_day` | one unit × source × day: claims open, liabilities open, overdue | day-end | D | claims = liabilities exactly (MON-I3) | K2a / 2 |

**H. Knowledge, designs and artefacts (K1; `0039_knowledge_designs.sql`, Tier 2)**

| Table | Grain and keys | Time | Level | Reconciles to | Pkg / Tier |
|---|---|---|---|---|---|
| `dim_design` | one design envelope: `design_id`, key, kind, payload table and row, prerequisites, complexity, knowledge and skill minima; plus `first_known_tick`, `how`, `formalised_tick`, `lost_tick` (type 1 with accumulating columns) | type 1 | S | rows = content table; first-known = first `DesignDiscovered` or seed | K1 / 2 |
| `fact_knowledge_spell` | one spell per (person, design, level band: heard of, understands, can build, can teach) | **H** | S | open spells at run end = `KnownDesign` entries (Full snapshot `fact_knowledge_snapshot`); no spell of a source closes or drops a band in the tick of a transfer it gave (KN-I1) | K1 / 2 |
| `fact_knowledge_transfer` | one acquisition: learner, design, channel (seeded, practice, research, taught, observed, read, revealed), source person or artefact lot, level and fidelity before and after, `intervention_id`, † `trespass` | event | S (observation gains HV) | one row per `KnowledgeAcquired`; revealed rows join `fact_miracle_effect` | K1 / 2 |
| `fact_design_discovery` | one first discovery of a design in the world, and later rediscoveries after loss | event | S | `DesignDiscovered`, `DesignLost` | K1 / 2 (3 for research) |
| `dim_artefact` | one document: `lot_id`, design, fidelity, origin (written, copied, seeded), maker, source lot, made and closed tick | type 1 accumulating | S | live rows = `Document` holdings in C1 (AR-I1); closed = `Consumed` of the lot | K1 / 2 |
| `fact_artefact_copy` | one write or copy act: maker, source lot, new lot, design, fidelity, materials, ticks, † `authorised` | event | S | `DesignFormalised`, `ArtefactCopied`; materials = `ConsumedBy(ProcessInput)` rows with this ref | K1 / 2 |
| `fact_skill_spell` | one spell per (person, skill class, band) if K1 lands the skill slots (else C7's table) | **H** | S | `SkillChanged` | K1 or C7 / 2 |
| `fact_research_project`, `fact_research_day` | project (sponsor, target, site, team, status); project × day (ticks, materials, progress) | accumulating; D | S | `ResearchProject*`, `ResearchProgress`; materials = `ConsumedBy(Research)` | K1 / 3 |

**I. Rights and licences (K1; `0040_rights_licences.sql`, Tier 2)**

| Table | Grain and keys | Time | Level | Reconciles to | Pkg / Tier |
|---|---|---|---|---|---|
| `dim_right` | one right: `right_id`, kind (patent, licence, open dedication, design title), design, jurisdiction org, office org, parent right, scope, exclusive, priority, granted, expires, deposit lot, terms, final status | type 1 accumulating | S | = `RightApplied` … `RightExpired` | K1 / 2 |
| `fact_right_holder_spell` | one spell per (right, holder) | **H** | S | exactly one open spell per live right (RT-I1); `RightAssigned` closes and opens in one tick; **no `fact_ownership_spell`, `fact_knowledge_spell` or asset row changes in that tick because of it** (D6 test 6) | K1 / 2 |
| `fact_licence` | one licence spell: licensor, licensee, parent, scope, exclusive, term, fee and royalty terms, fee `obligation_id` | **H** | S | within its parent's scope and term (RT-I2); exclusive spells never overlap (RT-I3) | K1 / 2 |
| `fact_right_event` | one lifecycle event: applied, examined, granted, refused, assigned, licensed, renewed, lapsed, expired, challenged, revoked, surrendered, with the acting persons | event | S | one row per rights event | K1 / 2 |
| `fact_patent_law_spell` | one spell per (government org, patent law version) | **H** | S | `LawAdopted`/`LawRepealed` (C10) of kind PatentLaw; no granted right outside a spell (RT-I4) | K1 with C10 / 2 |
| `fact_infringement` † | one truth classification: user party, design, right, asset or production cycle, jurisdiction, tick; then (joined) perceived by whom, complaint id, case id, finding | event | F (truth) | = `UnauthorisedDesignUse`; perceived rows = `InfringementPerceived`; complaints = C9 `ComplaintMade` of kind infringement | K1 / 2 (case columns 3) |

**J. Industrial tables (owned by the industrial documents; listed for one table of contents)**

| Migration (owner) | Tables | Reconciles to (exact) |
|---|---|---|
| `0031_industry_production.sql` (I1) | `dim_deposit`, `fact_deposit_knowledge`, `fact_extraction`, `dim_facility`, `fact_production_order`, `fact_production_cycle` (with `licensed` flag and `design_id`), `fact_facility_day`, `fact_embodied_matter`; `fact_goods_ledger_day` extended to the 16 reasons | initial − Σ extracted = remaining per deposit; Σ extracted = `ProducedBy(Extracted)`; cycle inputs and outputs = `ProcessInput`/`Processed` per ware; Σ embodied = `ConsumedBy(Embodied)` net of releases |
| `0032_vehicles.sql` (I2) | `dim_vehicle`, `fact_vehicle_ownership_spell` (H), `fact_vehicle_crew_spell` (H), `fact_vehicle_trip`, `fact_vehicle_day` (D), `fact_vehicle_event` | Σ unloaded = `ItemMoved(BinTransfer)` into destination bins; Σ fuel = `ConsumedBy(Combusted)` of tanks; every move row has a living driver |
| `0033_industrial_trade.sql` (I3) | `fact_supply_agreement`, `fact_supply_delivery`, `fact_industrial_sale` | every delivery = one `OwnershipChanged` basis `SupplyAgreement` and one `fact_obligation` (source supply); Σ sales per seller = Σ `fact_payment` received for them, never more |
| `0034_energy.sql` (I4) | `dim_station`, `dim_power_link`, `dim_grid_port`, `fact_grid_component_spell` (H), `fact_grid_interval`, `fact_station_interval`, `fact_power_delivery_day`, `fact_outage` (H), `fact_meter_bill`, `fact_energy_ledger_day` (D) | generated = delivered + curtailed + loss per component and interval (ELE-I1); station fuel = `ConsumedBy(Combusted)`; each bill ↔ one `fact_obligation` (source bill); paid = linked `fact_payment` |
| `0035_military_industry.sql` (I5) | `fact_procurement`, `fact_military_asset_spell` (H), `fact_ammunition_day`, `fact_strategic_event`, `fact_strategic_effect` | procurement deliveries = `OwnershipChanged` to military orgs; ammunition = `ConsumedBy(Expended)`; procurement payments = `fact_payment` purpose sale |
| `0036_waste_environment.sql` (I1, waste part) | `fact_waste_flow_day`, `fact_waste_site_day`, `fact_salvage`, `fact_pollution_day`, `fact_contamination_day`, `fact_environment_harm_day`, `fact_inspection` | flows = ledger by reason (WST-I1); site stocks = bin contents; pollution recomputes exactly (WST-I3); salvage ≤ embodied |
| `0037_industry_kpi_views.sql` (I6) | `v_kpi_ind_*`, `v_kpi_veh_*`, `v_kpi_ele_*`, `v_kpi_wst_*`, `v_kpi_imil_*` | K2 parity (KPI §16) |

**K. Monetary institutions and auctions (Tier 3)**

| Table | Grain and keys | Time | Level | Reconciles to | Pkg / Tier |
|---|---|---|---|---|---|
| `dim_money` | one institutional money: `money_id`, issuer org, form, redeemable ware and rate, reserve rule, founded, status | type 1 | S | = `MoneyFounded` | K2b / 3 (`0042_monetary_institutions.sql`) |
| `fact_money_issuance` | one issuance or destruction: money, mechanism (E1–E4, D1–D5), amount, account, counter-entry (loan obligation, reserve lot, expense purpose), officer | event | S | per money: Σ issued − Σ destroyed = Σ balances + notes outstanding (MON-I1) | K2b / 3 |
| `fact_account_spell`, `fact_account_balance_day` | account (holder, issuer, money) open to closed; account × day balance | **H**; D | S; D | balances = the issuer's `Deposit` obligations | K2b / 3 |
| `fact_auction` | one auction: auctioneer, seller, item kind and ref, format, unit, reserve, place, open and close, winner, price, `obligation_id`, status | accumulating | S | `AuctionAnnounced` … `AuctionSettled`/`Voided`; settled ⇒ one `fact_payment` purpose auction settlement and one transfer row (`fact_ownership_spell`, `fact_right_holder_spell`, `fact_licence` or asset title) **and no asset creation row** | K1 / 3 (`0043_auctions.sql`) |
| `fact_auction_bid` | one bid as the auctioneer perceived it | event | HV | `BidMade` | K1 / 3 |

### 13.4 Migration numbering by package (proposal for the SAC Industry addendum)
The D5 numbers (0007–0030) are untouched. A migration references only lower numbers; a merged file is never edited.

| Migration | Package | Tier | Contents |
|---|---|---|---|
| `0031_industry_production.sql` | I1 | 2 | deposits, extraction, facilities, orders, cycles, embodied matter, ledger reasons |
| `0032_vehicles.sql` | I2 | 2 | vehicles, crews, trips, vehicle days and events |
| `0033_industrial_trade.sql` | I3 | 2 | supply agreements, deliveries, industrial sales |
| `0034_energy.sql` | I4 | 2 | stations, links, ports, components, intervals, deliveries, outages, bills, energy ledger |
| `0035_military_industry.sql` | I5 | 2/3 | procurement, military assets, ammunition, strategic events |
| `0036_waste_environment.sql` | I1 (waste part) | 2/3 | waste flows and sites, salvage, pollution, contamination, inspections |
| `0037_industry_kpi_views.sql` | I6 (with Q1) | 2 | industrial KPI views |
| `0038_money_claims.sql` | **K2a** | 2 | `fact_payment`, `fact_payment_lot`, `fact_journal_line`, `fact_org_book_day`, `fact_unit_of_account_spell`, `fact_obligation`, `fact_obligation_event`, `fact_claim_balance_day`; `dim_code` sets `payment_purpose`, `obligation_source`, `account_class` |
| `0039_knowledge_designs.sql` | **K1** | 2 | `dim_design`, `fact_knowledge_spell`, `fact_knowledge_transfer`, `fact_design_discovery`, `dim_artefact`, `fact_artefact_copy`, `fact_skill_spell` (if K1 owns skills), research tables (empty until Tier 3) |
| `0040_rights_licences.sql` | **K1** | 2 | `dim_right`, `fact_right_holder_spell`, `fact_licence`, `fact_right_event`, `fact_patent_law_spell`, `fact_infringement` |
| `0041_knowledge_money_kpi_views.sql` | Q1 with K1, K2 | 2 | `v_kpi_knw_*`, `v_kpi_mon_*`, `v_ledger_nine_categories` (the 11.3 reconciliation of WORLD_ONTOLOGY as one view) |
| `0042_monetary_institutions.sql` | **K2b** | 3 | `dim_money`, `fact_money_issuance`, `fact_account_spell`, `fact_account_balance_day` |
| `0043_auctions.sql` | **K1** | 3 | `fact_auction`, `fact_auction_bid` |

`0044`–`0049` stay free for D6 packages found late (the lead allocates).

### 13.5 Events to add to the catalogue (3.7); codes by the SAC Industry addendum
| Family | Events | Tables |
|---|---|---|
| Money | `PaymentMade`, `PaymentStep`, `PaymentFailed`, `PaymentAbandoned`, `OrgBookDay`, `UnitOfAccountChanged`, `MediumOfExchangeObserved` | G |
| Claims (C2 with K2) | obligation opened, adjusted, settled, defaulted, written off, forfeited, assigned (C2 names; `LoanMade` etc. stay for loans) | G |
| Knowledge | `DesignDiscovered`, `KnowledgeAcquired`, `KnowledgeForgotten`, `DesignFormalised`, `ArtefactCopied`, `ArtefactClosed`, `DesignLost`, `SkillChanged`, `ResearchProjectStarted`, `ResearchProgress`, `ResearchProjectEnded` | H |
| Rights | `RightApplied`, `RightGranted`, `RightRefused`, `RightAssigned`, `LicenceGranted`, `LicenceEnded`, `RightRenewed`, `RightLapsed`, `RightExpired`, `RightChallenged`, `RightRevoked`, `RightSurrendered`, `UnauthorisedDesignUse` †, `InfringementPerceived`, `CompulsoryLicenceGranted`, `InjunctionOrdered` | I |
| Monetary institutions | `MoneyFounded`, `MoneyIssued`, `MoneyDestroyed`, `AccountOpened`, `AccountClosed`, `AccountTransferred`, `DepositAccepted`, `WithdrawalPaid`, `NotesIssued`, `NotesRedeemed` | K |
| Auctions | `AuctionAnnounced`, `BidMade`, `AuctionClosed`, `AuctionSettled`, `AuctionVoided` | K |
| Industry, vehicles, energy, military, waste | as named in the industrial documents (codes 1000–1099) | J |

### 13.6 Reconciliation rules (the nine categories; run by W-D6-1)
1. **Matter:** per ware and day, start holdings + Σ produced − Σ consumed (16 reasons) = end holdings; extraction = deposit depletion; embodied in = embodied held + released; waste flows = ledger by reason.
2. **Energy:** per component and interval, generated = delivered + curtailed + loss; station fuel burned matches generation by its rate; **no money or claim row is caused by an energy row** except through `fact_meter_bill`.
3. **Money:** commodity payments equal their lot movements; per institutional money, issued − destroyed = balances + notes; **no `fact_payment` or `fact_money_issuance` row has a cause of kind extraction, production cycle, delivery, unload or generation**.
4. **Claims:** per unit and day, claims = liabilities; outstanding = principal − settled − written off; settlement amounts = linked payments and deliveries.
5. **Knowledge:** open knowledge spells = minds' entries (Full snapshot); every opening has a transfer or discovery row; a transfer never lowers its source.
6. **Artefacts:** open `dim_artefact` rows = `Document` lots alive (C1 view); every new document has a write or copy row with materials consumed.
7. **Rights:** one open holder spell per live right; licences within parents; no right outside a patent-law spell; an assignment tick shows no ownership, knowledge or asset change caused by it.
8. **Productive assets:** every asset has a completed construction whose consumed materials equal its embodied matter; no asset row is caused by an auction, right, licence or knowledge event.
9. **Organisations:** per org, book revenue and expense by purpose = its payments; Receivable/Payable = its open obligations; its holdings in each category = the category tables filtered by owner `Org(id)`.

### 13.7 Acceptance hooks
| # | Test | What must hold |
|---|---|---|
| W-D6-1 | `Reconciliation_NineCategories` | On a Full run of the industrial slice scenario (D6 VS1–VS10) and the D6.1–D6.9 fixtures, every rule of 13.6 holds exactly; `v_ledger_nine_categories` returns no violation row |
| W-D6-2 | `Replay_ReRecord_Industry` | live Full record, Off run then `replay --record full`, and save-and-continue give identical warehouse content for every table of 13.3 and the same final hash (extends W-D5-2) |
| W-D6-3 | `OrgCashIdentity` | BK-I3: per org, commodity unit and day, Δ lots owned = payments in − out + non-payment flows of that ware |
| W-D6-4 | `NoMoneyFromIndustry` | joined over the run, no money or claim row has an industrial cause, and the money stock measures (MON-02) change only on payment, issuance or destruction rows |
| W-D6-5 | `Recording_Neutral_D6` | Off, Summary, Full and each new domain alone give one hash (extends P7 and W-D5-3) |
| W-D6-6 | `TruthFields_D6_OnlyAtFull` | the † columns of 13.1 are NULL below Full; reflection: no policy type references them (extends W-D5-4) |
| W-D6-7 | `Migration_0031_To_0043` | fixtures at v(0030) migrate forward; the 13.6 view runs on each intermediate version with the tables that exist |
