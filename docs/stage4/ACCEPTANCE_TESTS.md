# Stage 4 acceptance tests (the gate for every milestone)

This document gathers the tests and acceptance criteria from every design document in `docs/stage4/` and assigns them to the official milestones M0 to M15 ([DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md) section 10). A milestone is **done** only when:
- every criterion listed for it passes;
- every **standing gate** in section 2 passes;
- its short manual checks are signed off by the owner.

Where a source document still uses the plan's old milestone numbers, this document uses the conventions' numbers. Where a source gives a range of milestones, the assignment here is a proposal and is marked *(assigned)*.

**Updated 27 Nov 2026** for the owner's answers and further requirements ([conventions §13 and §14](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)): M13 is split into **M13a** (warehouse core) and **M13b** (institution KPIs and reports); the milestones are grouped into the functional checkpoints **4A–4F** and two functional releases (section 3.0); the owner's eight learning scenarios (LS1–LS8, plus LS9) and the tests for each answer are added; section 4.3 maps the owner's evidence list to tests. **Coverage is never reduced**: no criterion was removed or loosened in this update.

Contents: 1 How tests run · 2 Standing gates · 3 Milestones M0 to M15 (3.0 checkpoints and releases) · 4 Traceability · 5 Conflicts between documents

---

## 1. How tests run

### 1.1 Suites and where tests live

| Short name | What | Where | Run by |
|---|---|---|---|
| **EM** | Sim unit, property and reflection tests (NUnit). One `<Module>Tests.cs` per module, for example `PopulationTests.cs`, `LearningTests.cs`, `StateTests.cs` | `Assets/God/Tests/EditMode` (assembly `God.Sim.Tests`) | Unity EditMode, **and** `dotnet test` (the `DotNet/God.Sim.Tests` project compiles the same files, so every EM test must pass in both) |
| **RT** | Runner and multi-seed scenario tests: determinism matrix, parallel runs, replay, benchmarks, scenario checks | `DotNet/God.Runner.Tests` (new at M0, per [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md)) | `dotnet test DotNet -c Release` |
| **PM** | Unity views: HUD, boot scene, views, screenshots | `Assets/God/Tests/PlayMode` (assembly `God.Game.Tests`) | Unity PlayMode |
| **LAB** | Maths-only game-theory experiments with the book's payoffs | `Sim/Lab`, run by `God.Runner lab` | `lab all --check` and RT |
| **NIGHT** | Long or many-seed runs that do not fit a commit gate (200 sim years, 20 seeds × 10 years, Unity against runner) | runner `batch` and `verify` on scenario files | scheduled, and always before a milestone is declared done |

Tests are written before the code they test (`docs/testing/00_test_first_policy.md`). A test id such as `NPC_LEARNING L3` names the source row.

### 1.2 Commands

| Purpose | Command | Pass means |
|---|---|---|
| All C# tests | `dotnet test DotNet -c Release` | exit 0, no skipped test without a reason in the source row |
| Unity EditMode | `unity command test run` with the EditMode platform (one editor per folder; close it afterwards; see `CLAUDE.md`) | all green |
| Unity PlayMode | the same command with the PlayMode platform; never run a built player windowed in automation, use `-batchmode -nographics` | all green |
| Determinism checks | `God.Runner verify FILE` with `--parallel`, `--save-mid`, `--record-matrix`, `--against-unity`, `--every N` | exit 0; exit 4 means a checkpoint diverged (names the first bad tick and module) |
| Maths lab | `God.Runner lab all --check` | exit 0 and finishes in **under 60 s on one core**; exit 1 means a result outside tolerance |
| Experiments | `God.Runner experiment PLAN` (arms × seeds × environments) | the run completes with its control and writes a report; exit 0 |
| Performance | `God.Runner bench --compare baseline.json` (reference machine only) | exit 0; exit 7 means more than 15% below baseline or below a minimum |
| Scenario batch | `God.Runner batch SCENARIO --seeds LIST --workers N` | exit 0; the highest-priority exit code of the runs |

Exit codes: 0 success, 1 check failed, 2 usage, 3 invalid input, 4 determinism divergence, 5 invariant failed, 6 unhandled exception, 7 performance missed, 8 I/O or recorder error (sim result still valid), 130 interrupted. Any of 4, 5 and 6 blocks the milestone outright.

### 1.3 Kinds of criterion (used in every table below)

| Kind | Asserts | Fails the build when |
|---|---|---|
| **Unit** | An exact value from a formula or a fixture (golden values, exact equalities) | any difference |
| **Inv** | An invariant or equality over a whole run (conservation, hash equality, no leaks, reflection) | any violation, on any seed |
| **Lab** | A **numeric** result of a maths experiment with the book's payoffs, with a tolerance. No island, no consequences from the game | outside tolerance |
| **Mech** | On the island: a **mechanism** works (the treatment changed the payoff or the cost it claims to; a behaviour is possible and bounded; an incentive moves a choice in the stated direction on fixed controlled seeds) | the mechanism fails |
| **Dir** | On the island: a **direction of effect** over many seeds (sign, with the stated test); never a size | wrong sign beyond the stated significance, or the seed set is not fully run |
| **Hyp** | On the island: an **outcome hypothesis**, **recorded not asserted**. The test passes if it runs, with control, seeds and an interval, and writes the report | only if it does not run or does not report |
| **Perf** | A time, memory or size budget (reference machine for time) | budget missed |
| **Man** | A manual check by the owner | owner does not sign off |

**The owner's three tiers (27 Nov).** Every criterion belongs to exactly one tier, and reports keep them apart:

| Tier | Kinds | What it proves |
|---|---|---|
| **Deterministic unit tests** | Unit, Inv (and Perf hard caps) | exact values and invariants that hold on every run and every seed |
| **Controlled model tests** | Lab, Mech | a mechanism or a maths result works in a controlled setting (fixed seeds, scripted or benchmark counterparts) |
| **Statistical island experiments** | Dir, Hyp | directions and recorded hypotheses over many seeds on the real island |

### 1.4 Rules for statistical and many-seed tests

