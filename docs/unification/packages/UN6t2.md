# UN6t2: Training increment V2/V3 (roads, settlements, groups, multi-group encounters)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

**Later-stage spec (Tier UB).** Shorter than the UA specs; every owner-brief §40 field is present. All training-honesty rules of [UN6t-a.md](UN6t-a.md) and [UN6t-b.md](UN6t-b.md) apply unchanged.

| Field | Value |
|---|---|
| Package identifier | **UN6t2** (Tier UB; Checkpoint UB; hard prerequisite of UC1) |
| Branch / worktree | `unity/un6t2` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un6t2`, from `unity/poc` after **UN6t-b, UB1 and UN8b** merged |
| Effort | 4.0 agent-hours (including compute) |
| Status | Not started |
| Recommended agent | **Opus**. Never Fable |

## 1. Objective
Extend the founder policy to the UB mechanics and re-evaluate it honestly: curriculum stages **S9** (settlements and roads as sites, with the UB2 catalogue rows), **S10** (groups: proposals, joining, contributing, leaving) and **S11** (contested multi-group encounters), producing artifact `FounderPolicyV2` (and `V3` if S11 lands), with a **new predeclared thresholds file** committed before any held-out run, fresh held-out seeds, and controls (V1 artifact, V2 frozen, untrained). Also the extended TR-02 (generational exposure and retention).

## 2. User-facing outcome
People can use roads, settlements and group projects in ways they learned, measured against numbers fixed in advance; the report says whether V2 is better, no worse than V1 on basic survival, or failed and V1 stays the default.

## 3. Design anchors
Amendment §6 (training honesty; longer-horizon credit; different adults different experience; children inherit architecture and temperament, not adult memory; matched exposure tests; if training fails keep the result and flag any fallback), §7 TR-02 (UA initial; UB extended), CF-02 (UB extension), §D9.2 b–d (curriculum, thresholds, artifact, seed split, run-time learning), R83, D9.10, index §2 row UN6t2 and §7 UB.

## 4. Relevant existing source files (verified)
Everything in [UN6t-a.md](UN6t-a.md) §4 and [UN6t-b.md](UN6t-b.md) §4; `DotNet/God.Train/*` (UN6t-a), `Assets/God/Sim/Unified/Brain/Artifacts/*` (V1), `docs/unification/TRAINING_THRESHOLDS.md` (V1; read-only), `docs/runs/TRAINING_REPORT.md` (V1 results), `Unified/Settlements/*`, `Unified/Roads/*` (UB1), `Unified/Groups/*` (UN8b), `Unified/Catalogue/Generated/*` (UB2), `DotNet/God.Runner/Demog/DemogCommand.cs` (multi-generation runs for TR-02 cohorts). Not present at spec time: the UB folders and V1 artifact.

## 5. Allowed scope
1. **Curriculum S9–S11** as data files in `God.Train`, with predeclared stage criteria; teacher (training-only, labelled) extended for demonstrations of road, settlement and group acts; the V1 artifact is the starting point (warm start) and also the **control arm**.
2. **Thresholds V2:** `docs/unification/TRAINING_THRESHOLDS_V2.md` committed **before** any V2 held-out run, with its hash: (a) **no regression**: V2 must meet V1's T1–T6 on the V2 held-out set; (b) UB measures (roads or settlements completed by persons in held-out runs, group projects completed, hand-overs) with numeric thresholds chosen in the file; (c) social outcomes measured, never thresholds.
3. **Seeds:** training and validation seeds `< 900000`; V1's held-out seeds 900001–900020 are now **validation or regression-report only** (they have been seen); **V2 held-out seeds are a fresh range 910001–910020 and held-out layouts** that the trainer refuses to load (L default).
4. **Evaluation arms:** untrained, V1, V2 frozen (behaviour cloning of the new stages only), V2 trained; teacher-free, god-free, no path oracle; the 72-person full start and a UC-small start (e.g. 2 tribes × 40) for the new mechanics.
5. **Extended TR-02:** matched cohorts over generations (exposed versus unexposed children across 3 generations on the long run), acquisition, use, retention, transfer; no adult memory copied; failures reported.
6. **CF-02 UB extension:** contested multi-group encounters measured (aggression, avoidance, retaliation, non-retaliation, response to betrayal, coordination), compared across arms; no strategy required.
7. **Artifact and report:** `FounderPolicyV2.g.cs` (header per UN6t-a §5.8, with catalogue hash after UB2 and the V2 thresholds hash), `docs/runs/TRAINING_REPORT_V2.md` with the verdict first; a failed V2 keeps its artifact marked `FAILED-TR02V`, V1 remains the default, and nothing claims improvement.

## 6. Explicitly excluded scope
Editing V1 thresholds or results; evaluating on V1 held-out seeds as the gate; rewarding roads, settlements, groups, goods or milestones (returns remain experienced needs, pain, health, death); a teacher in evaluation; changing the simulation to help the policy; Hawk/Dove or cooperation targets; the scale training for 100–200 people per tribe beyond the UC-small check (UC1).

## 7. Dependencies
Hard: **UN6t-b** (harness, V1, report), **UB1**, **UN8b** (mechanics to train on); UB2 recommended (catalogue hash must be final before training). Merge-after: none. Downstream: UC1, UN11b.

## 8. Shared contracts
As UN6t-a/b: §23.6, §23.1 (`Brain.Artifact` version 2), the loader contract of UN6. Artifact version and hash saved with the game; a V1 save loads with V1 (artifact files for each version retained).

## 9. Files and modules owned
As UN6t-a §9 for new files: `DotNet/God.Train/**` (S9–S11), `Unified/Brain/Artifacts/FounderPolicyV2.g.cs` (and V3), `docs/unification/TRAINING_THRESHOLDS_V2.md`, `docs/runs/TRAINING_REPORT_V2.md`, tests `DotNet/God.Runner.Tests/TR03*.cs`.

## 10. Required domain events
`BrainAttached` 1320 with artifact version 2; no other events.

## 11. Save/load implications
Artifact version and hash in the save; both V1 and V2 artifact files stay in the build so old saves load; loading refuses a mismatched hash.

## 12. Warehouse implications
UN11b maps V2 metadata and TR results into the `0046`/`0047` tables; JSON schema versioned.

## 13. Acceptance tests
| Id | Test | Maps to | Pass |
|---|---|---|---|
| UN6t2-T1 | `Thresholds_V2_CommittedFirst`: file and hash exist before the first V2 held-out result; `eval` refuses without it | TR-01 honesty | exact |
| UN6t2-T2 | `Heldout_V2_Fresh`: V2 held-out seeds 910001–910020 and layouts unreadable to the trainer; V1 held-out seeds used only for regression reporting | TR-01 | refused |
| UN6t2-T3 | `V2_No_Regression`: V2 meets V1's T1–T6 on the V2 held-out set and beats the untrained arm as T1 requires | TR-01 (UB) | met, or failure reported and V1 stays default |
| UN6t2-T4 | `UB_Measures_Reported`: roads or settlements completed, group projects completed, hand-overs, per arm | UB | reported |
| UN6t2-T5 | `TR02_Generational`: matched cohorts across 3 generations; acquisition, use, retention, transfer; failures reported; no adult memory copied | TR-02 (UB) | reported |
| UN6t2-T6 | `CF02_MultiGroup`: contested multi-group encounters measured per arm; no scripted strategy | CF-02 (UB) | reported |
| UN6t2-T7 | `Artifact_V2_Verified`: loads with `Kind = 1` version 2; quantisation parity; QA-01 for the V2 policy | QA-01 | exact |
| UN6t2-P | pins U-P1, P1, P4, P8, Stage 3 hashes unchanged | all | exact |

## 14. Headless demonstration
`dotnet run --project DotNet/God.Train -c Release -- es --stage S9 --warm V1 --seeds 1-64 --workers 8`; `eval --arms untrained,v1,v2frozen,v2 --seeds 910001-910020 --thresholds-hash <sha256>`; the report.

## 15. Unity demonstration
Compile check `AgentScripts/check-game-compile.ps1` 0 errors (artifact under `Assets/God/Sim`); a boot of a unified game with V2 recorded by the lead at the UB checkpoint.

## 16. Performance considerations (D10)
Same inference budget (≤ 20,000 MAC, ≤ 4 KB state); training compute bounded and recorded; artifact size reported; evaluation at UC-small checks cost per decision does not grow.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un6t2 -b unity/un6t2 <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un6t2\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un6t2 record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.

## 18. Risks
Seed reuse leaking V1 evaluation into V2 selection (fresh range); catalogue hash change after UB2 invalidating work (finish UB2 first); policy exploiting new mechanics in ways the simulator mis-models (review high-return trajectories, report as mechanics bugs); compute shortfall (report; V1 stays default).

## 19. Definition of done
V2 thresholds committed with hash; V2 artifact and honest held-out evaluation done once; `TRAINING_REPORT_V2.md` with verdict first and all controls; compile check 0 errors; "UN6t2 record" in DEV_LOG; handed back.

## 20. Smaller checkpoints
**UN6t2-1** S9 roads and settlements (V2); **UN6t2-2** S10 groups; **UN6t2-3** S11 multi-group (V3 if time).

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
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN6t2 record" under "Design decisions taken during implementation".
- Training honesty, restated: thresholds committed before held-out runs; untrained, frozen and previous-version controls; fresh held-out seeds and layouts; versioned artifact with content hash; fallback labelled and never counted as trained; a failed gate is reported as failed.
