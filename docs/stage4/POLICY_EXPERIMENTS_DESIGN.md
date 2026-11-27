# Policy experiments design (stage 4 harness, stage 6 suite)

## 1. Purpose and scope

**Owns:**
- how to compare policies and institutional arrangements in controlled headless runs;
- the experiment file format and the runner `experiment` command's contract;
- the statistics rules: arms, seeds, power, burn-in, generations, effect sizes, multiple comparisons, pre-registration, sensitivity;
- the split between causal experiments and descriptive analysis;
- the neutrality checklist.

**Does not own (link only):**
- the runner, scenarios, parallel runs and hashes: [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md);
- tables, events and the SQLite/Parquet/DuckDB layout the analyses query: [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md);
- the definition of each KPI: [KPI_CATALOGUE.md](KPI_CATALOGUE.md);
- what each institution does, provision models and failure modes: [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md), [STATE_DESIGN.md](STATE_DESIGN.md);
- ownership forms and classes: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md), [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md);
- the maths-only lab ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md), [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md)): a different kind of experiment (section 2, principle 9);
- trained policies as arms: [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md).

**Stage 4 builds a small harness** (M13 and M14) and runs one or two Band-age experiments to prove it. The suite of institution experiments (healthcare, courts, regulation and so on) needs institutions from stages 5-7 and is stage 6.

## 2. Principles

1. **A controlled experiment changes one thing.** Arms are identical runs except for the treatment: the same scenario, tuning, map, population seed and random streams. Only the named policy or institution parameter differs.
2. **Randomise or pair.** The unit of analysis is a run. Assignment of treatments to runs is by pairing on seed (common random numbers) or by a seeded random draw. Never pick seeds by looking at results.
3. **Pre-register.** The hypothesis, the primary and secondary metrics, the analysis and the stopping rule are written in the experiment file and hashed before the first run. Anything added later is labelled exploratory.
4. **Report everything.** Null, negative and mixed results go in the report with the same prominence as positive ones. The runner publishes every arm and every metric it was asked to compute, not a selection.
5. **No theory is built to win.** The experiment tests what the mechanics produce. If an arrangement does badly because of how the game was tuned, that is a tuning finding, and sensitivity analysis must say so (section 4.7).
6. **Causal and descriptive are separate.** Only a designed contrast with randomised or paired assignment is a causal claim. Correlation inside runs is descriptive and is labelled so (section 4.9).
7. **Generations matter.** A policy that looks good in 5 sim years can look different over 5 generations. Experiments run long enough to measure what they claim (section 4.5).
8. **Outcome measures are real outcomes, reported alongside reported ones.** Institutions respond to reported KPIs and can game them. Each primary metric has an actual and a reported version in the KPI catalogue. The experiment compares both, so Goodhart effects are visible, not hidden.
9. **Lab and island stay separate.** Lab experiments (payoffs from the book's matrices) test strategy properties. Island experiments, here, take their payoffs from consequences in the simulated world and are never forced toward a theoretical equilibrium ([conventions](DESIGN_CONVENTIONS.md#2-owner-decisions-complete-list-2527-nov-2026)).
10. **The sim is a model, not the world.** Results say what happens in this model under stated rules. Reports say so in plain words, and do not claim real-world policy conclusions.

## 3. Data structures

### 3.1 What counts as a treatment
| Kind | Example | How it is applied |
|---|---|---|
| Faction policy parameter | tax rate, wage level, welfare payment | `SimTuning` or a command schedule in the scenario |
| Institution arrangement | public, private, cooperative or communal provision of care | institution configuration (funding model, staffing, powers) |
| Ownership regime | communal against private huts and stores | state or law setting at start, or a rule change at a given day |
| Law or rule | witness rules for theft, penalties | law configuration |
| Policy source | scripted against trained, benchmark strategy against another | `PolicyRegistry` entry per level and scope |
| Environment | land fertility, shock schedule | scenario options (these are factors, not treatments; section 4.8) |

### 3.2 Experiment definition (C#-like sketch; the file in section 4.1 maps to it)
```csharp
sealed class ExperimentSpec {
    string Id; int SchemaVersion;
    string Title, Hypothesis;              // hypothesis text, one sentence per claim
    string Scenario; int ScenarioVersion;   // from the runner's scenario list
    int[] Seeds;                           // or SeedRange {start, count}
    int BurnInDays, HorizonYears, SampleEveryDays;
    ArmSpec[] Arms;                        // Arms[0] is the control
    EnvironmentFactor[] Environments;      // optional crossed factors
    MetricSpec[] Primary, Secondary;       // names from KPI_CATALOGUE
    AnalysisPlan Analysis;
    SensitivitySpec Sensitivity;
    string PlanHash;                       // SHA-256 of the above (the pre-registration)
}
sealed class ArmSpec { string Name; Override[] Overrides; } // dotted path -> integer value, or policy id
sealed class MetricSpec { string Kpi; string Version; string Basis; string Window; string Direction; }
```
`Basis` is `actual`, `reported` or `gap` (reported minus actual, the Goodhart gap; only for KPIs an institution reports). `Direction` is the pre-registered expected direction (`increase`, `decrease`, `either`).

### 3.3 Result tables (written by the runner to the warehouse)
| Table | Grain | Key columns |
|---|---|---|
| `experiment` | one row per experiment | experiment id, plan hash, runner version, git commit, start and end time |
| `experiment_run` | one row per (arm, seed, environment) | run id, arm, seed, env, final state hash, status, wall-clock seconds |
| `experiment_metric` | one row per (run, metric, window) | value, unit, basis |
| `experiment_result` | one row per (contrast, metric) | estimate, CI low and high, p value, adjusted p, n pairs, effect size, flag (primary, secondary, exploratory) |

These sit beside the normal fact tables ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md)); the per-run tables already hold every event and snapshot, so deeper queries can join `experiment_run` to them.

