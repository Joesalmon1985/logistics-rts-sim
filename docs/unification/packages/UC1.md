# UC1: Scale start (large island, 3–4 settlements per tribe, 100–200 people per tribe)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

**Later-stage spec (Tier UC).** Shorter than the UA specs; every owner-brief §40 field is present. Starts only after UB; never in the UA window.

| Field | Value |
|---|---|
| Package identifier | **UC1** (Tier UC; Checkpoint UC "Scale") |
| Branch / worktree | `unity/uc1` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-uc1`, from `unity/poc` after **UNP, UB1, UN8b and UN6t2** merged |
| Effort | 3.0 agent-hours (+ Opus review) |
| Status | Not started. New in D9 (owner decision D10) |
| Recommended agent | **Sonnet**, with an **Opus** review of the generator and performance results. Never Fable |

## 1. Objective
Deliver the scale start the owner described: six factions, each with **3–4 settlements** made of collections of different buildings and **100–200 persons** (about 600–1,200 in total) on an **enlarged island** (radius 6 or more), with sustained back-and-forth conflict between factions emerging from individuals and groups (never faction AI), and the UC performance gate.

## 2. User-facing outcome
A "large island" game starts with hundreds of people across multiple villages per tribe; the player (a god) can watch settlements grow, roads connect them and fighting break out and recede, at a playable speed.

## 3. Design anchors
**D10** (D10.1 target 6 × 100–200; D10.2 island may be larger, hex count and settlement sites configurable and scaling; D10.3 UC after UB, every package avoids blocking it), §D9.6 (configuration, benchmarks, UC performance target: 1,200 people with trained-brain inference headless ≥ 100 ticks/s and ≥ 20 ticks/s in the player), §D9.5 Start (founder knowledge and family generation rules), §23.1 (`Board.Radius`, `Board.SeaRing`, `Start.*` configuration), index §7 UC, §2 row UC1.

## 4. Relevant existing source files (verified)
`Assets/God/Sim/Board/BoardGenerator.cs`, `Board/Board.cs`, `Board/BoardTopology.cs`, `Population/Households/Founders.cs` (legacy founders generator; `Founders.Tribal.cs` is UN5's new file), `Core/GameSetup.cs`, `Core/SimBuilder.cs`; benchmark tooling `DotNet/God.Runner/Bench.cs`, `Scenarios/bench_npc_1000.scenario.json`, `bench_npc_2000.scenario.json`, `DotNet/God.Bench/*` (UNP); unified code (not present at spec time): `Unified/Start/*` (UN5 generator and hall endowment), `Unified/Spatial/GridSpatialIndex.cs` (UNP), `Unified/Settlements/*`, `Unified/Roads/*`, `Unified/Groups/*`, `Unified/Brain/*`, `Unified/Conflict/*`. Tests to model on: `DotNet/God.Runner.Tests/IslandPopulationTests.cs`, UN5's `UN5*`.

## 5. Allowed scope
1. **Start generator extension (UC files in `Unified/Start/`):** parameters only: tribe count, adults and children per tribe (100–200 persons: varied family structures, couples, single parents, unpartnered adults, orphans cared for by kin, as UN5), settlements per tribe (3–4, each a Catan vertex with its development area and a completed hall or equivalent endowment ledgered as `Founder` reason), island radius (6+), starting positions; founder knowledge and skill distributions per §D9.5 Start scaled; the same endowment rule (nothing else injected).
2. **Scenarios** `unif_scale_600`, `unif_scale_1200`, `unif_scale_conflict` (inter-tribe contested resources and borders), with the grid spatial index and staggering on.
3. **Experiments:** sustained inter-tribe conflict measured over long runs: strike rates, deaths, retreats, group coordination (analytics), border dynamics, settlement survival; population curves; no strategy scripted, no faction AI; reported with seeds.
4. **Performance gate (L):** 1,200 people with the trained-brain artifact: headless ≥ 100 ticks/s and ≥ 20 ticks/s in the player (PlayMode measurement by the lead); results recorded with machine state; a miss is reported, with the profile, not hidden.
5. `docs/runs/UC_REPORT.md`.

## 6. Explicitly excluded scope
Faction AI or commanders; scripted wars or alliances; new mechanics; changes to the brain budget; behaviour tuning to force conflict; any ownership or authority concept; industry; touching UA tests' expectations.

## 7. Dependencies
Hard: **UNP**, **UB1**, **UN8b**, **UN6t2** (and transitively UA and UB). Merge-after: none. Downstream: the UC checkpoint.

## 8. Shared contracts
R: §23.1 configuration, §D9.6 rules, UN5 generator API, UNP bench API. M: UC files in `Unified/Start/`, `unif_scale*` scenarios, `UC*` tests, `docs/runs/UC_*`. Never edit frozen files.

## 9. Files and modules owned
`Assets/God/Sim/Unified/Start/*` (UC files, e.g. `Start.Scale.cs`), `DotNet/God.Runner/Scenarios/unif_scale*.scenario.json`, `DotNet/God.Runner.Tests/UC*.cs`, `docs/runs/UC_REPORT.md`, `docs/runs/UC_*`.

## 10. Required domain events
None new; uses existing unified events (`TribeStarted` 1206 for each tribe).

## 11. Save/load implications
Larger saves (state is bounded per person); `UC_SaveLoadParity` at 600 persons mid-run; the save version unchanged unless a state shape changes (lead).

## 12. Warehouse implications
None new; recording of the large runs uses summary level; example queries for settlement and conflict aggregates.

## 13. Acceptance tests
| Id | Test | Maps to | Pass |
|---|---|---|---|
| UC1-T1 | `Scale_Start_Composition`: tribe, settlement and person counts equal the configuration (6 × 3–4 settlements × 100–200 persons) at radius 6; families valid; endowment ledgered; U-C1 from tick 0 | D10 | exact |
| UC1-T2 | `Scale_Config_NoConstants`: changing tribe count, radius and settlements per tribe needs no code change (generator runs at 3 and 12 tribes, radius 4 to 8) | D10.2 | exact |
| UC1-T3 | `Scale_Determinism`: 600-person run, same seed, same hash; save and replay exact | U-P6 | exact |
| UC1-T4 | `Scale_Performance`: 1,200 persons with the trained artifact: ticks per second headless and in the player recorded; gate ≥ 100 and ≥ 20 (L) | D9.6 UC | met, or miss reported |
| UC1-T5 | `InterTribe_Conflict_Measured`: sustained conflict experiments reported with controls and seeds; nothing scripted; no faction object takes part (static) | D10.1, CF-02 | reported |
| UC1-T6 | `UC_Invariants`: U-C1…U-C6 at every checkpoint of the scale runs | invariants | exact |
| UC1-P | pins U-P1, P1, P4, P8, Stage 3 hashes unchanged | all | exact |

## 14. Headless demonstration
`dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_scale_1200 --seed 1 --days 100 --record summary --out <scratch>\uc1`; `dotnet run --project DotNet/God.Bench -c Release -- run --cases unif_1200 --ticks 3000`; the report.

## 15. Unity demonstration
Compile check `AgentScripts/check-game-compile.ps1` 0 errors; the lead measures the player at 1,200 people in PlayMode/batch (UN10b views) and records ticks per second; `unity close .`.

## 16. Performance considerations (D10)
This package validates them: per-person state ≤ 4 KB, ≤ 20,000 MAC per decision, decisions on completion, spatial index, O(active) costs, no all-pairs, no per-tick allocation; profile attached to the report.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-uc1 -b unity/uc1 <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-uc1\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "uc1 record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.

## 18. Risks
Performance miss (report with the profile; UNP continues); degenerate conflict dynamics (reported as measured outcomes); generator constants hiding in code (T2); save size; long run times.

## 19. Definition of done
UC1-T1…T6 and UC1-P green or results reported; `UC_REPORT.md`; compile check 0 errors; "UC1 record" in DEV_LOG; handed back.

## 20. Smaller checkpoints
**UC1-1** generator and 600-person scenario; **UC1-2** 1,200 persons, conflict experiments, performance report.

## 21. Recommended agent
**Sonnet** with **Opus** review. Never Fable.

## 22. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded or keyed `Rng` with a label unique per call site, no wall clock, no unordered enumeration, no allocation in per-tick hot paths; bots, people, possessed bodies and test fixtures use the same primitives); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `Assets/InitTestScene*` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done.
- **D9 rules, always:** no owner, title, permission, use right, theft or legitimate-owner fact in unified code (static guard U-P3); no scripted build order, profession, strategy, retaliation or moral judgement; no new primitive verb (the twelve controls only); every game-visible fact is physical (holder, location, quantity, condition, occupancy, barrier) or a belief held by a person; origin (policy, possessed, fixture, teacher) never changes a physical result; no hidden oracle (`NodePather`, truth queries) on the unified decision path. Every scale-sensitive structure is configurable (tribes, radius, settlements per tribe) and every per-tick cost is O(active entities), never all-pairs (D10).
- **Before any Unity open or test run** (CLAUDE.md): `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors. Only then open the editor (batch mode) and confirm a clean console.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. Run `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives a cut-off; never bare `git stash` (shared stash stack), use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`. Never use or suggest Fable.
- Report exactly which tests and runs happened and plainly what did not run. A fixture-driven result is labelled mechanical; it is never presented as autonomous behaviour.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UC1 record" under "Design decisions taken during implementation".
