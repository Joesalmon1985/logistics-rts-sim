# UN10a: Presentation of the unified world and possession HUD

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

| Field | Value |
|---|---|
| Package identifier | **UN10a** (Tier UA, Waves 2–3, Checkpoint UA-learned; holds the Unity token H11.6–H14.8) |
| Branch / worktree | `unity/un10a` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un10a`, from `unity/poc` after **UN1, UN2, UN3b, UN4** merged (UN9 is merge-after) |
| Effort | 4.0 agent-hours (planned H10.8–H14.8; merge H15.2) |
| Status | Not started. Replaces the D8 UN10 spec (UN10a here is the UA part; UB views are UN10b) |
| Recommended agent | **Sonnet** (bounded Unity view and UI work on a finished sim API; Unity token holder). Never Fable |

## 1. Objective
Make the unified world visible and playable as a god, without any view mutating sim state: continuous terrain for the seven terrains, coast and sea; forage places, water points and raw nodes; buildings, construction sites with progress, installed fixtures, carried and held-out items; primitive animation (turn, step, use, gesture, sleep, strike); a twelve-need panel; the possession HUD with all twelve controls including design placement and strike; and the **brain badge** (artifact version, or **UNTRAINED** / **UNTRAINED FALLBACK**). No omniscient UI is presented as NPC knowledge.

## 2. User-facing outcome
The player starts "Living island (you as a god)" (after UA-mech), sees six tribes living on the island, can select a person and read their twelve needs, what they carry, what they know and what they believe (labelled as the person's belief), watch them forage, build, carry, sleep, gesture and fight, possess one and drive the same twelve controls the brain uses, and always see which brain is running. Nothing on screen says who "owns" anything; physical contents, occupancy and a person's own claims appear as separate things.

## 3. Design anchors
- Amendment §2 and §3 (UI must not present omniscient truth as NPC knowledge; physical contents and observed claims are distinct), §7 QA-01 and QA-02 (view reads the same facts the analytics keep distinct).
- §D9.1 b (executor, interactions, construction, exchange, conflict), §D9.4 (movement lattice, facing, durations), §D9.5 G1–G9 (sea ring, coast nodes, water points, hall, herbs), §D9.2 a (observation, `BrainState` read model), §D9.6 (draw with instancing and level of detail for UC).
- §23.5 (`Prim`, `PrimitiveControl`, `ControlOrigin.Possessed`), §23.8 (record shapes read by views), §23.1 (`Brain.Kind`, `Brain.Artifact`), §23.12 (events the views animate), R79, R83.
- CLAUDE.md: `Assets/God/Game` only reads state, sends commands and animates events.

## 4. Relevant existing source files (verified)
- Board and ground: `Assets/God/Game/Board/BoardSpace.cs`, `HexDressing.cs`, `HexDressingView.cs`, `Assets/God/Game/Island/IslandView.cs`, `IslandGroundMesh.cs`, `IslandDressing.cs`, `IslandField.cs`, `Assets/God/Game/View/IslandGround.cs`, `TerrainBuilder.cs`, `Assets/God/Sim/Board/Board.cs`, `BoardGenerator.cs`, `BoardTopology.cs`, `HexCoord.cs` (`Terrain` enum at `:41`).
- Figures and props: `Assets/God/Game/Models/NpcLayer.cs`, `PeopleModels.cs`, `FigureParts.cs`, `BuildingLayer.cs`, `BuildingModels.cs`, `BuildingModels.Stage3.cs` (Stage 3 only), `PropModels.cs`, `ModelKit.cs`, `StandardLayer.cs`, `LifeStageLayer.cs`, `GestureLayer.cs`, `PoseLayer.cs`, `OverlayLayer.cs`, `FxLayer.cs`, `DivineFxLayer.cs`; `Assets/God/Game/Rendering/MeshBuilder.cs`, `MaterialFactory.cs`.
- Animation: `Assets/God/Game/Animation/PoseSequencer.cs`, `PoseClips.cs`, `GestureAnimationPlanner.cs`, `AnimationCue.cs`, `CharacterLook.cs`, `DivineFxPlanner.cs`.
- HUD and input: `Assets/God/Game/Hud/GodHud.cs`, `GodHud.People.cs`, `GodHud.Selection.cs`, `GodHud.Tabs.cs`, `GodHud.TrueState.cs`, `GodHud.Tutorial.cs`, `Hud/Divine/*` (toolbar, card, feed, alerts, `DivineCatalog.cs`), `Assets/God/Game/Input/DivineCommandFactory.cs`, `PlayerController.cs`, `IInputSource.cs`, `PlayerIntent.cs`, `Camera/CameraDirector.cs`, `SelectionModel.cs`; boot and menu: `Assets/God/Game/Boot/GodBootstrap.cs`, `DivineHost.cs`, `Menu/GameMenu.cs` (UN9 owns the host and menu hunks).
- Sim read models you consume (once merged; do not exist at spec time): `Unified/UnifiedState.cs`, `Unified/World/*` (nodes, forage places, water points, raw nodes), `Unified/Buildings/*`, `Unified/Construction/*` (sites), `Unified/Needs/*`, `Unified/Brain/*` (kind, artifact, `BrainState` read model), `Unified/Motor/*` (current action, facing, progress).
- Tests to model on: `Assets/God/Tests/PlayMode/IslandViewTests.cs`, `ModelViewTests.cs`, `PopulationViewTests.cs`, `DivineHudPlayTests.cs`, `GestureVocabularyPlayTests.cs`, `HudViewOnlyTests.cs`, `PlayMode/TestSetup.cs`, `Assets/God/Tests/EditModeGame/GestureAnimationPlannerTests.cs`, `PlayerCommandBuilderTests.cs`.
- Compile check: `AgentScripts/check-game-compile.ps1`; build `AgentScripts/BuildWindows.cs`; Unity CLI per runbook §9.

## 5. Allowed scope
1. **Terrain and board:** continuous ground for the seven terrains, the sea ring (default 54 hexes, any radius), coast nodes, shorelines and impassable sea; hex colours and a simple original dressing per terrain (original data, no third-party art; no Sims 2 derivatives).
2. **World objects:** forage places (seven foods), fresh-water and salt-water points, raw nodes with remaining stock shown as pile size, springs; each readable at a glance and instanced (one draw call per kind).
3. **Buildings and sites:** completed buildings per catalogue id (the six level-1 shelters, hearth, well, carpenter, hall, latrine: simple original models), **construction sites with visible progress** (labour fraction and delivered materials), upgrade sites, installed fixtures (beds, mats, benches, chests) and occupancy markers; destroyed and damaged states.
4. **People:** `NpcLayer` driven by unified state: facing (six sectors), step, turn, use, gesture, sleep (in a bed or on the ground), strike, injured, carrying (slots) and a **held-out** item pose; animation from events and current action only (`GestureAnimationPlanner` extended with the unified gesture acts); people of all tribes drawn with tribe colour as a **visual affiliation only**.
5. **Needs panel:** twelve channels (Hunger, Energy, Warmth, Safety, Pain, Thirst, Hygiene, Bladder, Social, Fun, Comfort, Room) as satisfaction bars with the active mask respected; health and injury; age band.
6. **Person card (new tab, view only):** *Physical*: carried items, equipped, location, current action and last result code. *Knows*: known designs, recipes and interactions from `BrainState`. *Believes*: the person's own claims and remembered places and people, **labelled "believes"**, with source (own, observed, told), confidence and expiry; contradictions shown side by side. No "owner", "owns" or "rightful" text anywhere (static string test). The god's truth overlay (`GodHud.TrueState.cs`) may show physical truth under the existing "true state" convention but never as NPC knowledge and never an ownership fact.
7. **Possession HUD:** one input path for all twelve controls (turn, four steps, TAKE, DROP, USE with source, target, slot and **design placement** on a chosen node, GESTURE with act and referents, SLEEP, WAIT, **strike** as USE slot strike); buttons and keys build `PrimitiveControl` through the existing command factory and send it as `ControlInputKind.Primitive` with `ControlOrigin.Possessed`; a click-to-move may expand into step primitives labelled `Possessed` (the view never paths for the sim; it plans from the visible map); failures show the `MotorStatus` reason.
8. **Brain badge** on the HUD and on the person card: "Brain: FounderPolicyV<n> (hash <8>)" when `Kind = 1`; "UNTRAINED" when `Kind = 0`; "UNTRAINED FALLBACK" (visibly different colour) when `Kind = 2`; a "FAILED-TR01" marker when the artifact carries it. A static test ensures the badge string comes from `Brain.Kind` and nothing else.
9. **Divine HUD kept:** miracles, suggestions, commandments, compulsion, possession work as before (`Hud/Divine/*`), with the physical-only wording.
10. **Level of detail and instancing** for UC: people, forage and raw nodes use instanced rendering and distance LOD; no per-frame allocation; the view reads state only.
11. **PlayMode tests and screenshots** for the lead's checkpoint (`Q3Shots`-style capture in `wt-lrs-ckpt` or the package worktree).

## 6. Explicitly excluded scope
Any mutation of sim state from `Assets/God/Game`; any ownership, permission, rights, theft or legitimate-owner text, icon or colour; an omniscient panel presented as a person's knowledge; settlements, roads, groups and conflict-at-scale views (UN10b, UB); third-party art or audio, Sims-2-derived assets, copying from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; changes to host or menu files owned by UN9 beyond calls into their API; new sim types (an amendment request to the lead); audio; the tutorial rewrite.

## 7. Dependencies
- **Hard (merged first):** UN1 (world, needs), UN2 (executor, `Percept`, possession adapter), UN3b (buildings, interactions), UN4 (construction sites); transitively UN0.
- **Merge-after:** UN9 (hosts, menu and gate hunks); UN6 supplies the `BrainState` read model and badge data (UN10a codes against the interface and merges after UN6).
- **Feeds:** UN11a (UA-learned gate needs PlayMode green and screenshots), UN10b, UC1.

## 8. Shared contracts
- **R:** §23.5, §23.8 record shapes and §23.1 tuning (read only), existing divine control seam (`Divinity/Control.cs`, `Embodiment.CheckAction`), `God.Game` read APIs; the badge reads `Brain.Kind`/`Brain.Artifact` from state.
- **M:** `Assets/God/Game/Unified/*` (new folder), flagged additive hunks in `GodHud*.cs`, `NpcLayer.cs`, `GestureAnimationPlanner.cs`, `DivineCommandFactory.cs`, `IslandView.cs` (each named in the record), PlayMode and EditModeGame tests. **Never** edit sim files.

## 9. Files and modules owned
`Assets/God/Game/Unified/**` (views, HUD, badge, person card, animation planner additions, instanced layers), the flagged hunks above, `Assets/God/Tests/PlayMode/Unified*.cs`, `Assets/God/Tests/EditModeGame/Unified*.cs`, new `.meta` files for each new file and folder (commit them), screenshots under `docs/runs/screens/un10a/` (small PNG, committed only if under 2 MB total).

## 10. Required domain events
None emitted (views only). The package consumes `ControlApplied`, `ControlFailed`, `BuildingCompleted`, `SitePlaced`, `MaterialContributed`, `LabourContributed`, `FixtureInstalled`, `OccupancyChanged`, `NeedRelieved`, `HeldOut`, `HandedOver`, `StrikeResolved`, `StrikeWitnessed`, `BrainAttached` (1200–1399 as §23.12).

## 11. Save/load implications
None. UI state (selected person, tab) is not saved. A save loaded mid-state must redraw identically (PlayMode test `Unified_LoadRedraws`: save with a site half built and a held-out lot, load, compare scene summaries).

## 12. Warehouse implications
None.

## 13. Acceptance tests
| Id | Test (behavioural) | Maps to | Pass |
|---|---|---|---|
| UN10a-T1 | `View_ReadsOnly` (static and run-time): no `Assets/God/Game/Unified` code mutates sim state (assembly-reference check and a state-hash equality before/after drawing 1,000 frames) | CLAUDE.md, QA-01 | exact |
| UN10a-T2 | `Terrain_SevenAndSea`: the seven terrains, coast and sea draw at radius 4 and 6; no exception; shorelines at coast nodes | UN1 | pass |
| UN10a-T3 | `World_Objects_Visible`: forage, water points, raw nodes (pile size follows stock), sites with progress, completed buildings, fixtures and occupancy appear and update from events in a PlayMode scene on a prepared unified state | EC-01, B-rows (view) | pass |
| UN10a-T4 | `People_PrimitiveAnimation`: turn, step, use, gesture, sleep, strike, carry and held-out each show their pose from the current action; facing matches state | MOTOR-1 | pass |
| UN10a-T5 | `Needs_Panel_Twelve`: twelve channels shown with the active mask; values equal state | NEED-1 | exact |
| UN10a-T6 | `Card_NoOwnership_Text`: static scan of all `Unified` strings and prefabs: no "owner", "owns", "rightful", "theft", "stolen", "permission" text; the Believes tab labels claims as the person's own with source; contradictions appear together; the Physical tab shows carried items and contents only | QA-02, amendment §3 | exact |
| UN10a-T7 | `Possession_AllTwelve`: a possessed person executes each of the twelve controls through the HUD; the physical result equals the same stream applied by a fixture (U-P4 re-run in PlayMode); design placement places a site on the chosen node or shows the `MotorStatus` reason | QA-01, MOTOR-1 | exact |
| UN10a-T8 | `Badge_Honest`: the badge shows the artifact version and hash with `Kind = 1`, "UNTRAINED" with `Kind = 0`, "UNTRAINED FALLBACK" with `Kind = 2`, and the failed marker; the string is derived only from state | TR-01 honesty | exact |
| UN10a-T9 | `Unified_Boot_And_Menu`: the menu starts a unified game in PlayMode (after the lead's flag), the HUD shows no Stage 3 elements (no HQ stock, no victory points) | R79 | pass |
| UN10a-T10 | `Scale_LOD`: 1,200 persons on a prepared state draw with instancing, no per-frame allocation (allocation counter over 300 frames), frame time recorded (report, not a gate) | D10 | pass / report |
| UN10a-T11 | `Unified_LoadRedraws`; EditModeGame tests green; `GestureAnimationPlannerTests` and existing divine HUD tests unchanged | regression | exact |
| UN10a-P | pins: U-P1, P1, P4, P8, Stage 3 hashes unchanged with the switch off (view-only, so unchanged by construction; verified) | all | exact |

D8 ids: none (views); B-rows' visible aspects are exercised in T3.

## 14. Headless demonstration
`dotnet test DotNet -c Release --filter "FullyQualifiedName~Unified"` for the sim-side read models; the package's evidence is the Unity run below (views cannot run headless without `-nographics` PlayMode).

## 15. Unity demonstration
Required. `AgentScripts/check-game-compile.ps1` 0 errors first; open the editor in batch mode in `wt-lrs-un10a` (this package holds the Unity token H11.6–H14.8; one editor per folder); confirm a clean console; EditMode (`unity command --project-path . test run --mode editor --async-tests true`) then PlayMode (`--mode playmode`), poll `Temp/pipeline_test_status.json`; capture screenshots of: the unified island, a site under construction, a person card with Believes, the possession HUD, the badge with each `Kind`; `unity close .`. Never run the built player windowed in automation; builds only `-batchmode -nographics`. Do not commit `Assets/InitTestScene*`.

## 16. Performance considerations (D10)
Instanced rendering per object kind, LOD by distance, no per-frame allocation, views updated from event deltas rather than full scans (cost O(changed entities) per frame); people drawn from the state array, not through per-person GameObjects beyond a visible cap (L: 400) with proxy billboards beyond it; record frame time at 72 and 1,200 persons (report; UC target ≥ 20 ticks/s in the player is a UC gate, not UA).

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un10a -b unity/un10a <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un10a\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un10a record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.
Package-specific: this package **must** pass the compile check and hold the Unity token; the lead merges after UN9 (index §6 merge 12), runs PlayMode in `wt-lrs-ckpt`, and checks the screenshots and the badge against `Brain.Kind`.

## 18. Risks
- Ownership language creeping into the UI (habit from legacy cards): static string scan and review of every label.
- A view mutating state (for example through a convenience setter): assembly-reference check and hash-equality test.
- Unity Safe Mode from a compile error: the compile check before the editor.
- Slow PlayMode runs (45–100 minutes under throttling): start them early and keep the token window.
- Shipping art that looks "owned" or derived: original simple models only; no copying from the forbidden folders.
- Possession path diverging from the brain path: T7 compares to the fixture.

## 19. Definition of done
UN10a-T1…T11 and UN10a-P green; EditMode and PlayMode green with counts recorded; screenshots captured; badge shown for each kind; compile check 0 errors; editor closed; "UN10a record" in DEV_LOG lists what ran, what did not, the flagged hunks and the defaults taken; handed back by H15.2.

## 20. Smaller checkpoints
- **UN10a-1** (≈ 2 h): terrain, world objects, buildings and sites, people animation, needs panel (T1–T5, T11). Cut plan (index §8 H6.0/H14.5): shrink to hex-coloured terrain, people, sites, buildings, HUD badge and primitive possession; merge behind a flag if late.
- **UN10a-2** (≈ 2 h): person card (Physical, Knows, Believes), possession HUD with all twelve controls and design placement, badge, LOD (T6–T10).

## 21. Recommended agent
**Sonnet** (Unity token holder). Never Fable.

## 22. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded or keyed `Rng` with a label unique per call site, no wall clock, no unordered enumeration, no allocation in per-tick hot paths; bots, people, possessed bodies and test fixtures use the same primitives); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `Assets/InitTestScene*` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done.
- **D9 rules, always:** no owner, title, permission, use right, theft or legitimate-owner fact in unified code (static guard U-P3); no scripted build order, profession, strategy, retaliation or moral judgement; no new primitive verb (the twelve controls only); every game-visible fact is physical (holder, location, quantity, condition, occupancy, barrier) or a belief held by a person; origin (policy, possessed, fixture, teacher) never changes a physical result; no hidden oracle (`NodePather`, truth queries) on the unified decision path. Every scale-sensitive structure is configurable (tribes, radius, settlements per tribe) and every per-tick cost is O(active entities), never all-pairs (D10).
- **Before any Unity open or test run** (CLAUDE.md): `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors. Only then open the editor (batch mode) and confirm a clean console.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. Run `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives a cut-off; never bare `git stash` (shared stash stack), use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`. Never use or suggest Fable.
- Report exactly which tests and runs happened and plainly what did not run. A fixture-driven result is labelled mechanical; it is never presented as autonomous behaviour.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN10a record" under "Design decisions taken during implementation".