## 4. Mechanics and algorithms

### 4.1 The experiment file
JSON (YAML is allowed and converted). The runner refuses a file whose `planHash` field does not match the content, once the file has been registered.

```json
{
  "schemaVersion": 1,
  "id": "band-huts-communal-vs-private-v1",
  "title": "Communal against private hut ownership in the Band age",
  "hypothesis": [
    "H1: Communal huts change child mortality to year 1 (POP-07) compared with private huts.",
    "H2: Communal huts reduce the wealth Gini (INQ-03) at year 5 compared with private huts.",
    "H3: Communal huts change food stocks in days of cover (LIV-01)."
  ],
  "scenario": { "name": "band_village", "version": 3 },
  "seeds": { "start": 1000, "count": 60 },
  "burnInDays": 30,
  "horizonYears": 6,
  "sampleEveryDays": 1,
  "arms": [
    { "name": "private", "overrides": { "law.hutOwnership": "private" } },
    { "name": "communal", "overrides": { "law.hutOwnership": "communal" } }
  ],
  "environments": [
    { "name": "fertile",   "overrides": { "map.fertility": 120 } },
    { "name": "average",   "overrides": { "map.fertility": 100 } },
    { "name": "poor",      "overrides": { "map.fertility": 80 }  },
    { "name": "shock-year3", "overrides": { "shocks": ["famine@year3"] } }
  ],
  "metrics": {
    "primary": [
      { "kpi": "POP-07", "basis": "actual", "window": "run", "direction": "either" },
      { "kpi": "INQ-03", "basis": "actual", "window": "year5", "direction": "decrease" }
    ],
    "secondary": [
      { "kpi": "POP-01", "window": "year6" },
      { "kpi": "LIV-01", "window": "after_burn_in" },
      { "kpi": "COO-01", "window": "after_burn_in" },
      { "kpi": "COO-03", "window": "after_burn_in" },
      { "kpi": "INQ-11", "window": "year6" },
      { "kpi": "POP-06", "window": "run" }
    ]
  },
  "analysis": {
    "design": "paired_by_seed",
    "primaryTest": "paired_bootstrap_mean_difference",
    "bootstrapResamples": 10000,
    "confidenceLevel": 0.95,
    "multipleComparison": { "primary": "holm", "secondary": "benjamini_hochberg", "alpha": 0.05 },
    "environments": "report_each_and_pooled_with_environment_as_block",
    "minimumEffectOfInterest": { "POP-07": 50, "INQ-03": 50 },
    "stopping": "fixed_n"
  },
  "sensitivity": {
    "parameters": [
      { "path": "tuning.fertilityRate", "values": [80, 100, 120] },
      { "path": "tuning.huntingYield", "values": [90, 100, 110] }
    ],
    "design": "latin_hypercube", "points": 12
  },
  "planHash": "sha256:..."
}
```
Rules for the file:
- Overrides use dotted paths into `SimTuning`, laws and configuration; the runner rejects unknown paths and any path that touches RNG seeds.
- `arms[0]` is the control.
- Every metric is a KPI id from [KPI_CATALOGUE.md](KPI_CATALOGUE.md) (for example `POP-07`, `INQ-03`), optionally with a breakdown filter such as `POP-03[cause=Starvation]`, or one of the run descriptors in section 4.10. The runner checks the id and version.
- Values and effects are in the catalogue's integer units (permille for shares, so a minimum effect of 50 is 5 percentage points).
- Planned contrasts are implied: each non-control arm against the control. Extra contrasts must be listed, or they count as exploratory.

