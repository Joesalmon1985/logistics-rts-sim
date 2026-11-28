# UN6t-a: Founder training harness and predeclared thresholds

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

| Field | Value |
|---|---|
| Package identifier | **UN6t-a** (Tier UA, **critical path**, Wave 1, part (a) of UN6t; Checkpoint UA-learned) |
| Branch / worktree | `unity/un6t` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un6t`, from `unity/poc` after **UN0** merged; the same branch and agent continue as UN6t-b (spec [UN6t-b.md](UN6t-b.md)) |
| Effort | 3 agent-hours (planned H4.6–H8.0; the agent keeps its slot into UN6t-b) |
| Status | Not started. The D8 plan had no training package; amendment §6 and D9.8/D9.10 make it a critical-path package |
| Recommended agent | **Opus** (training infrastructure whose honesty the whole checkpoint depends on). Never Fable |

## 1. Objective
Build, on UN0's frozen contracts and before the real mechanics have merged, everything needed to train and honestly evaluate the founder policy, and **commit the predeclared thresholds before any held-out run exists**: the headless vectorised environment wrapper over the real `GodSim`, the curriculum S1–S8, the labelled training-only teacher, behaviour cloning, evolution-strategies fine-tuning with exact int16 quantisation, the evaluation harness with untrained and frozen controls, the metrics T1–T7, the artifact writer and the report writer. UN6t-b then trains and evaluates against the merged world.

## 2. User-facing outcome
None in the game. The outcome is a reproducible command-line trainer and evaluator (`god-train`) and a thresholds file, so that "trained founders" is a claim backed by a versioned artifact, a content hash, held-out seeds and numbers fixed in advance, and so that a failure is reported as a failure.

## 3. Design anchors
- Amendment §6 (train against the authoritative simulator and the same twelve controls, observations, needs and catalogue; offline training; versioned deterministic inference; legality masks only; feedback from experienced needs, pain, material access, risk and relationships; thresholds before held-out results; untrained/frozen comparison; no teacher or god; unfamiliar layouts; interrupted tasks; no hidden path oracle; separate survival competence from social outcomes; fallback flagged, never counted).
- §D9.2 b (same world, tooling, curriculum S1–S8, feedback, teacher, artifact, seed split), §D9.2 c (thresholds T1–T7 and honest reporting).
- §23.5 (`ControlOrigin.Teacher`, `Motor.Submit`), §23.6 (`Obs v1`, `Act v1`, `IPrimitivePolicy`, schema hash), §23.1 (`Brain.Kind`, `Brain.Artifact`), §23.15 (UN6t owns `God.Train` and the thresholds file).
- D9.10 (training gate is on the critical path; a failed gate is not a green UA; no scripted fallback is counted as trained), R83 (children), R84.

## 4. Relevant existing source files (verified)
- Runner and session plumbing to model on and reuse (read-only): `DotNet/God.Runner/RunEngine.cs`, `SimSession.cs`, `Program.cs`, `Cli.cs` (`bench` command at `:46`, parallel workers), `Batch.cs`, `Verify.cs`, `DotNet/God.Runner/Scenarios/*.scenario.json` (format of scenario files), `Assets/God/Recording/Scenario.cs`, `Replay.cs`.
- Project files to copy the shape of: `DotNet/God.Runner/God.Runner.csproj` (target framework `net10.0`; the design says "net8" for `God.Train`; use the framework `God.Runner` uses, record the difference), `DotNet/God.Sim/God.Sim.csproj` (compiles `Assets/God/Sim/**`), `DotNet/God.sln` (the lead adds the project to the solution; UN6t-a ships the project file and requests the solution edit as a flagged hunk).
- Core: `Assets/God/Sim/Core/GodSim.cs`, `Core/Rng.cs`, `Core/SimTuning.cs`, `Core/SimBuilder.cs`; determinism tests `DotNet/God.Runner.Tests/DeterminismTests.cs`; hash pins `DotNet/God.Runner.Tests/PolicyRunTests.cs:27`.
- Learning vocabulary (read for measures, not reused as a policy): `Learning/Features.cs` (`Drive`), `Learning/Learner.cs` (`DriveReward`).
- Lab vocabulary for CF-02 measures only: `Lab/Experiments/` (Hawk/Dove and siblings; separate from the island).
- Unity-free requirement: the trainer references `God.Sim` only; nothing under `Assets/God/Sim` references `God.Train`.
- UN0 files you read once merged: `Unified/Schema/ObsSchema.cs`, `ActSchema.cs`, `CatalogueDefs.cs`, `IPrimitivePolicy`, `ControlOrigin`, `Unified/UnifiedState.cs`.

## 5. Allowed scope
1. **Project** `DotNet/God.Train/` (new): `god-train` commands `env-check`, `demo` (teacher demonstrations to a file), `bc` (behaviour cloning), `es` (evolution strategies), `eval` (held-out evaluation), `artifact` (write `FounderPolicyV<n>.g.cs`), `report`. Dependency-free C#; no ML framework. Threads across seeds with `--workers N`; results must not depend on N.
2. **Environment** (`Env/`): resets a real `GodSim` with `Unification.Enabled = 1`, the same catalogue, needs and executor, from a **scenario generator** (a small generated island of 1–12 people for S1–S8, and the full 72-person start for late stages) and a seed; steps to the next decision point; exposes `Percept` observations and legality masks exactly as UN2 provides them; computes returns **only** from the experienced change of need pressures, pain, health and death, discounted over long horizons (whole-episode return for ES so a 300-tick construction keeps its credit). Until UN1/UN2/UN3b/UN6 merge, the environment runs on a **stub world** (a toy food-and-water lattice behind the same interfaces) used only by the harness self-tests; it is never a result.
3. **Curriculum** (`Curriculum/`): S1 motor control and exploration; S2 food, water, sleep; S3 hygiene, comfort, social, fun, bladder; S4 take, store, withdraw and carrying; S5 selecting and completing useful construction; S6 raw collection, deposit and simple recipes; S7 offer, promise, hand-over and non-reciprocation; S8 aggression, avoidance, retaliation, contested goods, childcare and observation. Each stage is a data file: scenario generator, population mix, need mask (the target scenario's `Needs.ActiveMask`), **predeclared stage criteria** (advance only when met; written down here and in the thresholds file before use). S5–S8 are wired but wait for their effects (UN4, UN7, UN8a).
4. **Teacher** (`Teacher/`, training and fixtures only): a scripted, labelled demonstrator (`ControlOrigin.Teacher`) that may use a path oracle and truth to produce demonstrations for behaviour cloning. It is compiled into `God.Train` and test fixtures only; it never appears in a game scenario; an evaluation rollout containing any `Teacher` origin is **invalid** and aborts.
5. **Behaviour cloning** then **evolution strategies** (`Learn/`): float master weights; behaviour cloning from teacher demonstrations; ES fine-tuning (antithetic sampling, rank-normalised returns, keyed seeds) on episode returns; **every evaluation rollout runs the exact int16 quantised network through UN6's integer inference** (so training evaluates what the game runs). Python or any framework may be used for analysis, or as an alternative optimiser only if it consumes rollouts produced by the C# simulator and exports the same integer artifact with a bit-exact parity check (test below).
6. **Evaluation harness** (`Eval/`): arms **untrained** (same network, random initial weights), **frozen** (behaviour-cloned only, no ES) and **trained**; teacher-free, divinity off, no path oracle, same need mask as training; held-out seeds **900001–900020** and held-out layouts (mirrored, rotated, regenerated boards, unfamiliar start positions) that the trainer **refuses to load**; computes T1–T7 exactly as defined in the thresholds file; writes machine-readable results (JSON) and the report.
7. **Thresholds file** `docs/unification/TRAINING_THRESHOLDS.md`, **committed in the first push of this branch and before any held-out run exists**: the T1–T7 table of §D9.2 c verbatim (with the exact measure definitions, denominators and seed lists), the seed split, the arms, the stage criteria, the rule "this file does not change after the first held-out run; a later change is a new versioned file with a recorded reason and the earlier result stays reported", and its own SHA-256 (recorded in DEV_LOG and in the artifact header). The harness refuses `eval` unless `--thresholds-hash` equals the file's hash and writes the hash into every result.
8. **Artifact writer** (`Artifact/`): `Unified/Brain/Artifacts/FounderPolicyV<n>.g.cs` (generated: base64 int16 weights; header with `Obs`/`Act` schema versions and hash, catalogue content hash, tuning hash, curriculum id, training seed range, trainer commit, thresholds hash, training metrics, SHA-256 of the weights), deterministic and reproducible from the seeds.
9. Tests (`DotNet/God.Runner.Tests/TR*.cs` for the harness; they run in the standard suite on the stub world).

## 6. Explicitly excluded scope
Actual training and the production artifact (UN6t-b); the integer inference runtime and artifact verification at game start (UN6); the executor and encoder (UN2); any change to the simulation to make training easier (the environment adapts to the sim, never the reverse); any reward for professions, buildings completed, goods held, title, civilisational milestones, morality or winning a dispute; a teacher or path oracle in any evaluation, scenario or game build; Python at game run time; Hawk/Dove or cooperation targets (measured, never trained for); edits to frozen files.

## 7. Dependencies
- **Hard (merged first):** UN0 (schema, `IPrimitivePolicy`, `ControlOrigin`, `Unification` tuning).
- **Merge-after / feeds:** none to merge before it; UN6t-b (same branch) needs UN1, UN2, UN3b and UN6 merged and S5, S7, S8 wait for UN4, UN7, UN8a. The lead records the thresholds hash when UN6t-a reports (≈ H8.0); an early docs-only merge of the thresholds file is optional (default: not merged early; checked at merge 13).
- **Downstream:** UN6t-b, UN6t2, UN11a.

## 8. Shared contracts
- **R:** §23.5, §23.6, §23.1, `GodSim`, `SimBuilder`, `Rng`, scenario JSON format, `God.Recording`.
- **M:** `DotNet/God.Train/*`, `docs/unification/TRAINING_THRESHOLDS.md`, `Unified/Brain/Artifacts/*` (artifact files and nothing else under `Unified/Brain`), the solution entry (flagged). The artifact header format is part of the UN6 loader contract: agree it with UN6 in writing in the first commit message of both branches.

## 9. Files and modules owned
`DotNet/God.Train/**`, `docs/unification/TRAINING_THRESHOLDS.md`, `Assets/God/Sim/Unified/Brain/Artifacts/**`, `docs/runs/TRAINING_REPORT.md` (UN6t-b fills), tests `DotNet/God.Runner.Tests/TR01*.cs`, `TR02*.cs`, `TRHarness*.cs`, scenarios `DotNet/God.Runner/Scenarios/unif_train_*.scenario.json` (new).

## 10. Required domain events
No new sim events. `BrainAttached` 1320 (shared with UN6) carries the artifact fields this package defines. The trainer writes its own JSON logs (not sim events, not hashed).

## 11. Save/load implications
None in the save. The artifact version and hash are saved by the game (UN6/lead); the trainer's own checkpoints (weights, ES state, curriculum stage) are files under the run's output directory, resumable bit-exactly (test `Train_Resume_Exact`).

## 12. Warehouse implications
None directly. `docs/runs/TRAINING_REPORT.md` and the JSON results are ingested by UN11a into the `0046_unification_brain.sql` tables (artifact, training and evaluation metadata, TR-01/TR-02 results); this package defines the JSON shape and keeps it stable.

## 13. Acceptance tests
| Id | Test (behavioural) | Maps to | Pass |
|---|---|---|---|
| UN6ta-T1 | `Thresholds_CommittedFirst`: the thresholds file exists with T1–T7 as §D9.2 c, a recorded SHA-256, and `eval` refuses to run without a matching `--thresholds-hash`; the result files carry the hash | TR-01 honesty | exact |
| UN6ta-T2 | `Trainer_RefusesHeldOut`: `bc`, `es` and the environment reject seeds 900001–900020 and held-out layouts | TR-01 | refused |
| UN6ta-T3 | `Teacher_NeverEvaluated`: a rollout with any `ControlOrigin.Teacher` in an `eval` run aborts and is marked invalid; static check that the teacher type is not in `God.Sim` | amendment §6 | exact |
| UN6ta-T4 | `Quantisation_Parity`: float master weights → int16 artifact → UN6 integer inference gives identical outputs on 1,000 recorded observation vectors (an external-optimiser export passes the same test); until UN6 merges, parity is against the UN0 `IPrimitivePolicy` stub and re-run by UN6t-b | QA-01 | bit-exact |
| UN6ta-T5 | `Train_Deterministic_AcrossWorkers`: the same seed and config give identical weights with `--workers 1` and `--workers 4`; `Train_Resume_Exact` | determinism | exact |
| UN6ta-T6 | `Harness_SelfTest_StubWorld`: on the stub world behaviour cloning reaches ≥ 0.95 action agreement and ES improves episode return over the untrained arm; the report is labelled **HARNESS SELF-TEST, not a result** | harness only | pass |
| UN6ta-T7 | `Report_FailureIsReported`: fed fabricated failing metrics the report writer states "TR-01: not passed; UA-learned: not green", names the failed measures, recommends `Brain.Kind = 2` (labelled fallback) and cannot emit a "trained" verdict | TR-01 | exact |
| UN6ta-T8 | `Artifact_Reproducible`: the same inputs give byte-identical `FounderPolicyV<n>.g.cs`; the header lists every field of §5.8; a changed schema hash invalidates it | TR-01 | exact |
| UN6ta-T9 | `Returns_NoGoodsNoTitle` (static and run-time): the return function reads only need pressures, pain, health and death; no holder, lot, building count or ownership read | D9.1 d | exact |
| UN6ta-P | pins: U-P1, P1, P4, P8, Stage 3 hashes unchanged (the trainer is additive) | all | exact |

D8 ids: none map directly; TR-01 and EC-02 are completed by UN6t-b.

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN6ta|FullyQualifiedName~TRHarness|FullyQualifiedName~Neutrality"
dotnet run --project DotNet/God.Train -c Release -- env-check --stub --seed 1
dotnet run --project DotNet/God.Train -c Release -- bc --stage S2 --stub --seeds 1-8 --out <scratch>\un6ta
dotnet run --project DotNet/God.Train -c Release -- eval --stub --arms untrained,frozen,trained --thresholds-hash <sha256> --out <scratch>\un6ta
```
Report: the thresholds hash, stub-world metrics labelled as self-test, runtime per rollout and rollouts per second per worker.

## 15. Unity demonstration
None. `God.Train` is outside `Assets/`; run `AgentScripts/check-game-compile.ps1` only if any file under `Assets/` changed (the artifact folder is under `Assets/God/Sim` and must still compile there: 0 errors).

## 16. Performance considerations (D10)
Rollout throughput sets how far training can go in the ≈ 4 hours of compute (UN6t-b): measure ticks per second per worker on the small island and on 72 persons; keep environments allocation-light; default `--workers` leaves cores for the lead's suites (default 8 (L), the lead may raise it when the machine is quiet). Training must evaluate the same inference cost the game pays (≤ 20,000 MAC per decision).

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un6t -b unity/un6t <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un6t\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un6t record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.
Package-specific: this package is not merged on its own by default; at the end of UN6t-a the agent tags the branch (`un6t-a`), writes "UN6t-a record" (thresholds hash, harness commands, stub results labelled as self-test) and hands over to the lead for the hash record, then continues as UN6t-b on the same branch. The lead merges only at index §6 merge 13.

## 18. Risks
- **Thresholds written or changed after seeing results:** the file is committed first with its hash; `eval` refuses without it; the lead checks the commit order at merge 13.
- **Trainer overfits the evaluation:** held-out seeds and layouts are unreadable to the trainer; validation seeds < 900000 are used for model selection.
- **Hidden oracle in the learned policy through the observation:** the observation is only `Obs v1` (UN2's encoder, local perception); no true-state field; a static test lists the observation fields.
- **Compute shortfall:** the cut plan (index §8) drops S7–S8 from V1 and reports them untrained; the freeze at H14.0 is fixed.
- **Teacher leaks into scenarios:** the compile-time and static checks above.
- **Non-determinism from threading:** results keyed by seed, merged in seed order; test with 1 and 4 workers.

## 19. Definition of done
UN6ta-T1…T9 and UN6ta-P green; thresholds file committed with its hash recorded in DEV_LOG; harness commands documented; stub results labelled as self-test; "UN6t-a record" lists what ran and what did not; branch tagged `un6t-a`; handed over, then continued as UN6t-b.

## 20. Smaller checkpoints
- **UN6t-a1** (first hour): thresholds file and hash, project skeleton, held-out refusal, teacher quarantine (T1–T3).
- **UN6t-a2**: stub environment, behaviour cloning, ES, quantisation parity, determinism (T4–T6).
- **UN6t-a3**: artifact writer, report writer, returns audit (T7–T9).

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
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN6t-a record" under "Design decisions taken during implementation".
- Training honesty, restated for this package: predeclared thresholds committed before held-out runs; untrained and frozen controls; held-out seeds and layouts; a versioned artifact with a content hash; a labelled fallback is never counted as trained; a failed TR-01 means UA-learned is not green; scripted demonstrations are mechanics evidence, never evaluation.
