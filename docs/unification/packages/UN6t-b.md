# UN6t-b: Founder training, held-out evaluation and the TR-01 verdict

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

| Field | Value |
|---|---|
| Package identifier | **UN6t-b** (Tier UA, **critical path**, Waves 2–3, part (b) of UN6t; Checkpoint UA-learned) |
| Branch / worktree | `unity/un6t` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un6t` (continues UN6t-a on the same branch) |
| Effort | 7 agent-hours including about 4 hours of compute (training from H9.5, freeze H14.0, held-out evaluation H14.0–H15.0, merge H15.0–H15.4) |
| Status | Not started |
| Recommended agent | **Opus**. Never Fable |

## 1. Objective
Train the founder policy against the **merged** unified world (executor, needs, interactions, construction, collection, exchange, conflict as they land), freeze an artifact at **H14.0 whatever its state**, evaluate it honestly on held-out seeds and layouts against the thresholds committed in UN6t-a with untrained and frozen controls, and write the verdict. The verdict is TR-01: pass or fail. Also report EC-02, EP-03 (learned), CF-02, TR-02 (initial) and ECON-1 (learned) as measured, never required, outcomes.

## 2. User-facing outcome
A versioned `FounderPolicyV<n>` embedded in the game and a plain report (`docs/runs/TRAINING_REPORT.md`) that says whether the founders can survive and do basic construction and collection teacher-free on maps they have never seen. The owner sees founders either competent (UA-learned green) or an explicit "not passed" with numbers and a visibly labelled fallback; never a scripted controller presented as trained.

## 3. Design anchors
- Amendment §6 and §7 (EC-02, TR-01, TR-02, CF-02, EP-03 learned); §D9.2 b (curriculum, feedback, teacher, artifact, seed split), §D9.2 c (T1–T7, honest reporting), §D9.2 d (TR-02), §D9.7 question 1 (innate network).
- Index §4 (training timeline inside UN6t-b), §5 (critical path), §6 merge 13, §7 (UA-learned definition), §8 (cut plan: H11.0, H14.0, H15.0).
- §23.1 (`Brain.Kind`, `Brain.Artifact`), §23.6, §23.12 (1320), R83, D9.10, D9.11.

## 4. Relevant existing source files (verified)
- Everything in [UN6t-a.md](UN6t-a.md) §4, plus (after merge) the real mechanics: `Unified/Motor/*`, `Unified/Percept/*` (UN2), `Unified/World/*`, `Unified/Needs/*` (UN1), `Unified/Buildings/*`, `Unified/Interactions/*` (UN3b), `Unified/Construction/*` (UN4), `Unified/Exchange/*` (UN7), `Unified/Conflict/*` (UN8a), `Unified/Brain/*` (UN6), `Unified/Start/*` (UN5 for the 72-person start), `Unified/Catalogue/Generated/*` (UN3a). Not present yet at spec time (created by the named packages).
- Existing evidence format: `docs/runs/` (reports directory), `DotNet/God.Runner/Demog/DemogReport.cs` and `Kpi/*` as models for report writers; `DotNet/God.Runner/Bench/*.json` for the benchmark record format.
- Pins and hashes: `DotNet/God.Runner.Tests/PolicyRunTests.cs:27`, `F1NeutralityTests.cs`.

## 5. Allowed scope
1. **Training runs** on the merged sim, in curriculum order, with stage criteria from UN6t-a: S1–S4 from H9.5 (after UN1, UN2, UN3b and UN6 merge); S5–S6 from H10.8 (UN4 merged); S8 from H11.6 (UN8a); S7 from H12.3 (UN7). Training and validation seeds `< 900000` only. Every checkpoint of weights, metrics and stage decisions is kept under the run directory and summarised in the report (including stages that failed their criteria).
2. **Artifact** `FounderPolicyV1.g.cs` (and `V2` if S7–S8 land before the freeze): written by the UN6t-a writer; header per UN6t-a §5.8; stages not trained are listed as **not trained** in the header and the report.
3. **Held-out evaluation** (H14.0–H15.0), teacher-free, divinity off, no path oracle, same need mask as training, thresholds hash matching the committed file: **20 small-island seeds × {untrained, frozen, trained}** (T1–T5, T7) and **3 full 72-person seeds (900001–900003) × 20 days × trained** (T6), on held-out layouts (mirrored, rotated, regenerated, unfamiliar start positions). The interruption fixture for T5 interrupts construction by scripted displacement (`ControlOrigin.Fixture`), then the people run teacher-free.
4. **The verdict:** TR-01 passes only if T1, T2, T3, T4 and T6 hold as written in the thresholds file (basic survival T1, T2, T6; basic construction and collection T3, T4); T5 (interruption) and T7 (interaction) are evaluated and reported against their thresholds but are **not** part of the TR-01 gate (§D9.2 c; the thresholds file says so: copy it, do not reinterpret); a missed T5 or T7 is reported prominently. Social outcomes (strategy mix, cooperation rate, Hawk/Dove proportions, population growth) are measured and never thresholds.
5. **Companion reports** in `docs/runs/TRAINING_REPORT.md`, each with its control and seeds: **EC-02** (autonomous design and site choice on held-out layouts: designs chosen, sites chosen, no fixed sequence, measured as the spread of first-building design and location across seeds and the absence of any scripted order); **EP-03 learned** (T7: hand-overs between persons by day 10; fulfil, partial and defect cases observed; not a TR-01 blocker if S7 is untrained, then "not evaluated"); **CF-02** (contested-resource encounters: aggression, avoidance, retaliation, non-retaliation and response to betrayal measured for trained, frozen and untrained arms and, if S8 was trained, with and without it; no strategy is required or scripted); **TR-02 initial** (matched pairs of children, exposed to a demonstrator versus unexposed controls: acquisition, use, retention and transfer to a new target, 20 seeds, failures reported; no adult memory copied); **ECON-1 learned** (shelter → tool or furniture → exchange → install → measured benefit: whether it occurred, reported only).
6. **If TR-01 fails:** keep the artifact, metrics, curves and the cause; mark the artifact `FAILED-TR01` in its header; the default game start uses `Brain.Kind = 2` (labelled fallback, UN6's `FallbackPolicy`); the failed artifact may be loaded only by an explicit `Brain.Kind = 1` experiment tuning and is then badged as failed; never promote it silently. The report states "TR-01: not passed; UA-learned: not green" at the top.
7. **If S7–S8 are untrained at the freeze:** T7 and CF-02 learned are "not evaluated", not passed; TR-02 reduces to one matched-pair experiment (index §8, H11.0).
8. Lead-facing artifacts: `docs/runs/TRAINING_REPORT.md`, `docs/runs/training/*.json` (per-seed results, scrubbed of paths), the artifact, a one-page "how to reproduce" with commands and seeds.

## 6. Explicitly excluded scope
Any change to the thresholds after the first held-out run (a new versioned file with a reason is allowed; the earlier result stays reported and counts); any teacher, path oracle, god or divine input in an evaluation; reward for professions, buildings, goods, title, milestones, morality or winning a dispute; tuning of the simulation or catalogue to make the thresholds pass (a mechanics fix is a lead-approved amendment to the owning package, logged); cherry-picking seeds; re-running held-out evaluation until it passes (one evaluation of the frozen artifact; a re-evaluation after a fix is reported as a second attempt with both results); counting `Fallback` as trained; Hawk/Dove, cooperation or population targets; the extended curriculum for roads, settlements and groups (UN6t2).

## 7. Dependencies
- **Hard (merged first):** UN0, UN1 (world, needs), UN2 (executor, encoder), UN3b (interactions, buildings), UN6 (inference, brain state), UN6t-a (harness, thresholds).
- **Curriculum-stage waits (merge-after):** S5 needs UN4; S7 needs UN7; S8 needs UN8a; the 72-person evaluation needs UN5 (population start).
- **Downstream:** UN10a badge (artifact version), UN11a (UA-learned gate and `UA_REPORT.md`), UN6t2, UC1.

## 8. Shared contracts
- **R:** §23 in full (read), the merged packages' APIs through the public environment seam only; `Unified/Brain` loader contract.
- **M:** `Unified/Brain/Artifacts/*`, `docs/unification/TRAINING_THRESHOLDS.md` (read-only after the first held-out run), `docs/runs/TRAINING_REPORT.md`, the TR tests. A needed seam in another package's area is an amendment request to the lead.

## 9. Files and modules owned
As UN6t-a §9, plus `docs/runs/TRAINING_REPORT.md`, `docs/runs/training/**`, `DotNet/God.Runner.Tests/TR01*.cs`, `TR02*.cs`.

## 10. Required domain events
`BrainAttached` 1320 (artifact version, hash, `Kind`; the `FAILED-TR01` flag in the artifact header surfaces in the event's kind field as `Kind = 1` plus a failed bit (L); agree the encoding with UN6 and the lead before merge). No other events.

## 11. Save/load implications
The save stores `Brain.Kind`, artifact version and hash; loading refuses a mismatched artifact (UN6). A game saved with `Kind = 2` loads with `Kind = 2`; the badge keeps saying so. `UnifiedState` shapes are not changed by this package.

## 12. Warehouse implications
UN11a's `0046` tables receive: artifact id, hash, curriculum, seed ranges, thresholds hash, per-arm and per-seed T1–T7 values, TR-01 verdict, TR-02 rows, CF-02 rows. Keep the JSON schema stable and versioned; UN11a maps it.

## 13. Acceptance tests
| Id | Test (behavioural) | Maps to | Pass |
|---|---|---|---|
| UN6tb-T1 | **TR-01** `Founders_Survive_HeldOut`: the T1, T2, T3, T4 and T6 thresholds of the committed file (T1 adults alive ≥ 0.90 and ≥ untrained + 0.40 after 10 days; T2 pain-band person-ticks for hunger or thirst ≤ 0.10; T3 ≥ 14/20 seeds with a level-1 shelter by day 5; T4 ≥ 10/20 seeds with raw collected and deposited by day 8; T6 adults alive ≥ 0.75 on each of 3 full seeds after 20 days) on held-out seeds and layouts, teacher-free | TR-01 | all met, **or the failure is the reported result** |
| UN6tb-T2 | `Controls_Reported`: the same measures for the untrained and frozen arms in the report; the trained arm beats untrained as T1 requires | TR-01 | present |
| UN6tb-T3 | `Verdict_MatchesThresholds`: an independent recomputation (the lead's) of the verdict from the JSON equals the report; the thresholds hash in every result equals the committed file's and its commit predates the evaluation commit | TR-01 honesty | exact |
| UN6tb-T4 | `Artifact_VerifiedAtLoad`: the artifact in `unity/poc` loads with `Kind = 1` (hash, schema and catalogue hash match); `Quantisation_Parity` against the merged UN6 inference | TR-01, QA-01 | exact |
| UN6tb-T5 | **EC-02** `Design_Site_Autonomous_HeldOut`: on held-out layouts the people choose which design to place and where without any scripted sequence; reported spread of first design and site; zero uses of `ControlOrigin.Teacher` or fixtures in the evaluation | EC-02 | measured; the owner's "no fixed sequence" shown by variation and a no-script static check |
| UN6tb-T6 | **TR-02 initial** `Child_Exposure_Matched`: matched child pairs, exposed vs control, 20 seeds: acquisition, use, retention, transfer; no adult memory copied (UN6-T7 state check at start); failures reported | TR-02 | reported |
| UN6tb-T7 | **EP-03 learned / T7** and **CF-02** and **ECON-1 learned**: measured and reported, or "not evaluated" with the reason | EP-03, CF-02, ECON-1 | reported |
| UN6tb-T8 | `QA01_TrainedActors`: an identical primitive stream gives identical physics for the trained policy, a possessed actor and a fixture on the final artifact | QA-01 | exact |
| UN6tb-T9 | `Fallback_NeverTrained`: with `Kind = 2` the report, save, event and badge data say Fallback and no report line says trained | TR-01 | exact |
| UN6tb-P | pins: U-P1, P1, P4, P8, Stage 3 hashes unchanged with `Unification.Enabled = 0` | all | exact |

**Gate semantics:** UN6tb-T1 is a gate on the *evidence*, not on a green: the package is done when the held-out evaluation ran honestly and its result is recorded. A failed T1 leaves UA-learned **not green** and the checkpoint reported as "UA-learned: not passed". D8 ids: U2 (learned survival), U4 and B3 (autonomous first building, wording per D9), B10 (viability) are covered by T1/T3/T4 and the reports above.

## 14. Headless demonstration
```
dotnet run --project DotNet/God.Train -c Release -- es --stage S5 --seeds 1-64 --workers 8 --out <scratch>\un6tb
dotnet run --project DotNet/God.Train -c Release -- eval --arms untrained,frozen,trained --seeds 900001-900020 --thresholds-hash <sha256> --out <scratch>\un6tb
dotnet run --project DotNet/God.Train -c Release -- eval --full72 --seeds 900001-900003 --days 20 --arms trained --thresholds-hash <sha256> --out <scratch>\un6tb
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN6tb|FullyQualifiedName~TR01|FullyQualifiedName~TR02"
```
The demonstration is `TRAINING_REPORT.md` itself: per-seed table, arms, curves, verdict, and a short replay (`run --scenario unif_start --seed 900001 --set tuning.Unification.Brain.Kind=1 --days 3`) the owner can watch in the player.

## 15. Unity demonstration
Compile check (the artifact file is large generated C# under `Assets/God/Sim`): `AgentScripts/check-game-compile.ps1` 0 errors; UN10a shows the badge and the lead's checkpoint boots a unified game with the artifact.

## 16. Performance considerations (D10)
Training time and artifact size: weights ≈ 128-wide layers, tens of kilobytes; the embedded base64 must not slow compile (split into chunks of ≤ 64 KB strings (L)). The evaluated policy must stay within ≤ 20,000 MAC per decision; report decisions per person-day and ticks per second at 72 persons with trained inference (feeds UC: baseline for 600 and 1,200). Training compute is bounded to about 4 hours; the freeze is fixed.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un6t -b unity/un6t <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un6t\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un6t record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.
Package-specific: merge 13 (index §6): the lead re-runs two held-out seeds independently, checks that the thresholds file's commit predates the evaluation commit, and that the artifact hash, `Obs`/`Act` schema hash and catalogue hash match `unity/poc`; only if TR-01 passed does the lead set `Brain.Kind = 1` in `unif_*`; otherwise `Kind = 2` labelled. The branch is not merged before H15.0 unless the lead asks for an interim artifact.

## 18. Risks
- Training does not reach thresholds in time: report it; keep the labelled fallback; do not move the freeze or the thresholds.
- Policy exploits a simulator quirk (e.g. a loop that restores a need without cost): the environment returns come only from experienced needs and pain; review high-return trajectories; report quirks as mechanics bugs to the lead.
- Held-out leakage (layouts or seeds seen in training): the refusal tests and seed ranges; the report lists every seed used in training.
- Compute contention with other agents' suites: use `--workers` per UN6t-a §16; never start training while the lead's merge suite needs the machine without agreeing it.
- Overfitting to scenario generators: held-out layouts differ (mirrored, rotated, regenerated, unfamiliar starts).
- S7–S8 land too late: artifact V1 ships without them; reported as such.

## 19. Definition of done
Artifact `FounderPolicyV1.g.cs` (or V2) committed with hash; held-out evaluation run once on the frozen artifact with thresholds hash matching; `TRAINING_REPORT.md` with TR-01 verdict first, arms, per-seed tables, EC-02, EP-03 learned, CF-02, TR-02, ECON-1 (or "not evaluated"); the failure or success stated plainly; UN6tb-T3, T4, T8, T9 green; pins green; "UN6t-b record" lists what ran, what did not, compute used and defaults taken; handed back by H15.4.

## 20. Smaller checkpoints
- **UN6t-b1** (H9.5–H11): S1–S4 trained, validation metrics reported, interim artifact for UN10a boot tests (labelled interim).
- **UN6t-b2** (to H14.0): S5–S6 (and S7–S8 as available) trained; freeze.
- **UN6t-b3** (H14.0–H15.4): held-out evaluation, verdict, reports, merge.

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
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN6t-b record" under "Design decisions taken during implementation".
- Training honesty, restated for this package: thresholds committed before held-out runs (hash checked); untrained and frozen controls; held-out seeds 900001–900020 and held-out layouts; versioned artifact with content hash; fallback labelled and never counted as trained; a failed TR-01 means UA-learned is not green and is reported as such; one honest evaluation of the frozen artifact.