### 4.2 Running: the `experiment` command
```text
god-runner experiment <file> [--out DIR] [--workers N] [--warehouse FILE] [--dry-run] [--resume]
```
Steps:
1. **Validate** the file (paths, metrics, hash). `--dry-run` prints the run matrix and the estimated cost.
2. **Expand** to a run matrix: for each environment, for each seed, for each arm in the order of the file. Run id = `exp:{id}:{env}:{seed}:{arm}`.
3. **Run** in parallel across workers ([HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md)). Each run builds its sim from `(scenario, seed, env overrides, arm overrides)` and runs to the horizon with recording at the `experiment` level (snapshots and the metric-relevant events, [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md#41-recording-levels)). The scheduling order of runs never matters because runs share nothing.
4. **Record** the final state hash of every run. A rerun with the same file must reproduce every hash (acceptance test).
5. **Compute** metrics for each run from the warehouse (the same SQL as the KPI definitions) and write `experiment_metric`.
6. **Analyse** according to the plan and write `experiment_result` and a report: `report.md` (tables, plots, the pre-registered claims with outcomes) and `results.json`.
7. `--resume` skips runs whose hash and metrics are already stored. A run whose stored plan hash differs is refused.

Outputs in `--out`: `plan.json` (as registered), `runs.csv`, `metrics.csv`, `results.csv`, `report.md`, `warehouse.sqlite` (or Parquet), and `environment.txt` (versions, hardware, commit).

### 4.3 Common random numbers and pairing
- **Same seeds across arms.** For seed `s`, every arm starts from the same world: the same map, the same initial NPCs (same genomes and traits), the same forked streams `Rng.Fork("npc", id)`, `Rng.Fork("inst", id)` ([DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#3-layers-and-determinism-unchanged-from-stages-13)).
- **Why it works and where it stops.** Pairing removes the variance from starting conditions. Once the arms differ in behaviour, their streams stay aligned (each draw is keyed by entity, not by order of consumption), but the histories diverge, so the pairing helps most early and least in long runs. The analysis does not assume the variance reduction, and it measures it: the report prints the correlation between arms across seeds for each metric. A low correlation means pairing bought little, and the power calculation should have used the unpaired variance.
- **Streams keyed by entity and purpose**, not drawn from a shared sequence, is a requirement here. If a treatment changes how many draws one NPC makes, other NPCs' streams must not shift.
- **Treatments applied at a time, not at the start,** (for example a tax introduced at year 2) use a shared pre-treatment history: the run is saved at the treatment day and each arm resumes from that same save. The pre-treatment part is then identical by construction.

### 4.4 Sample sizes and power
For a two-arm comparison with a continuous metric, a rule of thumb for the number of runs per arm at 5% significance (two-sided) and 80% power:
`n ≈ 15.7 x (sd / delta)^2` for unpaired runs, and `n ≈ 7.85 x (sd_diff / delta)^2` for paired runs,
where `delta` is the smallest effect worth detecting and `sd_diff` the sd of the paired differences.

| delta / sd | runs per arm, unpaired | pairs, if pairing halves the variance |
|---|---|---|
| 1.0 | 16 | 8 |
| 0.5 | 63 | 32 |
| 0.3 | 175 | 88 |
| 0.2 | 393 | 197 |

- **Pilot first.** A pilot of 20 seeds per arm gives a first sd estimate. Pilot seeds are **not reused** in the final run (a separate seed range), and the pilot is not part of the reported test.
- **Power is stated in the file** as the minimum effect of interest and the chosen n. The report says whether the achieved CI width meets the plan.
- **Proportion metrics** (child survival) use per-run proportions as the outcome, because the run, not the child, is the independent unit. Many children in one run are not independent observations. Never pool individuals across runs as if they were independent samples.
- **Rare events** (extinction, collapse) need larger n: for a 5% event rate, about 60 runs per arm give an expected 3 events, so detect only large differences. Report counts and exact intervals.
- **Multiple environments** multiply the cost: `seeds x arms x environments`. A pilot sets the budget. Headless speeds matter ([HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md)): a 6-year Band run is about 120 real minutes at 1x, so only the fast runner makes this feasible.

### 4.5 Burn-in, horizon and generations
- **Burn-in.** The first `burnInDays` of each run are excluded from outcome windows. The initial population is not at its stable age structure, and early flows (births, stock build-up) reflect the scenario's start, not the treatment. The burn-in length is chosen by looking at **control-arm runs from a pilot** only, as the time at which the population age structure and stocks stop trending, and it is written in the file. It is never tuned on treatment effects.
- **Treatment start.** Either at day 0 (a different society from the start) or at a day after burn-in (a policy change in an established society). They answer different questions, so the file says which.
- **Horizon.** The horizon is at least `G` generations of the measured outcome. A generation is about 25-35 real minutes at 1x, or roughly 1.3-1.7 sim years at `LifeScale 100` ([time model](DESIGN_CONVENTIONS.md#4-time-model)). Social mobility and wealth across generations need at least 3 generations (about 4-5 sim years after burn-in). Cultural and institutional drift can take more.
- **`LifeScale`** can be set to compress generations. It is an environment factor, and a result found at one `LifeScale` is checked at another, because it changes the relative speed of learning, work and life.
- **Windows.** Each metric names its window: `run`, `after_burn_in`, `yearN`, `last_generation`, `generation_k`. Windowed values are computed per run, then analysed across runs.
- **Time series.** Besides end-of-run scalars, the report shows per-arm mean and interval bands over time (for example population by year), because a policy can differ in path and not in the end point.

### 4.6 Effect sizes, intervals and tests
- **Estimand:** for each planned contrast and metric, the mean of the within-seed difference (treatment minus control) over seeds, plus the standardised effect (difference over the sd of differences).
- **Interval:** a percentile **paired bootstrap** (resample seeds with replacement, `10,000` resamples, the resampling itself seeded and recorded) for the 95% interval. For small n (below 20 pairs) or heavily skewed metrics, also report a median difference with a sign-flip permutation test.
- **P-values:** from the paired permutation (sign-flip) test, or the bootstrap, as pre-registered. The report always shows the interval first and the p-value second. A p-value alone is never the headline.
- **Equivalence and null results:** when the hypothesis is "no meaningful difference", the plan uses a pre-registered equivalence margin and a two one-sided test. "Not significant" is not reported as "no effect".
- **Practical significance:** compare the interval with `minimumEffectOfInterest`. A tiny but significant difference is reported as tiny.
- **Several environments:** results are reported per environment and pooled with environment as a block (stratified paired bootstrap). A claim that holds only in some environments is stated as such.
- **Generations as repeated measures:** within a run, generation-by-generation values are correlated, so tests on them use the run as the unit (summaries per run) or a model with a run-level random effect. Never treat generation-years as independent.

### 4.7 Multiple comparisons
- **Primary metrics** (few, named in advance): **Holm** step-down correction across all primary hypotheses of the experiment, controlling the family-wise error rate at alpha.
- **Secondary and exploratory metrics:** the **Benjamini-Hochberg** false discovery rate at `q = 0.10`. They are labelled "hypothesis-generating". Exploratory findings need a **confirmation experiment** with a new seed range, pre-registered, before the project calls them results.
- **All arms:** with more than two arms, the planned contrasts are listed and corrected together. An all-pairs comparison is exploratory unless listed.
- **All environments:** a contrast in each environment counts as a separate test inside the family, unless a pooled test was the pre-registered primary.
- The report prints both raw and adjusted p-values, and the number of tests run, **including those in the plan that were not significant**.

### 4.8 Sensitivity analysis and environment variation
- **Tuning sensitivity.** The simulation has many tuning parameters (fertility, yields, drive rates, learning rates, perception ranges). A conclusion that vanishes when a parameter moves by 20% is fragile. The plan lists parameters and ranges. The runner runs the primary contrast at each design point (one-at-a-time for a few key parameters, or a Latin hypercube for many) and reports the primary effect as a function of the parameters: a table, plus a fraction of design points where the sign agrees with the main result.
- **Blind tuning.** Parameters are tuned **before** the hypothesis is registered, on control-arm behaviour only (plausible population, no early collapse), by someone or a script that does not look at the treatment contrast. Once an experiment is registered, tuning is frozen. If tuning must change, the experiment is re-registered as a new version and the old result stays in the record.
- **Environment variation.** Factors that matter for institutional comparisons: land fertility, resource spread (clustered or even), shock schedules (famine, plague, raids, flood), starting population size, number of factions and their distance, and climate. The file lists an environment grid. A policy that wins only in one environment is reported with that qualification.
- **Opponent and neighbour variation** for factions: results against scripted, benchmark and trained opponents are separate rows.
- **Policy-population variation:** which individual strategies and personalities exist matters (a society of mostly cautious NPCs responds differently to a tax). Initial trait distributions are an environment factor.

### 4.9 Causal experiments and descriptive analysis
| | Causal experiment | Descriptive analysis |
|---|---|---|
| Design | treatments assigned by the experimenter to otherwise identical runs, paired or randomised | observation within runs or across uncontrolled runs |
| Example | "Does a 10% tax change child survival, other things equal?" | "In these runs, richer NPCs live longer." |
| Claim allowed | a difference in outcome is caused by the treatment, in this model, within these environments | an association exists |
| Not allowed | generalising outside the tested environments without evidence | any causal claim, because wealth, health, age, household and luck are tangled and feed back into each other |
| Output label | `causal` | `descriptive` |

**Rules:**
- Every figure and table in a report carries one of the two labels.
- Descriptive analyses are useful for **generating** hypotheses ("richer NPCs live longer, so test a wealth transfer"), and for **explaining** mechanisms in a causal result (who gained, through which channel). They are never offered as proof of a mechanism.
- **Within-run confounding examples to avoid:** survivorship (those still alive at year 5 differ from the start), reverse causation (people become rich because they are healthy), selection into institutions (those who use a healer are sicker), and endogenous policy (a state that raises taxes in a crisis).
- **Mechanism checks inside a causal experiment** are descriptive but pre-registered: for example "if communal huts raise child survival, the effect should be larger in poor environments and show up first in households with many children." Support is evidence, not proof.
- **Mediation** (did the tax work through food stocks?) can be tested by a second experiment that fixes the mediator, if the sim allows it (an intervention that holds food stock constant).

### 4.10 Outcomes to record
Ids refer to [KPI_CATALOGUE.md](KPI_CATALOGUE.md), which owns exact definitions, denominators and the actual against reported pairs. Ids marked *(proposed)* are being added to the catalogue.

| Outcome family | KPI ids |
|---|---|
| Survival | `POP-01` population, `POP-03` deaths (and `POP-03[cause=Starvation]`), `POP-07` child mortality, `POP-06` life expectancy at birth |
| Welfare | `HLT-01` hunger rate, `HLT-02` undernourishment depth, `LIV-03` housing, `HLT-05` treatment coverage, `LIV-06` drive levels (five separate values, never combined) |
| Time use and care | `TIM-01` time use (paid work, independent production, communal work, unpaid care, subsistence, leisure), `TIM-02` unpaid care hours |
| Productivity | `EMP-05` productivity, `EMP-01` participation, `EMP-04` underemployment, `LIV-01` food stocks (days of cover) |
| Inequality | `INQ-03` wealth Gini, `INQ-04` income Gini, `INQ-05` top shares, `INQ-11` class composition (measured relation to property and labour) |
| Social mobility | `INQ-07` rank-rank slope, `INQ-08` transition matrix (the exit share of the bottom row stands in for "born poor who exit"), `INQ-09` within-life mobility |
| Population dynamics | `POP-02` births, `POP-09` net migration, `POP-10` age structure, `POP-11` household structure, `POP-08` dependency ratio |
| Institutional stability | `INS-01` institution survival *(proposed)*, `INS-02` staff turnover *(proposed)*, `FIS-10` staff pay arrears, `INS-03` rule changes *(proposed)*, and the `gap` basis of any reported KPI (the Goodhart gap) |
| Cooperation | `COO-01` promise keeping, `COO-02` barter completion, `COO-03` gifts and sharing, `COO-04` mean interpersonal trust *(proposed)*, `CRM-01` offences (theft; violence), `CRM-06` violent deaths |
| Environmental sustainability | `ENV-01` resource stock ratio, `ENV-02` sustainability ratio, `ENV-03` depleted hexes |

**Run descriptors** (not KPIs; computed by the runner per run from `fact_birth`, `fact_death` and `dim_run`): `time_to_extinction` (day the last NPC of a faction died, or censored at the horizon) and `generations_elapsed` (maximum generation depth of anyone born in the run). They may be used as metrics.

Each metric is stored per run at its window. Where an institution's reported KPI exists, store both `actual` and `reported`, and the **gap** is itself an outcome.

### 4.11 Experiment catalogue (planned suite; most are stage 6 or later)
Each row is a family with arms. The designs below are examples, not predictions of results.

| Family | Arms (examples) | Primary outcomes | Needs |
|---|---|---|---|
| Healthcare funding | none; communal care by custom; state-funded healer; private fee-for-service; cooperative clinic | `POP-06`, `POP-07`, `HLT-05`, `EMP-05`, `FIS-04` spending | healer (M12), hospitals (stage 6) |
| Education provision | none; family teaching only; state school; private tutoring; cooperative school | `EDU-02` mean skill, `EMP-05`, `INQ-07`, `EDU-08` trait prevalence | teaching ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)), schools (stage 5-6) |
| Taxation | no tax; flat tax on harvest; tax on trade; progressive in kind; tax with state purchase offers | `INQ-03`, `EMP-05`, `POP-01`, the `gap` basis, `FIS-02` tax compliance | state (M6) |
| Ownership | private, state, communal, cooperative ownership of huts, stores and workshops | `INQ-03`, `EMP-05`, `COO-01`..`COO-03`, `INQ-11`, `ENV-01` | ownership records (M6), enterprises (M8) |
| Criminal justice | no formal justice; communal mediation; elder judgement; courts and police with fines; prisons | `CRM-01`, `COO-04`, `CRM-06`, `CRT-06` cost of justice, `CRT-07` error rate | mediation (M12), courts (stage 6) |
| Financial regulation | unregulated lending; usury limits; reserve rules; deposit guarantee | `FIN-01`, `FIN-03`, `INQ-03`, `INS-01`, crisis frequency (KPI to be defined with banks) | money and banks (stage 6-7) |
| Welfare provision | none; kin only; granary relief; cash-like transfers (later); conditional transfers | `HLT-01`, `POP-03[cause=Starvation]`, `EMP-05`, `POP-08`, `POP-01` | shared store (M12), granary (stage 5) |
| Workplace safety | no rule; inspection by elders; licensed inspectors with fines; cooperative self-regulation | `HLT-03` injury incidence, `EMP-05`, enterprise founding rate (KPI to be defined with enterprises) | enterprises (M8), inspectors (stage 6) |
| Environmental regulation | open access; communal rules (Ostrom-style monitoring and graduated sanctions); state quotas; private ownership of the resource | `ENV-01`, `ENV-02`, `ENV-06` commons compliance, `CRM-01` | commons ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)); agency (stage 7) |
| Automation and redistribution | tools and machines available or not; with and without profit sharing, wage floor or public dividend | `EMP-04`, `INQ-03`, `EMP-05`, `INQ-11`, `TRU-07` collective action | machines and enterprises (stage 6-7) |
| Policy source | scripted faction AI against trained faction; benchmark individual strategies against each other | victory rate, prosperity, the same outcome families | M14, M15 |

For each family, the design must record that results hold **in this model** and list the mechanics that make an effect possible. A family's file lists its mechanisms and its measures, as the conventions require: say which mechanics make an outcome possible, and how to measure whether it happened.

### 4.12 Neutrality checklist
Every experiment file includes this checklist, and the report answers it. A box left unchecked is explained.

- [ ] **No theory is built to win.** No arm was tuned, patched or special-cased to make it look good or bad. Arms differ only in the listed overrides.
- [ ] **Hypotheses and metrics fixed in advance.** `planHash` registered before the first run; primary metrics and directions listed; any later addition marked exploratory.
- [ ] **Null and negative results are reported,** with intervals, in the same report and at the same prominence.
- [ ] **The result holds across environments.** The primary contrast is shown in every environment of the grid, and the report says where it fails.
- [ ] **The result holds across tunings.** The sensitivity analysis is complete and the fraction of design points agreeing with the main result is stated.
- [ ] **Tuning was blind to the hypothesis.** Parameters were tuned on control-arm plausibility before registration, by criteria written down in the file, and were frozen.
- [ ] **Outcomes include losers and winners.** Distributional results (who gains and who loses, by class and age) accompany any aggregate result, because an aggregate can hide a harmed group.
- [ ] **Actual and reported outcomes both shown** for institution-run services.
- [ ] **Causal and descriptive results are labelled.**
- [ ] **No outcome metric was used as a reward** for a trained policy in the experiment, unless stated ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)).
- [ ] **Model limits stated.** The report says what the model leaves out (money before stage 6, for example) and does not draw a real-world policy conclusion.
- [ ] **Code and data available.** The runner version, commit, seeds and warehouse are kept, so anyone can rerun the experiment and get the same hashes.
- [ ] **No hidden composite (owner decision Q23).** There is no universal composite wellbeing score. If this experiment uses a composite, it is defined in the plan file with its **components, weights and limitations** (what it leaves out and whose values the weights express), it is labelled as this experiment's own construct, and results for every component are shown beside it. No composite decides which society "succeeds".

