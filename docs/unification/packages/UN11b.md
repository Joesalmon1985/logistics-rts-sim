# UN11b: UB assurance (long run, viability experiments, 20-seed demography, UB report)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

**Later-stage spec (Tier UB).** Shorter than the UA specs; every owner-brief §40 field is present. Replaces the D8 UN11 UB part.

| Field | Value |
|---|---|
| Package identifier | **UN11b** (Tier UB; Checkpoint UB) |
| Branch / worktree | `unity/un11b` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un11b`, from `unity/poc` after **UN11a, UB1, UB2 and UN8b** merged |
| Effort | 3.0 agent-hours (plus long-run compute) |
| Status | Not started |
| Recommended agent | **Sonnet** (the lead reviews every figure). Never Fable |

## 1. Objective
Provide the UB evidence: the unified **long run** (seeds 1–3, 30 sim years, no god, no commands), the viability experiments reworded for D9, the 20-seed demographic evaluation, migration `0047` and mappers for UB events, group-aware analytics, and `docs/runs/UB_REPORT.md` with the same three columns (mechanical, learned/autonomous, long-run viability) and honest "did not run" sections.

## 2. User-facing outcome
The owner learns what a primitive society actually did over decades: whether it survived, built, traded, founded settlements and roads, formed groups, fought, and where it failed, with controls and seeds, and with failure reported as a measured outcome.

## 3. Design anchors
Amendment §7 (extend UB for QA-01, QA-02, TR-02, CF-02; mechanical, learned and long-run are separate; failure to exhibit a strategy, growth or new settlement is a measured outcome), §8 (industry deferral until UB plus credible autonomous primitive viability), D9.14, index §7 UB and §2 row UN11b, UNIFICATION_DESIGN §14.3 and D8.18 (long run and V1–V7, reworded below), §23.13 (`0047` for settlements, roads, groups), R82.

## 4. Relevant existing source files (verified)
`DotNet/God.Runner/Demog/DemogCommand.cs` (`demog report`, arms, seeds, years), `DemogReport.cs`, `DemogRecorder.cs`, `Demog/Plans/*`; `DotNet/God.Runner/Kpi/KpiEngine.cs`; `DotNet/God.Runner/Warehouse/*` (recorder, writer, mappings, `Migrations/*`, `ExampleQueries.sql`); `DotNet/God.Runner/Scenarios/demog_*.scenario.json`, `bench_npc_*.scenario.json`; tests `DotNet/God.Runner.Tests/DemogReportTests.cs`, `DemogConceptionTests.cs`, `ReplayTests.cs`; UN11a outputs (`UA*` tests, `UA_REPORT.md` generator, `unif_normal`, `unif_long` skeleton); UB packages' mapper stubs `UB1Mappings.cs`, `UN8bMappings.cs` (not present at spec time).

## 5. Allowed scope
1. **Long run** `Unified_LongRun`: `unif_long`, seeds 1–3, 30 sim years (the calendar year is `DaysPerYear` 10 days, `SimCalendar.cs:17`, so 300 days; 50 years are 500 days), `Divinity.Enabled = 0`, no commands, recording on, replayable from seed; per seed record **when (or whether)** each of these first happened: collection building, upgrade, exchange (hand-over pair), recipe run, group, road edge, new settlement; population curve, deaths by cause, births, mean nutrition, tribe extinctions. (D8's "first member-founded organisation" is **superseded**: organisations are off in unified mode.)
2. **Viability experiments** (reported, never asserted), reworded: **V1** baseline society survival at 5, 20, 50 years; **V3** forage richness sensitivity; **V4** supply sensitivity (allowance and materialisation interval); **V5** generations (knowledge across 3+ generations, teaching events); **V6** shocks (no dice rolls on a hex for 10 days, a famine of one food, a ruined building); **V7'** brain ablation (untrained, frozen and trained arms; overlay off) replacing the D8 planning ablation. **V2 (shared throughput)** belongs to the D8 design and is not part of D9 unless UN1 carries it as tuning; record "not applicable".
3. **Demographic evaluation:** the 20-seed demographic follow-up (preserved from the owner's earlier requirement) through `demog report` arms on the unified start, with the trained, frozen and untrained arms where available; the three-seed smoke is not proof of viability.
4. **Migration `0047_unification_ub.sql`** (settlements, roads as edge facts, groups as communications/analytics) and completion of `UB1Mappings.cs`, `UN8bMappings.cs`; reconciliation tests (materials delivered = consumed at completion; every contribution row links to an event; no ownership or authority table).
5. **Extended QA/TR/CF evidence:** QA-01 and QA-02 re-run on UB scenarios; TR-02 extended and CF-02 UB figures imported from UN6t2's JSON (verified, not recomputed); EC-02 on settlements/roads where trained.
6. **`docs/runs/UB_REPORT.md`:** three columns, UB acceptance rows (U9, U10, U12, B10 reworded, CF-02, TR-02), long-run tables, V-experiments, demography, performance at 600 and 1,200 persons, what did not run; a section stating whether the **industry deferral condition** (UB plus credible autonomous primitive viability, D9.14) is met on the evidence, as a statement of evidence for the owner, not a decision.

## 6. Explicitly excluded scope
Changing sim behaviour or tuning to improve results; asserting any viability outcome; marking rows passed that did not run; industry work; editing `SaveFile.cs`; fabricating or smoothing results; recomputing training results with different code; optimisation (UNP).

## 7. Dependencies
Hard: **UN11a**, **UB1**, **UB2**, **UN8b**; UN6t2 and UN10b feed the report (UB checkpoint needs them). Downstream: the UB checkpoint and UC1.

## 8. Shared contracts
R: §23.13, UB events 1340–1379, UN11a tables and report generator. M: `0047`, UB mappers completion, `Tables.Unified*.cs` (UB), scenarios `unif_long*`, `UB*` tests, `docs/runs/UB_*`. Never edit sim files or `SaveFile.cs`.

## 9. Files and modules owned
`DotNet/God.Runner/Warehouse/Migrations/0047_unification_ub.sql`, `UB1Mappings.cs`, `UN8bMappings.cs` (completion), `Tables.Unified*.cs` (UB additions), `DotNet/God.Runner/Scenarios/unif_long*.scenario.json`, `DotNet/God.Runner.Tests/UB*Assurance*.cs`, `docs/runs/UB_REPORT.md`, `docs/runs/UB_*`.

## 10. Required domain events
None new; mappers for 1340–1379.

## 11. Save/load implications
`UB_SaveReplay`: mid-run saves (site half built, road half built, proposal open, strike in progress) load and continue equal to the straight run; replay of the long run equals it at every checkpoint (U-P6 extended); the save version bump 7 → 8 is the lead's and only if UB state shapes changed.

## 12. Warehouse implications
`0047` and reconciliation as above; example queries for first-event timing and population curves.

## 13. Acceptance tests
| Id | Test | Maps to | Pass |
|---|---|---|---|
| UN11b-T1 | `Unified_LongRun`: 30-year, 3-seed, no-god run completes deterministically and replays; the report states per seed which of the first events happened and when, or that it did not | D8.18 (reworded), U14 | runs; outcome reported |
| UN11b-T2 | `Viability_Experiments`: V1, V3, V4, V5, V6, V7' run with controls and seeds and are written to the report (the test passes if they run and report, never on their values) | B10, viability | runs and reports |
| UN11b-T3 | `Demography_20Seed`: 20-seed demographic arms recorded and reported | amendment §8 | runs and reports |
| UN11b-T4 | `UB_Warehouse_Reconciles`: `0047` tables reconcile with events; no ownership, authority or leader table | QA-02 | exact |
| UN11b-T5 | `UB_SaveReplay_Exact` | U-P6 | exact |
| UN11b-T6 | `UB_Acceptance_Matrix`: U9, U10, U12 mechanical rows, CF-02, TR-02, QA-01, QA-02 on UB scenarios by their named tests; learned figures verified from UN6t2 JSON | UB | pass / reported |
| UN11b-T7 | `Report_Honest_UB`: generated from results, three columns, a "did not run" section, the industry-deferral statement; no row passed without a run | all | exact |
| UN11b-P | pins U-P1, P1, P4, P8, Stage 3 hashes; UA tests unchanged | all | exact |

## 14. Headless demonstration
`dotnet run --project DotNet/God.Runner -c Release -- batch --scenario unif_long --seeds 1-3 --days 300 --record summary --sqlite --out <scratch>\un11b` (30 years at 10 days per year); `demog report --scenario unif_normal --seeds 1-20 --years 20 --arm "trained: tuning.Unification.Brain.Kind=1" --arm "untrained: tuning.Unification.Brain.Kind=0"`; the generated `UB_REPORT.md`.

## 15. Unity demonstration
Compile check `AgentScripts/check-game-compile.ps1` 0 errors if `God.Game` types changed (normally none); Unity and the build are the lead's UB checkpoint steps; the report records their counts when supplied.

## 16. Performance considerations (D10)
Long runs use summary recording; HV events heavy: keep Full only for short windows; record ticks per second at 600 and 1,200 persons; the 30-year run's wall time is reported and scheduled on a quiet machine.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un11b -b unity/un11b <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un11b\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un11b record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.

## 18. Risks
Long runs expose determinism or conservation bugs late (report to the owning package via the lead); misleading smoothing of failures; compute time; migration numbering gaps (follow R64); industry-deferral statement read as a decision (it is evidence only).

## 19. Definition of done
UN11b-T1…T7 and UN11b-P green or failures recorded as results; `UB_REPORT.md` complete with "did not run" section; compile check 0 errors; "UN11b record" in DEV_LOG; handed back.

## 20. Smaller checkpoints
**UN11b-1** migration, mappers, reconciliation, matrix; **UN11b-2** long run, V-experiments, demography, report.

## 21. Recommended agent
**Sonnet**. Never Fable.

## 22. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded or keyed `Rng` with a label unique per call site, no wall clock, no unordered enumeration, no allocation in per-tick hot paths; bots, people, possessed bodies and test fixtures use the same primitives); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `Assets/InitTestScene*` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done.
- **D9 rules, always:** no owner, title, permission, use right, theft or legitimate-owner fact in unified code (static guard U-P3); no scripted build order, profession, strategy, retaliation or moral judgement; no new primitive verb (the twelve controls only); every game-visible fact is physical (holder, location, quantity, condition, occupancy, barrier) or a belief held by a person; origin (policy, possessed, fixture, teacher) never changes a physical result; no hidden oracle (`NodePather`, truth queries) on the unified decision path. Every scale-sensitive structure is configurable (tribes, radius, settlements per tribe) and every per-tick cost is O(active entities), never all-pairs (D10).
- **Before any Unity open or test run** (CLAUDE.md): `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors. Only then open the editor (batch mode) and confirm a clean console.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. Run `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives a cut-off; never bare `git stash` (shared stash stack), use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`. Never use or suggest Fable.
- Report exactly which tests and runs happened and plainly what did not run. A fixture-driven result is labelled mechanical; it is never presented as autonomous behaviour.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN11b record" under "Design decisions taken during implementation".
