# UNP: Performance for scale (grid spatial index, benchmarks, staggering, allocation removal)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

| Field | Value |
|---|---|
| Package identifier | **UNP** (Tier UC support; runs inside the UA window only when a slot is free; **first to cut**; never delays the UA critical path) |
| Branch / worktree | `unity/unp` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-unp`, from `unity/poc` after **UN0 and UN2** merged |
| Effort | 3.5 agent-hours (planned H11.1–H14.6 in Wave 3; merge ≈ H14.8 only if hash-neutral) |
| Status | Not started. New in D9 (owner decision D10, scale target) |
| Recommended agent | **Opus** (hot-path restructuring under byte-exact determinism). Never Fable |

## 1. Objective
Prepare the engine for the UC target (six tribes with 100–200 people each, 600–1,200 persons) without blocking UA: build the benchmark project and cases at 72, 600, 1,200 and 2,000 people (legacy and unified), profile the unified tick, replace the simple spatial index with a **grid** implementation behind the UN0 interface, stagger work by sim state (never by camera), and remove allocations from the unified hot paths. Hash neutrality is the rule: any change that moves a hash stays on its branch.

## 2. User-facing outcome
None directly. A benchmark record shows where the time goes and a faster unified tick; the owner later gets 1,200 people at the UC rate (headless ≥ 100 ticks/s with trained-brain inference, player ≥ 20 ticks/s: lead defaults, §D9.6).

## 3. Design anchors
- Owner decision **D10** (D10.1 long-term target 6 factions × 100–200 NPCs; D10.2 island may be larger; D10.3 UC checkpoint and the rules every package follows: bounded per-NPC brain cost, spatial indexing, staggering by sim state, no all-pairs scans, geometry not assuming 30 hexes; a performance package runs alongside where it does not touch the same files).
- §D9.6 (scale rules; UC performance target; baseline about 1,080 ticks/s at 500 and 384 ticks/s at 1,000 people), §D9.2 a (≤ 20,000 MAC and ≤ 4 KB state per decision), §D9.1 b (executor and interaction cost O(active persons + changed nodes)).
- §23.6 (`ISpatialIndex`, UN0 interface; UN2 ships the simple one, UNP may replace it behind the interface), §23.15 (UNP owns the spatial index implementation and `God.Bench`), §23.2 (U-P1 neutrality).
- Index §7 UC (what every UA package does now), §8 cut plan (UNP is cut first when behind at H6.0).

## 4. Relevant existing source files (verified)
- Existing benchmark tooling to extend, not replace: `DotNet/God.Runner/Bench.cs` (`bench` command; `Cli.cs` documents `--cases`, `--ticks`, `--parallel-ticks`, `--workers`), `DotNet/God.Runner/Bench/baseline.json`, `Bench/q1_baseline_2026-11-27.json`, scenarios `DotNet/God.Runner/Scenarios/bench_npc_100|120|300|500|1000|2000.scenario.json`, `bench_stage3_watch6.scenario.json`, `DotNet/God.Runner/Warehouse/RecordingBench.cs`, `DotNet/God.Runner.Tests/Q1Tests.cs`.
- Profiling: `Assets/God/Sim/Population/PopulationProfiler.cs`, `Core/GodSim.cs` (tick loop, `InstallModules`), `Population/PopulationSystem.cs` (the `PersonTick` site), `Population/NodePather.cs` (legacy path solver, never on the unified decision path), `Cognition/Thinker.cs` (legacy sight helpers), `Comms/CommsRange.cs`.
- Spatial and grid code to read: `Board/BoardTopology.cs`, `Board/Lattice.cs`, `Board/Board.cs`; `Population/Reach.cs`.
- UN0/UN2/UN6 outputs (read; not present at spec time): `Unified/Schema/ISpatialIndex` (interface), `Unified/Spatial/SimpleSpatialIndex.cs` (UN2), `Unified/Percept/*`, `Unified/Motor/*`, `Unified/Brain/*` (UN6: inference cost counters).
- New: `DotNet/God.Bench/*` (additive project; the existing runner `bench` command is untouched), `Assets/God/Sim/Unified/Spatial/GridSpatialIndex.cs`.

## 5. Allowed scope
1. **`DotNet/God.Bench`** (new project; the lead adds it to `DotNet/God.sln`, flagged hunk): cases for legacy and unified at 72, 600, 1,200 and 2,000 people (scenario generators for large unified starts use UN5's generator through configuration only), measuring ticks per second, ms per tick breakdown (executor, perception, inference, interaction resolution, materialisation, SocialMemory feed, recording), allocations per tick, bytes per person, MAC per decision; JSON records comparable with the existing baselines; runs headless on a quiet machine.
2. **Profile** the unified tick at 72, 600 and 1,200 persons and publish the top costs in `docs/runs/PERF_<date>.md`.
3. **`GridSpatialIndex`** implementing the UN0 `ISpatialIndex` (fixed-cell grid over the lattice, cell size from configuration, deterministic cell iteration order, queries returning the nearest-N or all-within-radius sorted by distance then id; incremental updates on movement; O(entities in nearby cells) per query; rebuilt bit-identically from a save). Selected by a **performance-only** configuration value that does **not** change the sim's behaviour: the grid must return exactly what the simple index returns (test below), so hashes are identical.
4. **Decision and work staggering by sim state:** where UN2/UN6/UN3b process lists per tick, bucket by `(person id + tick) mod k` or by due-tick queues; never by camera, never by view; results must be identical to the unstaggered order (test) or the change stays on its branch.
5. **Allocation removal** on unified hot paths (observation vectors, masks, event buffers, effect dispatch, brain scratch): pooling and preallocated arrays inside the owning packages' files **only as hash-neutral hunks named in the record** and approved by the lead; or, where the owning package is not yet merged, a written request to that package through the lead.
6. **Legacy think-loop work** only if hash-neutral (the legacy path is pinned): any change to a legacy file is proved neutral by U-P1, P1, P4, P8 and the Stage 3 hashes or is dropped.
7. **Scale-rule audit:** a static and run-time check that no unified code iterates all persons for another person's query (no all-pairs), no unified structure sizes by tribe count or hex count constant, and the board generator and start generator run at radius 6 with 12 tribes configurable (UN1 and UN5 own the code; UNP only measures and reports violations to the lead).

## 6. Explicitly excluded scope
Any change that alters a hash, a decision, a physical result or the order of `Rng` draws; camera-based or view-based level of detail in the sim; new gameplay; the UC1 scale start itself (UC1); rewriting the legacy engine; changing the network architecture or budget (UN6/UN6t); threads inside the sim tick unless proved order-independent and hash-exact (default: none); editing frozen files (§23.0); touching the executor's semantics (UN2).

## 7. Dependencies
- **Hard (merged first):** UN0 (interface, switch), UN2 (the simple index, executor and perception to profile).
- **Merge-after:** UN6 (inference counters and brain hot path; UNP measures them and may merge its brain-side hunks only after UN6).
- **Feeds:** UN11a (benchmark baseline), UC1 (hard prerequisite), the UC checkpoint.

## 8. Shared contracts
- **R:** §23.6 `ISpatialIndex` contract, §23.2 pins, `Goods`, `Motor`, `Percept` (profile and measure only).
- **M:** `Unified/Spatial/GridSpatialIndex.cs`, `DotNet/God.Bench/*`, `docs/runs/PERF_*`, hash-neutral hunks named in its record (each approved by the lead before merge). **Never** edit frozen files, schema files or `SaveFile.cs`.

## 9. Files and modules owned
`DotNet/God.Bench/**`, `Assets/God/Sim/Unified/Spatial/GridSpatialIndex.cs`, `docs/runs/PERF_*.md`, `docs/runs/PERF_*.json`, tests `DotNet/God.Runner.Tests/UNP*.cs`, scenarios `DotNet/God.Runner/Scenarios/bench_unif_*.scenario.json`, and the named hash-neutral hunks.

## 10. Required domain events
None. Telemetry is bench output only, never sim events.

## 11. Save/load implications
None (the index is rebuilt from state). Test `UNP_GridRebuild_FromSave`: save, load, query results equal the simple index's.

## 12. Warehouse implications
None.

## 13. Acceptance tests
| Id | Test (behavioural) | Maps to | Pass |
|---|---|---|---|
| UNP-T1 | `Grid_Equals_Simple`: for 1,000 random states (72 to 1,200 persons, radius 4 and 6 boards) every query (nearest-N, within-radius, line-of-sight candidates) returns identical ordered results from the grid and the simple index | D10, QA-01 | exact |
| UNP-T2 | `Perf_Neutral_Hashes`: a 20-day unified run (seeds 1–3) with the grid and staggering enabled has hashes identical to the same run with the simple index; with the switch off U-P1, P1, P4, P8 and Stage 3 hashes unchanged | U-P1 | exact |
| UNP-T3 | `Stagger_Order_Independent`: staggered and unstaggered schedules give identical physical results; staggering uses only sim state (static: no view or camera type in `Unified/`) | D10 | exact |
| UNP-T4 | `NoAllPairs` (static and run-time): doubling the population does not square the per-tick cost: measured ms per tick at 600 vs 1,200 persons grows by less than 2.6× (L), and a counter of pairwise scans is zero | D10 | pass / report |
| UNP-T5 | `Hot_Path_Allocations`: allocations per tick in the executor, perception, inference and effect dispatch at 600 persons are zero after warm-up (allocation counter) | D10 | pass |
| UNP-T6 | `Bench_Recorded`: legacy and unified at 72, 600, 1,200 and 2,000 persons recorded to JSON with ticks per second, ms per tick breakdown, allocations, bytes per person; the legacy case matches the baseline file within 3 % | D10 | report |
| UNP-T7 | `Scale_Config_Radius6`: the board and start generators run at radius 6 and 12 tribes configurable (UN1/UN5 code; UNP only runs the check and reports) | D10.2 | report |
| UNP-P | pins: U-P1, P1, P4, P8, Stage 3 hashes unchanged | all | exact |

UC performance gate targets for reference (lead defaults, §D9.6, not UNP gates): 1,200 people with trained-brain inference headless ≥ 100 ticks/s, player ≥ 20 ticks/s. D8 ids: none.

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UNP|FullyQualifiedName~Neutrality"
dotnet run --project DotNet/God.Bench -c Release -- run --cases unif_72,unif_600,unif_1200,unif_2000,legacy_500,legacy_1000 --ticks 3000 --out <scratch>\unp
dotnet run --project DotNet/God.Runner -c Release -- bench --cases bench_npc_500,bench_npc_1000 --ticks 3000 --workers 1
```
Report: the table of ticks per second before and after, the top five costs, allocations per tick.

## 15. Unity demonstration
Compile check only if `Assets/God/Sim` files change (they do: the grid index): `AgentScripts/check-game-compile.ps1` 0 errors. No Unity run needed.

## 16. Performance considerations (D10)
This package is the performance consideration. Budgets it defends: per-person state ≤ 4 KB, ≤ 20,000 MAC per decision, decisions only on action completion, cost O(active persons + changed nodes + open sites) per tick, no all-pairs scan, no per-tick allocation in hot paths, nothing sized by constants of tribes, hexes or population. Measure on a quiet machine (the lead assigns the slot); record machine state.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-unp -b unity/unp <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-unp\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "unp record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.
Package-specific: merges only if every hunk is hash-neutral (UNP-T2) and the lead has approved each hunk in files it does not own; otherwise the package stays on its branch and the report is delivered (index §6 merge 11). First to cut (index §8, H6.0).

## 18. Risks
- A "free" optimisation changes iteration order and therefore a hash: UNP-T2 on every commit.
- Staggering that depends on the camera or on wall-clock: static test.
- Benchmark noise from other agents' test suites: lead-assigned quiet slot; repeated runs; record variance.
- The grid index disagreeing with the simple index in an edge case (diagonals, board edge, sea): the equality test over many states and radii.
- Scope creep into gameplay or the legacy engine: excluded; a non-neutral change is dropped.

## 19. Definition of done
`God.Bench` runs the named cases and writes JSON; `GridSpatialIndex` equals the simple index; all hunks hash-neutral or kept on the branch; PERF report written with before/after and top costs; UNP-T1…T7 and UNP-P green; compile check 0 errors; "UNP record" in DEV_LOG lists what ran, figures and what did not; handed back (or left on its branch by the cut plan).

## 20. Smaller checkpoints
- **UNP-1** (≈ 1.5 h): `God.Bench`, baselines, profile report (can merge early: new files only).
- **UNP-2** (≈ 2 h): grid index and staggering with the neutrality proofs; allocation removal as approved hunks.

## 21. Recommended agent
**Opus**. Never Fable.

## 22. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded or keyed `Rng` with a label unique per call site, no wall clock, no unordered enumeration, no allocation in per-tick hot paths; bots, people, possessed bodies and test fixtures use the same primitives); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `Assets/InitTestScene*` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done.
- **D9 rules, always:** no owner, title, permission, use right, theft or legitimate-owner fact in unified code (static guard U-P3); no scripted build order, profession, strategy, retaliation or moral judgement; no new primitive verb (the twelve controls only); every game-visible fact is physical (holder, location, quantity, condition, occupancy, barrier) or a belief held by a person; origin (policy, possessed, fixture, teacher) never changes a physical result; no hidden oracle (`NodePather`, truth queries) on the unified decision path. Every scale-sensitive structure is configurable (tribes, radius, settlements per tribe) and every per-tick cost is O(active entities), never all-pairs (D10).
- **Before any Unity open or test run** (CLAUDE.md): `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors. Only then open the editor (batch mode) and confirm a clean console.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Run `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives a cut-off; never bare `git stash` (shared stash stack), use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`. Never use or suggest Fable.
- Report exactly which tests and runs happened and plainly what did not run. A fixture-driven result is labelled mechanical; it is never presented as autonomous behaviour.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UNP record" under "Design decisions taken during implementation".