### 4.13 Example analyses
**SQL on the warehouse** (DuckDB or SQLite). Table and column names are those published in [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md#11-the-published-table-list); analysis SQL runs outside the sim, so it may use floating point.

Paired difference per seed for one metric:
```sql
WITH m AS (
  SELECT r.seed, r.env, r.arm, em.value
  FROM experiment_run r
  JOIN experiment_metric em ON em.run_id = r.run_id
  WHERE r.experiment_id = :exp AND em.kpi = 'POP-07' AND em.window = 'run'
)
SELECT t.seed, t.env, t.value - c.value AS diff
FROM m t JOIN m c ON c.seed = t.seed AND c.env = t.env AND c.arm = 'private'
WHERE t.arm = 'communal'
ORDER BY t.env, t.seed;
```
Descriptive (not causal): wealth and lifespan within the control runs.
```sql
-- label: DESCRIPTIVE. Association only; wealth and health influence each other.
WITH w AS (                        -- mean lifetime wealth per person, reference units
  SELECT run_key, person_key, AVG(wealth_value_ref) AS mean_wealth
  FROM fact_person_day GROUP BY run_key, person_key
), d AS (
  SELECT p.run_key, r.life_scale, r.ticks_per_day, r.days_per_year,
         p.death_tick - p.birth_tick AS age_ticks,
         NTILE(10) OVER (PARTITION BY p.run_key ORDER BY w.mean_wealth) AS wealth_decile
  FROM fact_death fd
  JOIN dim_person p ON p.person_key = fd.person_key
  JOIN dim_run r ON r.run_key = p.run_key
  JOIN w ON w.person_key = p.person_key
  WHERE r.experiment_id = :exp AND r.arm = 'private'
)
SELECT wealth_decile,
       AVG(age_ticks * life_scale / 100.0 / (ticks_per_day * days_per_year)) AS mean_age_at_death_years,
       COUNT(*) AS n
FROM d GROUP BY wealth_decile ORDER BY wealth_decile;
```

**Python: paired bootstrap with Holm correction.**
```python
import numpy as np, pandas as pd

def paired_bootstrap(diff, resamples=10_000, seed=12345, level=0.95):
    rng = np.random.default_rng(seed)               # the analysis seed is recorded in the report
    n = len(diff)
    idx = rng.integers(0, n, size=(resamples, n))
    means = diff[idx].mean(axis=1)
    lo, hi = np.quantile(means, [(1 - level) / 2, 1 - (1 - level) / 2])
    return diff.mean(), lo, hi, diff.mean() / diff.std(ddof=1)

def signflip_p(diff, resamples=20_000, seed=54321):
    rng = np.random.default_rng(seed)
    obs = abs(diff.mean())
    signs = rng.choice([-1, 1], size=(resamples, len(diff)))
    return ((np.abs((signs * diff).mean(axis=1)) >= obs).sum() + 1) / (resamples + 1)

def holm(pvals, alpha=0.05):
    order = np.argsort(pvals); m = len(pvals); adj = np.empty(m); run = 0.0
    for rank, i in enumerate(order):
        run = max(run, (m - rank) * pvals[i]); adj[i] = min(1.0, run)
    return adj

df = pd.read_csv("metrics.csv")                      # columns: seed, env, arm, kpi, value
piv = df.pivot_table(index=["kpi", "env", "seed"], columns="arm", values="value").reset_index()
piv["diff"] = piv["communal"] - piv["private"]
rows = []
for (kpi, env), g in piv.groupby(["kpi", "env"]):
    d = g["diff"].to_numpy()
    est, lo, hi, dz = paired_bootstrap(d)
    rows.append(dict(kpi=kpi, env=env, n=len(d), est=est, lo=lo, hi=hi, dz=dz, p=signflip_p(d)))
res = pd.DataFrame(rows)
primary = res.kpi.isin(["POP-07", "INQ-03"])
res.loc[primary, "p_holm"] = holm(res.loc[primary, "p"].to_numpy())
print(res.sort_values(["kpi", "env"]).to_string(index=False))
```
(The plan's Holm family should be per hypothesis across environments, as pre-registered. This sketch applies one family for brevity.)

**Python: A/A test harness check.**
```python
# Under identical arms, the share of significant results should be near alpha.
false_pos = (res["p"] < 0.05).mean()    # over many metrics and environments
```

## 5. Interactions with other systems
- **Runner:** owns scenarios, parallelism, saves and hashes. The `experiment` command is part of it ([HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md)). Runs use the `experiment` recording level.
- **Warehouse:** stores every run and the four result tables; KPIs are computed with the catalogue's SQL ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md), [KPI_CATALOGUE.md](KPI_CATALOGUE.md)).
- **Institutions and state:** treatments are institution and state configuration ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md), [STATE_DESIGN.md](STATE_DESIGN.md)). Institution managers keep their own incentives, so an arm tests an arrangement including its staff's behaviour.
- **Information:** the player-visible reports are reported KPIs; the experiment records both actual and reported ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)).
- **Policies and training:** benchmark and trained policies are arms. Trained models need the `verified` stamp ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)).
- **Lab:** the maths-only lab has its own harness for strategy tournaments ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)); the experiment runner can reuse the seed and statistics code.
- **Lifetime learning:** NPCs learn in every run. Arms are compared at the population level, and an experiment may add a "learning off" arm to measure how much learning mediates a treatment ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)).

