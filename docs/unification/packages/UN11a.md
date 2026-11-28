# UN11a: Assurance, analytics and the UA report (mechanical, learned, long-run columns)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

| Field | Value |
|---|---|
| Package identifier | **UN11a** (Tier UA, Waves 1–4; preparation from W1, UA-mech gate H13.0–H13.5, UA-learned gate H15.4–H16.0) |
| Branch / worktree | `unity/un11a` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un11a`, from `unity/poc` after **UN0** merged (preparation); the gate runs after the prerequisites below |
| Effort | 3.5 agent-hours spread over the run (planned H11.1–H16.0) |
| Status | Not started. Replaces the D8 UN11 spec (acceptance D8 rows are reworded by the D9 acceptance table) |
| Recommended agent | **Sonnet** (bounded tests, mappers, scenarios and reporting; the lead reviews every figure). Never Fable |

## 1. Objective
Turn the D9 acceptance matrix into runnable, honest evidence and write the UA report: migrations `0044`–`0046`, the unified warehouse mappers (completing the stubs each package wrote), analytics that **separate events, observations, claims and promises** (QA-02), the reconciliation, the unified save and replay parity (U-P5, U-P6), the matrix tests (EC, EP, CF, TR, QA, CATALOG, MOTOR, NEED, ECON, U and B rows), the `unif_*` scenarios and runs, benchmarks at 72, 600 and 1,200 people, and `docs/runs/UA_REPORT.md` with separate **mechanical**, **learned/autonomous** and **long-run viability** columns.

## 2. User-facing outcome
The owner gets a report that says, row by row, what is proven mechanically (scripted fixtures, labelled), what the trained founders actually did on held-out maps, and what long-run viability looked like, each with the controls, seeds, counts, hashes and a plain statement of what did not run or did not pass. No fixture result is presented as autonomous behaviour; no failed gate is hidden.

## 3. Design anchors
- Amendment §7 (acceptance additions EC-01…QA-02; mechanical, learned and long-run results are separate; failure to exhibit a strategy is a measured outcome, failure of the basic survival gate is a blocker), §6 (training honesty), §3 (analytics: no legitimate-owner or theft fact), §8.
- Index §7 (UA-mech and UA-learned definitions), §6 merges 10 and 14, §10 (acceptance map), §2 row UN11a; §D9.6 (benchmarks at 72, 600 and 1,200 from UA on); design §0.2.5 rows CATALOG-1, MOTOR-1/2, TRAIN-1/2, NEED-1, ECON-1/2 (as folded into the index §10).
- §23.2 (pins), §23.11 (U-C1–U-C6), §23.12–§23.14 (events, migrations, hash part and save), §23.16 (required shared tests), R82, R84.
- `docs/stage4/ACCEPTANCE_TESTS.md` "D9. Unification acceptance (28 Nov 2026)" (the rows this package implements).

## 4. Relevant existing source files (verified)
- Warehouse: `DotNet/God.Runner/Warehouse/WarehouseRecorder.cs`, `WarehouseWriter.cs`, `EventMapping.cs`, `Tables.cs` and `Tables.*.cs`, `*Mappings.cs` (pattern: `C1Mappings.cs`, `D1Mappings.cs`), `BatchIndex.cs`, `Export.cs`, `Migrations/*.sql` (present up to `0026_early_views.sql` on `unity/poc`; gaps are tolerated: `DotNet/God.Runner.Tests/MigrationSetMembershipTests.cs`, R64), `post_load.sql`, `ExampleQueries.sql`, `RecordingBench.cs`; reconciliation pattern in `DotNet/God.Runner.Tests/F1WarehouseTests.cs`, `M4WarehouseTests.cs`, `M5WarehouseTests.cs`, `PopulationWarehouseTests.cs`.
- Replay, save and hashing: `Assets/God/Recording/Recording.cs`, `Recorders.cs`, `Replay.cs`, `EventJson.cs`, `Scenario.cs`, `SmokeVerdict.cs`; `Assets/God/Sim/Save/SaveFile.cs` (read; the version bump is the lead's), `Core/GodSim.cs` (`AllHashPartNames` at `:35`, `StateHash` at `:160`), tests `DotNet/God.Runner.Tests/ReplayTests.cs`, `DeterminismTests.cs`, `SmokeParityTests.cs`.
- Runner and scenarios: `DotNet/God.Runner/RunEngine.cs`, `SimSession.cs`, `Batch.cs`, `Verify.cs`, `Bench.cs`, `Bench/baseline.json`, `Bench/q1_baseline_2026-11-27.json`, `Scenarios/bench_npc_100|120|300|500|1000|2000.scenario.json`, `Demog/DemogReport.cs`, `Kpi/KpiEngine.cs` (report-writer models).
- Pins: `DotNet/God.Runner.Tests/PolicyRunTests.cs:27`, `F1NeutralityTests.cs`.
- The unified sources the tests exercise (created by UN0–UN9; not present at spec time): `Unified/*`, with event codes 1200–1399 and `UnifiedState` tables.
- Report precedent: `docs/runs/` and `docs/stage4/DEV_LOG.md` result tables.

## 5. Allowed scope
1. **Migrations** `0044_unification_core.sql` (world, motor, interactions, construction, needs day rows), `0045_unification_social.sql` (exchange, claims, conflict: **separate tables for events (objective moves and strikes), observations (who perceived what) and claims/promises (beliefs and communications)**; no legitimate-owner or theft fact), `0046_unification_brain.sql` (artifact, training and evaluation metadata, knowledge spells, TR-01/TR-02 results); `0047` stays reserved for UB (UN11b). References only earlier migrations that exist on `unity/poc`.
2. **Mappers:** complete `UN1Mappings.cs`, `UN2Mappings.cs`, `UN3bMappings.cs`, `UN4Mappings.cs`, `UN6Mappings.cs`, `UN7Mappings.cs`, `UN8aMappings.cs` (stubs from the event-owning packages) for events 1200–1339; `Tables.Unified*.cs`; `control_code` columns fill from the event `Origin`; example queries added to `ExampleQueries.sql`. Legacy `fact_theft`, `fact_ownership_spell`, `fact_use_right`, `fact_inheritance` stay legacy-only (documented; zero rows in unified runs).
3. **Reconciliation** (`UA*Warehouse*` tests): goods ledger and environment books balance to zero error from warehouse rows alone (produced − consumed = carry + ground + bins per ware: U-C1); every `HandedOver` has one `ItemMoved` and no ownership row; every site's contributions sum to its materials and labour; every strike row has an event; observations and claims never appear in the events table.
4. **U-P5** `Unified_Recording_Neutral`: Off, Summary and Full recording of domains 80–88 give one state hash. **U-P6** `Unified_SaveReplay_Exact` (full; UN2 supplied the stub): a mid-state with a site half built, a reservation open, a held-out lot, a strike in progress and a person asleep in a bed; save → load → continue equals the uninterrupted run; a replay of a unified run equals it at every checkpoint.
5. **The matrix as runnable tests** (names in the table of §13): mechanical rows (UA-mech list), learned rows that **read** UN6t-b's JSON results and verify rather than recompute (TR-01 verdict from thresholds and results, hash predating, artifact hash match), reported rows (EC-02, TR-02, EP-03 learned, CF-02, ECON-1 learned), invariants U-C1–U-C6 on every `unif_*` run, the per-package `*_SaveLoadParity` collected in one list.
6. **Scenarios and runs:** `DotNet/God.Runner/Scenarios/unif_*.scenario.json` (`unif_start` the 72-person unified start, `unif_world`, `unif_exchange`, `unif_conflict` are named by their packages; UN11a owns `unif_normal`, `unif_long`): 72 people, seeds 1–3, 20 days at UA-mech with the **untrained** brain (outcome reported, not judged), and at UA-learned with the artifact or the labelled fallback; every run records `Brain.Kind`.
7. **Benchmarks:** `bench` cases for unified 72, 600 and 1,200 people (headless; baseline before trained-brain inference: about 1,080 ticks/s at 500 and 384 ticks/s at 1,000 people, legacy); report ticks per second, MACs per decision, bytes per person; compare with the baseline files; recorded from UA on for UC (UNP optimises).
8. **`docs/runs/UA_REPORT.md`:** three columns, **mechanical**, **learned/autonomous**, **long-run viability**; for every EC/EP/CF/TR/QA/CATALOG/MOTOR/NEED/ECON/U/B row: kind, result (passed, failed, reported, not run, not evaluated), evidence (test name, seeds, counts), and a plain note; the CATALOG-1 coverage (12 controls, 12 needs, 138 objects, 65 buildings: counts by stage and status), active versus defined needs, learned policy version, unresolved bootstrap dependencies, goods reconciliation, population curves, first-event timing per seed (first shelter, first collection, first exchange, first recipe), save and replay parity, build path and performance; separate sections for what did not run.
9. **UA-mech gate part** (H13.0–H13.5): runs the mechanical matrix on the merged prerequisites and produces the UA-mech section; the lead then bumps save 6 → 7 in the merge commit and tags `unif-ua-mech`. **UA-learned gate part** (H15.4–H16.0): adds the learned column from UN6t-b, the PlayMode and build results supplied by the lead, and the final report; the lead tags `unif-ua` only if green.

## 6. Explicitly excluded scope
Changing sim behaviour to make a test pass (a bug is reported to the owning package through the lead); weakening, deleting or skipping a matrix row (a row that cannot run is reported **not run**); presenting fixture traces as autonomous behaviour; recomputing training results with different code (verify only); moving a legacy hash; UB migrations and long-run `UB_REPORT.md` (UN11b); editing `SaveFile.cs` (the lead); fact tables for theft, ownership, use right or inheritance in unified runs; performance optimisation (UNP).

## 7. Dependencies
- **Hard (merged first):** UN0 for the preparation (schemas, event ranges, mapper stubs); for the **UA-mech gate**: UN1, UN2, UN3a, UN3b, UN4, UN5, UN7, UN8a, UN9 (index §3); for the **UA-learned gate**: additionally UN6, UN6t-b, UN10a.
- **Merge-after:** none (UN11a is the last package of each gate).
- **Feeds:** the lead's UA-mech and UA-learned checkpoints; UN11b; UC1 (benchmark baselines).

## 8. Shared contracts
- **R:** §23 in full; every package's events and tables as merged; `God.Recording` and the warehouse writer API.
- **M (own):** migrations 0044–0046 (UN11 owns all unification migrations, §23.13), `UNnMappings.cs` completion, `Tables.Unified*.cs`, `Scenarios/unif_*`, `UA*` tests, `docs/runs/UA_*`. **Never** edit sim files, `SaveFile.cs`, enums or schema files; a missing event field is an amendment request to the owning package via the lead.

## 9. Files and modules owned
`DotNet/God.Runner/Warehouse/Migrations/0044_unification_core.sql`, `0045_unification_social.sql`, `0046_unification_brain.sql`, `DotNet/God.Runner/Warehouse/UN1Mappings.cs`…`UN8aMappings.cs` (completion), `Tables.Unified*.cs`, `DotNet/God.Runner/Scenarios/unif_normal.scenario.json`, `unif_long.scenario.json` and the shared `unif_*` list, `DotNet/God.Runner.Tests/UA*.cs`, `docs/runs/UA_REPORT.md`, `docs/runs/UA_*` (JSON, benchmark records), status columns of the acceptance tables in `docs/stage4/ACCEPTANCE_TESTS.md` (the D9 section only).

## 10. Required domain events
None new. Completes mappers for 1200–1339 (1340–1399 belong to UB); verifies each package's `*_EventsRoundTrip` exists.

## 11. Save/load implications
Owns the **full U-P6** test and the collected per-package parity list; checks that brain weights are not in the save and that artifact version and hash are; verifies v7 round trip at UA-mech (the lead bumps the version in the same merge commit; UN11a's test is written against `SaveFile.CurrentVersion` and fails if v6 loads).

## 12. Warehouse implications
This package **is** the warehouse work for unification: tables and mappers for events, observations and claims kept separate, brain and training metadata, reconciliation tests, example queries, legacy-only labels. Row-count equality with events for each mapped code; open spells and sums as the table design states; no table named theft, ownership or use right is written in unified mode.

## 13. Acceptance tests
| Id | Test (behavioural) | Maps to | Pass |
|---|---|---|---|
| UN11a-T1 | `UA_Matrix_Mechanical`: every UA-mech row runs by its named test on the merged head (EC-01, EP-01, EP-02, EP-03 mech, EP-04, EP-05, CF-01, QA-01, QA-02, CATALOG-1, MOTOR-1, MOTOR-2, NEED-1, ECON-1 mech, ECON-2, U1, U2 mech, U3, U6, U7, U11, U13, U14, B1–B9, B11) with fixtures labelled `Fixture` | UA-mech | all pass or the failure is the result |
| UN11a-T2 | `Unified_Invariants_AllRuns`: U-C1…U-C6 hold at every checkpoint of `unif_start` seeds 1–3 × 20 days, untrained brain, from tick 0 | U-C1–U-C6 | exact |
| UN11a-T3 | **QA-01** `Origins_Identical_Physics`: one primitive stream gives equal `unification` and `population` hashes for Policy, Possessed, Fixture, Compelled origins; legacy off hashes unchanged (U-P1); replay and save exact | QA-01 | exact |
| UN11a-T4 | **QA-02** `Analytics_Separate_Kinds`: in a unified recorded run events, observations, claims and promises land in separate tables; `fact_theft`, `fact_ownership_spell`, `fact_use_right`, `fact_inheritance` have zero rows; no column anywhere holds a legitimate owner or a theft verdict; a query "who owns X" is impossible from the schema | QA-02 | exact |
| UN11a-T5 | `Warehouse_Reconciles_Unified`: goods ledger and environment books balance to zero error; every `HandedOver` has one `ItemMoved`; site contributions sum; event-to-row counts equal for 1200–1339 | QA-02, EC-01 | exact |
| UN11a-T6 | **U-P5** `Unified_Recording_Neutral` and **U-P6** `Unified_SaveReplay_Exact` (full mid-state) | U-P5, U-P6 | exact |
| UN11a-T7 | `Pins_AllHold`: U-P1, U-P2, U-P3 (static and runtime), P1, P4, P8 (and present P2, P3, P5, P9), Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` | U-P1… | exact |
| UN11a-T8 | `Learned_Verify_TR01`: from UN6t-b's JSON the TR-01 verdict is recomputed against the thresholds file; the thresholds hash in the results equals the committed file's and its commit predates the evaluation commit; artifact, schema and catalogue hashes match the head; a failed TR-01 sets the report's learned column to "not passed" and `UA-learned: not green` | TR-01 | verdict equals the trainer's |
| UN11a-T9 | `Learned_Rows_Reported`: EC-02, EP-03 learned, CF-02, TR-02, ECON-1 learned appear with control and seeds, or "not evaluated" with the reason; no row is silently absent | EC-02, EP-03, CF-02, TR-02 | present |
| UN11a-T10 | `Brain_Kind_Recorded`: every `unif_*` result records `Brain.Kind`; a run with `Kind = 2` can never be reported as trained | TR-01 honesty | exact |
| UN11a-T11 | `Benchmarks_Recorded`: unified 72, 600, 1,200 people ticks per second, MACs per decision, bytes per person recorded against the baseline files (report; the legacy bench within 3 % at UN0 merge is the lead's check) | D10 | report |
| UN11a-T12 | `Report_Honest`: `UA_REPORT.md` has the three columns, a "did not run" section, counts that equal the test results, and no row marked passed whose test did not run (the report generator reads results, not prose) | all | exact |
| UN11a-P | migrations apply on an empty database and on a populated one; `MigrationSetMembershipTests` green with the new numbers | warehouse | exact |

The full D9 row list, kinds and checkpoints are in `docs/stage4/ACCEPTANCE_TESTS.md` "D9. Unification acceptance". D8 ids: U1, U2, U3, U6, U7, U11, U13, U14 and B1–B9, B11 are kept (reworded) and implemented here as UA-mech rows; U4, U5, U8, U9, U10, U12, B10 follow the disposition table there.

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UA|FullyQualifiedName~Unified_|FullyQualifiedName~Neutrality"
dotnet run --project DotNet/God.Runner -c Release -- batch --scenario unif_normal --seeds 1-3 --days 20 --record full --sqlite --out <scratch>\un11a
dotnet run --project DotNet/God.Runner -c Release -- verify --scenario unif_normal --seed 1 --ticks 12000 --save-mid --record-matrix
dotnet run --project DotNet/God.Runner -c Release -- bench --cases bench_unif_72,bench_unif_600,bench_unif_1200 --ticks 3000 --workers 1
```
Then queries from `ExampleQueries.sql` (events by code, observations versus events, zero rows in the legacy-only tables) and the generated `UA_REPORT.md`.

## 15. Unity demonstration
Compile check where `God.Game` types are touched (normally none): `AgentScripts/check-game-compile.ps1` 0 errors. Unity EditMode, PlayMode and the build are the lead's checkpoint steps; UN11a records their counts into the report when the lead supplies them and states "not run" otherwise.

## 16. Performance considerations (D10)
Recording must not change the hash (U-P5) and must stay within the existing recording budget (Full recording of domains 80–88 measured; HV events are the heavy ones: 1200, 1203, 1204, 1221, 1260, 1261, 1242, 1243, 1303); benchmarks at 72, 600 and 1,200 people are tracked from UA on; reconciliation queries run on the SQLite file, not in the sim.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un11a -b unity/un11a <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un11a\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un11a record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.
Package-specific: preparation commits (migrations, stubs completion, test skeletons that report **not run** until prerequisites merge) may merge early with disjoint files; the gate parts run after the prerequisites of §7 and are the last merges of UA-mech (index §6 merge 10) and UA-learned (merge 14). The lead re-runs the mechanical matrix and reads the report before tagging.

## 18. Risks
- **Fixture results mistaken for autonomy:** every row carries its kind; the report generator refuses to mark a learned row passed from a fixture result.
- **Reconciliation hides a conservation bug:** zero tolerance; a failure is reported to the owning package.
- **Migration numbering gaps** (`0027`–`0043` absent on `unity/poc`): follow `MigrationSetMembershipTests` (R64); never renumber.
- **Report prose drifting from results:** generated from result files; the lead checks every figure.
- **Late prerequisites:** rows report **not run** with the missing package named; nothing is marked passed by omission.
- **Benchmark noise:** run on a quiet machine slot; record machine state; compare with baselines only within tolerance.

## 19. Definition of done
Migrations and mappers complete and reconciling; UN11a-T1…T12 and UN11a-P green or their failures recorded as results; UA-mech section delivered by H13.5; UA-learned section and final `UA_REPORT.md` by H16.0; "what did not run" section present; compile check 0 errors; "UN11a record" in DEV_LOG lists counts, hashes, benchmark figures and defaults; handed back.

## 20. Smaller checkpoints
- **UN11a-1** (preparation, from H11.1): migrations, mapper completion as events merge, matrix skeleton reporting not-run, reconciliation, U-P5.
- **UN11a-2** (UA-mech gate, H13.0–H13.5): mechanical matrix, U-P6 full, benchmark baseline, UA-mech section.
- **UN11a-3** (UA-learned gate, H15.4–H16.0): learned column, verification of TR-01, final report.

## 21. Recommended agent
**Sonnet** (the lead reviews every figure and every verdict). Never Fable.

## 22. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded or keyed `Rng` with a label unique per call site, no wall clock, no unordered enumeration, no allocation in per-tick hot paths; bots, people, possessed bodies and test fixtures use the same primitives); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `Assets/InitTestScene*` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done.
- **D9 rules, always:** no owner, title, permission, use right, theft or legitimate-owner fact in unified code (static guard U-P3); no scripted build order, profession, strategy, retaliation or moral judgement; no new primitive verb (the twelve controls only); every game-visible fact is physical (holder, location, quantity, condition, occupancy, barrier) or a belief held by a person; origin (policy, possessed, fixture, teacher) never changes a physical result; no hidden oracle (`NodePather`, truth queries) on the unified decision path. Every scale-sensitive structure is configurable (tribes, radius, settlements per tribe) and every per-tick cost is O(active entities), never all-pairs (D10).
- **Before any Unity open or test run** (CLAUDE.md): `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors. Only then open the editor (batch mode) and confirm a clean console.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. Run `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives a cut-off; never bare `git stash` (shared stash stack), use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`. Never use or suggest Fable.
- Report exactly which tests and runs happened and plainly what did not run. A fixture-driven result is labelled mechanical; it is never presented as autonomous behaviour.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN11a record" under "Design decisions taken during implementation".
