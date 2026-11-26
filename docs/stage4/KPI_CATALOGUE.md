# KPI catalogue (stage 4 and later)

Every measure the game can report, with its exact definition. How reports are produced, degraded and shown is in [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md). Table names come from the published list in [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md#11-the-published-table-list); the fields this catalogue needs are listed in [Tables assumed](#4-tables-assumed). Time, ages and milestones are in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md).

## 1. Purpose and scope

**Owns:** the definition of each KPI (id, numerator, denominator, period, eligibility, source, breakdowns, the actual-versus-reported pair, gaming risk, age), the dashboards, the balanced scorecards, the Stage 4 minimum set, and the integer formulas.

**Does not own:** the mechanisms behind a figure (see the system documents), the degradation model (INFORMATION_AND_MEDIA_DESIGN), or the table schema (DATA_WAREHOUSE_DESIGN).

## 2. Principles

- **Every KPI has two values.** The **actual** value is computed over true state from the warehouse (or the sim's truth, for the debug overlay and `FullInformation`). The **reported** value is computed from what a named institution recorded. Their difference is itself a measure of the information system, not an error to remove.
- **One formula, two inputs.** The pure integer functions in `Sim/Information/Kpi` take a `KpiInput` series. Actual feeds them truth rows, reported feeds them records. The warehouse SQL views replicate the formulas and a parity test compares them.
- **Nothing rewards a reported KPI.** No game mechanic pays out for a KPI. The KPI is a mirror the player and managers look into. When managers are rewarded by a target, the incentive is paid only on the **returns** they file (`ReturnFiled`, from the `FileReturn` command), which can be gamed (Goodhart); the catalogue lists how.
- **Reported means measured.** `KpiReported` is the faction's measured value. Under `FullInformation` it equals the actual value. In later ages it is built from institutional records and returns, so it can drift from the actual one ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)).
- **Measures are paired.** No headline KPI stands alone; see [Balanced scorecards](#balanced-scorecards-against-goodharts-law).
- **A KPI reports what is possible, not what is wanted.** Nothing here says a value is good. Direction notes are descriptive.
- **Never present an outcome as designed in.** For emergent outcomes (class, mobility, legitimacy) the KPI says how to *measure* whether it happened.
- **No composite wellbeing score (owner decision Q23, 27 Nov).** There is no universal composite score anywhere in the catalogue, the HUD or the reports. The underlying measures are reported separately (see [Wellbeing dimensions](#wellbeing-dimensions-reported-separately)). An experiment may use an explicitly defined composite only with its components, weights and limitations documented in its plan file ([POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)). There is no hidden political or moral weighting of which societies succeed.
- **Unpaid care is visible (owner decision Q12).** Time use, including unpaid care, is reported beside paid work ([5.16](#516-time-use-and-unpaid-care-tim)).
- **Neutral names.** KPI names and report labels use neutral, descriptive terms (owner decision Q2, [VISION_AND_PHILOSOPHY.md](VISION_AND_PHILOSOPHY.md#22-neutrality-binding-on-every-document)).

## 3. Conventions (read once; the tables use them)

### 3.1 Units and fixed point

| Unit | Meaning | Integer form |
|---|---|---|
| `‰` | permille, a share or rate | `i32`, 0–1000 (can exceed 1000 for ratios) |
| `‱` | per 10 000, for small rates | `i32` |
| `pm1000` | a value ×1000 (fixed point with 3 decimals) | `i32` or `i64` |
| `d` | days (sim days) | `i32` |
| `y` | sim years | `pm1000` (so 1.5 years = 1500) |
| `FE` | **food-equivalent**: value of a bundle at `WareRefValue`, owned by [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) and copied into `dim_ware.ref_value`. A measurement index only: NPCs never read it. Holdings are also stored per ware (`fact_person_day_ware`), so no value theory is built in. Currency replaces it as the reporting unit from the Town age. | `i64` |
| `n` | a count | `i32` |

Rounding is **half away from zero on the last digit**: `div_round(a, b) = (2·a + sign·b) / (2·b)`. The `FullInformation` equality test (reported = actual within rounding) means `|R − A| ≤ 1` unit in these integers.

### 3.2 Time

- `Y` = one sim year = `DaysPerYear` days (default 10, so 1 year = 10 days = 12 000 ticks at the defaults). The HUD shows human-scale ages as sim year × 16.
- Reporting periods used below: `D` day; `R5` rolling 5 days; `Y` one sim year (`DaysPerYear` days, period closes on day boundaries); `C` cohort (events followed for a fixed window); `E` event-driven (on change). Every period is a half-open interval `[start, end)` in ticks, aligned to day boundaries.
- A **report lag** (`PublishedTick − PeriodEnd`) is a property of the producing institution, not of the KPI.
- **Rates per year** use person-years: `PY = Σ person-days alive in the period / DaysPerYear`, in `pm1000`.
- Many KPIs are noisy at 10 days a year. Annual KPIs are therefore shown as a rolling mean over the last 3 years by default (a smoothed series), with the raw series one click away.

### 3.3 Breakdown codes

`G` geography (hex or region; `dim_location`) · `A` age band (`dim_code` set `age_band`; and sex as an optional split) · `O` occupation (`dim_occupation`) · `H` household type (`dim_code` set `household_type`) · `C` class position (`dim_class`, the measured relation to property and labour, see [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)) · `F` faction (`dim_faction`; citizenship or location, stated per KPI) · `I` institution (`dim_institution`: the staffed unit that provides or records).

A breakdown cell with fewer than `MinCell` rows (default 5) is reported as `Missing` in the view, to avoid reading noise as signal.

### 3.4 Distortion codes for the actual-versus-reported pair

Reported = Distort(Actual). Codes for how, per KPI:

| Code | Meaning |
|---|---|
| **C** | Coverage: only part of the population or events is seen |
| **D** | Delay: the report describes an earlier period |
| **N** | Noise: counting or sampling error (a survey interval) |
| **B** | Bias: systematic under- or over-detection (a detection rate) |
| **S** | Selection: only those who report, apply or are found are counted |
| **F** | Falsification: staff alter records (shade, omit, invent) |
| **Df** | Definition drift: the recorder's category differs from the catalogue's, or changes over time |

### 3.5 Eligibility words

- **Alive** means `alive = 1` on the day. **Citizen of F** means `faction_key = F`.
- **Working age** = life stage adult (year 1 to year 3 at LifeScale 100); **elder** = after year 3; **child** = before year 1. (Equivalent to shown ages 16–48 and above 48.)
- **Employed** = a job contract in force and at least one work shift in the last 3 days (`fact_person_day.employed = 1`). **Seeking** = no job, and an NPC decision to look in the last 3 days.
- **Resident** = in the faction's territory at the end of the day.

### 3.6 Source naming

`table.field` as in [Tables assumed](#4-tables-assumed). `fpd` = `fact_person_day`, `ftx` = `fact_transaction`, `ffd` = `fact_faction_day`, `fid` = `fact_institution_day`, `fhd` = `fact_hex_day`, `fk` = `fact_kpi`. Each event lands in the table that [DATA_WAREHOUSE_DESIGN.md §3.7](DATA_WAREHOUSE_DESIGN.md#37-event-catalogue-event-to-table-map) maps it to; events with no dedicated table are rows in `fact_event`.

## 4. Tables assumed

Table names follow DATA_WAREHOUSE_DESIGN.md's published list. The field lists are what this catalogue needs; the warehouse adds any missing column to its table. Columns written `*_fe` here are the warehouse's reference-value columns (`*_ref`, the FE unit). Dimensions have surrogate keys (`*_key`) separate from sim ids.

| Table | Grain | Fields used by this catalogue |
|---|---|---|
| `dim_date` | one day | `date_key`, `tick`, `day`, `year`, `season` |
| `dim_person` | one NPC | `person_key`, `npc_id`, `born_tick`, `sex`, `mother_key`, `father_key`, `genome_*` |
| `dim_faction` | one faction | `faction_key`, `faction_id`, `age` (Band…City) |
| `dim_location` (kind hex) | one hex | `location_key` (`hex_key` below), `region_key`, `terrain`, `carrying_capacity` |
| `dim_institution` | one institution | `institution_key`, `type`, `faction_key`, `provision_model` (public, private, cooperative, communal) |
| `dim_occupation`, `dim_class`, `dim_ware` (with `ref_value`), `dim_outlet`, and `dim_code` sets `age_band`, `household_type`, `death_cause` | lookup | `*_key`, `label` |
| `fact_person_day_class` | one adult per day | `person_key`, `date_key`, `class_key` (`fpd.class_key` below is this join) |
| `fact_person_day` | one NPC per day alive | `person_key`, `date_key`, `faction_key`, `hex_key`, `household_key`, `age_ticks`, `age_band_key`, `life_stage`, `alive`, `health`, `injury`, `nutrition`, `hunger_days` (consecutive days below the hunger threshold), `wealth_fe`, `income_fe`, `consumption_fe`, `occupation_key`, `class_key`, `employed`, `seeking`, `work_ticks`, `loyalty`, `skill_*`, `tech_known_mask`, `housed`, `in_custody`, `soldier`, `morale`, `trust_in_inst_*` (sampled beliefs) |
| `fact_household_day` | one household per day | `household_key`, `date_key`, `type_key`, `size`, `food_fe`, `wealth_fe`, `income_fe` |
| `fact_event` | one domain event with no dedicated table | `event_code`, `tick`, `ids...`, `payload` |
| `fact_birth` | one birth | `person_key`, `date_key`, `mother_key`, `father_key`, `faction_key`, `hex_key` |
| `fact_death` | one death | `person_key`, `date_key`, `age_ticks`, `cause_key`, `faction_key`, `treated` |
| `fact_migration` | one move between factions | `person_key`, `date_key`, `from_faction_key`, `to_faction_key`, `kind` (migrate, defect, captured, immigrate) |
| `fact_transaction` | one hand-over or transfer | `tick`, `kind` (barter step, gift, wage, tax, loan, repayment, theft, fine, purchase), `from_key`, `to_key`, `item_key`, `qty`, `value_fe`, `completed`, `due_tick` |
| `fact_health_encounter` | one treatment episode | `person_key`, `institution_key`, `request_tick`, `start_tick`, `end_tick`, `outcome`, `cost_fe` |
| `fact_education_activity` | one teaching or imitation episode | `teacher_key`, `learner_key`, `kind` (teach, observe, apprentice, imitate), `skill_or_trait`, `gain` |
| `fact_crime` | one offence (actual) | `crime_id`, `tick`, `offender_key`, `victim_key`, `kind` (theft, violence, fraud, other), `value_fe`, `hex_key`, `witnessed`, `observed_by_inst`, `reported_tick` |
| `fact_court_case` | one case before any forum (elders' mediation, later courts); investigations in `fact_investigation` from stage 6 | `case_id`, `institution_key`, `crime_id`, `opened_tick`, `decided_tick`, `outcome`, `appealed`, `reversed`, `cost_fe` |
| `fact_grievance` | one wrong believed by a person | `person_key`, `tick`, `kind`, `forum_key` (0 if not brought), `resolved` |
| `fact_detention` | one custody spell (inmate-days are computed from spells) | `person_key`, `valid_from_day`, `valid_to_day`, `institution_key`, `programme`, `capacity`, `release_reason`, `reoffended_by_tick` |
| `fact_service_day` | one soldier per day | `person_key`, `date_key`, `unit`, `fit`, `kitted`, `fed`, `assigned`, `morale`, `pay_arrears_fe` |
| `fact_combat` | one engagement | `tick`, `faction_a`, `faction_b`, `killed_a`, `killed_b`, `wounded_a`, `wounded_b`, `captured_*` |
| `fact_loan` | one loan | `loan_id`, `lender_key`, `borrower_key`, `principal_fe`, `repay_due_fe`, `due_tick`, `repaid_fe`, `status` (open, repaid, overdue, restructured, written_off) |
| `fact_faction_day` | one faction per day | `faction_key`, `date_key`, `treasury_stock_fe`, `revenue_fe` (per tax type fields), `spend_fe` (per category fields), `liabilities_fe`, `food_stock_units`, `food_need_units_per_day`, `price_index`, `vp` |
| `fact_institution_day` | one institution per day | `institution_key`, `date_key`, `staff`, `budget_fe`, `demand`, `served`, `queue`, `records_held`, `mean_report_lag` |
| `fact_hex_day` | one hex per day | `hex_key`, `date_key`, `resource_stock_*`, `resource_capacity_*`, `extracted_*`, `regrown_*` |
| `fact_job_day` | one contract per day | `contract_key`, `employer_key`, `worker_key`, `wage_fe`, `value_added_fe` |
| `fact_belief_sample` | sampled beliefs | `person_key`, `tick`, `subject`, `believed_value`, `true_value`, `source_kind`, `source_outlet` |
| `fact_media_publication`, `fact_outlet_day`, `fact_exposure` | one publication / outlet-day | `outlet_key`, `tick`, `subject`, `value`, `matched_truth`, `audience`, `perceived_credibility`, `accuracy` |
| `fact_collective_action` | one strike, protest or assembly | `tick`, `kind`, `participants`, `eligible`, `faction_key` |
| `fact_record`, `fact_record_audit`, `fact_return`, `fact_kpi` | from INFORMATION_AND_MEDIA_DESIGN and INSTITUTIONAL_STATE_DESIGN | `fk`: `kpi_key`, `faction_key`, `institution_key`, `period_from_day`, `period_to_day`, `actual_value`, `reported_value`, `lo_value`, `hi_value`, `sample_n`, `coverage`, `method_code`, `reporter_institution_key`, `report_tick` |

## 5. KPIs

Columns: **Num ÷ Den (unit)** is the numerator over the denominator; **Elig** is who or what is counted; **A≠R** gives the distortion codes and the specific reason; **Age** is the age when an institution can first report it (the actual value is available to the warehouse from M13). `S4` marks the Stage 4 minimum set. For all rows the reported series is produced by the institution named in A≠R, and under `FullInformation` it equals the actual one.

### 5.1 Population: births, deaths, life expectancy, migration (POP)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| POP-01 `S4` | Population | count of NPCs alive at period end (n) | D, Y | alive, resident or citizen (state which) | `fpd.alive`, `faction_key` | G A H C F | C, D, N: tally by the elder sees those known; a census is the exact one. Early tallies miss children and newcomers | Counting heads to raise rations, or to lower a levy; ghost entries (F) | Band |
| POP-02 `S4` | Births | count of `NpcBorn` in period (n); crude birth rate = births ÷ PY × 1000 (‰ per person-year) | Y | all births to citizens | `fact_birth` | G A(mother) H C F | C, D: register records births only when told. Home births missed | Recording pre-term or unviable births as stillborn | Band |
| POP-03 `S4` | Deaths | count of `NpcDied` (n); crude death rate = deaths ÷ PY × 1000 | Y | all deaths of citizens | `fact_death` | G A cause C F I | C, F: causes written by the healer, who may misattribute (starvation recorded as "illness") | Misrecording cause to hide a famine | Band |
| POP-04 | Natural increase | births − deaths (n); rate = (births − deaths) ÷ PY × 1000 | Y | as above | `fact_birth`, `fact_death` | G F | inherits POP-02, 03 | as POP-02, 03 | Band |
| POP-05 | Total fertility rate (period) | Σ over age bands of (births to women in band ÷ person-years of women in band) × band width in years (pm1000 children per woman) | Y | women in fertile bands; need ≥ 20 PY per band else `Missing` | `fact_birth`, `fpd` (sex, age_band) | G C H | N: small-number noise (10-day years) | Pro-natal payments counted as a success while real fertility falls (tempo effects) | Town |
| POP-06 | Life expectancy at birth `e0` | period life table: `e0 = Σ_x L_x ÷ l_0` (y, pm1000) with bands of `TicksPerYear/4`; formula in section 6.4 | Y (3-year pooled) | all citizens; needs ≥ 100 PY per pooled period | `fpd`, `fact_death` | G C F | N, B: needs complete deaths and exposure. Missing child deaths inflates it | Wars on the books: removing deaths of foreign-born, or of "non-citizens" | Town |
| POP-07 | Child mortality | deaths before year 1 ÷ births in the cohort (‰) | C (birth cohort, 1 y follow-up) | children born to citizens | `fact_birth`, `fact_death` | G H C F | C, B: unregistered births with early deaths vanish from both | A births register that records babies only once they survive | Band |
| POP-08 | Dependency ratio | (children + elders) ÷ working-age adults (pm1000) | Y | alive residents | `fpd.life_stage` | G F | inherits POP-01 | Reclassifying age bands | Band |
| POP-09 `S4` | Net migration | arrivals − departures (n); and the flow table by kind (immigrate, migrate, defect, captured) | Y | persons moving to or from a faction | `fact_migration` | F G O C | C, D: the guards at the edge record arrivals, rarely departures | Border guards counting only entries | Band |
| POP-10 | Age structure | share in each age band (‰); median age (y) | Y | alive residents | `fpd.age_band_key` | G H C F | as POP-01 | none material | Band |
| POP-11 | Household structure | households by type and mean size (n, pm1000) | Y | households with ≥ 1 alive member | `fact_household_day` | G H C | C: unregistered huts | none material | Band |

### 5.2 Health: need, access, waiting, outcomes (HLT)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| HLT-01 `S4` | Hunger rate | persons with `hunger_days ≥ HungerDays` (default 2) ÷ alive residents (‰) | D, R5 | alive, not infant | `fpd.hunger_days` | G A O H C F | C, N: the elder sees who comes asking; the hungry hide, or are far | Raising rations to the *known* hungry only; lowering the threshold | Band |
| HLT-02 | Undernourishment depth | mean of `max(0, NutritionNeed − nutrition)` over those below need (pm1000 of need) | R5 | alive residents | `fpd.nutrition` | G A O H C | as HLT-01 | Feeding people just above the threshold | Band |
| HLT-03 | Injury and illness incidence | new cases of `injury` or `health < IllThreshold` onsets ÷ PY × 1000 | Y | alive residents | `fpd.health`, `fpd.injury` | G A O C | B, C: only cases brought to a healer are recorded; a healer raises detection (see section 4.3 of the information doc) | Not treating minor cases, to keep the "ill" count low | Band |
| HLT-04 | Treatment demand | requests for care (n); per 1000 PY | Y | all requests | `fact_health_encounter.request_tick` | G A O C I | S: only those who ask and know where to ask | Making requests costly to cut demand | Band |
| HLT-05 | Treatment coverage | requests with `start_tick` set ÷ requests (‰) | Y | requests in period | `fact_health_encounter` | G A C I | S, F: unseen unmet need | Rejecting at the door, counted as "not a case" | Band |
| HLT-06 | Waiting time | median and p90 of `start_tick − request_tick` (ticks, reported in days); open requests counted with `now − request_tick` (censored) | Y | requests in period | `fact_health_encounter` | G A C I | F, S: the clock starts when staff log the request, not when the person asked; closed-as-lost removed | Starting the clock late; triaging so that queue-jumpers hide a long tail | Band |
| HLT-07 | Treatment outcome | episodes ending recovered (health ≥ `RecoverThreshold` within `RecoverWindow`) ÷ episodes ended (‰) | C | treated episodes | `fact_health_encounter.outcome` | A C I | B, F: selecting easy cases; recording deaths after discharge as "other" | Cherry-picking; discharging the dying | Band |
| HLT-08 | Treatable (avoidable) mortality | deaths with a cause marked treatable and no treatment ÷ PY × 1000 | Y | deaths in period | `fact_death.cause_key`, `.treated` | G A C I | B, Df: the cause is the healer's judgement | Recording causes as untreatable | Band |
| HLT-09 | Care capacity and utilisation | staff days ÷ population (pm1000); served ÷ capacity (‰) | Y | institutions of type healer, hospital | `fact_institution_day` | G I | C: slight | Padding rosters | Band |

### 5.3 Education, skills, attainment, literacy (EDU)

Literacy maps to knowing a cultural technique or symbol system (`tech_known_mask`, see [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)) once one exists.

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| EDU-01 | Technique rate (literacy) | adults knowing technique T (bit in `tech_known_mask`) ÷ adults (‰) | Y | adults and elders | `fpd.tech_known_mask` | G A O H C F | N, B: tested by a survey or exam; people hide or overstate | Passing students by lowering the test | Village |
| EDU-02 | Mean skill | mean of `skill_k` over adults, for each skill k (0–255 as pm1000) | Y | adults | `fpd.skill_*` | G A O H C | N, C: a sample or a trial; staff judgement | Easy assessment | Band |
| EDU-03 | Teaching reach | children (before year 1) with ≥ 1 teaching or apprentice episode in the year ÷ children (‰) | Y | children | `fact_education_activity` | G H C I | C: counts what is recorded | Counting any contact as teaching | Band |
| EDU-04 | Enrolment | children enrolled in a school or apprenticeship ÷ children (‰) | Y | children; needs a school or master | `fact_education_activity`, `fpd` | G H C I | S | Enrolling for headcount grants | Village |
| EDU-05 | Attainment | persons reaching skill ≥ threshold ÷ persons reaching the matching age (‰) | C | birth cohort at year 3 | `fpd.skill_*` | G H C | N, B: exam design | Teaching to the test | Town |
| EDU-06 | Skill transmission (gain per teaching event) | mean `gain` over teach events ÷ mean `gain` of unaided practice (pm1000) | Y | learners | `fact_education_activity` | I O C | N | Choosing only quick-learners | Band |
| EDU-07 | Skill-use match | employed persons whose job uses their highest skill ÷ employed (‰) | Y | employed | `fpd.skill_*`, `occupation_key` | O C | Df | Matching quotas | Town |
| EDU-08 | Trait prevalence | persons holding cultural trait T ÷ alive residents (‰) | Y | alive residents | `fpd` (trait mask) | G A C F | N: survey of what people say they believe | Propaganda that changes the statement, not the trait | Town |

### 5.4 Work, productivity and living standards (EMP, LIV)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| EMP-01 `S4` | Labour-force participation | (employed + seeking) ÷ working-age adults (‰) | R5, Y | working age, alive residents | `fpd.employed`, `.seeking` | G A H C F | C, S: unpaid household work and gathering for own use may not be recorded as "employed" | Labelling discouraged workers inactive to flatter unemployment | Band |
| EMP-02 `S4` | Employment rate | employed ÷ working-age adults (‰) | R5, Y | as above | `fpd.employed` | G A O H C F I | Df: what counts as a job (a state job-contract vs. informal work) | Make-work jobs, short shifts | Band |
| EMP-03 | Unemployment rate | seeking and not employed ÷ (employed + seeking) (‰) | R5, Y | labour force | `fpd` | G A O C F | S: only those who register with a job post are counted | Ending benefits to shrink registers | Village |
| EMP-04 | Underemployment | work-hours short of desired: (desired − worked ticks) ÷ desired ticks of employed (‰) | Y | employed | `fpd.work_ticks` | G O C | N | Counting 1 shift as employed | Village |
| EMP-05 | Productivity | value added (FE) ÷ worker-days in the period (pm1000 FE) | Y | enterprises and public works with ≥ 1 worker | `fact_job_day.value_added_fe` | G O I | Df, N: value added at reference prices; staff-reported output | Raising output counted, lowering quality | Village |
| EMP-06 | Median daily wage; real wage | median `wage_fe` (FE); real = median ÷ price index × 1000 | Y | paid workers | `fact_job_day`, `ffd.price_index` | G O C I | S: unpaid and in-kind work | Paying in overvalued goods | Band |
| EMP-07 | Wage-labour dependence | adults whose main income is wages ÷ adults with any income (‰) | Y | adults | `fpd.income_fe` shares | G O C | Df | none | Village |
| EMP-08 | Labour share | wages paid ÷ value added of enterprises (‰) | Y | private and cooperative enterprises | `fact_job_day`, `ftx` | G O I | F: owners' books | Reclassifying owner pay as profit or the reverse | Town |
| LIV-01 `S4` | Food stocks (days of cover) | total edible stock in the faction's stores ÷ daily need of residents (units ÷ units per day, pm1000 days) | D | the faction's stores (state, communal; private reported separately) | `ffd.food_stock_units`, `ffd.food_need_units_per_day` | G I F | C, N: store keeper's count; spoilage and hidden private stock not seen | Hoarding, counting rotten stock | Band |
| LIV-02 | Median consumption | median `consumption_fe` per person-day over persons (FE per day, pm1000) | Y | alive residents | `fpd.consumption_fe` | G A H C F | N: survey of households | Counting gifts as consumption | Town |
| LIV-03 | Housing | residents with a bed (sleeping right) ÷ residents (‰); people per bed | Y | alive residents | `fpd.housed` | G H C | Df | Counting empty beds | Band |
| LIV-04 | Price index | cost of reference basket at observed barter ratios ÷ cost in the base period × 1000 | Y | traded items | `ftx.value_fe`, `qty` | G F | S, B: only recorded trades; barter ratios noisy | Price controls leading to non-trade | Band |
| LIV-06 | Drive levels | mean over adults of **each** of the hunger, fatigue, cold, fear and pain drives (0–1000), reported as five separate values, **never combined** (owner decision Q23; this was an equal-weight composite before 27 Nov) | D, Y | alive adult residents | `fpd` drive columns | G A H C F | actual-only | none | Band |
| LIV-05 | Median wealth | median `wealth_fe` over persons (FE) | Y | alive residents | `fpd.wealth_fe` | G A H C F | S, N: only what the register knows; hidden stores | Hiding assets from tax | Village |

### 5.5 Taxation, spending, liabilities, reserves (FIS)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| FIS-01 | Revenue | sum of `TaxCollected` value (FE), by tax type | D, Y | all collections | `ffd.revenue_fe`, `ftx` (kind tax) | F I | F, Df: tax collectors skim; timing of booking | Booking next period's revenue early | Band |
| FIS-02 | Tax compliance | collected ÷ due (‰), due from the tax rule and *true* liabilities | Y | persons or enterprises liable | `ftx`, `fpd.income_fe` | G O C I | B, S: *due* is computed from declared, not true, income in the reported version | Raising assessments on the known; ignoring evaders | Village |
| FIS-03 | Tax burden | tax paid ÷ income (‰) | Y | persons with income | `fpd.income_fe`, `ftx` | G A O C | N: survey for income | Cutting rates but adding fees | Village |
| FIS-04 | Spending | sum of treasury outflows by category: wages, purchases, construction, welfare, upkeep (FE) | D, Y | all outflows | `ffd.spend_fe` | F I | F: category assignment | Labelling consumption as investment | Band |
| FIS-05 | Budget balance | revenue − spending (FE); ÷ revenue (‰) | Y | faction | `ffd` | F | D, F | Moving items off-book to contractors | Band |
| FIS-06 | Treasury reserves | `treasury_stock_fe`; days of spending cover = stock ÷ mean daily spend over last 5 days (pm1000 days) | D | faction | `ffd.treasury_stock_fe` | F | none under the state's own books; with a banker or collectors: D, F | Paying wages late to show a high balance | Band |
| FIS-07 | Liabilities | sum of owed amounts (arrears to staff, contracts due, loans) (FE); debt service ratio = repayments ÷ revenue (‰) | Y | faction | `ffd.liabilities_fe` | F I | F, D: off-book obligations | Delaying booking of commitments | Village |
| FIS-08 | Public works delivery | contracts completed by the due tick ÷ contracts due (‰); cost overrun = actual ÷ committed − 1000 (‰) | Y | sites with a contract | `fact_event` (contract events) | G I | F, Df | Re-baselining due dates | Band |
| FIS-09 | Unit cost of service | institution budget used ÷ units served (FE per unit) | Y | an institution with `served > 0` | `fid.budget_fe`, `fid.served` | I | F, B | Serving easy cases to cut the unit cost | Band |
| FIS-10 | Staff pay arrears | staff with unpaid wages > 1 day ÷ staff (‰) | D | state staff | `fid`, `ftx` | I | F | Not recording unpaid staff | Band |

### 5.6 Crime, investigation, public safety (CRM)

Crime has the strongest actual-versus-reported gap. `fact_crime` is the actual series (every offence). *Observed* offences are the subset an institution came to know.

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| CRM-01 `S4` | Offences, observed and actual (theft; violence) | offences of the kind ÷ PY × 1000 (‰ per person-year). **Actual** = all rows in `fact_crime`. **Observed/reported** = rows with `observed_by_inst` set and recorded | Y, R5 | citizens and visitors as victims | `fact_crime.kind` | G A(victim) O H C F I | B, C, S, F: detection rate = coverage × reporting × recognition; more guards raise it; violence between kin is under-reported; staff may downgrade a robbery to a "loss" | Downgrading offences to hit a target; not recording | Band |
| CRM-02 | Victimisation rate | persons victim of ≥ 1 offence ÷ alive residents (‰), from a survey | Y | residents | `fact_crime.victim_key` | G A O H C | N, S: survey sampling, recall | none material | Town |
| CRM-03 | Reporting rate | observed ÷ actual (‰) | Y | offences of the kind | `fact_crime` | G C I | the A≠R gap itself; actual-only | Pressure not to report | Band |
| CRM-04 | Clearance | cases with an identified offender and an outcome ÷ observed offences (‰) | Y | observed offences | `fact_court_case` | G I | F: closing cases as "no crime", and picking convenient culprits | Quick, easy closures (including of the wrong person; pair with CRT-07) | Band |
| CRM-05 | Investigation duration | median `decided_tick − opened_tick` (days); open cases censored | Y | cases | `fact_court_case` | G I | F | Closing early | Band |
| CRM-06 | Violent deaths | deaths of cause "violence" ÷ PY × 1000 (‰) | Y | all deaths | `fact_death.cause_key` | G A C | Df | Robust to reporting bias; misattribution at the healer | Band |
| CRM-07 | Perceived safety | persons whose belief "danger here" in own hex > threshold ÷ residents (‰) (a survey of beliefs, or sampled `Mind.Beliefs`) | Y | residents | `fact_belief_sample` | G A C | N | Publicity campaigns that move the belief, not the danger (media effect) | Town |
| CRM-08 | Guard coverage and response | guard patrol hex-days ÷ hex-days (‰); median ticks from an alarm to arrival | Y | hexes with residents | `fid`, `fact_event` | G I | F | Counting a sit-in as patrolling | Band |
| CRM-09 | Property recovered | value recovered ÷ value stolen (‰) | Y | observed theft | `fact_crime`, `ftx` | G I | B | Recovering low-value items | Band |

### 5.7 Courts, mediation, access to justice (CRT)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| CRT-01 | Cases filed | `CaseOpened` events (n); per 1000 PY | Y | disputes and offences | `fact_court_case.opened_tick` | G I | S | Discouraging filing | Band (elders), Town (court) |
| CRT-02 | Clearance rate | cases decided ÷ cases filed (‰) | Y | cases | `fact_court_case` | G I | F | Dismissing hard cases | Band |
| CRT-03 | Time to decision | median and p90 of `decided_tick − opened_tick` (days); censored | Y | cases | `fact_court_case` | G I | F | Settling quickly, unfairly | Band |
| CRT-04 | Backlog | open cases at period end (n); mean age (days) | D | cases | `fact_court_case` | I | F | Closing the oldest by dismissal | Band |
| CRT-05 | Access to justice | grievances brought to any forum ÷ grievances believed by persons (‰); resolved ÷ brought (‰) | Y | all grievances | `fact_grievance` | G A O C | S: actual needs persons' beliefs, the institution sees only those who come | Seen as a success when many do not come | Band |
| CRT-06 | Cost of justice | `cost_fe` of the case to the litigant ÷ value claimed (‰) | Y | decided cases with a value | `fact_court_case.cost_fe` | C I | F | Fees hidden as "gifts" | Village |
| CRT-07 | Error rate (appeal reversal) | decisions reversed on appeal ÷ appealed (‰); and wrongful convictions (verdict against an offender who did not commit it, from `fact_crime`) ÷ convictions (‰) | Y | appealed cases; convictions | `fact_court_case.reversed`, `fact_crime.offender_key` | I C | S: appeals only from those who can afford them; wrongful-conviction rate is an **actual-only** audit | Discouraging appeals | Town |
| CRT-08 | Enforcement | rulings carried out by the due tick ÷ rulings with a duty (‰) | Y | cases with a remedy | `fact_court_case`, `ftx` | I C | F | Marking compliance without checking | Band |
| CRT-09 | Equal treatment | odds ratio of a verdict against the accused, class A vs class B, among cases with equal evidence strength (pm1000) | Y | cases with an evidence score | `fact_court_case`, `fpd.class_key` | C I | needs `evidence` truth: **actual-only** | Dropping cases against the powerful | Town |

### 5.8 Prisons and reoffending (PRI)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| PRI-01 | Occupancy | inmate-days ÷ capacity-days (‰) | D, Y | prisons | `fact_detention` | G I | F | Releasing early before a count | Town |
| PRI-02 | Time served | mean days served by released inmates ÷ sentence (‰) | Y | released | `fact_detention` | I C | F | Early release to relieve crowding | Town |
| PRI-03 | Rehabilitation | released who completed a programme and gained ≥ δ skill or work-habit ÷ released (‰) | Y | released | `fact_detention.programme`, `fpd.skill_*` | I C | B, F | Easy programmes | Town |
| PRI-04 | Reoffending | released persons convicted again (or committing an offence in `fact_crime`) within `DaysPerYear` days of release ÷ released (‰). **Reported** counts only those caught | C | released | `fact_detention`, `fact_crime` | I C A | B, C: reoffending that is undetected is missing | Releasing only low-risk, or not counting reoffending elsewhere | Town |
| PRI-05 | Post-release work | released with a job within 3 days of release ÷ released (‰) | C | released | `fpd.employed` | I C | S | Placing in make-work | Town |
| PRI-06 | Cost per inmate-day | institution spend ÷ inmate-days (FE) | Y | prisons | `fid`, `fact_detention` | I | F | Cutting food and care | Town |
| PRI-07 | Harm in custody | injuries and deaths of inmates ÷ inmate-years × 1000 | Y | inmates | `fact_detention`, `fact_death` | I | F, B: not recorded | Hiding deaths | Town |

### 5.9 Military: readiness, recruitment, morale, casualties (MIL)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| MIL-01 | Readiness | soldiers with `fit`, `kitted`, `fed` and `assigned` all true ÷ enlisted soldiers (‰) | D | enlisted | `fact_service_day` | G I F | F, D: commanders report kit that exists on paper | Counting kit issued but unusable | Band |
| MIL-02 | Recruitment | enlistments ÷ eligible adults (‰); fill = soldiers ÷ establishment (‰) | Y | adults not already serving | `fact_military_service`, `fpd` | G C F | C | Pressing the poor | Band |
| MIL-03 | Retention and desertion | deserters ÷ soldier-years × 1000 (‰); and leavers by cause | Y | enlisted | `fact_service_day`, `fact_military_service` | I C | F: a deserter may be "missing" or "on leave" | Reclassifying desertion | Band |
| MIL-04 | Morale | mean `morale` of soldiers (pm1000) | D | enlisted | `fact_service_day.morale` | I F | B: officers' estimate | Reporting a high morale before a battle | Band |
| MIL-05 | Casualties | killed + wounded in an engagement (n); per 1000 soldier-days; loss-exchange ratio = enemy dead ÷ own dead (pm1000) | E, Y | engagements | `fact_combat` | F I G | F: enemy casualties exaggerated; own hidden | Overstating enemy losses | Band |
| MIL-06 | Supply and pay arrears | soldier-days without rations ÷ soldier-days (‰); soldiers with pay arrears > 1 day ÷ soldiers (‰) | D | enlisted | `fact_service_day` | I | F | Late payment recorded as on time | Band |
| MIL-07 | Veteran skill | mean `skill_fighting` of enlisted (pm1000) | Y | enlisted | `fpd.skill_*` | I | N | Counting drills as skill | Band |
| MIL-08 | Prisoners held and exchanged | prisoners (n); exchange ratio (n) | E | engagements | `fact_combat.captured_*` | F | F | none | Band |

### 5.10 Finance: lending, default, stability (FIN)

Band: informal lending between kin and friends, recorded by the elder if told. Banks in the Town age.

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| FIN-01 | Credit outstanding | sum of `repay_due_fe − repaid_fe` over open loans (FE) | D, Y | open loans | `fact_loan` | G C H F I | C, S: informal loans between friends are not written down | Not recording | Band |
| FIN-02 | Implied interest | mean of `(repay_due_fe − principal_fe) ÷ principal_fe` weighted by principal (‰ per loan; per year = × `DaysPerYear ÷ term days`) | Y | loans repaid or due | `fact_loan` | C H I | Df | Fees outside the interest | Band |
| FIN-03 | Default rate | loans overdue > `GraceDays` (or written off) ÷ loans due in the period (‰) | Y | loans due | `fact_loan.status` | G C H I | F: restructured loans are shown as performing | Rolling over bad loans (evergreening) | Band |
| FIN-04 | Credit access | households with a loan opened in the year ÷ households asking for credit (‰); asked and refused (n) | Y | households | `fact_loan`, `fact_event` | C H G | S: refused requests aren't recorded | Refusing quietly | Band |
| FIN-05 | Debt burden | household debt service ÷ household income (‰), median | Y | indebted households | `fact_household_day`, `ftx` | C H | N | Not recording informal debts | Band |
| FIN-06 | Restructured and written-off share | loans `restructured` or `written_off` ÷ loans (‰) | Y | loans | `fact_loan.status` | I | the counterweight to FIN-03 | none | Band |
| FIN-07 | Debt bondage | debtors working for their creditor ÷ debtors (‰) | Y | debtors | `fact_loan`, `fact_job_day` | C | S | Informal arrangements | Band |
| FIN-08 | Reserve coverage (banks) | bank liquid reserves ÷ deposits (‰); withdrawals in the last 3 days ÷ deposits (‰) | D | banks | `fid` | I | F, D | Window dressing | Town |

### 5.11 Media: credibility, reach, ownership (MED)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| MED-01 | Reach | residents exposed to ≥ 1 outlet in the period ÷ residents (‰) | Y | residents | `fact_outlet_day.audience`, `fact_exposure` | G A O C F | B: circulation counted, not read | Counting copies printed | Village |
| MED-02 | Audience share | audience of outlet ÷ total audience (‰) | Y | outlets | `fact_outlet_day` | G C | B | Free distribution | Village |
| MED-03 | Perceived credibility | mean `trust_o` of exposed residents for outlet o (0–1000) | Y | exposed residents | `fact_outlet_day.perceived_credibility` | O C F | N (a survey of trust) | Branding | Town |
| MED-04 | Accuracy | publications whose claim matches truth (within tolerance) ÷ publications with a checkable claim (‰). **Actual-only** (needs `matched_truth`) | Y | checkable claims | `fact_media_publication.matched_truth` | outlet owner kind | no reported version unless a fact-checking inspection runs | Publishing only safe claims | City |
| MED-05 | Credibility gap | perceived credibility − accuracy (pm1000) | Y | outlets | MED-03, MED-04 | outlet | actual-only | none | City |
| MED-06 | Ownership concentration | Σ over owners (audience share)² (Herfindahl, 0–10 000) | Y | outlets with audience | `fact_outlet_day`, `dim_outlet.owner` | owner kind | C | Straw owners | City |
| MED-07 | Independence | audience served by non-state outlets ÷ total audience (‰) | Y | outlets | `dim_outlet.owner_kind` | G C | Df | State-controlled "private" outlets | City |
| MED-08 | Misbelief prevalence | sampled beliefs from media sources with value off truth by > tolerance ÷ beliefs from media sources (‰) | Y | beliefs on checkable subjects | `fact_belief_sample` | G A C outlet | actual-only | none | City |
| MED-09 | Belief accuracy (all sources) | mean absolute error of sampled beliefs about reference facts (prices, stocks), relative to the true value (‰), by source kind: seen, told, posted, published | Y | beliefs on checkable subjects | `fact_belief_sample` | A C source | actual-only | none | Band |

### 5.12 Poverty, inequality, mobility (INQ)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| INQ-01 | Poverty headcount | persons with `income_fe` or `consumption_fe` per day below the line ÷ persons (‰). Line (absolute) = cost of a subsistence basket; (relative) = 50% of the median | Y | alive residents | `fpd.income_fe`, `consumption_fe` | G A H C F | N, B: survey, hidden resources | Moving the line; transfers just over it | Village |
| INQ-02 | Poverty gap | mean of `max(0, line − income) ÷ line` over all persons (‰) | Y | as above | `fpd` | G A H C | as above | The headcount trap | Village |
| INQ-03 `S4` | Wealth Gini | formula in section 6.5 over `wealth_fe` of adults (‰) | Y | adults; `n ≥ 10` | `fpd.wealth_fe` | G A C F | C, S, F: only recorded wealth; hidden assets and private stock | Hiding assets; levelling down | Band |
| INQ-04 | Income Gini | as above over `income_fe` per person-year (‰) | Y | adults | `fpd.income_fe` | G A C F | N | as above | Village |
| INQ-05 | Top shares | share of total wealth held by the top 10% and top 1% of adults (‰); bottom 50% share | Y | adults | `fpd.wealth_fe` | G C F | C, F | Wealth held by nominees | Band |
| INQ-06 | Palma ratio | share of the top 10% ÷ share of the bottom 40% (pm1000) | Y | adults | `fpd` | C | as INQ-05 | none | Band |
| INQ-07 | Intergenerational mobility (rank-rank) | the slope of child adult-wealth percentile rank on parent's rank (pm1000), formula in 6.6 | C (cohorts reaching adulthood in a 3-year window) | persons with a known parent and ≥ 30 pairs | `dim_person.mother_key`, `fpd.wealth_fe`, `class_key` | G C F | needs parent links: kinship records; **mostly actual-only** | Hard to game | Town |
| INQ-08 | Transition matrix | `T[i][j]` = children in class or wealth quintile *j* ÷ children whose parents were in *i* (‰, rows sum to 1000) | C | as INQ-07 | as above | G F | as above | none | Town |
| INQ-09 | Within-life mobility | adults who changed class or quintile between years y−1 and y ÷ adults in both (‰) | Y | adults present in both years | `fpd.class_key` | C A | N | none | Village |
| INQ-10 | Inheritance share | wealth received by estate transfers ÷ total adult wealth (‰) | Y | estates settled | `ftx` (kind estate), `fpd` | C | C | Pre-death gifts | Village |
| INQ-11 `S4` | Class composition | adults in each class position ÷ adults (‰), class from the relation to property, labour and surplus | Y | adults | `fact_person_day_class` | G A F | Df: the class definition belongs to ECONOMIC_CLASSES; actual-only in the Band age (recorded from M13) | none | Band |

### 5.13 Environment and resources (ENV)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| ENV-01 | Resource stock ratio | Σ `resource_stock_r` ÷ Σ `resource_capacity_r` (‰) per resource *r* (forest, fish, soil, game) | D, Y | hexes with resource r | `fact_hex_day` | G F | C, D: surveyed hexes only; stock seen from far is guessed | Reporting the best patches | Band |
| ENV-02 | Sustainability ratio | extracted ÷ regrown over the period (pm1000); > 1000 means falling stock | Y | hexes | `fact_hex_day.extracted_*`, `.regrown_*` | G F r | C | Quotas on reported, not real, catch | Village |
| ENV-03 | Depleted hexes | hexes with stock < 200‰ of capacity ÷ hexes with that resource (‰) | Y | hexes | `fact_hex_day` | G F | C | Redrawing hex borders | Band |
| ENV-04 | Years of supply (non-renewables) | reserves ÷ mean annual extraction (y, pm1000) | Y | a depletable resource | `fact_hex_day` | G F | C, N: unexplored reserves | Reporting the reserve only when needed | Town |
| ENV-05 | Soil and yield trend | mean yield per farm-day this year ÷ yield 3 years ago (‰) | Y | farms | `fact_job_day`, `fact_hex_day` | G I | N | Inputs raising yield while soil drops | Village |
| ENV-06 | Commons compliance | harvest visits within the rule ÷ harvest visits in commons (‰); sanctioned breaches (n) | Y | commons | `fact_event` | G F | B: observed visits only | Making the rule hard to break only on paper | Band |

### 5.14 Trust, participation, legitimacy, cooperation (TRU, COO)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| TRU-01 `S4` | Mean loyalty | mean `loyalty` of citizens (0–1000) | D, Y | citizens, adults | `fpd.loyalty` | G A O H C F | N, S: stated loyalty in a survey vs revealed (migration) | Propaganda inflates the stated; coercion | Band |
| TRU-02 | Low-loyalty share | citizens with `loyalty < 300` ÷ citizens (‰) | Y | citizens | `fpd.loyalty` | G C F | as TRU-01 | as above | Band |
| TRU-03 | Trust in institution *i* | mean of `trust_in_inst_i` over residents who know *i* ÷ (0–1000); share using it when needed (‰) | Y | residents | `fpd` (sampled), `fact_belief_sample` | G A C I | N | Branding | Band |
| TRU-04 | Participation | those who took part in a collective decision (elders' gathering, a vote) ÷ eligible (‰) | E, Y | eligible adults | `fact_collective_action` | G A C | S: who is eligible and who knows | Making it easy only for allies | Band |
| TRU-05 | Revealed legitimacy: exit | defections, desertions and emigrations ÷ citizens (‰ per PY) | Y | citizens | `fact_migration`, `fact_service_day` | C F | C | Exit barriers | Band |
| TRU-06 | Voluntary compliance | FIS-02 (tax compliance), CRT-08 (enforcement) and CRM-03 (reporting) together; reported as the three values, never as one number | Y | as each | see each | I | see each | An index invites mixing | Village |
| TRU-07 | Collective action | strikes, protests and assemblies (n); participants ÷ eligible (‰) | E, Y | eligible | `fact_collective_action` | C O G F | C | Suppressing | Village |
| TRU-08 | Class interest alignment | the share of adults whose beliefs about a policy match their material interest (‰); see [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md) | Y | adults | `fact_belief_sample`, `fpd.class_key` | C F | actual-only | none | Village |
| COO-01 | Promise keeping | promises kept by the due tick ÷ promises falling due (‰) | Y | recorded promises | `fact_promise` | G C F | S: unrecorded promises | none | Band |
| COO-02 | Barter completion | exchanges where both hand-overs completed ÷ exchanges begun (‰) | Y | exchanges | `ftx.completed` | G C F | C | none | Band |
| COO-04 | Mean interpersonal trust | mean of `fpd.mean_trust` over living adults (0–1000) | D, Y | alive adults | `fpd.mean_trust` | G A C F | actual-only | none | Band |
| COO-03 | Gifts and sharing | `GiftGiven` value (FE) ÷ total household consumption (‰) | Y | citizens | `ftx` | H C G | C | none | Band |

### 5.15 Institutions (INS)

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| INS-01 | Institution survival | days from founding to dissolution per institution; open institutions censored at run end (6.3) | E, Y | institutions founded | `dim_institution_history`, `fid` | I F | actual-only | none | Band |
| INS-02 | Staff turnover | staff leaving ÷ mean staff (‰ per person-year) | Y | staffed institutions | `fid.staff_left`, `dim_person_history` | I F | F: the manager's own staff list | Keeping absent staff on the books | Band |
| INS-03 | Rule changes | `PolicyChanged` count per faction or institution (n per year) | Y | factions, institutions | `fact_policy_change` | I F | actual-only | none | Band |

### 5.16 Time use and unpaid care (TIM)
Owner decision Q12: care work is real work, and unpaid care is reported separately so that it is not economically invisible. Definitions of the categories are in [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md#47a-time-use-owner-decision-q12).

| ID | Name | Num ÷ Den (unit) | Period | Elig | Source | Brk | A≠R | Gaming / Goodhart | Age |
|---|---|---|---|---|---|---|---|---|---|
| TIM-01 `S4` | Time use | ticks in each of paid work, independent production, communal work, unpaid care, subsistence, leisure ÷ awake ticks (‰ each; six values, never combined) | R5, Y | alive adults (children and elders as separate breakdowns) | `fpd.ticks_*` | G A H C F | actual-only in Stage 4 | none | Band |
| TIM-02 `S4` | Unpaid care hours | unpaid care ticks per adult per day (hours, pm1000), with its four parts (children, relatives, food preparation, household maintenance) | R5, Y | alive adults | `fpd.ticks_care_*` | G A H C F | actual-only in Stage 4 | none | Band |
| TIM-03 | Unpaid share of all work | unpaid care ticks ÷ (all work ticks including unpaid care) (‰) | Y | alive adults | `fpd.ticks_*` | G A H C | actual-only | none | Band |

### Wellbeing dimensions (reported separately)
Owner decision Q23: no universal composite. Each dimension below is shown as its own series, and a report or dashboard never sums them.

| Dimension | KPIs (each shown on its own) |
|---|---|
| Health | HLT-01 hunger rate, HLT-03..08 where available, POP-03 deaths by cause |
| Nutrition | HLT-01, HLT-02, LIV-01 food stocks, LIV-06 hunger level |
| Life expectancy | POP-05, POP-06 |
| Security | CRM-01, CRM-02, CRM-06, MIL casualties, LIV-06 fear level |
| Comfort | LIV-03 housing, LIV-06 cold and fatigue levels |
| Leisure | TIM-01 leisure share |
| Education | EDU-01..05 (from the Town age; skill measures earlier) |
| Autonomy | EMP-07 wage-labour dependence, the dependency rate and subsistence access ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md#44-subsistence-access-and-dependency)), TRU-05 exit |
| Material living standards | LIV-02 median consumption, LIV-05 median wealth, EMP-06 real wage |
| Inequality | INQ-01..06 |
| Social relationships | COO-03 gifts and sharing, COO-04 mean interpersonal trust, household membership (HOUSEHOLDS measures) |
| Environmental sustainability | ENV-01..03, ENV-06 |

## 6. Formulas (integer or fixed point)

All use `i64` unless noted. `mulDiv(a, b, c) = (a·b)/c` with 128-bit intermediate (`Int128`) where `a·b` can exceed 2^62, and half-away rounding as in 3.1. No floats.

### 6.1 Share and rate

- Share (‰): `‰ = div_round(1000 · num, den)` (0 if `den = 0`; the KPI is `Missing` when `den < MinDen`).
- Rate per person-year (‰): `div_round(1000 · events · DaysPerYear, person_days)`.
- Person-years: `PY_pm1000 = div_round(1000 · person_days, DaysPerYear)`.

### 6.2 Median, percentile

Deterministic order statistic. Sort the values ascending, ties by `person_key`. Percentile `p` (permille): index `k = (p · (n − 1) + 500) / 1000` rounded half up, value `v[k]`. The median is `p = 500`. For an even count, the median is `div_round(v[n/2 − 1] + v[n/2], 2)`. Use introselect with fixed pivot rule or a full sort (n ≤ a few thousand).

### 6.3 Waiting times with censoring

For each request: `w = (start_tick ?? now) − request_tick`, flag `open` if `start_tick` is null. Median and p90 are taken over all `w` (open requests included at their current age, so a rising backlog raises the figure), and the report also gives the **open share** (‰). A second figure `median_closed` excludes open requests; the dashboard shows both.

### 6.4 Life expectancy (period life table)

Band width `W = TicksPerYear / 4` ticks, bands `x = 0..B−1`, `B = 20` (the last band is open-ended). For each band: exposure `E_x` (person-ticks lived in band during the pooled period) and deaths `D_x`.

```text
m_x   = D_x * 1000 / E_x                         // deaths per 1000 person-ticks (pm1000 hazard per tick)
q_x   = min(1000, div_round(1000 * m_x * W, 1000 + m_x * W / 2000))   // conditional death probability in the band (‰), standard a=1/2 conversion
l_0   = 1_000_000;  l_{x+1} = l_x * (1000 - q_x) / 1000
L_x   = (l_x + l_{x+1}) / 2 * W                  // person-ticks lived in band (last band: l_x / m_x)
e0    = sum_x L_x / l_0  / TicksPerYear          // years, pm1000
```

A band with `E_x = 0` borrows the previous band's `q`. `e0` is `Missing` if total exposure < 100 PY.

### 6.5 Gini (‰) over `n` non-negative values

Sort ascending `w_1 ≤ … ≤ w_n`, `S = Σ w_i`, `S1 = Σ i·w_i`:

```text
G‰ = div_round( 2000 * S1 - 1000 * (n + 1) * S ,  n * S )     // S = 0 gives 0
```

`2000 · S1` needs `Int128` when `n² · max(w)` exceeds 2^52; scale wealth down by a fixed shift per run if needed (`w >>= k`, same `k` for actual and reported). Reported Gini is the same function over the recorded wealth vector, where unrecorded persons are *absent* (not zero) and hidden assets are missing. **Top share (‰):** let `m = ceil(n · pct ÷ 1000)`, `top‰ = div_round(1000 · Σ of the m largest, S)`. **Palma:** top10 ÷ bottom40, `pm1000 = div_round(1000 · Σtop10, Σbottom40)`.

### 6.6 Rank-rank slope and transition matrix

For parent-child pairs `(p_i, c_i)` (adult wealth, each measured at the same relative age), assign percentile ranks `rp_i`, `rc_i` ∈ 0..1000 by sorting (ties get the mean rank, kept in ×2 units). With `n` pairs: `Sp = Σ rp`, `Sc = Σ rc`, `Spp = Σ rp²`, `Spc = Σ rp·rc`:

```text
slope_pm1000 = div_round( 1000 * (n*Spc - Sp*Sc) ,  (n*Spp - Sp*Sp) )     // 0 if the denominator is 0
```

Because ranks are on a common scale, the slope equals the rank correlation. Transition matrix: quintile `q(r) = min(4, r / 200)`; `T[i][j] = div_round(1000 · count(i→j), count(i→*))`, rows summing to 1000 (the last cell absorbs rounding). Cells with `count(i→*) < 5` are `Missing`.

### 6.7 Survey estimate (share)

`p̂‰ = div_round(1000 · k, m)`, `se = isqrt( p̂(1000 − p̂) · (N − m) ÷ ( m · (N − 1) ) )` (pm1000-scaled integers, `isqrt` exact Newton), interval `p̂ ± div_round(1960 · se, 1000)`. A mean has `se = s ÷ √m · √((N−m)/(N−1))` with `s` the integer sample standard deviation (Welford in `i64`).

### 6.8 Detection and reported events

`reported = round(actual × detect ÷ 1000)` in expectation; the realised count is a sum of Bernoulli draws on the recorder's stream. `detect = coverage · report_rate · recognition ÷ 10⁶` (each in ‰).

### 6.9 Price index

Laspeyres on the fixed reference basket `q_i`: `PI = div_round(1000 · Σ q_i · p_i^t, Σ q_i · p_i^0)`, `p_i` the median barter ratio in FE for item *i* from `ftx` in the period. Items with < 5 trades carry forward the last value (flagged stale).

### 6.10 Herfindahl (MED-06)

`HHI = Σ_o (share‰_o)²` ÷ 100, so one owner gives 10 000 and four equal owners give 2 500.

## 7. Dashboards

### 7.1 Layout

The reports panel has three levels:
1. **Overview:** one card per domain with its 1–3 headline KPIs (value, interval, `as of`, sparkline of the last 10 periods, direction arrow versus the previous period and versus the 3-year-equivalent mean).
2. **Domain page:** a **scorecard** of paired KPIs (below) and a small multiples chart per breakdown.
3. **KPI page:** the full series with policy markers, breakdown selector, provenance (who reported it, how, coverage, interval) and the drill-down.

### 7.2 Comparisons over time

- **Series with markers:** every `PolicyChanged` event, every `CollectionRun`, every institution founded or closed, and every war, famine or unlock is a vertical marker. A **series break** marker is added where the method or coverage changed.
- **Compare modes:** vs the previous period; vs the same point one generation ago (a year-lag at the same life stage); vs a rolling 3-year mean; vs the target the player set; vs another faction only where the view allows (foreign data is limited to what is visible).
- **Pre/post helper for policies:** pick a marker, the panel shows the window before and after with the change and its interval. It labels the result **descriptive**: other things also changed, and the Experiments suite ([POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)) is the place for causal claims.
- **Lag awareness:** the report lag is shown as a shaded band, so the player does not judge a policy on figures that predate it.

### 7.3 Drill-down paths (KPI to facts)

Each path ends in rows the warehouse holds, and each step shows what is *reported* (through the view) and, with the overlay on, what is *actual*.

| Start | Path |
|---|---|
| HLT-01 hunger rate | by hex → by household type or class → the households (list) → each person's nutrition and last meals (`fpd`) → events (`WagePaid`, `GiftGiven`, theft) that fed or failed them |
| LIV-01 food stocks | by store → inflow (harvest, taxes, purchases) and outflow (rations, spoilage, theft) → the producers and workers → the labour KPIs EMP-01..05 |
| CRM-01 observed offences | by hex and type → the observing institution and the detection rate → cases (`fact_court_case`) → victims and offenders (class position, household) → witnessed or not |
| CRT-03 time to decision | by institution → queue and staff (`fid`) → the oldest cases |
| INQ-03 Gini | by class → top and bottom persons (names) → their income sources and inheritance (INQ-10) |
| TRU-01 loyalty | by class and hex → components (fed, housed, paid, taxed, treated by soldiers, beliefs) → media exposure (MED) and gossip |
| FIN-03 default | by lender and borrower class → loan list → causes (harvest, illness, theft) |

### 7.4 Investigating causes, setting policy, watching consequences

A **KPI tree** gives the player a starting hypothesis, not an answer. For example:

```text
Hunger rate (HLT-01)
 ├─ Food stocks (LIV-01) ← production (EMP-05, ENV-01..02) ← labour (EMP-01..02) ← wages, loyalty (EMP-06, TRU-01)
 ├─ Distribution ← wealth and class (INQ-03, INQ-05), prices (LIV-04), gifts and sharing (COO-03), welfare spend (FIS-04)
 └─ Access ← housing and distance (LIV-03, G breakdown), the elders' and healer's reach (HLT-05)
```

- **Investigate:** open a KPI, split by breakdown, follow the tree, open the facts. If a figure looks wrong, check its coverage, method and series breaks; fund a **survey or inspection** (INFORMATION_AND_MEDIA_DESIGN, section 4.2) to learn how wrong.
- **Set policy:** from a KPI card the player opens the levers that the owning documents define (taxes, wages, rations, guards, enlistment terms). The card shows which levers *historically* moved this KPI in this game (the markers) but never recommends one.
- **Watch consequences:** after a lever change a **watch** is created: the affected KPIs and their pairs are pinned with the policy marker, the expected lag from the report lags, and an alert if the *guard* measure of a pair moves the wrong way while the headline moves the right way (a Goodhart warning). Alerts are descriptive.

## Balanced scorecards against Goodhart's law

Each pair holds one measure that institutions are likely to be pushed on (the **target**) and one that is expensive to fake and moves when the target is gamed (the **guard**). The dashboard shows them together, and the pair divergence is itself a KPI (the "gap alert" in 7.4).

| Target | Guard | What the pair catches |
|---|---|---|
| CRM-01 reported offences | CRM-02 victimisation survey; CRM-06 violent deaths | Downgrading and not recording; detection changes (4.3 of the information doc) |
| CRM-04 clearance | CRT-07 reversal and wrongful-conviction rates; CRT-05 access | Closing cases on convenient culprits |
| HLT-06 waiting time | HLT-07 outcome; HLT-08 treatable mortality; HLT-05 coverage | Starting the clock late, rejecting, discharging the dying |
| HLT-01 hunger rate | LIV-01 food stocks; HLT-02 depth; POP-03 deaths by cause | Feeding the counted; recording starvation as illness |
| EMP-02 employment | EMP-06 real wage; EMP-04 underemployment; EMP-05 productivity | Make-work, short shifts |
| FIS-01 revenue | FIS-02 compliance; TRU-05 exit; INQ-01 poverty | Over-collection that drives exit or evasion |
| FIS-06 reserves | FIS-10 pay arrears; FIS-07 liabilities | Wages paid late to look solvent |
| MIL-01 readiness | MIL-04 morale; MIL-03 desertion; MIL-06 arrears | Paper readiness |
| FIN-03 default rate | FIN-06 restructured share; FIN-04 credit access | Evergreening and refusing loans |
| PRI-01 occupancy | PRI-04 reoffending; PRI-07 harm in custody | Early release; poor conditions |
| PRI-04 reoffending | CRM-02 victimisation; PRI-05 post-release work | Selecting low-risk releases |
| MED-01 reach | MED-04 accuracy; MED-03 credibility | Reach bought with sensation or falsehood |
| EDU-04 enrolment | EDU-05 attainment; EDU-02 mean skill | Enrolment without learning |
| INQ-03 Gini | LIV-05 and LIV-02 median levels; INQ-01 poverty | Levelling down (equality by destroying wealth) |
| EMP-05 output | ENV-01..03 stocks; ENV-02 sustainability | Output by depletion |
| TRU-01 loyalty (stated) | TRU-05 exit; TRU-04 participation | Stated loyalty vs revealed behaviour |
| POP-01 population | POP-09 migration; HLT-01 hunger | Headcount growth by immigration into hunger |

## 8. The Stage 4 minimum

M13b delivers the Band-age set through `FactionKnowledgeView` (reports panel); the facts the person, household and economy KPIs need are recorded from M13a ([conventions §10](DESIGN_CONVENTIONS.md#10-roadmap-and-stage-4-milestones-the-official-list)):

| KPI | Measure | Recorder (Band) |
|---|---|---|
| POP-01 | population | the elders' tally |
| POP-02, POP-03 | births, deaths (and crude rates) | the elders' register, the healer's log |
| HLT-01 | hunger rate | the elders and the store keeper |
| LIV-01 | food stocks | the shared-store keeper |
| EMP-01, EMP-02 | labour participation, employment | the headman or elder holding the job contracts |
| INQ-03 | wealth Gini | the elders' account of personal goods and huts (coarse) |
| CRM-01 (theft, violence) | crime-lite: theft and violence **observed** | the guards and the elders |
| TRU-01 | loyalty | the elders' assessment; actual from NPC state |
| POP-09 | migration | the guards at the edge |
| INQ-11 | class composition | none in the Band age: actual-only, from the class measures ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)) |
| TIM-01, TIM-02 | time use and unpaid care | none in the Band age: actual-only, from the time-use ticks ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md#47a-time-use-owner-decision-q12)) |

- **`FullInformation` default:** every one equals its actual value within rounding (test I2).
- **A degraded `Reported` profile** exists for the same set, so the view and dashboards work when information is narrowed (test I17).
- **Also available at M13 as actual-only series** (warehouse, overlay) but not yet reported: HLT-03..08, CRM-03..09, INQ-01..02, 04..10, ENV-01..03, COO-01..03, MED-09, FIS-01..10 (the treasury's own books are exact from M6), MIL-* (from M9).
- **Not built:** surveys, census and statistics office (Village to City), media KPIs (City), courts and prisons (Town).

## 9. Interactions

- **Information layers:** [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md) owns how a report is produced.
- **Warehouse:** `fact_kpi` holds actual and reported side by side with provenance, and `fact_return` holds managers' returns; views replicate the formulas; parity test against `Sim/Information/Kpi`.
- **Institutions:** each institution type in [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md) lists the KPIs it records and the targets its manager may face.
- **Experiments:** KPIs are the outcome measures in [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md); the gap measures are outcomes too.
- **Training:** reward signals for trained policies must use *actual* values (warehouse-side), and observations the *reported* ones; see [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md).

## 10. Telemetry

`KpiReported` (kpi, scope, period, reported value, lo, hi, producer, coverage, method, published tick) at each publication. A **KPI engine** run in the warehouse writes the actual series into `fact_kpi` at each period end. Both are outside the sim's hash; the in-sim reported series (records) is inside it. Snapshot fields needed to compute every KPI are listed in [Tables assumed](#4-tables-assumed); a recording level that drops `fact_person_day` columns disables the KPIs that need them and the engine marks those `Missing`.

## 11. Stage 4 minimum vs later

See section 8 and the Age column. In short: M13 builds the Band set above; Village adds poverty, unemployment, tax compliance, literacy, courts (headman) and granary KPIs; Town adds census-based population, surveys, courts, police, prisons, healthcare, education and banking KPIs; City adds media, statistics-office, regulation and environment-agency KPIs with limited information enforced.

## 12. Migration from the existing code

`SmokeRun` and `AiBrain` already compute ad-hoc figures (VP, stocks, soldiers). They move to `Sim/Information/Kpi` as the Stage 4 set and report through the view. `fact_faction_day` is the first table filled (M0 to M13).

## 13. Tests and acceptance criteria

| # | Test | Pass criterion |
|---|---|---|
| K1 | `Kpi_Formulas_Golden` | Each function in section 6 reproduces hand-computed values on fixed vectors: Gini of `{1,1,1,1}` = 0; of `{0,0,0,100}` = 750‰; top-10% share of 10 equal values = 100‰; median of `{1,2,3,4}` = 3 (half-away rounding of 2.5); a life table with constant `q` gives `e0` within 2% of the analytic value. |
| K2 | `Kpi_SQL_Parity` | For 5 seeds, every KPI in the warehouse view equals the C# function over the same rows exactly (integers). |
| K3 | `FullInformation_Report_Equals_Actual` | = I2 in the information document: `|R − A| ≤ 1` unit for every `S4` KPI and every actual-available KPI at every period end. |
| K4 | `Min_Cell_Suppression` | A breakdown cell with `< MinCell` rows is `Missing` in the view and present in the warehouse. |
| K5 | `Gaming_Gap_Detectable` | Scenario with one falsifying recorder (information doc I6): the scorecard gap alert for CRM-01 vs CRM-02 fires within 3 years in at least 90% of 20 seeds, and does not fire in honest controls (≤ 5%). |
| K6 | `Better_Counting_Series_Break` | Raising guard detect marks `SeriesBreak` on CRM-01 reported while the actual series is flat (information doc I7). |
| K7 | `Waiting_Time_Censoring` | 10 requests, 5 served at 1 day and 5 open at 20 days: median counts the open ones (= 10.5 days, 11 after rounding), `median_closed` = 1 day, open share = 500‰. |
| K8 | `Reoffending_Cohort` | For 100 releases with 30 true reoffences of which 20 are detected: actual PRI-04 = 300‰, reported = 200‰, both within rounding. |
| K9 | `Hash_Unchanged_By_Kpi_Engine` | The warehouse KPI engine on or off: identical `StateHash` (information doc I12). |
| K10 | `Band_Set_Present` | A 20-year Band run has a value or an explicit `Missing` reason for each `S4` KPI on every period, with source and `AsOfTick`. |
| K11 | `Rank_Slope_Known` | Pairs with child rank = parent rank give slope 1000; with independent random ranks, 1000 pairs give a slope in ±100. |
| K13 | `No_Composite_Wellbeing` | No KPI definition, report template or dashboard sums or weights KPIs from different wellbeing dimensions; a static check over `dim_kpi` and the report templates finds no composite outside experiment plan files, and every composite in a plan file lists components, weights and limitations |
| K14 | `TimeUse_Reconciles` | TIM-01 categories plus `TicksOther` sum to the ticks of the day for every adult-day; TIM-01 and TIM-02 equal their recomputation from events exactly (5 seeds) |
| K12 | `Rounding_Rule` | `div_round` is symmetric: `div_round(-5, 2) = -3` and `div_round(5, 2) = 3`; no KPI function uses floating point (static check on `Sim/Information/Kpi`). |

## 14. Open questions

No open questions remain. Owner decisions of 27 Nov 2026 ([conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)):
- **Q23 Composite wellbeing:** no universal composite score. The underlying measures are reported separately (health, nutrition, life expectancy, security, comfort, leisure, education, autonomy, material living standards, inequality, social relationships, environmental sustainability; [Wellbeing dimensions](#wellbeing-dimensions-reported-separately)). Experiments may use explicitly defined composites with documented components, weights and limitations. No hidden political or moral weighting of which societies succeed. LIV-06 is therefore five separate drive levels (K13).
- **Q12 Care work:** unpaid care is reported separately so it is not economically invisible; reports include time on paid work, independent production, communal work, unpaid care, subsistence and leisure (TIM-01 to TIM-03, K14).

Settled with defaults:
1. **Smoothing and the 10-day year:** life expectancy (POP-05, POP-06) is *defined* over a 3-year window, not just displayed smoothed; other annual KPIs keep the 1-year definition with a 3-year rolling display.
2. **Wealth for a Band-age Gini:** INQ-03 uses personal wealth (goods, tools, huts); communal and cooperative shares are reported as a separate series. [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md) may add a class-based variant.
3. **Evidence strength for CRT-09:** deferred to the courts design (stage 6); CRT-09 is not built before then.
4. **Theory-dependent KPIs:** TRU-08 follows the political-interest measures of [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md) and changes with them.