## 6. Player and view
- **In the game:** nothing in stage 4. The player's reports panel reads reported KPIs through `FactionKnowledgeView`, not experiment outputs.
- **Developer or analyst view (later):** a notebook template and a small report viewer: plots of arm means with bands over time, forest plots of effects with intervals, sensitivity heatmaps.
- **Sandbox link (later):** a finished experiment can be loaded as a scenario, so a player can try an arrangement that the experiment tested. The game never tells the player what is best.

## 7. Telemetry
- **Domain events used:** `PolicyChanged` (treatment start), `KpiReported`, and the events behind each KPI (`NpcBorn`, `NpcDied`, `TaxCollected`, `OwnershipChanged`, `CaseDecided` and others). No new domain events.
- **Experiment tables** (section 3.3) are written by the runner, not the sim.
- **Run attributes:** experiment id, arm, seed, environment, plan hash, tuning hash, model hashes.
- **Recording level `experiment`:** daily person, institution and faction snapshots plus the events needed for the outcome KPIs, with the heaviest tables off. Recording changes no hash.

## 8. Stage 4 minimum vs later stages

**Stage 4 (M13 and M14):**
- The `experiment` command with the file format of section 4.1, supporting `paired_by_seed`, burn-in, windows, a few environments, and the outputs of section 4.2.
- Analysis: paired bootstrap, sign-flip test, Holm and Benjamini-Hochberg, a plain-text and Markdown report.
- The neutrality checklist as part of the report template.
- KPIs from the Stage 4 Band set of the catalogue: `POP-01`, `POP-02`, `POP-03`, `POP-07`, `HLT-01`, `LIV-01`, `INQ-03`, `INQ-11` (class composition, recorded from M13 by [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)), `TRU-01`, actual-only `COO-01` and `COO-03`, and the `gap` basis where an institution reports.
- **Demonstration experiments (two):**
  1. Communal against private hut ownership on the Band-age outcomes above (needs M4, M6, M8 basics, M12).
  2. Tax rate comparison (for example 0%, 10% and 20% of the harvest) on productivity, inequality and population (needs M6, M14).
