# UN10b: Views for settlements, roads, groups and conflict at scale

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

**Later-stage spec (Tier UB).** Shorter than the UA specs; every owner-brief §40 field is present. Replaces the D8 UN10 UB part.

| Field | Value |
|---|---|
| Package identifier | **UN10b** (Tier UB; Checkpoint UB; Unity token holder when scheduled) |
| Branch / worktree | `unity/un10b` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un10b`, from `unity/poc` after **UN10a and UB1** merged (UN8b is merge-after) |
| Effort | 2.0 agent-hours |
| Status | Not started |
| Recommended agent | **Sonnet** (Unity token). Never Fable |

## 1. Objective
Draw the UB mechanics for the god and the player without presenting authority or ownership: settlements and their development areas, roads (completed edges and sites in progress), group proposals and contributions as the people's own communications, and conflict at scale (strike effects, injury markers, aggregated overlays) with level of detail for hundreds to over a thousand people.

## 2. User-facing outcome
The player sees villages grow, roads extend edge by edge, who gathered to build or fight, and where fighting is happening, with clear labels that group views are reconstructions of what people did and said, never a chain of command or a title.

## 3. Design anchors
Amendment §3 and §5 (no ownership or command presentation; observed injuries and claims distinct), §D9.6 (instancing and level of detail; UC targets ≥ 20 ticks/s in the player), D10, §23.8, §23.12 (events 1340–1379 and 1300–1304), index §2 row UN10b, CLAUDE.md (views read only).

## 4. Relevant existing source files (verified)
`Assets/God/Game/Unified/*` (UN10a), `Assets/God/Game/Models/BuildingLayer.cs`, `NpcLayer.cs`, `OverlayLayer.cs`, `FxLayer.cs`, `PropModels.cs`, `Assets/God/Game/Island/IslandView.cs`, `Assets/God/Game/View/TerritoryView.cs` (legacy Stage 3 territory view: not reused), `Hud/GodHud*.cs`, `Hud/GodHud.TrueState.cs`, `Camera/CameraDirector.cs`, tests `Assets/God/Tests/PlayMode/IslandViewTests.cs`, `ModelViewTests.cs`, `PopulationViewTests.cs`, `Q3Shots.cs`. Sim read models (not present at spec time): `Unified/Settlements/*`, `Unified/Roads/*` (UB1), `Unified/Groups/*` (UN8b), `Unified/Conflict/*` (UN8a).

## 5. Allowed scope
Settlement markers and development-area outlines; road edges (completed, site in progress) as instanced strips; group views in the person card and a "Gatherings" overlay derived from events (proposer, joiners, contributors; labelled "reconstructed from what people did"); conflict at scale: strike and injury effects with LOD, an optional aggregated "recent fighting" heat overlay (god's overlay, labelled), counts only; instancing and distance LOD for 1,200 people; performance report from PlayMode.

## 6. Explicitly excluded scope
Any ownership, title, command, rank, leader or authority symbol or text; mutation of sim state; new sim types; Stage 3 territory or HQ views; third-party art or audio; the tutorial.

## 7. Dependencies
Hard: **UN10a**, **UB1**. Merge-after: **UN8b**. Downstream: UN11b, UC1.

## 8. Shared contracts
R: §23.8, UB events, `God.Game` read APIs. M: `Assets/God/Game/Unified/*` (UB files), flagged additive hunks named in the record. Never edit sim files.

## 9. Files and modules owned
`Assets/God/Game/Unified/**` (UB files), `Assets/God/Tests/PlayMode/UnifiedUB*.cs`, `.meta` for each new file, screenshots under `docs/runs/screens/un10b/`.

## 10. Required domain events
None emitted; consumes 1340–1344, 1360–1365, 1300–1304, 1262–1263.

## 11. Save/load implications
None; `Unified_LoadRedraws` extended to a save with a road half built and a proposal open.

## 12. Warehouse implications
None.

## 13. Acceptance tests
| Id | Test | Maps to | Pass |
|---|---|---|---|
| UN10b-T1 | `View_ReadsOnly` extended: no state change from drawing 1,000 frames with UB views | CLAUDE.md | exact |
| UN10b-T2 | `Settlements_Roads_Drawn`: settlements, development areas, completed road edges and road sites appear and update from events on a prepared state | U9, U10 (view) | pass |
| UN10b-T3 | `Groups_Labelled_Reconstruction`: the Gatherings overlay and card section state they are reconstructed from acts; static string scan finds no ownership, title, leader, command or authority text | QA-02, amendment §3 | exact |
| UN10b-T4 | `Conflict_LOD`: 1,200 persons with 200 simultaneous strike effects draw with instancing; no per-frame allocation; frame time recorded | D10 | pass / report |
| UN10b-T5 | `Overlay_Labelled`: the fighting heat overlay is labelled as the god's aggregate overlay, not NPC knowledge | amendment §3 | exact |
| UN10b-P | pins U-P1, P1, P4, P8, Stage 3 hashes unchanged | all | exact |

## 14. Headless demonstration
`dotnet test DotNet -c Release --filter "FullyQualifiedName~Unified"` for read models; the evidence is the Unity run.

## 15. Unity demonstration
Required: `AgentScripts/check-game-compile.ps1` 0 errors; editor in batch mode holding the Unity token; EditMode and PlayMode; screenshots of a village with roads, a gathering overlay, and a 1,200-person battle scene; `unity close .`.

## 16. Performance considerations (D10)
Instancing, LOD, event-delta updates, no per-frame allocation, people beyond a visible cap drawn as proxies (L: 400); report frame time at 600 and 1,200 persons.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un10b -b unity/un10b <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un10b\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un10b record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.

## 18. Risks
Authority language creeping into group views; overdraw at 1,200 persons; Unity Safe Mode (compile check first); slow PlayMode (start early).

## 19. Definition of done
UN10b-T1…T5 and UN10b-P green; EditMode and PlayMode counts recorded; screenshots; compile check 0 errors; editor closed; "UN10b record" in DEV_LOG; handed back.

## 20. Smaller checkpoints
**UN10b-1** settlements and roads; **UN10b-2** groups, conflict and LOD.

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
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN10b record" under "Design decisions taken during implementation".