1. **Fixed seed sets, named in advance.** Seed lists are `S10` = 1–10, `S20` = 1–20, `S30` = 1–30, `S50` = 1–50 and `S100` = 1–100 (and so on), committed with the test. A test never picks seeds after seeing results, never retries, and never drops a seed. A failure is therefore reproducible from its seed and tick.
2. **Thresholds are fixed in advance.** The numbers in this document were set before any run. A threshold changes only by a commit that edits this document and states why (a documented bug, or balance unrelated to the effect), never to make a run pass.
3. **Calibration targets are pinned once.** Where a source says "baseline recorded at the first run" (culture, estates, class measures, benchmarks), the first accepted run is stored as a baseline file. From then on the baseline is a **regression bound of ±15%**, and re-baselining needs a commit with a reason.
4. **A measured hypothesis is recorded, not asserted.** A **Hyp** test has no assertion on the sign or size of an effect. It passes when it runs to completion with its control, uses the named seeds, and writes the mean paired difference, a bootstrap 95% interval and the share of seeds with the same sign. "No effect" and "the opposite effect" are valid results and are published as found.
5. **No tuning to get an effect.** If an expected direction does not appear, the finding is recorded and the model reviewed. Parameters change only for a documented bug or for balance reasons unrelated to the effect. No rule may be added that assigns an activity, a class or a strategy.
5a. **Never weaken a criterion to pass (owner, 27 Nov).** If an expectation proves wrong, the evidence is documented (runs, seeds, what was found) in [DEV_LOG.md](DEV_LOG.md#deviations-from-plan) and the criterion is amended **transparently**, by a commit to this document that states the old value, the new value and the evidence. A failing test is never skipped, `[Ignore]`d or loosened silently (G1.4).
6. **Direction tests:**
   - paired by seed (same world, one change), at least 30 seeds per arm for island scenarios (stricter of the sources; see conflicts);
   - "significant" means a two-sided Mann–Whitney test at p < 0.05 for unpaired arms, or a one-sided paired sign test at 5% for the direction of a paired difference;
   - several outcomes in one experiment use the Holm correction;
   - "at least k of n seeds" tests use fixed seeds and the exact count.
7. **Lab tests are numeric, island tests are not.** A lab criterion asserts the book's number with a tolerance. An island criterion never asserts that a book result appears (see section 1.5).
8. **Common random numbers.** Streams are keyed by entity and purpose (`Rng.Fork("npc", id).Fork("forage")`), so paired arms share their random draws until a change takes effect.
9. **Perf tests** run on the reference machine (one core of a mid-range 2024 desktop, recording Off unless stated). Elsewhere they print a warning and do not fail, except the hard caps (memory, no per-tick allocation) which always apply.

### 1.5 Lab versus island (the rule, stated once)

| | **Lab** (`Sim/Lab`, `lab` command) | **Island** (the real sim) |
|---|---|---|
| Payoffs | the book's payoff matrices, fixed | come from real consequences: hunger, injury, goods, trust, institutions |
| What a test asserts | **numbers**: fixed points, shares, scores, ranks, thresholds, with tolerances | **mechanisms** (the payoff or cost really changed) and a **direction** over many seeds |
| What a test never does | touch the island or the Unity game | assert that Tit for Tat, Hawk/Dove, Grudger, a class, a strike or any theory's result appears |
| Outcome hypotheses | not applicable | recorded in the report (**Hyp**), never pass/fail |
| "Not inevitable" | not applicable | a headline outcome claimed as *possible* must occur in more than 0% and fewer than 100% of its seeds (section 2.4) |

---

## 2. Standing gates (every milestone, M0 to M15)

A milestone is not done, and nothing merges as "done", unless all of the following pass **in addition to** its own criteria. The gates are cumulative: a gate added at Mk applies at every later milestone.

### 2.1 G1. All existing suites green

| # | Gate |
|---|---|
| G1.1 | `dotnet test DotNet -c Release` is green (all EM tests through `God.Sim.Tests`, plus `God.Runner.Tests` from M0). |
| G1.2 | Unity EditMode and PlayMode are green. The existing stage 3 suites (`AiTests`, `AiTradeTests`, `BoardTopologyTests`, `DuelTests`, `EconomyTests`, `FactionTests`, `GodSimTests`, `IslandBoardTests`, `MilitaryTests`, `RngTests`, `SaveTests`, `Stage3EconomyTests`, `TerritoryTests`, `TradeTests`, `TutorialTests`, `TutorialEndToEndTests` and the PlayMode view tests) stay green until the system they test is **replaced** by a listed milestone. A replaced system's tests are replaced by equal-or-stronger tests in the same commit, never deleted. |
| G1.3 | The milestone's own tests are added **before** the code (test first). |
| G1.4 | No test is skipped, `[Ignore]`d or loosened to pass. |

### 2.2 G2. Determinism suite

| # | Gate | Source |
|---|---|---|
| G2.1 | **Same seed, same hash.** Two runs of seeds 1–8 (scenario `watch6`, plus the milestone's scenario) give the same `StateHash` at every checkpoint. Different seeds give different final hashes. | HEADLESS `SameSeedSameCommands_SameHashes`, `DifferentSeeds_DifferentHashes` |
| G2.2 | **Recording on or off.** Off, Metrics, Experiment, Summary, Full, and each domain switched alone, give identical checkpoint hashes. CI: seeds 1–4 × 2 sim years. NIGHT: seed 1 × 200 sim years; and 20 seeds × 10 sim years for the neutrality check. The levels that exist at a milestone are the ones tested. | HEADLESS, DATA_WAREHOUSE 10.5, VISION 11.3 |
| G2.3 | **Save mid-run equals a straight run.** Save at T/3 and 2T/3 (including mid-day with open learning traces, open estates, pending promises), load in a fresh sim, continue: the checkpoint hashes equal the straight run. A second save of a loaded game is byte-identical. | HEADLESS `SaveLoadMid_EqualsStraight`, NPC_LEARNING L9 |
| G2.4 | **Parallel equals serial.** 16 seeds with `--workers 8` and with `--processes` give the same per-seed hashes and a byte-identical `summary.json` (wall-clock fields excluded). Two tunings on two threads each equal their own serial run. | HEADLESS `Parallel_EqualsSerial`, `TwoTunings_InParallel_DoNotLeak` |
| G2.5 | **Unity agrees with the runner.** NIGHT, player build in `-batchmode -nographics`: the smoke replay for seeds 1–2 verifies in the runner at every checkpoint. | HEADLESS `Unity_Agrees_With_Runner` |
| G2.6 | **Replay round trip.** A recorded run with 200 random human commands replays to every checkpoint (`verify` exit 0). Changing one command tick makes `verify` exit 4 and name the first bad checkpoint and module. | HEADLESS `Replay_RoundTrip`, `Replay_DetectsTampering` |
| G2.7 | **No mutable statics.** The reflection audit passes; every allow-listed field has a written reason. Tuning lives on `SimTuning` and is saved with the state. | HEADLESS `StaticAudit_NoMutableStatics` |
| G2.8 | **Snapshot builders are read-only.** Calling every snapshot builder every tick, against never, gives identical hashes over 5,000 ticks. | HEADLESS `SnapshotBuilders_AreReadOnly` |
| G2.9 | `StateHash` covers every module that exists (new modules are added to the hash in the milestone that adds them, with a test that changing a module's state changes its per-module hash). | CONVENTIONS section 3 |
| G2.10 | Every random draw uses a keyed forked stream. Adding an NPC or switching one NPC's policy does not change another NPC's draws (common-random-numbers test). | CONVENTIONS section 3, POLICY_EXPERIMENTS `Experiment_CRN_Alignment` |

### 2.3 G3. Invariants (`SimInvariants`, checked at every day boundary and in every NIGHT run)

| # | Invariant | Source |
|---|---|---|
| G3.1 | **Conservation of goods.** For every ware: `start + Σ Produced − Σ Consumed = Σ (carry + stores + piles + site deliveries)`, exactly, including `Consumed(Spoiled)`, estates (`destroyed`, `taxed`) and kit. Also after save and load. | ECONOMY A1, MATERIAL 2, MILITARY M-INV-2, STATE S-INV-1 |
| G3.2 | **No dead owners.** No ownership record names an NPC dead for more than 3 days; every estate settles within 2 days. | MATERIAL 1, 3 |
| G3.3 | **Ids never reused.** `NpcId`, `ItemLotId` and every other id kind are monotonic and never reused, including after death, save and load. | CONVENTIONS section 5, MATERIAL 2 |
| G3.4 | **Bounds.** No `ItemLot.Count ≤ 0`, no store over its weight cap, no carry over its limit; per NPC learning, memory, social and culture stores within their caps; household and membership invariants hold. | ECONOMY A2, NPC_LEARNING L11, SOCIAL 1, 3, CULTURAL 1, HOUSEHOLDS `Membership_Invariants` |
| G3.5 | **Never spawned.** Every NPC doing a job (staff, soldier, builder, hauler) has an earlier `NpcBorn` or immigration event, is alive and adult. | INSTITUTIONAL I-INV-1, MILITARY M-INV-1 |
| G3.6 | **No magic.** A site, wage, kit issue or institution action that needs an NPC with none able to act changes nothing, exactly (no work, no delivery, no handout, no kit). | STATE S-INV-2, INSTITUTIONAL I-INV-2, MILITARY M-INV-3 |
| G3.7 | Event and record integrity: one `NpcDied` cause per death; spells never overlap; every event type maps to a table. | POPULATION `Death_HasCause`, DATA_WAREHOUSE 10.1, 10.7 |

### 2.4 G4. Reflection and neutrality tests (static; run in EM; extended every time a module is added)

| # | Test | Source |
|---|---|---|
| G4.1 | **No policy reads `GodState`.** No type in `Sim/Policy`, `Sim/Cognition`, `Sim/Institutions/Policies` or `bench.*` has a field, parameter, local, generic argument or call target of `GodState`, `EconomyState`, `TerritoryState` or `PopulationState`. | COMMUNICATION, STATE, INSTITUTIONAL I-POL-1, MILITARY M-POL-1 |
| G4.2 | **No hidden truth in decisions.** Types in `Policy`, `Cognition`, `Learning`, `Social`, `Comms`, `Economy`, `State`, `Institutions` do not reference `ClassPosition`, `ClassProfile`, `ClassMeasures`, `PedigreeRecord`, `Genome`, the relatedness `r` function, `DealTrace`, `PropertyRecord` truth, `Ledger`, `TradeTuning.ValueOf`, `RecordAudit` or `ITrueStateOverlay`. | CLASSES 10, BIOLOGICAL `Relatedness_NotVisibleToNpcs`, HOUSEHOLDS `Kin_NoPedigreeAccess`, ECONOMY B6, INFORMATION I1 |
| G4.3 | **No benchmark labels in the island.** `Npc` and every type reachable from a policy observation have no field of a benchmark-strategy type, no enum value named Hawk, Dove, Sucker, Cheat or Grudger, no `Fingerprint` type; only `bench.*` policies mention them. No code outside `Sim/Lab` and tests references a target frequency (7/12, 5/6, 5/8). | GAME_THEORY 1, 3 |
| G4.4 | **No outcome flags.** Sim code outside `Lab` and `Telemetry` references no analysis category: class positions, measured strategy names, ideology labels. | VISION 1 |
| G4.5 | **The HUD reads only `FactionKnowledgeView`.** `God.Game.Hud*` types have no field, parameter, local or generic argument of `GodState`, `EconomyState`, `TerritoryState`, `PopulationState` outside the single view adapter; the HUD's command types equal the set an `IFactionPolicy` can return. Faction AIs read only the same view. | INFORMATION I1, STATE S-CMD-1, S-CMD-2 |
| G4.6 | **Form neutrality.** No method in production, job, exchange or theft code branches on `EnterpriseForm` or `OwnerKind`; identical inputs give identical output for every ownership form, to the unit, across 1000 random fixtures. | POLITICAL 7, 8, VISION 2 |
| G4.7 | **Inheritances stay separate.** Only `PhenotypeBuilder`, `Reproduction`, genetics metrics and tests reference allele data; no code path writes the learning store from the culture store or the reverse. | BIOLOGICAL `NoFitnessFunction`, CULTURAL 2 |
| G4.8 | **No enforcement of promises or customs by the engine.** The `Social` module contains no call into `Economy` mutation; no record changes because a promise was made or broken. | SOCIAL 7 |
| G4.9 | **Integer purity.** IL scan: no `float`, `double`, `decimal`, `System.Random`, `DateTime` or `UnityEngine` in `Sim/Learning`, `Sim/Lab`, `Sim/Policy/IntNet*`, `Sim/Information/Kpi` and all **new** Stage 4 sim code; the legacy `double` code is listed in an allow-list that only shrinks. | NPC_LEARNING L12, STRATEGY_BENCHMARKS 14, AI_TRAINING `IntNet_NoFloat`, KPI K12 |
| G4.10 | **Recording stays outside the sim.** `God.Sim` and `God.Game` do not reference `Microsoft.Data.Sqlite` or the writer. | DATA_WAREHOUSE 10.13 |
| G4.11 | **Not inevitable.** For every headline emergent outcome a scenario claims is *possible* (commons collapse, concentration of property, desertion, a lie told, a failed institution), the outcome occurs in more than 0% and fewer than 100% of its seeds (at least 100 seeds in NIGHT) unless the source explains why it is physically determined. Scenarios that set a stricter band use it (5% to 95%). | VISION 4, POLITICAL 13, CLASSES 14 |
| G4.12 | **Every causal claim names its control** and its seed count; every report figure is labelled `causal` or `descriptive`. | VISION 5, POLICY_EXPERIMENTS `Report_Labels` |

### 2.5 G5. Performance budgets (reference machine, `bench`; budgets apply from the milestone that introduces the case)

| Case | Minimum | Introduced | Source |
|---|---|---|---|
| `bench_stage3_watch6` (stage 3 game, no NPCs) | 2,000 ticks/s | M0 | HEADLESS 4.10 |
| `bench_npc_500`, recording Off | 200 ticks/s (20× real time), mean tick ≤ 5 ms, p99 ≤ 4 × mean, max ≤ 50 ms, ≤ 200 MB, steady-state allocation ≤ 64 KB per tick | M3 | HEADLESS 4.10 |
| `bench_npc_1000` | 100 ticks/s | M3 | HEADLESS 4.10 |
| Learning cost, 500 NPCs | ≤ 20 µs mean per think and ≤ 25% of NPC update time; consolidation ≤ 5 ms per sim day | M3 | NPC_LEARNING L19 |
| Economy, 500 NPCs, 4 factions, 10 sim days | whole sim ≥ 20× real time; economy system ≤ 20% of tick time; haul planner ≤ 2 ms per run; no per-tick allocation in the economy loop after warm-up | M6 | ECONOMY F2 |
| Save or load at 500 NPCs | ≤ 1 s each | M3 | HEADLESS 4.10 |
| Class measures, 600 NPCs | < 2 ms per sim day | M8 | CLASSES 16 |
| Interest updates, 600 NPCs | < 3 ms per sim day; joining scores add < 5% to think-tick time | M10 | POLITICAL 16 |
| Recording overhead vs Off | Metrics ≤ 3%, Summary ≤ 15%, Full ≤ 60% | M13a | DATA_WAREHOUSE 10.10 |
| 200 sim years, `band_village_500`, Summary | ≤ 3.5 h, warehouse ≤ 1 GB | NIGHT from M4 (warehouse from M13a) | HEADLESS, DATA_WAREHOUSE 10.9 |
| Parallel scaling, 8 workers | ≥ 0.8 × 8 runs' throughput | M0 | HEADLESS 4.10 |
| Regression gate | `bench --compare` fails (exit 7) below 15% under the stored baseline | M0 | HEADLESS 4.10 |

**Benchmark early (owner, 27 Nov).** From M0, every milestone records the owner's benchmark columns (ticks/s, sim years per real second, NPC decisions/s, memory per NPC, pathfinding cost, learning cost, communication cost, telemetry and database overhead, save size and load time, parallel throughput) in [DEV_LOG.md](DEV_LOG.md#performance-benchmarks), using the report of [HEADLESS_SIMULATION.md §4.10](HEADLESS_SIMULATION.md#410-performance-targets-and-profiling). A capability is never cut to meet a budget without the owner.

Scaling checks repeat at M3, M6, M9 and M12. A miss blocks the milestone, and the fix is level of detail or budget tuning before any rule change. Level-of-detail bias is tested at M3 (mean population, births and food per head within 5% of LOD off, 20 seeds × 5 sim years).

### 2.6 G6. The stage 3 game stays playable, and nothing leaves the game broken

| # | Gate |
|---|---|
| G6.1 | **Stage 3 mode.** With `Population` off and `VictoryPoints10`, the stage 3 game runs: the headless smoke (`run --check smoke` on `watch6`, seeds 1–4) gives the same verdict as the old `SmokeRun`, and a faction wins by taking the enemy base. This stays until the owner retires stage 3 mode at M14. |
| G6.2 | **Boots and plays.** `BootSceneTests` and the menu and HUD PlayMode tests pass; the Windows build starts in `-batchmode -nographics` and completes a smoke replay. |
| G6.3 | **The chain works.** On at least 9 of 10 seeds every faction completes plank → building → gear → its first soldier within 1.5× the stage 3 median ticks for that seed (from M6; before M6, equal to stage 3). |
| G6.4 | **No milestone may leave the game broken.** Every milestone merges in a state where: the stage 3 smoke passes, new features that are not finished are behind a scenario switch that defaults to the previous behaviour, and the owner can start a game from the menu and play it. A milestone that cannot meet this is split. |
| G6.5 | **Stage 3 hashes.** Seeds 1–12 in stage 3 mode replay with the same end tick and winner as the **M0 baseline** at every later milestone (see conflict C1 for why the pre-M0 baseline is not used). The pre-M0 game itself is kept as the git tag `stage3-final` (at `d7eeb95`); Stage 3 save compatibility is not required (owner, 27 Nov). |
| G6.6 | **Temporary mechanisms are listed.** Every temporary compatibility mechanism in the code is in the "Temporary mechanisms" table of [DEV_LOG.md](DEV_LOG.md#temporary-mechanisms) with its removal milestone; at that milestone it is gone (reflection or search test where possible). |
| G6.7 | **Progressive inspection.** The milestone delivers its Unity inspection or view from [conventions §10](DESIGN_CONVENTIONS.md#10-roadmap-and-stage-4-milestones-the-official-list) (the "Manual" line below). |

### 2.7 G7. Hygiene

Every milestone's report states which tests actually ran and which did not (`CLAUDE.md`). `git status` is checked before each commit. Nothing derived from Sims 2 or third-party art enters the repository.

---

## 3. Milestones

### 3.0 Checkpoints and functional releases
| Checkpoint | Milestones | Done when |
|---|---|---|
| **4A Simulation laboratory** | M0, M1, M2 | each milestone below is done |
| **4B Living population** | M3, M4 | as above |
| **4C Social economy** | M5, M6, M7 | as above |
| **4D Political economy** | M8, M9, M10, M11 | as above |
| **4E Institutional state** | M12, M13a, M13b (M13a may be done earlier, any time after M6) | as above |
| **4F Integrated gameplay and network readiness** | M14, M15 (no training) | as above |

**Functional releases** (replacing the old "minimum viable stage 4", which wrongly included M13 although M13 needed M12, M12 needs M10, and M10 needs M7 and M9):
- **First playable new game = end of 4C plus M9 (M0–M7 and M9).** Gate: every criterion of M0–M7 and M9, all standing gates, plus an owner play session from the menu in the sandbox with a living, learning population, households, the social economy with NPC labour and state incentives, and persistent soldiers.
- **Analytical recording:** JSONL events from M0 (M0.2, M0.14); the SQLite warehouse core at M13a; institution KPIs and reports at M13b.

Tables list **numbered criteria**. Column *Kind* is from section 1.3, *Source* is `doc id` (see the abbreviations list in section 4.1), *Suite* is from section 1.1. "N" after a suite means the NIGHT tier. Counts such as "20 seeds" refer to the named seed sets `S20`.

### M0. Headless runner, `SimTuning`, `SimCalendar`, victory conditions, events and replay

**Goal:** the sim runs without Unity, reproducibly, with events, replay and hash checks, and no mutable statics.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M0.1 | Inv | `watch6`, seeds 1–8, 40,000 ticks: two runs have equal hashes at every checkpoint; seeds 1–8 give 8 distinct final hashes | HEADLESS | RT |
| M0.2 | Inv | Recording Off, Metrics and JSONL Full give identical checkpoint hashes (seeds 1–4, 2 sim years; seed 1 at 200 sim years N) | HEADLESS, DW 10.5 | RT, N |
| M0.3 | Inv | Snapshot builders called every tick vs never: identical hashes over 5,000 ticks | HEADLESS | RT |
| M0.4 | Inv | 16 seeds, `--workers 8` and `--processes`: per-seed hashes equal serial; `summary.json` byte-identical; two tunings on two threads (`Military.CritPct=0` vs default) each equal their own serial run | HEADLESS | RT |
| M0.5 | Inv | Static audit passes: `MilitaryTuning`'s 22 statics, mutable arrays in `EconomyDefs`, the lazy `TradeTuning` table and mutable `static readonly` arrays are gone or allow-listed with a reason | HEADLESS | EM |
| M0.6 | Inv | Save at T/3 and 2T/3 for seeds 1–4, load in a fresh sim: checkpoint hashes equal the straight run; a second save is byte-identical | HEADLESS | RT |
| M0.7 | Inv | Replay round trip with 200 random commands verifies (exit 0); one changed command tick gives exit 4 naming the first bad checkpoint and module; an injected nondeterminism is found at the exact tick | HEADLESS | RT |
| M0.8 | Inv | `Unity_Agrees_With_Runner`, seeds 1–2, player build | HEADLESS | N |
| M0.9 | Unit | Exit codes 2, 3, 4, 5, 6, 7, 8 each produced by a constructed case; unknown scenario keys, unknown tuning paths and seed overrides are refused with code 3 | HEADLESS | RT |
| M0.10 | Inv | `run --check smoke` on `watch6` gives the same verdict, counters and final standings line as the old `SmokeRun` for seeds 1–4 | HEADLESS | RT |
| M0.11 | Unit | `Rng.Fork(label, id)` overloads: streams keyed by entity and purpose; adding an entity does not shift other streams | CONVENTIONS 3 | EM |
| M0.12 | Unit | Calendar: child before 1.0, adult 1.0–3.0, elder after 3.0 at `LifeScale` 100; at 200 the same ages in half the ticks; HUD age = sim year × 16 | POPULATION `LifeStages_Boundaries` | EM |
| M0.13 | Inv | Sandbox: 10 sim years on 5 seeds never emits `GameOver` while two factions remain. Stage 3 mode with `VictoryPoints10` replays seeds 1–12 with the same end tick and winner as the M0 baseline (conflict C1) | STATE S-VIC-1, S-VIC-2 | RT |
| M0.14 | Unit | Event stream: every `DomainEvent` subclass has a unique code in its module's range and a `meta_event_map` row (or `analysis-none` with a reason); `EventBuffer.Add` stamps `Seq`; JSONL has one line per event | DW 10.1 | EM |
| M0.15 | Perf | `bench_stage3_watch6` ≥ 2,000 ticks/s; baseline stored in `DotNet/God.Runner/Bench/baseline.json` | HEADLESS | RT |

**Suites:** `God.Runner.Tests` (new), `RngTests`, `CalendarTests`, `EventStreamTests`. **Manual:** owner plays one stage 3 game from the menu. **Done means:** a headless run, a replay and a verify all work; two runs agree; stage 3 still plays; no mutable statics remain.

### M1. Policy seams and `FactionKnowledgeView`

**Goal:** the four policy interfaces exist, `AiBrain` is an `IFactionPolicy`, and the HUD and AI read only the view.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M1.1 | Inv | `Policies_DoNotReferenceGodState` (G4.1) passes for all four interfaces and every implementation including the `AiBrain` wrapper | COMMUNICATION, MILITARY M-POL-1 | EM |
| M1.2 | Inv | `View_Only_Reads` (G4.5): HUD and policy namespaces have no `GodState`-family reference outside `ViewFiller` and report builders | INFORMATION I1 | EM |
| M1.3 | Inv | HUD command types equal the set an `IFactionPolicy` can return; no `GodState` in `God.Game.Hud` outside the adapter | STATE S-CMD-1, S-CMD-2 | EM |

*M1.2 and M1.3 as built (27 Nov 2026):* the policy half runs under `dotnet test`. The HUD half (`Hud_ReadsOnlyTheView`, `Hud_CommandTypes_EqualWhatAFactionPolicyCanReturn`) runs only in the Unity EditMode assembly `God.Game.EditTests`, because the HUD needs UnityEngine. Its exception list is empty. `AiBrain` is the single temporary entry in the policy reflection test, until M14.
| M1.4 | Inv | `AiBrain` behind the adapter is unchanged in behaviour: stage 3 seeds 1–12 replay with identical hashes to the M0 baseline; `Stage3MilitaryPolicy` likewise | MILITARY M-DET-1 | RT |
| M1.5 | Inv | Toggling the true-state overlay every day leaves `StateHash` identical at every checkpoint | INFORMATION I11 | RT |
| M1.6 | Unit | `PolicyRegistry` resolves names; an unknown policy name is refused at load; a policy cannot name an intent outside its interface *(derived)* | CONVENTIONS 7 | EM |
| M1.7 | Inv | `FullInformation` view: HUD figures equal the previous direct-read figures (snapshot comparison on 5 seeds) *(derived)* | INFORMATION 8 | PM |
| M1.8 | Unit | **Overlay off in competitive play (Q22):** a competitive VP scenario has the overlay disallowed and no toggle; a sandbox scenario has it off unless enabled; developer, lab and observation scenarios allow it; no policy interface receives it | INFORMATION I18 | EM |
| M1.9 | Inv | **No foreign truth (Q21):** under `FullInformation`, changing a foreign faction's unreported treasury, population or records leaves the viewing faction's view unchanged; each foreign item names its source channel | INFORMATION I19 | RT |

**Manual:** screenshots of the HUD before and after are visually identical. **Done means:** nothing in the HUD or AI touches true state; behaviour is unchanged.

### M2. Game-theory lab (maths only)

**Goal:** the book's benchmarks are reproduced as **numbers** in `Sim/Lab`. No island code is touched. Every item is **Lab** unless stated.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M2.1 | Unit | State machines produce exact golden move sequences (TFT against `C D D C C D` answers `C C D D C C`; TF2T never defects against `C D C D C D`, defects on round 4 only against `C D D C`; Grudger defects 100 rounds after one defection; Remorseful Prober cooperates once after a probe answered by defection; Retaliator/Bully/Dove turns; Prober `q=1000` probes on turn 1, `q=0` never; Random(p) over 100,000 draws within p ± 5 permille) | STRATEGY_BENCHMARKS 1–7 | EM |
| M2.2 | Lab | Every cell of the 4.2 matrices within 3 (50,000 contests per cell); **amended A1: the three Prober cells are R vs P 11, P vs R 11, P vs P 7.4 (published 14, 14, 13.2), because a failed probe against a Retaliator becomes a fight worth -25; the other 38 cells as published**; dollar payoffs satisfy `T > R > P > S` and `(T+S)/2 < R` (violating it fails) | STRATEGY_BENCHMARKS 8, 9, L2b | LAB |
| M2.3 | Lab | L1 Hawk/Dove: fixed point 583 ± 3 permille from starts 0.01, 0.5, 0.99; agents N=1000 0.583 ± 0.03 (N=100 ± 0.05); mean payoff at ESS 6.25 ± 0.5; ESS payoff < 0.5 × group optimum (16.67); one Hawk among 999 Doves passes 0.3 within 30 generations; mixed-strategy mean 0.583 ± 0.03; phase reader gains at least 10 | STRATEGY_BENCHMARKS L1, 10 | LAB |
| M2.4 | Lab | L2: Dove share stays 0.1 ± 0.001 over 20,000 generations in a Retaliator population; Bully rises from (R 0.5, D 0.45, B 0.05) and falls from (R 0.65, D 0.30, B 0.05); ~~N=200 Retaliator < 0.75 and Hawk+Bully > 0.10~~ **amended A2: N=200 Retaliator stays dominant under the default parameters (seeds 1 to 13: R 0.77–0.94, H+B 0.03–0.05; the system is bistable; open finding for the owner). Cause investigated 26 Nov 2026 ([HAWK_BULLY_RETALIATOR_INVESTIGATION.md](research/HAWK_BULLY_RETALIATOR_INVESTIGATION.md)): the turn-rule matrix makes R payoff-equal to H against H and B, so no Hawk/Bully ESS exists; with mutation the only attractor is R-dominant (replicator R 0.718; agents with mu 2 or 20 at N 100–1000: 117 of 120 runs in the R basin, none in H/B). Criterion unchanged; the published value stays struck through, not reinstated.** Investigation tests (EM): payoff matrix values, exact invasion verdicts, replicator fixed points, basin census, and the matched agent comparison (`HawkBullyStudyTests`); N=1000 Retaliator in 0.65–0.90 (unchanged) | STRATEGY_BENCHMARKS L2 | LAB |
| M2.5 | Lab | L3 war of attrition: mean `m` = V ± 0.2 V; CV 1.0–1.5; every fixed limit `L` in [1, V−2] invaded by `L+1`; a tell gains 0.368 V ± 0.01 V over 100,000 contests; **A3 (added check, published "readers above 0.9 within 100 generations"): reader share 0.780 ± 0.05, because a reader's opponent earns 0.104 V and readers have an edge only below `1/(4 − e)`** | STRATEGY_BENCHMARKS L3 | LAB |
| M2.6 | Lab | L4 resident/intruder: boundary 0.5 ± 0.01 (B 0.51 → above 0.99; B 0.49 → A above 0.99); Hawk/Dove mix resists B at 0.05 and 0.2 (**amended A4: published 0.4 too; the replicator boundary is 0.394 and B from 0.4 takes over, ending 0.896, because B earns 11.9 against a mean of 11.25**) and B ends 0.9 ± 0.05 from 0.6; Bourgeois vs Bourgeois drops from 25 to −25 | STRATEGY_BENCHMARKS L4 | LAB |
| M2.7 | Lab | L5 pecking order: hens last-100 fight share ≤ 0.02; crickets last/first between 0.7 and 1.3; hen fights < cricket fights. Landau's h reported only | STRATEGY_BENCHMARKS L5 | LAB |
| M2.8 | Lab | L6 coy/fast: time averages (0.833 ± 0.01, 0.625 ± 0.02); the state keeps cycling (SD of c > 0.1, of f > 0.2); conserved `H` drifts < 0.1%; an Euler step fails the check; agents `c` ~~0.83 ± 0.03~~ **0.859 ± 0.03**, `f` ~~0.60 ± 0.05~~ **0.559 ± 0.05** (amended A5: the agent model's mutation–selection point, independently confirmed 0.8588 and 0.5586) | STRATEGY_BENCHMARKS L6, 17 | LAB |
| M2.9 | Lab | L7: Grudger share 0.04 ends Cheat > 0.99, 0.07 ends Grudger > 0.99 (boundary 1/18 ± 0.003); order of events Sucker falls, Cheat booms 0.25–0.40 then < 0.01 by generation 150, Grudgers end 0.70 ± 0.05, Suckers 0.30 ± 0.05 | STRATEGY_BENCHMARKS L7 | LAB |
| M2.10 | Lab | L8 round robin, 30 seeds: scores within ± 15 of the table (**amended A6: Suspicious TFT 387 → 406.5, Grudger 445 → 413.8, Naive Prober 349 → 332.0, Tester 484 → 455.7, Sneak 400 → 419.2, because the 4.3 state machines give these; TFT's row matches the prototype**); TFT and TF2T in the top 4; Always Defect in the bottom 2; at least 4 of the top 5 nice; TFT scores 600 against every nice strategy and never beats a deterministic partner by more than 5; TFT vs Naive Prober 253 ± 50, vs Remorseful Prober 490 ± 50; `TFT-End` 602 vs 597 exactly, no gain at unknown length (~~within ± 3~~ **gain ≤ +3 over 200 seeds, amended A7: a TFT-End that guesses round 200 loses about 36**) | STRATEGY_BENCHMARKS L8, L8b | LAB |
| M2.11 | Lab | L9 ecological run (**amended A8: final shares TFT .669, Grudger .004, Pavlov .138 ± .05, published .33/.22/.19, because Grudger dies early after A6; TF2T .14 and AllC .11 as published**): nasty strategies and probers below 1% by generation 200, Always Defect by 30; TFT largest final share; survivors all nice. L9b: replicator boundary equals `p* = (1−w)/(3w−1)` ± 0.003 for w in {0.6, 0.75, 0.9, 0.95, 0.99, 0.995}; agents at 1.5 p* take over in ≥ 90% of 20 seeds, at 0.5 p* die in ≥ 90%; clustering k = 0.10 takes over, k = 0.02 does not (**A9, report line: exact `k*` is 0.040, not 0.049, which counts only TFT's own meetings**); Always Cooperate time average > 0.02 | STRATEGY_BENCHMARKS L9, L9b | LAB |
| M2.12 | Lab | L10 digger wasps mean `x` 0.28 ± 0.03 from starts 0.9 and 0.05 (**A12, modelling note: the check uses expected payoffs; with realised draws and exp soft selection the equilibrium is about 0.55, report-only**), `|payoff(Dig) − payoff(Enter)| < 3`. L11 Hamilton's rule (**label A11: Exact (as recorded in research notes C1), checked against the notes, not the book**): the example chooses Call (19.5 vs 18), without the cousin a tie resolves to Keep; `b = 2.6` helper above 0.95, `b = 1.4` below 0.05, threshold 2 ± 15%; with `r' = 0.55 r` threshold `c/r'`. L12 selfish herd: nearest-neighbour ratio < 0.2 | STRATEGY_BENCHMARKS L10–L12 | LAB |
| M2.13 | Lab | L13 (vampire bats) runs and reports; **no numeric expectation** | STRATEGY_BENCHMARKS L13 | LAB |
| M2.14 | Inv | The same `(config, seed)` gives the same `ResultHash` twice in a process, in two processes and under `dotnet test`; replicator shares sum to exactly 2^30; pairing shuffles use forked streams, so one experiment's agent count does not change another's hash | STRATEGY_BENCHMARKS 11–13 | RT |
| M2.15 | Lab | ABM vs replicator at N=10,000: Hawk share within 0.03; L9b threshold within 0.01. Negative controls fail as designed (Euler step in L6; `w0=100` in L1; **A13: the L1 agent mean payoff 6.4–6.9 is report-only, the 6.25 check is made at the replicator ESS**) | STRATEGY_BENCHMARKS 16, 17 | LAB |
| M2.16 | Inv | No `UnityEngine`, `System.Random`, `float` or `double` in `Sim/Lab` (source scan); no code outside `Sim/Lab` and tests references 7/12, 5/6 or 5/8 | STRATEGY_BENCHMARKS 14, GAME_THEORY 3 | EM |
| M2.17 | Perf | `lab all --check` runs in under 60 s on one core (**measured 33.8–34.1 s for 70 runs**); exit 1 on a miss, 0 on a pass | STRATEGY_BENCHMARKS 10 | RT |
| M2.18 | Inv | A game with no benchmark assigned gives the same hash as before the benchmark module existed | STRATEGY_BENCHMARKS 20 | RT |
| M2.19 | Inv | **There is no acceptance test that a book result appears on the island.** Island adapter checks (STRATEGY_BENCHMARKS 18, 19, 21–23) start at M3 and later milestones | GAME_THEORY section 10 | n/a |
| M2.20 | Unit | **Honest labels:** every experiment in `lab list` carries a reproduction status (Exact; Exact payoffs with a corrected or different result; Derived; Invented); no report of a non-Exact experiment calls itself a reproduction | STRATEGY_BENCHMARKS 24 | EM |

**M2 amendments (27 Nov 2026).** Every amended criterion above shows the published value struck through or named next to the amended one, with a one-line reason. The full evidence is in [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md#lab-amendments-m2-27-nov-2026) (A1 to A13). A10 (contest rules: a display block ends the contest; both sides withdrawing with no prior mutual exchange pays nothing) is recorded in the 4.2 turn rules and underlies M2.2. All reproduction-status labels are unchanged (M2.20).

**Manual:** none. **Done means:** `lab all --check` exits 0 and every number above is within tolerance; the lab is independent of the island.

### M3. NPC core: body, drives, perception, beliefs, instincts and the associative learning core

**Goal:** persistent NPCs that perceive, believe, choose and learn within their lifetimes; they never read the true state.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M3.1 | Mech | L1 naive forager: eats within 5 think ticks of becoming hungry in ≥ 95% of hungry episodes, in ≥ 95% of 100 seeds | NPC_LEARNING L1 | RT |
| M3.2 | Mech | L2: eats in ≤ 15% of think ticks with hunger below comfort while a bush is attended | NPC_LEARNING L2 | RT |
| M3.3 | Mech | L3 delayed reward, 30 ticks: after 200 trials `TestA` is chosen in ≥ 90% of the last 50 trials in ≥ 95% of 100 seeds; with traces disabled (`TraceShift` 16) it stays ≤ 65%; the filler verb's mean `Q` stays within ± 100 vp | NPC_LEARNING L3 | RT |
| M3.4 | Mech | L4 avoidance: after ≤ 3 tastes of `Toxic`, `Eat` on it is chosen in ≤ 5% of opportunities over 5 days in ≥ 95% of 100 seeds | NPC_LEARNING L4 | RT |
| M3.5 | Mech | L5 forgetting: ≤ 10% of trips go to the emptied bush A on days 8–10; by day 40 A's `PlaceRef` entries have `Ltw ≤ 0` or are evicted. L6: trips to A back above 50% by day 15 after it regrows | NPC_LEARNING L5, L6 | RT |
| M3.6 | Mech | L7 learning on vs off: `Off` runs without errors with deaths within the scenario baseline; decision-sequence hashes differ between modes in 100% of 20 seeds; `Frozen` leaves the store hash unchanged | NPC_LEARNING L7 | RT |
| M3.7 | Mech | L8 same nature, different nurture: identical-genome pairs differ by ≥ 30 percentage points in a behaviour rate in ≥ 95% of 50 seed pairs; store hashes differ; the identical-setup control behaves identically | NPC_LEARNING L8 | RT |
| M3.8 | Inv | L9: save at day 2.5 with open traces, load, run to day 5: `StateHash` equals the uninterrupted run at every 100-tick checkpoint, 10 seeds. L10: same seed twice, recording full vs off: identical, 10 seeds | NPC_LEARNING L9, L10 | RT |
| M3.9 | Inv | L11 bounds over 50 sim years, 200 NPCs: at every daily check `Count ≤ 256`, slots ≤ 64, `RingCount ≤ 16`, `OpenCount ≤ 8`, learning bytes ≤ 10,240, all weights within ± 4000; no allocation in `Score` or the update rule | NPC_LEARNING L11 | RT |
| M3.10 | Inv | L12 integer purity of `God.Sim.Learning` (G4.9) | NPC_LEARNING L12 | EM |
| M3.11 | Mech | L13 generalisation: first `Eat` on fish within 3 think ticks in ≥ 80% of 100 seeds (control ≤ 40%); a `Hunger & Food` conjunction recruited by day 3 in ≥ 90% of seeds | NPC_LEARNING L13 | RT |
| M3.12 | Inv | **Perception only:** an NPC never chooses an intent whose target it has not perceived, remembered or been told about; checked on every decision of a 20-NPC, 10-day run | COMMUNICATION | RT |
| M3.13 | Inv | Decision sequence is the same for the same seed in 10 seeds, and recording on or off gives the same hash | COMMUNICATION | RT |
| M3.14 | Unit | Starvation: an adult at Nutrition 0 with no food dies in 5–10 days, a child in 3–7 days; every `NpcDied` has exactly one cause and counts by cause sum to the total | POPULATION `Starvation_Time`, `Death_HasCause` | EM |
| M3.15 | Unit | Food spoils at `SpoilDay` with one `Consumed(Spoiled)`; the 3rd berry eaten in a day relieves 1000/1500 of the first | ECONOMY A3, A4 | EM |
| M3.16 | Inv | Benchmark policies read only `NpcObservation` and the mind view; the island adapter and the lab state machine give the same move for a scripted history | STRATEGY_BENCHMARKS 18, 19 | EM |
| M3.17 | Unit | The utility policy's score for continuing a task does not change with the amount already spent (Concorde), but changes with remaining cost | GAME_THEORY 11 | EM |
| M3.18 | Hyp | S10 dig-or-enter: share entering and payoff of each route, hut register on/off, scarce vs plentiful huts, 30 seeds per arm. Mechanism check (the register adds the stated penalty) is **Mech** and must pass | STRATEGY_BENCHMARKS S10 | RT |
| M3.19 | Perf | L19: 500 NPCs, 1 sim day: learning ≤ 20 µs per think, ≤ 25% of NPC update time; consolidation ≤ 5 ms per day. `bench_npc_500`, `bench_npc_1000` and save/load budgets (G5). LOD bias within 5% over 20 seeds × 5 sim years (N) | NPC_LEARNING L19, HEADLESS | RT, N |
| M3.20 | Mech | **Owner learning scenario LS1, hungry near food:** L1 and L2 pass in the LS1 scenario, and `Frozen`-from-birth with no instincts eats within 5 think ticks in ≤ 40% of hungry episodes (so the result is learned) | NPC_LEARNING LS1 | RT |
| M3.21 | Mech | **LS3, tired near shelter:** by days 4–5 the learner sleeps in the hut in ≥ 20 points more fatigue episodes than `Frozen`, in ≥ 90% of 50 seeds; with no hut within 10 nodes it does not make wasted walks (≥ 95%) | NPC_LEARNING LS3 | RT |
| M3.22 | Mech | **LS4, gathering wood with an axe:** by days 6–8 the learner equips the axe before gathering in ≥ 20 points more trips than `Frozen`, in ≥ 90% of 50 seeds; a conjunction with `ObjClass(LogPile)` is recruited by day 8 in ≥ 80%; with a useless axe, no gain over `Frozen` (± 10 points) | NPC_LEARNING LS4 | RT |
| M3.23 | Unit | Decay is independent of `LifeScale` and configurable: `LifeScale` 200 leaves a fixture store's daily decay unchanged; `DecayScalePermille` 2000 doubles it (within rounding) and 0 stops it (Q1) | NPC_LEARNING 4.8 | EM |
| M3.24 | Perf | First benchmark row with NPCs: every owner benchmark column that exists at M3 is recorded in DEV_LOG (NPC decisions/s, memory per NPC, pathfinding, learning cost, save size and load time) | HEADLESS 4.10 | RT |

**Manual:** screenshot of the NPC inspector (drives, attention, beliefs, top learned associations) and of a foraging NPC. **Done means:** NPCs live in the game, learn, never read the truth, and the budgets hold at 500 NPCs.

### M4. Life cycle, households, kinship, `Genome`, basic estates

**Goal:** birth to death; partnership, reproduction, child-rearing, care; a genome that carries bodies and predispositions; goods pass to heirs by custom. Learned state is never inherited.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M4.1 | Unit | Natural death only (starvation, injury, disease off): every NPC dies between 3.3 and 4.7 years; mean 3.9–4.1 | POPULATION `NaturalDeath_Window` | EM |
| M4.2 | Unit | Fertility needs both partners: no conception in 1,000 couple-days if either says no; both yes and fertile: conception within 12 days in ≥ 90% of couples. No conception below 1.0 (adulthood; the threshold moved from 0.9 on 27 Nov because reproduction is for adults only) or above the fertility end; no birth sooner than 0.35 y after the last; full-term pregnancy 0.25 ± 0.005 y | POPULATION `Fertility_*`, `Pregnancy_Length` | EM |
| M4.3 | Mech | Disease: one infected adult in a hut of 6 well-fed adults gives 2–5 infections, deaths under 1 in 6; a hungry hut shows ≥ 2× the deaths | POPULATION `Disease_Outbreak` | RT |
| M4.4 | Mech | Immigration responds: surplus food and free beds give ≥ 3× the arrivals of a starving, full faction over 5 years. Capacity emerges: food ×1, ×2, ×0.5 gives mean population (years 8–15) in ratio 1 : 1.6–2.4 : 0.4–0.6 | POPULATION | RT |
| M4.5 | Mech | **Stable over 10 generations:** default sandbox seed, 15 sim years: never extinct, minimum population ≥ 10, end population 0.5×–2× of year 3, no year with growth above 60% after year 3; households per capita 0.17–0.4 (the stricter of 0.15–0.5 and the size band); ≤ 1% of living NPCs without a household for over 1 year | POPULATION, HOUSEHOLDS `Stable_10Generations` | RT |
| M4.6 | Mech | Age pyramid in steady state: children under 1.0 are 20–40%; elders over 3.0 under 20%; bins sum to headcount. Generation time (mean mother's age at birth, years 4–15) 1.1–1.8 | POPULATION | RT |
| M4.7 | Inv | Child mortality reported equals the recomputation from events (exact); all measures (family strategies, birth metrics) equal their recomputation and nothing in the sim reads them | POPULATION, HOUSEHOLDS `Family_Strategies_Measured` | EM |
| M4.8 | Unit | Gamete deterministic; mutation events 400 ± 60 per 100,000 copies at `MutationPer10k` 40 (0 at 0); down-bias share 0.80 ± 0.04; crossovers 1.05 ± 0.08 per chromosome and each parental allele 50% ± 1% | BIOLOGICAL | EM |
| M4.9 | Unit | Offspring: mean (child − mid-parent) within ± 5 permille, variance about mid-parent 0.5 × population variance within [0.42, 0.58]; genotypic slope 1.0 ± 0.07; phenotypic slope 0.50 ± 0.10; dominance lowers the narrow-sense slope by ≥ 0.05; group heritabilities within the 4.3 table; personality equals mapping + noise and children's mean trait within 10 of the mid-parent mean over 500 families | BIOLOGICAL, POPULATION `Personality_FromGenome` | EM |
| M4.10 | Unit | Relatedness: hand-made pedigree gives the 4.6 values exactly; offspring of full siblings `F` = 0.25. Resemblance cue AUC 0.60–0.80 (2,000 sibling vs 2,000 unrelated pairs) | BIOLOGICAL | EM |
| M4.11 | Inv | **Four inheritances separate:** changing a parent's learned weights, skills, culture and goods (same seed) leaves the child's genome and phenotype hashes identical. No NPC type reads `PedigreeRecord`, `Genome` or `r`; every `KinBelief` has a source | BIOLOGICAL, HOUSEHOLDS `Kin_NoPedigreeAccess` | EM |
| M4.12 | Hyp | Selection is measured: scarce-food scenario against control, 20 paired seeds × 10 generations. The selection differential and the paired difference in mean Size genotypic value equal a recomputation from events (exact, asserted); the size and sign of the difference are reported with an interval, never asserted. Food costs are not tuned to make a difference appear | BIOLOGICAL `Selection_Measured` | RT |
| M4.13 | Mech | Inbreeding depression: mean Health phenotype ≥ 15 permille lower in 300 sibling-mated against 300 unrelated births (calibrate once and pin) | BIOLOGICAL | RT |
| M4.14 | Inv | Household membership invariants every day (one household per NPC, sorted lists, sizes ≤ 12, sleeping rights match huts); membership telemetry matches (`HouseholdJoined`/`Left` pairs, no overlap, last interval ends at death) | HOUSEHOLDS | EM |
| M4.15 | Mech | Children fed: over 10 years the child eats ≥ 80% of need in > 90% of child-days when household food ≥ 2 days of need; the fed share is ≥ 25 points lower when household food < 0.5 days | HOUSEHOLDS | RT |
| M4.16 | Mech | Orphans: with believed kin of `r̂ ≥ 250` fostered within 5 days in ≥ 80% (≥ 30 orphans, 10 seeds); with only unrelated adults within 10 days in ≥ 50%; fostered orphans reach 1.0 at ≥ 2× the rate of unfostered | HOUSEHOLDS | RT |
| M4.17 | Mech | Partnership forms (40–95% of adults aged ≥ 1.2 have a partnership by age 2.0); mean household size 2.5–6.0, none above 12 | HOUSEHOLDS | RT |
| M4.18 | Mech | Care: tended illness severity 30% ± 10% lower and recovery ≥ 1 day earlier; care order 500 > 250 > 0 in ≥ 90% of 20 seeds. Foster child believes a wrong kin in ≥ 70% of un-told fostered cases. Divorce splits the store within 1 unit with each child in one household and histories closed | HOUSEHOLDS | RT |
| M4.19 | Inv | **Estates settle:** 300 NPCs, 40 sim years: every `NpcDied` has `EstateSettled` within 2,400 ticks; goods conserved over 1,000 random estates for every custom (`Σ before = Σ after + destroyed + taxed`); each `ItemLotId` has one owner; `EstateTransfer` value equals `InheritedValueRef` increases plus destroyed, taxed and ownerless value, exactly | MATERIAL 1–3, 18 | EM |
| M4.20 | Unit | Custom rules exact: `EqualChildren` 3 children and 10 logs gives 4, 3, 3 (one hut to A; a following tool worth 30 to B); `EldestChild`; `PartnerFirst` (two estates conserve); `HouseholdKeeps`; `BackToCommunity`; `BuriedWithDead` 10 logs at 50% destroys 5; no custom makes every asset ownerless; unacknowledged child inherits nothing; minor heir holds an asset by name | MATERIAL 5–11 | EM |
| M4.21 | Unit | Ownerless asset claimed by first arrival (tick = arrival tick; simultaneous claimants raise one contest and one winner); ownerless food rots on schedule. Wills: known to the executor honoured 100%; known only to a dead witness ignored with `WillIgnored`. Disputes: 0 of 1000 estates with the same custom; raised for every non-eldest child whose shortfall ≥ 25% and for none below; grudge `min(255, shortfall/2) × 3` exact | MATERIAL 12–16 | EM |
| M4.22 | Inv | Determinism: 10 generations twice and with recording on/off give the same hash at every day boundary and the same membership table, pedigree hash and `EstateTransfer` stream; save and load with an open estate and pending will give an identical stream | POPULATION, BIOLOGICAL, HOUSEHOLDS, MATERIAL 4 | RT |
| M4.23 | Hyp | S7 partnership strategies (fast/philanderer seeded 20% each; courts or none; welfare or none): cycle amplitude and shares recorded. MATERIAL 17 estate measures (Gini, parent-child rank correlation) are recorded here, with the first baseline pinned at M8 | STRATEGY_BENCHMARKS S7, MATERIAL 17 | RT, N |
| M4.24 | Perf | `LongRun_200Years` `band_village_500` seed 1, 200 sim years at Summary-equivalent: exit 0 in ≤ 3.5 h (N from M4) | HEADLESS | N |
| M4.25 | Inv | **Adults only, never replenished:** every `PregnancyStarted` has both parents aged ≥ 1.0 (10 seeds × 15 years, zero exceptions); with `immigration Off` the NPCs ever alive equal founders plus births exactly, and no vacancy creates an NPC; immigration arrivals after a scripted epidemic equal the rule's prediction from its own inputs (exact) | POPULATION `Reproduction_AdultsOnly`, `No_Spawned_Replacements`, `Immigration_NotReplenishment` | RT |
| M4.26 | Mech | **Disease (Q3):** `Off` gives zero infections; `Mild` at most half the disease deaths of `On`; hungry, over-capacity and high-`Fouling` huts are each sicker than their controls and tended patients less severe (paired sign test, 20 seeds each); every infection records one route | POPULATION `Disease_Settings`, `Disease_Linkage`, `Disease_RouteRecorded` | RT |
| M4.27 | Hyp | **Multigenerational stability, owner default:** `band_6x20` on 20 seeds and `band_6x10`, `band_6x40` on 10 seeds each, 15 sim years; and `Stable_10Generations` with immigration Off (20 seeds). Population per faction, births, deaths by cause, extinctions and generations are reported with intervals; every extinction or collapse is classified (meaningful outcome, defective mechanic, bad baseline) in DEV_LOG. Passes when every seed completes without an invariant failure and every extinction is classified; nothing is tuned to prevent extinction | POPULATION `Default_6x20_Generations`, `Stable_Closed` | RT, N |
| M4.28 | Inv | **Partnerships (Q5, Q6):** same-sex fixtures give identical household, care, estate and social-memory outcomes to the unit (conception disabled) over 1000 fixtures, and no code outside `Reproduction` and `PhenotypeBuilder` branches on partner sex; orphans are fostered by same-sex couples and single adults within ± 10 points of the mixed control; every partnership forms and ends by the NPCs' own decisions; with `MaxLivePartnerships` 2 concurrent partnerships occur and every invariant holds | HOUSEHOLDS `SameSex_EqualCapabilities`, `Adoption_OpenToAll`, `Partnership_Voluntary`, `Partnership_CapConfigurable` | EM, RT |
| M4.29 | Mech | **Shared childcare (Q15):** at least one `CareArrangementMade` in ≥ 10 of 20 seeds of the two-household scenario; an arrangement exploited by one side is ended by the other within 10 days in ≥ 80% of cases | HOUSEHOLDS `SharedChildcare_Possible` | RT |
| M4.30 | Unit | **Mutation configurable (Q4):** `MutationPer10k` 10 gives 100 ± 30 events per 100,000 copies; 0 gives exactly 0; per-scenario values are honoured and do not leak between parallel runs | BIOLOGICAL `Mutation_RateHonoured`, `Mutation_ScenarioOverride` | EM, RT |
| M4.31 | Inv | **Estates (Q13, Q14):** assets transferred, never duplicated (1,000 estates per custom and ladder: id multisets conserved, no new ids); the executor ladder is configurable; no executor receives more than its custom gives it | MATERIAL 20–22 | EM |
| M4.32 | Inv | **Care is counted (Q12):** unpaid care ticks and the time-use categories are counted from M4 and sum exactly to the ticks of each day | CLASSES 18 | RT |

**Manual:** screenshot of a household panel and family tree; owner watches one generation turn over. **Done means:** a population lives, breeds and dies for 15 years without extinction or explosion; inheritances are separate; goods pass to heirs and conservation holds.

### M5. Social memory, gestures, honest barter and gifts

**Goal:** NPCs remember who is who, communicate by gesture, trade honestly in separate hand-overs and give gifts. Lifetime learning extends to social domains.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M5.1 | Inv | After meeting 200 distinct NPCs `RelCount ≤ 40` with partners, household mates and open obligations kept; eviction by lowest importance, ties to lowest slot, same seed gives same evictions 3 times; `sizeof(SocialMind)` ≤ 4096 B; 600 NPCs ≤ 2.5 MB | SOCIAL 1–3 | EM |
| M5.2 | Unit | Recognition miss rate over 10,000 draws: 30–36% at `Familiarity 50`, 5–9% at 800, 0 at 1000. A cheat meeting a stranger again is unrecognised in 30–36% of meetings; after 5 interactions under 3% | SOCIAL 4, 5 | EM |
| M5.3 | Unit | Trust and forgiveness: grudge 300 at `sev` 100 after a value-100 betrayal; at `Vengefulness +100` ≥ 245 after 20 days, at −100 ≤ 30; helpful act after grudge 600 raises trust by `delta × 0.4`; `Apologise` with gift adds `giftValue/2`; repeat apology after 3 betrayals scores 120 lower | SOCIAL 12–14 | EM |
| M5.4 | Inv | **Honest gestures:** every `GestureMade` has a `MessageDecoded` for each attending listener; with misunderstanding off decoded fields equal sent fields in 100% of cases | COMMUNICATION | RT |
| M5.5 | Mech | Communication learning: an NPC whose `NeedFood` to a friend is always answered with food uses it ≥ 2× more often by day 5 than one whose friend always ignores it, in ≥ 90% of 50 seeds | COMMUNICATION | RT |
| M5.6 | Mech | L14 avoidance after cheating: accept rate for S's next 5 offers ≤ 20% (baseline ≥ 60%); other strangers fall ≥ 15 points but stay above S's; after 10 honest trades acceptance recovers above 50% within 5 days. Learned entries and social trust compared both ways (open question) | NPC_LEARNING L14 | RT |
| M5.7 | Mech | **Defection is possible:** `TakeAndLeave` vs a utility NPC that gives first: A holds both goods, B neither, `ExchangeClosed.Outcome = Defected`, nothing returns the goods, B's trust in A falls | ECONOMY B1 | RT |
| M5.8 | Mech | Honest pair: complementary utility NPCs complete ≥ 90% of accepted deals in 20 runs. Price beliefs: after 10 observed trades at 3 berries for 1 fish, `P(fish)/P(berries)` within 2.5–3.5. Incremental trade used in ≥ 50% of deals where trust < 300 (reported) | ECONOMY B2–B4 | RT |
| M5.9 | Mech | Gifts follow belief: sibling > hut-mate > stranger in ≥ 90% of 20 seeds; changing the `Kin weight` trait from −50 to +50 raises the sibling share | HOUSEHOLDS `KinGifts_OrderedByBelief` | RT |
| M5.10 | Inv | Reflection for ECONOMY B6 (no policy references `DealTrace`, `PropertyRecord` truth, `Ledger`, `TradeTuning.ValueOf`) added to G4 | ECONOMY B6 | EM |
| M5.11 | Mech | L16 domain progress: for each domain introduced (communication, barter, cooperation, relationships), the learned NPC's rate of the rewarding choice differs by ≥ 20 points from `Frozen`-from-birth in ≥ 90% of 50 seeds | NPC_LEARNING L16 | RT |
| M5.12 | Hyp | S2 resident/intruder by custom (20% resident rule, 20% intruder rule seeded; hut register on/off) and S5 hut-mates share on unlucky days (shared store on/off; stable vs high-turnover huts): 30 seeds per arm. **Mech** checks (the register carries its penalty; the store changes the payoff) must pass | STRATEGY_BENCHMARKS S2, S5 | RT |
| M5.13 | Mech | **LS2, hungry with no food (producing goods enables a later food exchange):** by days 8–10 the learner, hungry with no food, gathers logs or goes to the trader with them in ≥ 20 points more think ticks than `Frozen`, in ≥ 90% of 50 seeds; with food in sight it still eats first (≥ 90%); with episodes off and traces disabled the gap shrinks by at least half (reported); with a trader who gives nothing, no gain over `Frozen` (± 10 points) | NPC_LEARNING LS2 | RT |
| M5.14 | Unit | Social-memory decay is independent of `LifeScale` and configurable: test 12 of SOCIAL_MEMORY with `ScalePermille` 2000 and with grudge decay 0 (Q1) | SOCIAL 12 | EM |

**Manual:** screenshot of gesture animation (the view animates sim events and decides nothing). **Done means:** NPCs recognise, remember and gesture; an exchange can be cheated; gifts happen; nothing enforces honesty.

### M6. NPC labour, basic state, ownership records

**Goal:** NPCs do the work that walkers did; the state taxes, pays, posts jobs, designates sites and commits goods; the stage 3 chain still works.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M6.1 | Inv | **Conservation with population:** 10 seeds, 20 sim days, at every day end for every ware; also after save and load at day 7 (G3.1 on the full game). Treasury ledger: faction stock change equals the sum of logged flows every tick, 10 seeds × 5 days | ECONOMY A1, STATE S-INV-1 | RT |
| M6.2 | Inv | **Walkers gone:** after M6 no `Walker` type exists in the sim assembly and no code path creates a worker for a job | ECONOMY E3 | EM |
| M6.3 | Mech | Jobs are chosen: haul pay 0 gives no haul takers in 5 days; default pay: ≥ 80% of haul postings taken within 600 ticks | ECONOMY E1 | RT |
| M6.4 | Mech | **The chain still works:** default 4-faction game with `AiBrain` via the M6 adapter: on ≥ 9 of 10 seeds every faction completes plank → building → gear → its first soldier within 1.5× the stage 3 median ticks for that seed (G6.3 enters force) | ECONOMY E2 | RT |
| M6.5 | Inv | No magic: a fully committed site with no NPC able to reach it has `Delivered` and `WorkDone` unchanged for 10 days, exactly. Escrow on: no `WageUnpaid` ever (10 seeds × 10 days) | STATE S-INV-2, S-INV-3 | RT |
| M6.6 | Unit | Tax rounding: 1000 sale-tax payments of 1–9 units at 125 permille total within 1 unit of the exact sum; `SetTaxRate(Wage, …)` cannot reduce the state's own contract pay (`no_self_tax`) | STATE S-TAX-1, S-TAX-2 | EM |
| M6.7 | Mech | Pay matters: a zero-pay build contract with no advance finds no taker within 5 days on ≥ 95% of 20 seeds. **Dir:** doubling a haul contract's pay lowers the median days-to-fill by ≥ 30% (significant, 20 seeds). A forced default to 5 workers makes the next contract at the same pay take ≥ 50% longer to fill (significant, 20 seeds) | STATE S-PAY-1..3 | RT |
| M6.8 | Mech | A sawmill designated with full commitment and default pay completes within 3 days on ≥ 95% of 20 seeds. A purchase offer of 2 grain per log attracts a delivery within 2 days on ≥ 90% of 20 seeds when logs are reachable; an offer of 0 attracts none in 5 days on 100% | STATE S-SITE-1, S-PUR-1 | RT |
| M6.9 | Dir | Handouts: in a famine scenario a staffed granary with handouts lowers starvation deaths over 10 days (significant, 20 seeds); labour supply change reported. An unstaffed granary gives zero handouts over 5 days, exactly *(S-WEL assigned to M6; conflict C9)* | STATE S-WEL-1, S-WEL-2 | RT |
| M6.10 | Mech | **Specialisation:** `TwoFoods`, 40 adults: by day 10 on ≥ 7 of 10 seeds `SpecialisationIndex ≥ 650` permille and ≥ 150 permille above the `ExchangeOff` control; ≥ 0.5 completed exchanges per adult per day; mean nutrition not below the control's. With identical skills and `ExchangeOff`, index < 550. Food surplus per adult positive on day 10 on ≥ 7 of 10 seeds | ECONOMY C1–C3 | RT |
| M6.11 | Inv | Same seed and commands give the same hash with recording off, default and detailed; Perf: 500 NPCs, 4 factions, 10 sim days at ≥ 20× real time, economy ≤ 20% of tick time, haul planner ≤ 2 ms, no per-tick allocation in the economy loop | ECONOMY F1, F2 | RT |
| M6.12 | Inv | **Same command set:** the HUD "designate a site and post a contract" sends the commands an `IFactionPolicy` can return (G4.5) | STATE S-CMD-1 | EM |
| M6.13 | Mech | L16 domains: work, production, gathering, gear (each: learned rate differs by ≥ 20 points from `Frozen` in ≥ 90% of 50 seeds) | NPC_LEARNING L16 | RT |
| M6.14 | Mech | **LS7, working for a reliable state:** by days 12–15 the learner prefers state postings over an identical private posting ≥ 20 points more in the reliable-state arm than in the defaulting-state arm, in ≥ 90% of 50 paired seeds; a reliable private employer is not avoided in either arm (± 10 points); with episodes off the arm difference shrinks by at least half (reported) | NPC_LEARNING LS7 | RT |
| M6.15 | Inv | **No anonymous workers in either mode (Q8):** in VP mode and the sandbox, every recipe cycle, build tick and gather has a staff NPC present (10 seeds × 10 days, exact) | ECONOMY E6 | RT |
| M6.16 | Unit | **Walk-out suspends (Q19):** `RefuseWork` makes the employment `Suspended` with no pay and no work, no `JobAbandoned` after 300 ticks, ended only by `Resign` or `Dismiss`; a lockout likewise | ECONOMY E5, POLITICAL 17 | EM |
| M6.17 | Mech | **Reasons to trade and hand gathering (Q9, Q10):** `LogsAndClay` gives ≥ 0.2 log-for-clay exchanges per adult per day and ≥ 1.3× the huts of `ExchangeOff` on ≥ 7 of 10 seeds; the hand-versus-capital output ratio is reported for every default run and lies in 3–5 at default tuning | ECONOMY C4, C5 | RT |
| M6.18 | Unit | **Forest stock (Q11):** on in the sandbox and off in VP by default; a depleted forest hex gets no piles until regrowth (exact); with depletion off pile size never changes (exact); depletion and sustained stock are both possible in the forest commons scenario | ECONOMY D5 | EM, RT |
| M6.19 | Inv | **Temporary HQ paymaster (Q16):** pays only goods the store holds, emits `WageUnpaid` for every shortfall, ledger exact; `IPayPoint` can be replaced by a queuing test implementation with no other code change | STATE S-TMP-1, S-TMP-3 | RT |

**Manual:** owner places a sawmill by designating a site and posting a contract and watches NPCs build it; screenshots of the state and jobs panels. **Done means:** the stage 3 economy works with NPC labour and no walkers; the state governs only by pay and commitment.

### M7. Deferred exchange, promises, reputation, lies, social learning by observation

**Goal:** credit, broken promises, gossip and lies exist as mechanics, with no engine enforcement; NPCs learn from watching others.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M7.1 | Unit | A promise whose due tick passes with nothing handed over is `Broken` by `T + 150`, `PromiseBroken` is emitted once, `Betrayals` = 1, trust falls ≥ 200 for a value-100 promise. A liar's promise shows `Intended = false` and no debtor entry. A creditor's death makes the obligation `Orphaned` within 1 tick, unpaid unless an heir claims it. A registered obligation changes only the expected cost term | SOCIAL 6, 9–11 | EM |
| M7.2 | Inv | **No enforcement:** across a run no inventory or ownership record changes because a promise was made or broken; the `Social` module has no call into `Economy` mutation (G4.8) | SOCIAL 7 | EM |
| M7.3 | Mech | A debtor with `Honesty +100` and an important creditor keeps ≥ 90% of 200 promises when able; `Honesty −100` keeps < 60% | SOCIAL 8 | RT |
| M7.4 | Unit | Gossip: a 3-hop accusation from confidence 800 arrives at 560, 392, 274; hop 4 and above is not passed on; a friend (`Familiarity ≥ 600`, `Trust ≥ 400`) moves ≤ 20 on a false accusation and trust in the source drops 100; no NPC receives the same subject from the same source twice within 2 days; a stranger's effective trust within 5% of its hearsay rep, a friend's within 5% of its own trust | SOCIAL 15–18 | EM |
| M7.5 | Mech | Decode error rate within ± 2 points of the configured rate at each distance band over 10,000 messages (misunderstanding). A `NeedFood` from a speaker visibly carrying food lowers the listener's trust in ≥ 95% of attended cases (**lies get caught**) | COMMUNICATION | RT |
| M7.6 | Mech | L15 vicarious learning: observer's first-choice `Eat` rate for `Toxic` ≤ 50% of a naive control's, for the safe food ≥ 150%, in 100 seeds; the observer never tasted either | NPC_LEARNING L15 | RT |
| M7.7 | Mech | Loans: a scripted repayment emits `LoanRepaid`; none by due + 1 day `LoanDefaulted`; half is partial | ECONOMY B5 | EM |
| M7.8 | Unit | Fingerprints from events: five defections and three retaliations give `RetaliationRate` 600 permille and the known median lag; a rate with fewer than `MinObs` observations is −1; a replayed TFT policy has `FirstMoveCooperation`, `ReciprocationRate`, `RetaliationRate` all above 950, a Grudger has `ForgivenessLagTicks` −1 after a defection, Always Defect has `FirstMoveCooperation` 0; likeness of TFT to its shadow above 990 and to Always Defect below 500 | GAME_THEORY 7–10 | EM |
| M7.9 | Hyp | **Island:** S3 help on credit survives cheats (5% `bench.alld`, 10% `bench.grudger`, 10% `bench.tft`; none / elders' mediation); S4 barter as a prisoner's dilemma; S9 are fakes caught (inspector on/off). **Mech** checks (the treatment changed the payoff it claims to) must pass; outcomes are recorded only, 30 seeds per arm | STRATEGY_BENCHMARKS S3, S4, S9 | RT, N |
| M7.10 | Mech | L16 domains: exploitation, cooperation, reputation | NPC_LEARNING L16 | RT |
| M7.11 | Mech | **LS5, helping someone (future reciprocity):** by days 12–15 the learner's `Help` rate is ≥ 20 points higher with `bench.tft` neighbours than with `bench.alld` neighbours, in ≥ 90% of 50 paired seeds; with episodes off the difference shrinks by at least half (reported) | NPC_LEARNING LS5 | RT |
| M7.12 | Mech | **LS6, lending to an unreliable person:** after 3 defaults by A the learner lends to A in ≤ 20% of A's requests and to reliable B in ≥ 60%, in ≥ 90% of 50 seeds; a new stranger's requests fall ≥ 10 points but stay above A's | NPC_LEARNING LS6 | RT |

**Manual:** screenshots of a gossip chain and a broken-promise event. **Done means:** trust, credit, lies and gossip operate with no referee; the benchmark policies can be measured after the fact.

### M8. Enterprise, communal property, theft and witnesses, class measures

**Goal:** NPCs found workshops and employ each other; ownership forms are neutral; classes are measured, never assigned.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M8.1 | Unit | Class fixtures: owner with two full-time workers: `EmployerOwner`, `HiresLabour` 2000 ± 10, workers `WageWorker`, `SellsLabour` 1000. Independent forager: `IndependentProducer`, `WorksOwnMeans` 1000. Cooperative members paid "wages": `CooperativeMember`, `SellsLabour` 0. Dependency: no plot, no commons, no wild food: `SubsistenceAccess < 100`, `Dependent`; a communal share covering need: 1000 and not dependent. Mixed 450/450/100: `Mixed` with memberships within 50 | CLASSES 1–5 | EM |
| M8.2 | Unit | Surplus conventions: net product 1000 FE, wages 600, owner who did no work: labour convention owner retention 10,000 (clamped), workers 600; factor convention within 1 FE. Gini known values: [0,0,0,100] = 750; [5,5,5,5] = 0; [1..100] = 330 ± 1. Mobility rows sum to 1000 ± 9; intergenerational slope 1000 for identical ranks, `|slope| < 100` for independent ranks over 1000 pairs | CLASSES 6–9 | EM |
| M8.3 | Inv | **Class measures neutral:** no decision type references `ClassPosition`, `ClassProfile`, `ClassMeasures` (G4.2); 20 seeds × 10 sim years with measures on and off give identical `StateHash` at every checkpoint; two runs give byte-identical class snapshot rows; an NPC that never saw or was told who owns its workplace has `SeenEmployedBy` unknown | CLASSES 10–13 | EM, RT |
| M8.4 | Perf | Class measures for 600 NPCs < 2 ms per sim day on one core | CLASSES 16 | RT |
| M8.5 | Inv | **Equal rules:** identical recipe, inputs, tools, skill and labour ticks give identical output per tick for `Private`, `Communal`, `Cooperative`, `State` and an independent `Npc` owner across 1000 random fixtures; no production, job, exchange or theft code branches on `EnterpriseForm` or `OwnerKind`; swapping labels between two otherwise identical enterprises leaves `StateHash` equal after relabelling ids | POLITICAL 7–9, ECONOMY D4, VISION 2 | EM |
| M8.6 | Mech | Theft with witnesses: a take against a claim in daylight within sight of 2 attending NPCs gives both an observation; the same take at night beyond sight radius gives none. First arrival: using an ownerless hut plot gives a `FirstArrival` record; a later arriver appears in `Disputes` only if it acts | ECONOMY D1, D2 | EM |
| M8.7 | Mech | **Commons, both outcomes possible:** `Commons30` with no norm: stock falls below 20% of capacity by day 15 on ≥ 6 of 10 seeds; with norm *q* = 3 seeded in 80% of members and sanctions on, stock stays above 50% on ≥ 6 of 10 seeds. Both results are reported | ECONOMY D3 | RT |
| M8.8 | Mech | Enclosure: 30 NPCs, all forage sources claimed by 3 owners who post gather jobs: mean `WageDependencePermille` of non-owners above 600 by day 10 on ≥ 7 of 10 seeds; with open access (same seeds) below 300 | ECONOMY E4 | RT |
| M8.9 | Hyp | Material estate measures: wealth Gini, parent-child rank correlation and inherited share by custom over 20 seeds × 5 generations. Expected direction (`EldestChild` gives higher within-family inequality than `EqualChildren`) is **recorded, not asserted**. The first run pins the baseline; later runs stay within ± 15% | MATERIAL 17 | N |
| M8.10 | Mech | L16 domains: enterprise, employment, property | NPC_LEARNING L16 | RT |
| M8.11 | Inv | No class label, no outcome flag in sim code (G4.4); no constructor or scenario loader can set a class position | VISION 1 | EM |
| M8.12 | Unit | **Care work and time use (Q12):** a full-time carer is not `NotInWork` and has relation shares 0; the six time-use categories plus `TicksOther` sum to the day for every NPC-day and equal the recomputation from events; unpaid care appears in every economy report | CLASSES 17–19 | EM, RT |

**Manual:** screenshot of a workshop with its employer and workers and of the class-measures view (analysis only). **Done means:** NPCs found and staff enterprises under the same rules whatever the form; classes show up only in measures.

### M9. Soldiers are NPCs

**Goal:** soldiers are persistent NPCs who enlist, are armed from the quartermaster's store, are paid and fed, and can lose morale, desert or surrender. The combat maths is unchanged.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M9.1 | Inv | Never spawned: every `ServiceRecord.NpcId` has an earlier `NpcBorn` or immigration event outside `DevCommands` scenarios (20 seeds × 20 days, zero violations) | MILITARY M-INV-1 | RT |
| M9.2 | Inv | Kit conservation each tick: quartermaster's store stock + lent + kept by deserters + dropped + spoils + destroyed = made + bought. A quartermaster's store with no keeper on shift issues no kit over 5 days, exactly | MILITARY M-INV-2, M-INV-3 | RT |
| M9.3 | Inv | `DuelSim.Series` over all 25 bare weapon pairs × 200 duels each gives exactly the pre-stage-4 counts | MILITARY M-INV-4 | EM |
| M9.4 | Inv | Replay hashes equal with recording on and off; `Stage3MilitaryPolicy` equal to the M1 baseline | MILITARY M-DET-1 | RT |
| M9.5 | Mech | **Stage 3 equivalence:** with `Stage3MilitaryPolicy`, five-a-side line-ups (all-melee vs three melee plus two ranged) over 36 seeds give A wins within ± 3 of stage 3 and survivors within ± 10%; an Attack assignment with 8 takers captures an undefended HQ within ± 20% of stage 3 time on 10 seeds | MILITARY M-EQ-1, M-EQ-2 | RT |
| M9.6 | Mech | **Enlistment is incentive-driven:** zero pay, zero rations and no signing gift find no recruit within 5 days on ≥ 95% of 20 seeds. **Dir:** doubling daily pay raises recruits in the first 5 days (significant, 20 seeds) | MILITARY M-ENL-1, M-ENL-2 | RT |
| M9.7 | Mech | **Soldiers can desert:** stopping pay and rations for 20 enlisted lowers mean morale ≥ 30% within 3 days and desertion exceeds 25% within 6 days on ≥ 18 of 20 seeds; the same force paid and fed has desertion < 5% on ≥ 18 of 20 seeds. Squad sees its leader killed: mean morale drop ≥ 100 within 1 tick (unit) | MILITARY M-MOR-1..3 | RT, EM |
| M9.8 | Mech | Rout: in 10-a-side battles with morale on, the losing side's survivors retreat, flee or surrender before death in ≥ 30% of battles over 36 seeds (stage 3 policy: 0%). A soldier below 20% health with no comrade in sight and ≥ 2 enemies within 10 m chooses `Retreat`, `Flee` or `Surrender` in ≥ 80% of 500 trials | MILITARY M-ROUT-1, M-SUR-1 | RT |
| M9.9 | Mech | Prisoners: with no guard assignment a prisoner escapes within 2 days in ≥ 70% of 50 trials, with 2 guards ≤ 20%; on `PeaceMade` every prisoner is released within 1 tick | MILITARY M-PRI-1, 2 | RT |
| M9.10 | Mech | Truce measure works: both posts on a fixed hold-fire policy emit `TruceFormed` after exactly `TruceQuietDays`; one damaging shot emits `TruceBroken` with the right cause. Herd measure: domain-of-danger values match a hand-built layout exactly | MILITARY M-TRU-1, M-HERD-1 | EM |
| M9.11 | Hyp | **Island:** M-TRU-2 bounty effect on quiet pair-days (20 seeds per arm, with and without `PerKill`); M-HERD-2 casualty rate by quartile of domain of danger over 20 raid seeds; S1 contests over berries and beds with 0, 10, 30% `bench.hawk`; S6 truces; S8 herd. Reported with confidence intervals, **no direction required**; **Mech** checks must pass | MILITARY, STRATEGY_BENCHMARKS S1, S6, S8 | RT, N |
| M9.12 | Perf | Scaling check at M9 (`bench_npc_500`, `bench_npc_1000`) | HEADLESS | RT |
| M9.13 | Mech | **LS8, joining a poorly managed military:** after the first tour, re-enlistment is ≥ 20 points lower and `Desert` while hungry and unpaid ≥ 20 points more frequent in the poorly managed arm than in the well-managed arm, in ≥ 90% of 50 paired seeds; `Frozen` learners show no arm difference in re-enlistment (± 10 points) | NPC_LEARNING LS8 | RT |
| M9.14 | Mech | **Conduct in war (Q18):** a witnessed killing of the surrendered changes every witness's trust and group reputation by the evidence-table deltas and shocks the victims' comrades' morale, while an unwitnessed one changes no record; soldiers who know of it surrender ≥ 20 points less often to that faction (20 seeds); acceptance of surrender varies between individuals and is neither 0% nor 100% overall | MILITARY M-CON-1..3 | EM, RT |

**Manual:** screenshot of soldiers as NPCs, their kit and morale; owner plays a skirmish and a defection-free war. **Done means:** the army is the population; stage 3 battles give the stage 3 results under the stage 3 policy; desertion occurs only through morale.

### M10. Citizenship, loyalty, migration, defection, political-interest measures

**Goal:** NPCs belong to a faction by choice and circumstance; they may migrate, defect or be captured; interests are measured.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M10.1 | Unit | Loyalty moves towards its target, never leaves 0..1000, and changes more slowly for high loyalty-trait NPCs. Pay default: 3 defaults in 5 days lower affected NPCs' mean loyalty by ≥ 100 vs unaffected | STATE S-LOY-1, S-LOY-2 | EM, RT |
| M10.2 | Dir | Voting with feet: two factions identical except A's sale tax at 500 permille, B's at 0: net migration from A to B over 20 days is positive on ≥ 16 of 20 seeds. `Closed` border: zero migrants become citizens over 20 days, exactly. After an HQ capture every resident's citizenship changes and its loyalty equals `CapturedLoyalty` | STATE S-MIG-1..3 | RT |
| M10.3 | Mech | With `AcceptDefectors = false` there are zero `CitizenshipChanged(defected)` events over 20 days, exactly | MILITARY M-DEF-1 | RT |
| M10.4 | Unit | Laws v1: an NPC that sees 5 unpunished thefts lowers its believed chance of being caught by at least half. With a theft law and no enforcer measured theft over days 5–15 is within ± 15% of no-law control on 20 seeds | STATE S-LAW-3, S-LAW-1 | EM, RT |
| M10.5 | Unit | Political interest: identical NPCs except one mostly state-paid: after a perceived `StateWage` posting `Material` > 0 vs ≈ 0 (`|x| ≤ 20`); an NPC whose hunger rose after two perceived tax rises has `Learned` on `(TaxRate, Up)` below −100, a control stays within ± 20; no constructor or scenario loader can set interest or political trait values; an NPC that never perceived a policy change has no trace for it | POLITICAL 1–3, 6 | EM |
| M10.6 | Mech | A broken wage promise creates a grievance with `TargetId` = employer and `Severity` > 0 and lowers trust in the employer. A state that pays every contract for 5 days raises `Procedural` legitimacy in contractors; one that defaults on half lowers it. Hash unchanged with politics telemetry off | POLITICAL 4, 5 | EM |
| M10.7 | Perf | Interest updates for 600 NPCs < 3 ms per sim day; joining scores add < 5% to think-tick time | POLITICAL 16 | RT |
| M10.8 | Mech | L16 domains: military service, faction membership, migration | NPC_LEARNING L16 | RT |
| M10.9 | Inv | **Loyalty earned, not rewarded (Q7):** no reward channel, command or scenario key writes `Loyalty` or rewards it; a faction that pays and feeds reliably has higher mean loyalty than an otherwise identical defaulting one on ≥ 18 of 20 seeds by day 20; if the optional `Belonging` drive is on, it is relieved only by contact with known people (unit) | STATE S-LOY-3, NPC_LEARNING 4.4 | EM, RT |
| M10.10 | Inv | **Temporary immigration desk (Q16):** HQ answers to 50 scripted `AskJoin` follow `ImmigrationPolicy` exactly, each with its event; no `WelcomeGift` without treasury goods | STATE S-TMP-2 | EM |
| M10.11 | Inv | No political trait or interest is set from a class measure (static) | POLITICAL 19 | EM |

*Political scenarios `Scenario_CollectiveActionCanArise`/`Fails`, `Lab_PublicGoods`, `Lab_Threshold` and the repression check are stage 5 and not part of any M0–M15 gate (POLITICAL 10–15).*

**Manual:** screenshot of the citizenship and loyalty view. **Done means:** NPCs can leave, switch sides and lose loyalty for traceable reasons only.

### M11. Cultural transmission

**Goal:** teaching, imitation, memes, dialects and traditions pass between individuals and generations, separate from individual learning.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M11.1 | Inv | `CultureStore` ≤ 280 B, 600 NPCs ≤ 170 KB, never more than 32 entries, eviction by lowest `Strength × (Visibility+64)/256` with ties to the highest index; no path writes `AssociationStore` from `CultureStore` or the reverse; a copied association is absent from the census, a discovered Technique is present | CULTURAL 1–3 | EM |
| M11.2 | Unit | Adoption maths over 10,000 draws: a child (Susceptibility 1000) from parents with Strength 200 acquires at ≈ 97 per mille per exposure ± 1%, an adult ≈ 39; `Conformity +100` on share 800 gives `FrequencyBias` +600 (odds ×1.6), on share 200 gives −600 (×0.4) | CULTURAL 4, 5 | EM |
| M11.3 | Inv | Same seed gives identical `TraitAcquired` streams; recording on/off gives the same hash; save and load mid-run gives an identical stream; the census equals a recount from the stores for every faction and generation (exact) | CULTURAL 6, 15, 16 | RT |
| M11.4 | Dir | Useful technique, 40 adults, seeded at 60%: all `Conformity +80` reaches 90% holders at least 25% sooner than all `−80`, over 20 seeds. The same experiment seeded at 20% is **reported only** (crossover) | CULTURAL 7 | RT |
| M11.5 | Mech | Neutral drift: two neutral variants at 50% in 30 NPCs, `Conformity 0`: mean share 0.5 ± 0.1 across 20 seeds after 10 sim years and SD across seeds > 0.15 | CULTURAL 8 | RT |
| M11.6 | Hyp | Harmful norm persists: 10% productivity cost, seeded 70%, `Conformity +80`: share at 3 years expected ≥ 50%; with `Conformity −60` expected < 30%. **Recorded**, "measured, not forced" | CULTURAL 9 | RT |
| M11.7 | Mech | Teaching has a cost: a session spends 300 ticks of teacher and learner time; an NPC teaching 3 times a day loses 3 × 300 ticks of work output where not combined with work. Innovation spread: time to 25% of a village of 40 is reported and always longer than the discoverer's time to teach one person | CULTURAL 10, 11 | RT |
| M11.8 | Mech | Dialect drift: two factions with no contact, `MutationPermille 20`: mean Hamming distance ≥ 2 bits after 5 sim years; with ongoing contact < 1 bit | CULTURAL 12 | RT |
| M11.9 | Mech | Migrants: one carrier into a village at 100% opposing variant with `Conformity +80` has under 5% share after 2 years in ≥ 80% of 20 seeds; a group of 10 migrants (in 40) keeps over 30%; migrants' children hold the local variant more often than their parents | CULTURAL 13 | RT |
| M11.10 | Hyp | Complex invasion: share a rival variant reaches after 3 years from 5%, with and without synergy, 20 seeds (reported only). Baselines of the controlled runs are pinned at the first M11 run, then ± 15% | CULTURAL 14 | RT |
| M11.11 | Inv | L17 import: a parent with 200 entries teaches a child (`Import`, shift 1): same seed gives the same child store hash, caps hold, imported entries are changed by the child's own experience within 5 days in a contradicting environment | NPC_LEARNING L17 | RT |
| M11.12 | Mech | L16 domains: teaching and cultural domains | NPC_LEARNING L16 | RT |

**Manual:** screenshot of the cultural traits census view. **Done means:** a technique and a convention can spread and drift between generations, bounded, with learning and culture kept apart.

### M12. Band-age institutions

**Goal:** the generic institution model with elders and mediation, healer, guards, shared store and welfare, informal lending; staff are NPCs with their own motives; Goodhart gaps are possible.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M12.1 | Inv | Never spawned: every `StaffHired` NPC had an earlier `NpcBorn` or immigration event and is alive and adult (20 seeds × 20 days). No staff, no effect: zero closures, handouts, fines or teaching over 10 days, exactly | INSTITUTIONAL I-INV-1, 2 | RT |
| M12.2 | Inv | **No KPI reward:** with no incentive terms, a forced falsified manager's return changes no state outside the institution's own records and `ReturnFiled` events (hash of every other module equal over 10 days). Spending never exceeds budget plus store; pay shortfalls emit defaults | INSTITUTIONAL I-INV-3, 4 | RT |
| M12.3 | Inv | `Sim/Policy` and `Sim/Institutions/Policies` do not reference `GodState`; replay hashes match with recording on and off | INSTITUTIONAL I-POL-1, I-DET-1 | EM, RT |
| M12.4 | Unit | `FoundInstitution` for a type whose rule fails is refused with `not_unlocked`, on every clause in turn | INSTITUTIONAL I-UNL-1 | EM |
| M12.5 | Dir | **Unlocks emerge:** a faction of 30 adults and one of 6: the larger reaches more Band unlocks within 20 days on ≥ 18 of 20 seeds; their measured ages may differ | INSTITUTIONAL I-UNL-2 | RT |
| M12.6 | Dir | **Institutions change the environment** (all vs no institution, 20 seeds, Mann–Whitney p < 0.05 unless stated): 3 staffed guards lower measured theft per 100 adults over days 10–30 (I-GRD-1); a staffed healer lowers deaths from wounds over 20 days in a raid scenario (I-HEAL-1); a staffed shared store lowers the variance of daily hunger across adults (I-WEL-1); a guard institution with zero pay has no staff after 5 days on 95% of 20 seeds and no significant theft difference (I-GRD-2); with a theft law and 3 guards measured theft is lower than no-law (S-LAW-2, size reported) | INSTITUTIONAL I-GRD, I-HEAL, I-WEL; STATE S-LAW-2 | RT |
| M12.7 | Dir | Mediation: the share of `Accuse` events followed by a fight within 2 days is lower with elders and mediation (20 seeds, effect size reported). Lending circle: a recorded circle makes a default known to ≥ 3× as many NPCs within 5 days as an unrecorded loan | INSTITUTIONAL I-MED-1, I-LEND-1 | RT |
| M12.8 | Mech | **Goodhart gap is possible** (forced traits, falsification disabled, 20 seeds): a healer with a waiting-time target, a bonus incentive and a manager with Industry and Generosity −60 improves mean wait over all cases ≥ 30% vs control while severe-case wait and severe-case deaths within 5 days are not significantly better (I-GH-1); an equal-weight severe-deaths measure shrinks the gap (I-GH-2); inspections of 20% of closed cases every 5 days with a dismissal penalty cut the falsification rate (I-GH-3); guards with a recorded-thefts-down target show a report gap with returned fall exceeding true fall and turned-away complaints rising (I-GH-4); with the same targets and **no incentive terms** neither gap differs significantly from control (I-GH-5) | INSTITUTIONAL I-GH-1..5 | RT |
| M12.9 | Inv | Under `FullInformation` the faction view's theft figure equals the actual within rounding in the same runs | INSTITUTIONAL I-GH-6 | RT |
| M12.10 | Hyp | **Natural rate of gaming on the island:** the rate at which unforced managers cherry-pick or falsify is measured over 50 seeds and recorded (no threshold) | INSTITUTIONAL open question | N |
| M12.11 | Perf | Scaling check at M12 | HEADLESS | RT |
| M12.12 | Inv | **Institutions without the state (Q17):** a group-founded communal institution runs the full daily cycle with no faction command in its history, and no code path requires a faction owner | INSTITUTIONAL I-COM-1 | EM |
| M12.13 | Mech | **Communal founding (Q17):** if M12 implements autonomous founding, a communal shared store is founded by member agreement in ≥ 10 of 20 seeds within 20 days and none without agreement; if M12 keeps only the `FoundCommunal` interface, the intent is refused with `not_yet`, the choice is recorded in DEV_LOG, and this criterion moves to Stage 5 as outstanding work | INSTITUTIONAL I-COM-2 | RT |

**Manual:** screenshot of the institution panel with staff and budget; owner founds a store and a guard post. **Done means:** institutions exist only through staffed NPCs, change the environment in the stated direction, and can fail or game their targets.

### M13a. Analytical recording core: the SQLite warehouse with person, household, economy and citizenship facts

**Goal:** a SQLite warehouse that never changes sim outcomes and reconciles exactly with the events of every milestone done so far. **Needs:** M0 and the event types of the milestones done; may be pulled forward any time after M6, after which each later milestone adds its tables.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M13a.1 | Inv | M13.1 below, for the tables whose sources exist | DW 10.1, 10.2 | RT |
| M13a.2 | Inv | M13.2 below (counts and snapshots reconcile exactly with events and state) | DW 10.3, 10.4 | RT |
| M13a.3 | Inv | M13.3 below, without the KPI-engine clause (recording never changes outcomes, every level and domain) | DW 10.5, VISION 3 | RT, N |
| M13a.4 | Inv | M13.4 below (deterministic warehouse, live equals replayed, spells consistent, no SQLite reference in sim or game) | DW 10.7, 10.8, 10.13, 10.14 | RT |
| M13a.5 | Unit | **Life history:** for 20 sampled NPCs the life-history report contains every event naming the NPC, in order, and nothing else; spells half-open and non-overlapping | DW 10.16 | RT |
| M13a.6 | Perf | M13.13 below for the recorder (budgets and size) | DW 10.9, 10.10 | RT, N |
| M13a.7 | Unit | M13.14 below (Parquet round trip, migrations) | DW 10.11, 10.12 | RT |

**Status (M13a core merged, 27 Nov 2026).** The M13a.x numbers in the table above map to the content of the same name here (checked against the table, no renumbering needed). Tests are in `DotNet/God.Runner.Tests/WarehouseTests.cs`. Met means met for the **stage 3 event set** (factions, production, combat, commands); each criterion is rerun as the NPC events arrive from M3 onwards.
- **M13a.1: met.** `EveryEventType_MapsToATable`, `ARegisteredMapper_ChangesTheMapAndTheRows_WithoutTouchingTheRecorder`, `ForeignKeys_Hold_AtFull_AndAtSummary`. Not yet covered: the 5-sim-year `band_village_100` run and `dim_party` (no NPC tables exist).
- **M13a.2: met.** `Counts_Reconcile_AtFull_ExactlyWithTheEventStreamAndTheMetrics`, `Counts_Reconcile_AtSummary_AggregatesExact_SamplesWithinTenPercent`, `Snapshots_Reconcile_WithState_OnTheLastDay`, `Kpis_FromSql_EqualTheInMemoryMetrics_Exactly`.
- **M13a.3: met.** `SqliteRecording_NeverChangesTheHashes_AtAnyLevelDomainOrSampleRate`, `SqliteRecording_MakesNoSimCalls_AndTheStateAfterEqualsTheStateWithoutIt`, `Levels_WriteWhatTheyPromise`. The 200-year nightly part waits for the nightly run.
- **M13a.4: met.** `Warehouse_IsDeterministic`, `ReRecording_AReplay_GivesTheSameContentAsTheLiveRecord` (`dim_run.stop_reason` differs legitimately, `maxTicks` live against `toTick` replay, and is excluded with the wall-clock columns), `SimAndRecordingAssemblies_DoNotReferenceSqlite`. Spell consistency has nothing to test until NPC spells exist.
- **M13a.5: open.** The life-history report needs the person, household and citizenship events from M3 onwards.
- **M13a.6: open.** The budgets and size test run on `bench_npc_500` and the 20-year `band_village_500` run, which need the NPC events. Stage 3 numbers (`bench --cases recording`, no NPCs, 30 days): SQLite metrics 3.3%, summary 7.4%, full 6.3% over off, all within the budgets of 3%/15%/60% except that metrics is 0.3 points over 3% on this small scenario; size 12.6, 34.7 and 54.9 KB per sim day. The criterion stays open.
- **M13a.7: met for what exists.** `Cli_RunWithSqlite_WritesTheWarehouse_AndExportWritesCsvAndParquet` (the Parquet files are read back and the row counts and integer column sums equal SQLite), `Schema_IsCreated_AtTheCurrentVersion_WithTheCoreTables`, `Schema_RefusesAFileOfANewerVersion`. Not yet covered: a DuckDB read of the lake, and a migration from a schema-v1 fixture (only v1 exists).

**Manual:** screenshot of the dev-mode NPC life-history view. **Done means:** any recorded run can be queried, equals the sim to the unit, and switching recording off changes nothing. The criteria M13.1–M13.4, M13.13 and M13.14 below are met here for the tables M13a builds, and again at M13b for the tables it adds.

### M13b. Institution KPIs and reports (the full M13)

**Goal:** a SQLite warehouse and Band-age KPIs that never change sim outcomes, reported through `FactionKnowledgeView` with a measurable reported-vs-actual gap. **Needs:** M12 and M13a. The criteria keep their M13.x numbers; those also listed under M13a are rerun here for the tables M13b adds.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M13.1 | Inv | Every event type maps to a table; foreign keys hold after a 5-sim-year Full run on `band_village_100` (`PRAGMA foreign_key_check` empty); polymorphic `dim_party` rows have exactly one reference | DW 10.1, 10.2 | RT |
| M13.2 | Inv | Counts reconcile: at Full, rows for births, deaths, transactions, lines, tax and promises equal Metrics counters exactly; at Summary daily aggregates equal exactly and sampled rows × weight within ± 10% for series ≥ 1000 events. Snapshots reconcile with state on the last day | DW 10.3, 10.4 | RT |
| M13.3 | Inv | **Recording never changes outcomes:** seeds 1–4 × 2 sim years (CI) and seed 1 × 200 sim years (N): Off, Metrics, Experiment, Summary, Full, each domain alone at Full, `sampleEvery` 1, 16, 64 all give identical checkpoint hashes; also 20 seeds × 10 sim years at every level (N); KPI engine on/off identical | DW 10.5, VISION 3, KPI K9, INFORMATION I12 | RT, N |
| M13.4 | Inv | Warehouse is deterministic (ordered dump hash equal, wall-clock columns excluded); a run recorded live equals the same run replayed with `--record full`; spells are consistent (no overlaps, contiguous Type 2 rows); `God.Game` and `God.Sim` do not reference SQLite | DW 10.7, 10.8, 10.13, 10.14 | RT |
| M13.5 | Unit | KPI formulas: Gini {1,1,1,1} = 0, {0,0,0,100} = 750‰; top-10% share of 10 equal values = 100‰; median {1,2,3,4} = 3; life table with constant `q` gives `e0` within 2%; waiting-time censoring (10 requests, 5 served at 1 day, 5 open at 20 days: median 10.5, rounds to 11, `median_closed` 1, open share 500‰); reoffending (100 releases, 30 true, 20 detected: actual 300‰, reported 200‰); rank slope 1000 / ± 100; `div_round(−5,2) = −3`, `div_round(5,2) = 3`; no floating point in `Sim/Information/Kpi` | KPI K1, K7, K8, K11, K12 | EM |
| M13.6 | Inv | KPI parity: for 5 seeds every KPI in the warehouse view equals the C# function exactly; `fact_kpi.actual_value` equals the catalogue reference SQL for every period of a 2-year run | KPI K2, DW 10.6 | RT |
| M13.7 | Inv | **FullInformation equals truth:** every KPI at every period end over 5 sim years and 5 seeds: `|reported − actual| ≤ 1` unit of the last digit, `Lo == Hi`, `Coverage == 1000`, `Quality == Exact`. A min-cell suppression (`< MinCell` rows) is `Missing` in the view and present in the warehouse | INFORMATION I2, KPI K3, K4 | RT |
| M13.8 | Mech | **Information layers can degrade, so Goodhart gaps are possible:** a survey at `N = 2000`, true share 300‰: RMSE ratio `n=50`/`n=800` 4.0 ± 25%, `n=200`/`n=800` 2.0 ± 20%, 95% intervals contain truth 93–97% of 500 repeats; full-frame exact; nonresponse bias ≥ 100‰ low at 400‰ response of the aggrieved; one falsifying recorder (`MaxShade` 150‰, 100% of 400 theft records): reported/actual 850‰ ± 30‰ with 400 `Falsified` audit rows and no distinguishing field; more inspection shrinks the gap monotonically over three intensities | INFORMATION I3–I6 | EM |
| M13.9 | Mech | Better counting looks worse: with `actual` forced identical, raising guard `detect` from 300‰ to 600‰ doubles the reported count ± 10% while actual is unchanged and the view marks a `SeriesBreak`. Reports respect delay (nothing readable before `PublishedTick`; with `d = 2` `AsOfTick` lags `PeriodEnd` ≥ 2 days). Loss 200‰ gives `Coverage` 800‰ ± 40‰; symmetric noise ± 100‰ has mean error within ± 10‰ | INFORMATION I7–I10, KPI K6 | EM |
| M13.10 | Mech | The scorecard gap alert (CRM-01 vs CRM-02) fires within 3 years in ≥ 90% of 20 seeds with one falsifying recorder and in ≤ 5% of honest controls | KPI K5 | RT |
| M13.11 | Inv | Records are sim state, deterministic and saved: save and load mid-run gives an identical hash; the same seed draws identical survey samples; the true-state overlay does not change the sim | INFORMATION I11–I13 | RT |
| M13.12 | Mech | Band KPI set present: a 20-year run has a value or an explicit `Missing` reason for each `S4` KPI (POP-01..03, HLT-01, LIV-01, EMP-01..02, INQ-03, CRM-01, TRU-01, POP-09) on every period, with source and `AsOfTick`, in both `FullInformation` and a degraded `Reported` profile | INFORMATION I17, KPI K10 | RT |
| M13.13 | Perf | Recording budgets (Metrics ≤ 3%, Summary ≤ 15%, Full ≤ 60% on `bench_npc_500`); 20-sim-year Summary `band_village_500` × 2000 days ≤ 1 GB (target 600 MB); 200-year nightly file ≤ 1 GB | DW 10.9, 10.10 | RT, N |
| M13.14 | Unit | Parquet round trip: `export` then DuckDB gives equal row counts and sums per table; a schema-v1 fixture migrates and the example queries return results of the right shape | DW 10.11, 10.12 | RT |
| M13.15 | Hyp | Scenario descriptive checks, 50 seeds × 6 sim years: Enclosure dependency rate above 300 permille by year 5 is computed for every seed and is neither 0% nor 100% (G4.11); no threshold on the rate itself | CLASSES 14 | N |
| M13.16 | Unit | Media: only the notice-board `Outlet` exists; I14–I16 (reach, learned credibility, dual-channel beliefs) are stage 6–7 and **not** gated | INFORMATION 8 | n/a |
| M13.17 | Inv | **No composite wellbeing (Q23):** no KPI definition, report template or dashboard sums or weights KPIs from different wellbeing dimensions; LIV-06 is five separate drive levels | KPI K13 | EM |
| M13.18 | Inv | **Time use reconciles (Q12):** TIM-01 and TIM-02 equal their recomputation from events for every adult-day (5 seeds) and are present in the reports with unpaid care beside paid work | KPI K14 | RT |

**Manual:** screenshots of the HUD reports panel (each KPI with source, as-of date and quality). **Done means:** the warehouse can be queried for any recorded run, equals the sim to the unit, and switching it off changes nothing.

### M14. Incentive faction AI, balance, HUD and tutorial rewrite, play guide

**Goal:** AI factions govern by incentives only; the sandbox is balanced; the HUD and tutorial are rewritten for the new game; the policy-experiment harness works.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M14.1 | Inv | **Incentives only, nothing built by magic:** with the incentive AI on 20 seeds × 10 sim years, every building completed was built by NPC labour and committed goods (G3.6 holds with AI factions); the AI reads only `FactionKnowledgeView` and uses only commands the HUD can send (G4.1, G4.5) | STATE, INSTITUTIONAL, CONVENTIONS 2 | RT, EM |
| M14.2 | Inv | Stage 3 mode (`VictoryPoints10`, `Population` off) still plays and wins (G6.1); the 10 VP race on the Stage 4 rules runs 12 seeds to a result without invariant failures; a sandbox of 10 sim years on 5 seeds never ends while two factions remain | STATE S-VIC-1, G6 | RT |
| M14.3 | Mech | **Balance (set before the run, from the sandbox-first rule):** over 20 seeds × 10 sim years the default sandbox (`band_6x20`) has no invariant failure; no extinction of any faction caused by a rule bug (extinctions are reported, and each is classified as meaningful outcome, defective mechanic or bad baseline; genuine collapse is not tuned away); mean daily nutrition and population within the bands of M4.5. Thresholds recorded in this document before the first balance run *(derived, to be filled in at M14)* | CONVENTIONS 10, 14 | RT, N |
| M14.4 | Inv | `experiment` rerun gives identical per-run hashes, metrics and `results.csv` (bitwise with the recorded analysis seed); 1 worker and 8 workers, or arms in a different order, give identical results; changing a registered plan field makes the runner refuse (an unlisted metric is flagged exploratory); an unknown or seed-touching override path is rejected before any run; recording level `none` and `experiment` give equal final hashes | POLICY_EXPERIMENTS | RT |
| M14.5 | Inv | CRN: two arms that differ only in a parameter first used on day 10 have equal hashes at day 10 on every seed | POLICY_EXPERIMENTS `Experiment_CRN_Alignment` | RT |
| M14.6 | Mech | **A/A tests:** different independent seed sets: over 20 repeated A/A experiments of 40 runs per arm the unadjusted share of significant tests lies in 2–9%, Holm family-wise error ≤ 6%; same seeds and no overrides: every pair of run hashes is equal and every estimated difference exactly 0 | POLICY_EXPERIMENTS `AA_*` | RT |
| M14.7 | Mech | **Planted effect:** a test-only override (+10% food per forage action, or death-rate × 0.8) is detected with power ≥ 80% at the planned n, estimated within 20% of the planted effect, the interval contains it in ≥ 90% of 50 repeats; a metric the effect cannot influence shows no significant change after correction | POLICY_EXPERIMENTS `Planted_*` | RT |
| M14.8 | Unit | Holm and Benjamini–Hochberg match textbook vectors; the bootstrap interval is reproducible under the recorded seed and varies < 1% in width over 10 other seeds; the power calculator reproduces the 4.4 table; a null-only report lists every primary metric with interval and adjusted p-value; every table and figure is labelled `causal` or `descriptive` | POLICY_EXPERIMENTS | EM |
| M14.9 | Hyp | **Demonstration experiments** (≥ 30 seeds per arm, time budget documented): communal against private hut ownership on the Band outcomes (survival, child survival, `wealth_gini`, `cooperation_rate`, `food_surplus_days_share`, `population`); tax rate 0%, 10%, 20% of the harvest on productivity, inequality and population; a three-parameter one-at-a-time sensitivity sweep gives the sign-agreement table. The experiments must run to completion with their reports and the neutrality checklist; **no effect is required** | POLICY_EXPERIMENTS | N |
| M14.10 | Inv | **Not inevitable:** each headline emergent outcome the scenarios claim as possible (commons collapse, property concentration, desertion, a failed institution) occurs in > 0% and < 100% of 100 seeds (G4.11); each report names its control and seed count | VISION 4, 5 | N |
| M14.11 | Inv | Recording neutrality at every level over 20 seeds × 10 sim years; no outcome flag in sim code; form neutrality (G2.2, G4.4, G4.6) | VISION 1–3 | N |
| M14.12 | Man | The tutorial is rewritten: `TutorialTests`, `TutorialEndToEndTests` and `TutorialHudTests` are replaced by equal-strength tests for the new flow (designate a site, post a contract, read a report); the PlayMode HUD, menu and boot tests pass; the play guide matches the game | UNITY_POC_PLAN | EM, PM |
| M14.13 | Inv | **Neutral vocabulary (Q2):** HUD strings, tutorial cards, KPI names and report labels contain no theorist's name or listed theory term outside the About/Philosophy page | VISION 11.6 | EM |
| M14.14 | Inv | **Composites declared (Q23):** an experiment plan using a composite without components, weights and limitations is refused; a report with a composite shows every component | POLICY_EXPERIMENTS `Composite_Declared` | EM |

**Manual:** the owner plays a full sandbox for several generations and one 10 VP game from the menu; screenshots of every new HUD panel; the owner decides whether stage 3 mode is retired. **Done means:** a playable, balanced sandbox and race on the new rules, with the experiment harness proven by A/A and planted-effect tests.

### M15. Network readiness

**Goal:** policies can be replaced by integer-only networks that replay exactly, with a verified Python bridge. No network is trained in stage 4.

| # | Kind | Criterion | Source | Suite |
|---|---|---|---|---|
| M15.1 | Unit | `IntNet` dense layers match hand-computed outputs exactly including rounding and saturation; the exporter rejects a model whose worst-case accumulator exceeds int32 (tests run `checked`); no `float`, `double` or `Math.` in `Sim/Policy/IntNet*` (G4.9) | AI_TRAINING | EM |
| M15.2 | Inv | C# `IntNet` and the Python integer mirror give byte-identical logits on 10,000 recorded observations per level; the same test passes under `dotnet test` and Unity EditMode (Mono, and IL2CPP when available) | AI_TRAINING `IntNet_CsMatchesPython_10k`, `IntNet_Platform` | RT, EM |
| M15.3 | Inv | Model file write, read, hash equal; one flipped bit fails to load; a model with a different encoder or `MindView` layout version is refused; `verify-model` passes a good model and exits non-zero when one weight differs in the Python mirror | AI_TRAINING | RT |
| M15.4 | Inv | A game with `IntNet` civilian and faction policies for 2 sim years run twice gives the same `StateHash` at every checkpoint, also with a mid-run save and load; all 16 scripted/trained combinations of the four levels run and replay identically | AI_TRAINING `Policy_Replay_Identical`, `Policy_Mixed_Levels` | RT |
| M15.5 | Inv | **Encoding never leaks truth:** encoding the same state twice gives equal tensors regardless of collection insertion order (test build); encoder types reference no `GodState`; changing true-state fields the layer does not expose (an unseen far stockpile, another faction's hidden treasury) leaves the tensor unchanged; values within documented ranges, padded rows zero, masks match row counts | AI_TRAINING `Encoding_*` | EM |
| M15.6 | Inv | Masks match validation on 10,000 sampled decision points (legal passes, illegal fails); masked argmax ties go to the lowest index, all-masked returns `Stop` | AI_TRAINING `Masks_MatchValidation`, `Masked_Argmax_Ties` | EM |
| M15.7 | Inv | **Network inputs use learned state:** `IntNetNpcPolicy` with a test model copying `Q(c)` into its score picks the same action as the default policy with the instinct term zeroed in 100% of 1,000 recorded decisions. Loopback: an external client via encode/decode gives the same hashes as the scripted policy | NPC_LEARNING L18, HEADLESS `Loopback_External_EqualsScripted` | RT |
| M15.8 | Inv | Dataset recording on/off gives the same hash | AI_TRAINING | RT |
| M15.9 | Mech | **Behaviour cloning sanity:** a small `IntNet` (2 × 128) cloned from the scripted civilian reproduces its verb on > 99% of held-out decisions when deterministic (≥ 95% of the noise ceiling otherwise); the scripted faction policy > 99% on verb and > 97% on the full action; a game played by the cloned civilian matches the scripted game on survivors at day 30 and food stock within 10% over 20 seeds | AI_TRAINING `BC_*` | RT |
| M15.10 | Inv | The Python environment with a fixed action rule and seed gives the same observation hash sequence on two runs | AI_TRAINING `Bridge_Determinism` | RT |
| M15.11 | Perf | Bridge throughput is printed; more than 30% regression from the stored baseline logs a warning and **does not fail** the build | AI_TRAINING `Bridge_Throughput_Report` | RT |
| M15.12 | Mech | **LS9, shared network, personal experience:** two identical-genome NPCs on the same `IntNetNpcPolicy` with different LS6 histories differ in lending to A by ≥ 30 points in ≥ 95% of 50 seed pairs; identical histories give identical decisions; the store keeps changing under the network | NPC_LEARNING LS9 | RT |

**Manual:** none. **Done means:** a network can be plugged in at any of the four levels, replays exactly, and can only see what a human at that level sees.

---

## 4. Traceability

### 4.1 Abbreviations for sources

POPULATION, BIOLOGICAL (BIOLOGICAL_INHERITANCE), HOUSEHOLDS, NPC_LEARNING, COMMUNICATION (COMMUNICATION_AND_COGNITION), SOCIAL (SOCIAL_MEMORY), CULTURAL (CULTURAL_TRANSMISSION), MATERIAL (MATERIAL_INHERITANCE), ECONOMY, CLASSES (ECONOMIC_CLASSES), POLITICAL (POLITICAL_ECONOMY), STATE, INSTITUTIONAL (INSTITUTIONAL_STATE), MILITARY, INFORMATION (INFORMATION_AND_MEDIA), KPI (KPI_CATALOGUE), DW (DATA_WAREHOUSE), GAME_THEORY, STRATEGY_BENCHMARKS, HEADLESS (HEADLESS_SIMULATION), POLICY_EXPERIMENTS, AI_TRAINING, VISION (VISION_AND_PHILOSOPHY), CONVENTIONS (DESIGN_CONVENTIONS). All files are in `docs/stage4/` and end in `_DESIGN.md` or `.md` as listed in the conventions' document map.

### 4.2 Owner requirement to test

| Owner requirement | Proven by | Kind |
|---|---|---|
| **Lifetime learning in all domains** (survival, gathering, production, gear, barter, cooperation, exploitation, relationships, communication, work, military service, faction membership) | M3.1–M3.11 (L1–L13: eat when hungry, delayed reward, avoidance, forgetting, nature vs nurture, generalisation), M5.5–M5.6, M5.11, M6.13, M7.6, M7.10, M8.10, M10.8, M11.11–M11.12 (L14–L17: **L16 per domain, added in the milestone that introduces each domain**); L7 learning on vs off, `Frozen` unchanged; M4.11 (learning never inherited); M15.7 (L18: networks take learned state); the owner's eight scenarios LS1–LS8 (M3.20–M3.22, M5.13, M6.14, M7.11, M7.12, M9.13) and LS9 (M15.12) | Mech, Inv |
| **NPCs never read true state** | G4.1, G4.2, G4.5 (reflection); M1.1–M1.3; M3.12 (perception-only on every decision); M3.16; M8.3 (`Perception_NotTruth`); M10.5 (no trace of an unperceived change); M15.5 (encoding leak test); M4.11 | Inv |
| **Gestures as real mechanics incl. lies and misunderstanding** | M5.4 (every `GestureMade` has `MessageDecoded`), M5.5, M7.1 (lying promise), M7.4 (gossip errors), M7.5 (decode error rate within ± 2 points; lies caught), M5.7 (defection possible); Unity only animates (G4.5, manual screenshot at M5) | Inv, Mech |
| **Four inheritances kept distinct** | G4.7; M4.8–M4.11 (`FourInheritances_Separate`, `NoFitnessFunction`, genome mutation and recombination numbers); M4.19–M4.21 (material, conserved, by custom); M11.1 (no cross-write between learning and culture stores); M11.11 (imports bounded and revised by own experience) | Unit, Inv |
| **Classes emergent, not assigned** | M8.1–M8.3 (measured as relation to property, labour and surplus), M8.11, G4.2, G4.4; M13.15 and G4.11 (property concentration possible but not inevitable); M8.8 (enclosure, dependence by circumstance) | Unit, Inv, Mech |
| **No forced outcomes** | G4.3 (no target frequencies), G4.11 (not inevitable), M2.19 (no island test that a book result appears), M8.7 (both commons outcomes possible), M9.11, M11.6, M12.10, M14.9, M14.10 (**Hyp**: recorded, not asserted); §1.4 rule 5 (no tuning to an effect) | Inv, Hyp |
| **Exact determinism and replay** | G2.1–G2.10; M0.1–M0.8; every milestone's determinism row (M3.8, M4.22, M6.11, M9.4, M11.3, M12.3, M13.11, M15.4); lab hashes M2.14 | Inv |
| **Recording never changes outcomes** | G2.2; M0.2; M13.3; M14.4 (recording level `none` vs `experiment`); M14.11; M13.5/M13.7 for the KPI engine on or off | Inv |
| **Incentives only; nothing built by magic** | G3.6; M6.2 (walkers removed); M6.5 (no magic); M6.7–M6.8 (pay and purchase offers move choices); M9.2 (no keeper, no kit); M9.6; M12.1 (no staff, no effect), M12.2 (no KPI reward); M14.1 | Inv, Mech |
| **Soldiers as persistent NPCs who can desert** | G3.5; M9.1 (never spawned); M9.5 (stage 3 equivalence of combat); M9.6–M9.8 (enlist for pay; unpaid forces desert > 25% in 6 days, paid < 5%; surrender and rout); M9.9; M10.3 (defection closed) | Inv, Mech |
| **Goodhart gaps possible** | M12.8 (I-GH-1..5 target and report gaps, no gaming without incentive), M12.9; M13.8–M13.10 (falsifier, better-counting-looks-worse, gap alert); M12.2 (nothing rewards a reported KPI directly); M12.10 (natural rate recorded) | Mech, Hyp |
| **Information layers** | G4.5 (layer 7 is the only HUD and AI input); M1.2, M1.5; M13.7 (`FullInformation` equals truth, layers still separated); M13.8–M13.12 (layer 4 records, degradation, delay, coverage); M7.4 and M7.5 (layer 3); M3.12 (layer 2); M13.16 (layer 5 minimal); layer 1 only by G4.1, G4.2 | Inv, Mech |
| **Lab vs island kept separate** | §1.5; M2 (all numeric, lab only); M2.19; island rows M3.18, M5.12, M7.9, M9.11 (mechanism + direction, outcomes recorded); G4.3 | Lab, Mech, Hyp |
| **Conservation, no dead owners, ids never reused** | G3.1–G3.3; M4.19; M6.1; M9.2 | Inv |
| **Performance at the stated scale** | G5; M3.19; M6.11; M8.4; M10.7; M13.13 | Perf |
| **Owner answers of 27 Nov (Q1–Q23)** | Q1: M3.23, M5.14 · Q2: M14.13 · Q3: M4.3, M4.26 · Q4: M4.8, M4.30 · Q5, Q6: M4.28 · Q7: M10.9 · Q8: M6.15 · Q9, Q10: M6.17 · Q11: M6.18 · Q12: M4.32, M8.12, M13.18 · Q13, Q14: M4.31 · Q15: M4.29 · Q16: M6.19, M10.10 · Q17: M12.12, M12.13 · Q18: M9.14 · Q19: M6.16 · Q20: POLITICAL 18 (stage 5) · Q21: M1.9 · Q22: M1.5, M1.8 · Q23: M13.17, M14.14 | all kinds |

### 4.3 Owner evidence list (27 Nov)
The owner asked for evidence of each item below. Each row names the tests that provide it and their tier (section 1.3).

| Evidence wanted | Proven by | Tier |
|---|---|---|
| Genuine individual learning | M3.1–M3.11 (L1–L13), M3.20–M3.22 (LS1, LS3, LS4), M5.13 (LS2), M6.14 (LS7), M7.11–M7.12 (LS5, LS6), M9.13 (LS8); every LS compares against `Frozen` | controlled model |
| Contextual and delayed reinforcement | M3.3 (L3 traces), M3.11 (L13 conjunctions), LS2, LS5, LS7 (episodes on vs off), LS6 (`NpcRef` specificity), LS1–LS4 context controls | controlled model |
| Different behaviour from different experience | M3.7 (L8 same nature, different nurture), LS5, LS7, LS8 (arms differ only in experience), M15.12 (LS9 under one network) | controlled model |
| Multigenerational population stability | M4.5, M4.6, M4.27 (owner default `band_6x20`, size variants, immigration off), M4.24 | controlled model, statistical island |
| Genealogical integrity | M4.10 (relatedness exact), M4.11, M4.22 (pedigree hash), G3.3 (ids never reused), G3.7 (one death cause) | deterministic unit |
| Resource conservation | G3.1, M6.1, M9.2, M6.18 (forest stock exact) | deterministic unit |
| Population conservation | M4.25 (NPCs ever alive = founders + births + immigrants; never spawned), G3.5, M9.1, M12.1 | deterministic unit |
| Estate and ownership conservation | M4.19, M4.31 (transferred, never duplicated), G3.2 | deterministic unit |
| Knowledge isolation | G4.1, G4.2, G4.5, M1.1–M1.3, M1.9 (no foreign truth), M3.12, M8.3, M10.5, M15.5 | deterministic unit |
| Meaningful non-verbal communication | M5.4, M5.5 (gesture learning), M7.1, M7.4, M7.5 (lies caught, misunderstanding), M5.7 | controlled model |
| Cooperation, exploitation and retaliation | M5.6, M5.7, M7.3, M7.8 (fingerprints), M7.9 (S3, S4), M4.29 (shared childcare exploited and answered), LS5, LS6 | controlled model, statistical island |
| Emergent specialisation | M6.10 (`TwoFoods` with `ExchangeOff` control), M6.17 (`LogsAndClay`) | controlled model |
| Different ownership and employment relationships | M8.1–M8.3, M8.5 (form neutrality), M8.8 (enclosure), M6.16 (suspension), M12.12 (communal without the state) | deterministic unit, controlled model |
| Deterministic replay | G2.1, G2.6, M0.1, M0.7, M3.8, M4.22, M15.4 | deterministic unit |
| Save/load continuation parity | G2.3, M0.6, M3.8 (L9 with open traces), M4.22 (open estates), M13.11 | deterministic unit |
| Parallel independence | G2.4, M0.4, M4.30 (mutation rates in parallel), M14.4 | deterministic unit |
| Headless and Unity parity | G2.5, M0.8 | deterministic unit (NIGHT) |
| Telemetry independence | G2.2, M0.2, M3.8 (L10), M8.3, M13.3, M14.11 | deterministic unit |
| KPI reconciliation | M13.2, M13.6, M13.7, M13.18, M4.7 | deterministic unit |

---

## 5. Conflicts between the documents' test numbers (stricter value taken)

| # | Conflict | Resolution here |
|---|---|---|
| C1 | **Stage 3 seeds.** STATE S-VIC-2 and MILITARY M-DET-1 ask that stage 3 seeds 1–12 replay with the **same** end tick, winner and hashes as before stage 4. CONVENTIONS section 3 says M0 moves dice and other draws onto keyed forks, which changes stage 3 results once, and accepts it. | M0 re-baselines once. The M0 reference (end tick, winner, hashes) is the baseline for G6.5, M1.4 and M9.4. Pre-M0 equality is not required; the stage 3 smoke verdict (a win) is. **Settled by the owner on 27 Nov:** Stage 3 save compatibility is not needed, and the pre-M0 game is kept as the git tag `stage3-final` (at `d7eeb95`). |
| C2 | **Households per capita:** HOUSEHOLDS `Stable_10Generations` 0.15–0.5, `Household_Size` 2.5–6.0 mean (equivalent to 0.17–0.4). | Use 0.17–0.4 (M4.5). |
| C3 | **Seeds per island arm:** GAME_THEORY says at least 20 seeds with a one-sided sign test at 5%; STRATEGY_BENCHMARKS says at least 30 seeds per arm, paired, with bootstrap; STATE, INSTITUTIONAL and MILITARY use 20 seeds with Mann–Whitney. | Island scenarios in STRATEGY_BENCHMARKS use 30 per arm; the STATE, INSTITUTIONAL and MILITARY direction tests keep their stated 20 seeds and Mann–Whitney p < 0.05 (a different, unpaired design); the paired sign test is reserved for paired scenarios. |
| C4 | **Recording-neutrality scale:** HEADLESS and DW use seeds 1–4 × 2 sim years (CI) and 1 × 200 years (N); VISION 11.3 asks for every level, 20 seeds × 10 sim years; CLASSES 11 asks 20 seeds × 10 years with measures on/off. | Both: the CI set at every milestone and the 20 × 10 set in NIGHT before a milestone closes. |
| C5 | **Determinism checkpoint spacing:** POPULATION, BIOLOGICAL and HOUSEHOLDS use every day boundary for 10 sim years; NPC_LEARNING L9 uses every 100 ticks; HEADLESS uses its configured checkpoints. | Every day boundary for module determinism tests; every 100 ticks for the learning and save/load tests. |
| C6 | **"Not inevitable" bands:** VISION 4 says > 0% and < 100% of 100 seeds; POLITICAL 13 says strictly between 5% and 95% over 200 seeds; CLASSES 14 says neither 0% nor 100% over 50 seeds. | Each scenario keeps its own stricter band; the general rule is 100 seeds (G4.11). |
| C7 | **Milestone of the experiment command:** CONVENTIONS and HEADLESS put `experiment` at M14; POLICY_EXPERIMENTS says M13 and M14. | Harness tests are gated at M14 (M13 gates only the KPI inputs). |
| C8 | **Old milestone numbers:** POPULATION and COMMUNICATION still use the plan's old numbers (for example "M10" for families, "M6" for lies); ECONOMY open question 1 notes walker removal at M5 vs M6; GAME_THEORY open question 5 repeats this. | Conventions' numbers used throughout. |
| C9 | **Welfare tests:** STATE S-WEL-1 and S-WEL-2 need a staffed granary and a keeper, but institutions arrive at M12; STATE lists them under "M6 onwards". INSTITUTIONAL I-WEL-1 is the M12 version. | S-WEL-1 and S-WEL-2 are gated at M6 with a keeper as an ordinary state job (not an institution); I-WEL-1 is the M12 gate. If no keeper job exists at M6, the tests move to M12. |
| C10 | **Conservation wording:** ECONOMY A1 requires `start + Produced − Consumed = Σ stocks` exactly; MATERIAL 2 allows `destroyed` and `taxed`; MILITARY M-INV-2 adds `kept by deserters`, `dropped`, `spoils`. | One ledger with every reason code (G3.1); the stricter exact equality holds with all reasons counted. |
| C11 | **Audit sidecar in the hash:** INFORMATION open question 2. | Hashed, so falsification is replay-checked (M13.11). Confirm. |
| C12 | **Tolerances for the same mechanism:** NPC_LEARNING L1 ("within 5 think ticks, ≥ 95% of 100 seeds") against COMMUNICATION's reuse of L1, L7, L8 for the decider. | The learning figures apply unchanged; the decider tests assert the same numbers. |
| C13 | **Class measure inputs:** KPI INQ-03 uses personal wealth (goods and huts), CLASSES may redefine wealth; both documents flag it. | The reference index is `WareRefValue` (`dim_ware.ref_value`, owned by ECONOMY); the KPI test K1 uses the vector stated in KPI; the classes tests use the FE index. Both are recorded beside the observed valuation. |
| C14 | **Unbounded "calibration" numbers:** many sources mark numbers as prototypes (L2 shares, L8 scores, L8 ranks, L3 CV, Inbreeding_Depression, class and culture baselines). | Treated as calibrate-once-and-pin with ± 15% regression bounds (§1.4 rule 3), not as design constants. |