- The harness test suite of section 9, including the A/A test and a planted effect.

**Later:**
- **Stage 5:** cooperatives, collective action and granary experiments; schools-lite.
- **Stage 6 (the suite):** healthcare, education, taxation at scale, criminal justice, welfare, workplace safety, with Goodhart-prone institution managers; sensitivity design with Latin hypercubes at scale; a notebook toolkit.
- **Stage 7:** financial regulation, environmental regulation, automation and redistribution; experiments with trained policies; limited-information institutions.
- Adaptive designs (sequential tests, bandit allocation) are out of scope. Fixed-n designs keep the statistics honest.

## 9. Tests and acceptance criteria

| Test | Criterion |
|---|---|
| `Experiment_Rerun_Identical` | running the same file twice gives identical per-run state hashes, identical metrics, and an identical `results.csv` (bitwise, with the recorded analysis seed) |
| `Experiment_Order_Independent` | running with 1 worker and with 8 workers, or with arms in a different order, gives identical results |
| `Experiment_PlanHash` | changing any field after registration without re-registering makes the runner refuse; adding an unlisted metric is flagged exploratory in the report |
| `Experiment_UnknownPath` | an override path that does not exist, or that touches seeds, is rejected before any run |
| `Experiment_CRN_Alignment` | with two arms that differ only in a parameter not used until day 10, the first 10 days of every seed are identical across arms (hash at day 10 equal) |
| `Experiment_RecordingNoEffect` | final hashes equal with recording level `none` and `experiment` |
| `AA_Test_FalsePositiveRate` | the two arms use **different, independent seed sets** (the same configuration): over 20 repeated A/A experiments of 40 runs per arm, the unadjusted share of significant tests at 0.05 lies between 2% and 9%, and with Holm correction the family-wise error is at most 6% |
| `AA_Identical_Hash` | the two arms use the same seeds and no overrides: every pair of run hashes is **equal** (the same run twice), and every estimated difference is exactly 0 |
| `Planted_Effect_Detected` | a test-only override that adds a known effect (for example +10% food per forage action, or a death-rate multiplier of 0.8) is detected with power of at least 80% at the planned n; the estimated effect lies within 20% of the planted effect, and the interval contains it in at least 90% of 50 repeats |
| `Planted_Null_Metric` | the same planted run shows no significant change (after correction) in a metric the effect cannot influence |
| `Holm_BH_Known_Vectors` | the correction functions give the textbook adjusted values for fixed input vectors |
| `Bootstrap_Reproducible` | the bootstrap interval for a fixed input is the same under the recorded seed and differs by under 1% in width across 10 other seeds |
| `Power_Calculator` | the sample-size helper returns the table of section 4.4 within rounding for known inputs |
| `Report_Contains_Nulls` | a report for an experiment with only null results lists every pre-registered primary metric with its interval and adjusted p-value (no omitted rows) |
| `Report_Labels` | every table and figure in the report carries `causal` or `descriptive`; a report check fails if a figure from within-run association lacks the label |
| `Demo_Hut_Experiment_Runs` | the communal against private hut experiment runs to completion with at least 30 seeds per arm in a documented time budget and produces the report |
| `Composite_Declared` | a plan that uses a composite without listing components, weights and limitations is refused at registration; a report that shows a composite also shows each component's result (owner decision Q23) |
| `Sensitivity_Runs` | a three-parameter one-at-a-time sensitivity sweep produces the sign-agreement table |

## 10. Open questions
No owner question remains.

**Owner decision Q23 (27 Nov 2026, [conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)): no universal composite score.** Report the underlying measures separately (health, nutrition, life expectancy, security, comfort, leisure, education, autonomy, material living standards, inequality, social relationships, environmental sustainability; [KPI_CATALOGUE.md](KPI_CATALOGUE.md#wellbeing-dimensions-reported-separately)). Experiments may use explicitly defined composites with documented components, weights and limitations (checklist 4.12, `Composite_Declared`). There is no hidden political or moral weighting of which societies succeed.

Settled here, with defaults:
- **Seed budget.** Set from the runner's measured speed (`bench_npc_500`, [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md)) when the harness is built in M13: the default plan is 60 seeds per arm, 2 arms and up to 3 environments, with a pilot of 20 seeds per arm first.
- **Pairing.** Whether pairing on seed reduces variance is measured: every report prints the correlation across arms (section 4.3).
- **Pre-registration** is enforced by the plan hash plus the commit hash of the plan file in the repository before the first run.
- **Treatments at a date** use saved states (section 4.3), so the pre-treatment history is identical by construction. A scheduled command is allowed only for treatments at day 0.
