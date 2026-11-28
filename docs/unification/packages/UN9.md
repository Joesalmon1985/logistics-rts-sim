# UN9: Legacy isolation and decoupling (unified mode has no title, no estates, no faction economy)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

| Field | Value |
|---|---|
| Package identifier | **UN9** (Tier UA, Wave 2, Checkpoint UA-mech; holds the Unity token H10.6–H11.1) |
| Branch / worktree | `unity/un9` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un9`, from `unity/poc` after **UN0** merged |
| Effort | 3.0 agent-hours (planned H8.1–H11.1; merge H11.6 after UN2) |
| Status | Not started. Replaces the D8 UN9 spec ("disable Stage 3 AI"): D9 widens it to every ownership, permission, estate, theft and prescriptive path of the audit |
| Recommended agent | **Opus** (many shared files, hash-sensitive gates). Never Fable |

## 1. Objective
Make a unified game (`Unification.Enabled = 1`) contain **only** physics, biology, perception, gestures, divinity and the new unified executor, and make every legacy decision or economy path unreachable from it: no faction AI, Stage 3 systems, estates, executors, store permissions, title-based filters, theft classification, organisations or employment. UN0 landed the master switch and the install gates (§23.3); UN9 applies the **call-site gates of the audit §8**, wires both hosts, routes divine compulsion through a labelled goal driver on the primitive executor, keeps the Stage 3 game as an isolated regression scenario, labels legacy warehouse facts, and proves it with runtime and static tests.

## 2. User-facing outcome
The start menu's "Living island (you as a god)" starts the **unified** game once UA-mech passes (the god-only host flag, never faction AI). The old Stage 3 game still starts from its own menu entry and reproduces its hashes. In the unified game a god can still suggest, command, compel, possess and work miracles, all through the same primitives as everyone else, and no person is ever "the owner" of anything.

## 3. Design anchors
- §D9.1 c (removed from unified play), §D9.1 d (goods without title), §D9.3 (switch, legacy isolation rule, pins), §D9.8; §23.3 (isolation list), §23.2 (U-P1, U-P3 runtime, U-P6), §23.11 (U-C1–U-C6 replace C4), §23.13 (warehouse legacy-only labels), §23.15 (host bootstrap, legacy call-site gates, compulsion goal driver are UN9's).
- Audit §1 (ownership and permission: writers, readers, `Permits.*`, `Goods.Move` call sites, invariants), §2 (estates, `ItemMoved` side effects, employment pay, rations, take-out, household sharing, gifts, M5 settlement, miracles), §3 (theft and crime classification), §4 (prescriptive behaviours: verbs, instinct tables, C1 build logic, care reflexes, reflexes in the sim loop), §5 (faction-level economy), §7 (seams to reuse), §8 (prioritised changes).
- Amendment §3 (physical possession is not ownership: audit every reader and writer; no silent adapter that recreates title), §5 (no automatic theft classification), §7 EP-01, EP-05, QA-02.
- R79 (VP and HQ-loss elimination removed in unified mode; unified mode is a saved tuning flag honoured by both hosts), R81, R84, D9.13.

## 4. Relevant existing source files (verified)
Hosts and menu (Unity): `Assets/God/Game/Boot/GodBootstrap.cs` (`StartAi` path: `PendingStartAi` at `:114`, `startAi` at `:176`, `StartGodOnly`, flags `-godonly`, `-godsmoke`), `Assets/God/Game/Boot/AiHost.cs`, `Assets/God/Game/Boot/DivineHost.cs`, `Assets/God/Game/Boot/SmokeRun.cs`, `Assets/God/Game/Menu/GameMenu.cs` (`LivingIslandGodOnly` at `:128`, button at `:239`, `StartLivingIsland` at `:121`), `Assets/God/Game/Menu/SaveStore.cs`, `ChainBotsSave.cs`.
Core: `Core/GodSim.cs` (`InstallModules` at `:123`, `AllHashPartNames` at `:35`), `Core/GameSetup.cs`, `Core/SimBuilder.cs`, `Core/SimInvariants.cs` (`:70`), `Core/Reasons.cs` (`NotOwner` at `:17`).
Ownership writers and readers (the audit's sites, to gate in unified mode): `Population/Goods.cs` (UN0 has landed the unified branch; UN9 verifies each caller), `Population/PopulationSystem.cs` (`FinishHut` bed rights around `:1052`, hut sleep rights `:925`, forage ownership `:958`, `:983`, pick-up `:1013`), `Population/PopulationModule.cs` (`Kill`, `DeathSpill`, `OnDeath` at `:92`), `Population/PopulationState.cs` (`Hut.Rights`, `Contributors`), `Population/PopulationInvariants.cs` (C4 at `:12`, `:102`), `Estate/Estates.cs` (`Estates.Open`, `EstateSystem` at `:407`), `Population/Households/HouseholdHooks.cs` (`EstateSystem` order at `:27`, `OnDeath` `:46`, `SenseBins` `:120`, `UsableFood` `:364`), `Population/Households/HouseholdActs.cs` (own-store gates `:164`, `:262`, `:275`, `:300`, `:325`, `:342`, `:405`), `Population/Households/Households.cs` (`StoreAccess.MayTake`, `HutRights` at `:121`), `Population/Households/HouseholdInvariants.cs`, `Civ/CivSystem.cs` (`Ownership.Classify` at `:17`, `DayStart` `:23`), `Civ/Ownership.cs` (`OwnerRelation`, `RightOn`, `Truth`, `Classify`, `Witness`, `Property.Claim`), `Civ/Work.cs` (`:86`, `:112`, `:174`, `:191`, `:229`, `:254`, `:284`, `:311`, `:316`), `Civ/Employment.cs` (`:84`, `:208`, `:222`, `:578`, `:585`, CIV3 `:803`), `Civ/CivMindHooks.cs` (`:76`, `:98`, `:158`, `:176`), `Civ/CivMindHooks.C3.cs`, `Civ/Assets.cs`, `Civ/CivEvents.C1.cs` (`TheftCommitted`), `Cognition/Affordances.cs` (`Permits.*`), `Social/SocialCandidates.cs` (`Held` at `:42`), `Social/SocialSim.cs` (own-lot filter at `:708`), `Social/SocialHooks.cs`, `Orgs/CommunalStore.cs` (`:129`, `:153`, `:216`), `Orgs/OrgMindHooks.C6.cs` (`:377`), `Orgs/Orgs.cs` (`Dissolve`, `Truth`), `Orgs/OrgModule.cs` (ORG4), `Orgs/Health.cs`, `Divinity/DivineActs.cs` (`OwnLotOf` at `:403`), `Divinity/DivinityHooks.D2.cs` (`:55`, `:73`), `Divinity/DivinityHooks.D4.cs` (`:61`, `:65`), `Divinity/DivinityMindHooks.D4.cs` (`:183`), `Divinity/Miracles.cs` (`:487`, `:514`, `:529`), `Information/PeopleCards.Civ.cs` (truth-layer owner rows), `Learning/Instincts.cs` (`InstinctSet.None` at `:24`, shelter rows `:43`).
Divinity seams you route through (read; the frozen types are UN0's): `Divinity/Control.cs`, `Divinity/Possession.cs` (`Embodiment`, `CheckAction`), `Divinity/Compulsion.cs`, `Divinity/Avatars.cs`, `Divinity/GodDirector.cs`, `Divinity/DivineActs.cs`.
Legacy Stage 3: `Ai/AiDirector.cs`, `Ai/AiBrain*.cs`, `Economy/*`, `Trade/*`, `Factions/*`, `Military/MilitarySystem.cs`.
Warehouse: `DotNet/God.Runner/Warehouse/PopulationMapping.cs` (`:95`), `C1Mappings.cs`, `HouseholdMappings.cs`, `Tables.C1.cs`, `WarehouseRecorder.cs`.
Tests to model on: `DotNet/God.Runner.Tests/PolicyRunTests.cs` (Stage 3 hashes at `:27`), `SmokeParityTests.cs`, `F1NeutralityTests.cs`, `M5BarterTests.cs`, `C1RunTests.cs`, `Assets/God/Tests/EditMode/EstateTests.cs`, `Assets/God/Tests/PlayMode/BootSceneTests.cs`, `MenuTests.cs`.

## 5. Allowed scope
1. **Both hosts** (`GodBootstrap`, `AiHost`, `DivineHost`, `SmokeRun`): a unified game (saved `Unification.Enabled`) never attaches `AiDirector`/`AiBrain` or any faction policy; `StartAi` is a no-op for it; the headless runner (`SimSession`/`RunEngine`) and the player produce **equal hashes** for the same seed (test); the god-only path (`GodDirector`, possession, miracles, commandments) is kept.
2. **Menu:** "Living island (you as a god)" starts the unified game **only after UA-mech is declared by the lead** (a single bool default flipped by the lead in the UA-mech merge; until then the entry starts the existing god-only game exactly as today); the Stage 3 game keeps its own entry. No other menu change.
3. **Call-site gates (the audit §8 list), each a unified-only branch and a no-op in legacy mode:** goods-owner readers and writers listed in §4 above (`Held`, own-lot and own-store filters, `SenseBins`, `UsableFood`, `StoreAccess.MayTake` gates inside acts, `Work.cs` owner gates, `OwnerRelation` and `Permits.*` readers in candidate generation, `CommunalStore`/`OrgMindHooks.C6` own filters, divine own-lot filters); the physical replacement is reach, weight, capacity, occupancy and barrier only. In unified mode **none of these paths is reached** (UN0 skipped the systems; UN9 makes the remaining reachable ones inert and proves it with the static guard U-P3).
4. **Estates:** `Estates.Open`, executors and `EstateOut` are unreachable; death leaves goods where they physically are (the existing carry spill); unified location and conservation invariants U-C1–U-C6 (UN0) replace C4 and `HouseholdInvariants` estate rules, which run only in legacy mode; ORG4, CIV1 and CIV3 run only in legacy mode.
5. **Theft and crime:** `Ownership.Classify`, `DayStart` lost property, `Property.Claim`, `TheftCommitted`, use-right and estate events are not emitted in unified mode (runtime test). Perception-based beliefs and `SocialMemory` stay (fed by unified perception).
6. **Prescriptive rows:** in unified mode `InstinctSet.None` and no `Thinker.BuildCandidates` (UN0's `PersonTick` replaces the think/act), so the M3/M4/M5/C1/C3/C6 instinct tables, the scripted shelter goal (`Instincts.cs:43`), the C1 plot and build logic and the `Eat/Flee` reflexes are unreachable; a static test lists every site of audit §4.2 and shows it is behind the unified gate.
7. **Divine compulsion and possession:** a `CompulsionDriver` (labelled goal driver, `ControlOrigin.Compelled`) that turns a compulsion goal into primitive controls through `Motor.Submit` (nearest legal step toward the goal using the person's own perceived map; no `NodePather` truth shortcut beyond what the compulsion contract already allows, recorded), and routing of `ControlInputKind.Primitive` possession and avatars through `Embodiment.CheckAction` against the same `Motor.Submit` (UN2 supplies the adapter; UN9 integrates and tests it); commandments and suggestions become observation inputs (UN6 reads them). Divine own-lot filters (`DivineActs.OwnLotOf`, D2/D4 give) use physical possession (the lot the body carries).
8. **Stage 3 regression scenario and pins:** the Stage 3 game is kept compiled and tested as an isolated scenario with `Unification.Enabled = 0`; hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` unchanged; a unified game rejects Stage 3 setups (clear message) and vice versa. No Stage 3 code is deleted.
9. **Warehouse labels:** `fact_theft`, `fact_ownership_spell`, `fact_use_right`, `fact_inheritance` documented and labelled **legacy-only** (no schema change; UN11a owns migrations): a unified run produces zero rows in them (test).
10. **U-P3 runtime** (UN0 landed the static guard): a unified 10-day run emits no `OwnershipChanged`, `TheftCommitted`, `UseRightGranted`, `EstateTransfer`; the static guard is extended over every site gated here.
11. Files: only those named in §9; any other shared file needing a gate is a flagged additive hunk named in the record and approved by the lead before merge (the audit §8 list is the approved set).

## 6. Explicitly excluded scope
Deleting Stage 3, M4, M5, C1–C7 code or their tests (they stay compiled and pinned for legacy scenarios); changing legacy behaviour or any legacy hash; a silent adapter that recreates title (for example "holder owns" through `ToReceiver`, or `OwnerRelation` returning allowed while filters remain); a new permission, access control or ownership concept of any kind; replacing deleted logic with scripted behaviour; building the unified brain, executor, buildings or exchange (UN2, UN3b, UN6, UN7); migrations (UN11a); view work beyond the host and menu hunks (UN10a); touching `SaveFile.cs` (the lead).

## 7. Dependencies
- **Hard (merged first):** UN0 (switch, install gates, `PersonTick` site, goods unified branch, U-P3 static, `ControlInputKind.Primitive`).
- **Merge-after:** UN2 (possession adapter, `Motor.Submit` for the compulsion driver); may start earlier.
- **Downstream:** UN10a (merge-after UN9: hosts and menu), UN11a (UA-mech gate, U-P3 re-run, ECON-2), the UA-mech tag.
- **Coordinates with:** UN3b and UN7 (their effects replace the deleted paths; EP-01 is jointly evidenced), UN8a (shares merge 7).

## 8. Shared contracts
- **R:** §23.3, §23.4 (goods without title), §23.5 (`Motor.Submit`, `Embodiment.CheckAction`), §23.11, §23.15; `Goods` unified branch; `ControlOrigin`.
- **M (flagged gate hunks, each a unified-only branch):** the files of §4 under "Ownership writers and readers" plus the host files; `Unified/Legacy/*` (new). **Never** edit `Goods.cs`, `LotBin.cs`, enums, `GodSim.cs` install order, `SaveFile.cs` or schema files (UN0/lead).

## 9. Files and modules owned
`Assets/God/Game/Boot/*` (host hunks), `Assets/God/Game/Menu/GameMenu.cs` (menu hunk), `Assets/God/Sim/Unified/Legacy/*` (guards, `CompulsionDriver`, legacy-mode helpers), flagged unified-only gate hunks in the audit §8 files listed in §4 above, `DotNet/God.Runner/Scenarios/stage3_regression*.scenario.json` (if not already present), tests `DotNet/God.Runner.Tests/UN9*.cs`, `EP05*.cs`, `Unified_NoStage3*.cs`, EditMode `Assets/God/Tests/EditMode/Unified/UN9*.cs`, PlayMode additions in `Assets/God/Tests/PlayMode/BootSceneTests.cs` and `MenuTests.cs` (hunks).

## 10. Required domain events
None new (UN9 removes emissions in unified mode). `ControlApplied` 1220 (UN2) carries `Origin = Compelled` for the driver. Legacy event definitions and registry tests are unchanged.

## 11. Save/load implications
No new state except the compulsion driver's per-person goal progress, which is the existing compulsion record (saved by D-package state). The unified flag is saved with the tuning (UN0). A unified save cannot be loaded as legacy and vice versa (clear refusal). Test `UN9_SaveLoadParity` (U-P6): save during a compulsion in progress and a possessed primitive stream, load, continue: equal.

## 12. Warehouse implications
No schema change. Legacy-only labels in the documentation of the four tables and in the example queries (`ExampleQueries.sql` hunk allowed); a unified run yields zero rows in them; UN11a's reconciliation depends on it (QA-02).

## 13. Acceptance tests
| Id | Test (behavioural) | Maps to | Pass |
|---|---|---|---|
| UN9-T1 | `Unified_NoStage3_SystemList`: the built unified sim's system list contains none of `DiceSystem`, `EconomySystem`, `MilitarySystem`, `TradeSystem`, `FactionSystem` (VP, elimination), `EstateSystem`, `CivSystem`, `OrganisationSystem`; no `AiDirector`/`AiBrain` attached in either host; zero intents from a faction-level policy seat | D8 U13, B11 (D9 wording) | exact |
| UN9-T2 | **U-P3 runtime** `Unified_NoLegacyTitle_Runtime`: 10 unified days, seeds 1–3: no `OwnershipChanged`, `TheftCommitted`, `UseRightGranted`, `EstateTransfer`; every lot's owner `None`; static guard clean over every gated site (list in the record matches audit §8) | U-P3, QA-02 | exact |
| UN9-T3 | **EP-05 (death)** `Death_Conserves_Goods`: a person dies carrying goods: the lots stay on the ground at the node (existing spill), no estate, executor, heir or title change; U-C1/U-C4 hold; another person can pick them up by `TAKE`; a dead person holds nothing | EP-05 | exact |
| UN9-T4 | **EP-01 (legacy fields)** `Legacy_Fields_Neutral`: with legacy `Owner`, `UseRight`, household and group fields set on a bin or lot (test fixtures write them directly), an accessible lot or bin is still taken by any physically able person; physical barrier, capacity, reach and occupancy still refuse; no owner-based candidate filter exists (static) | EP-01 | exact |
| UN9-T5 | `Hosts_Equal_Hash`: the headless runner and the player (batch smoke, `-batchmode -nographics`) give equal hashes for the same unified seed; `AiHost` attaches nothing | R79 | exact |
| UN9-T6 | **ECON-2** `Divine_Possession_Unified`: miracles, suggestions, commandments, possession and avatar run in a unified game; a possessed primitive stream and a `Compelled` driver give physical results identical to a `Policy` stream (U-P4 re-run with origins Possessed, Compelled, Fixture); the driver labels `Compelled` | ECON-2, QA-01 | exact |
| UN9-T7 | `Stage3_Regression_Pins`: Stage 3 smoke hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87`; P1, P4, P8; U-P1 with the switch off; U-P2 re-run | U-P1 | exact |
| UN9-T8 | `NoFlags_NoCarriers` (D8 U11 with D9 wording): over 20 unified days zero flags, road-segment carriers, walkers; every goods move names a person mover | U11 | exact |
| UN9-T9 | `Prescriptive_Unreachable` (static and run-time): instinct tables (audit §4.2), the scripted shelter goal, `Work.CanFound`, `Thinker.BuildCandidates`, `Assets.Create` and `Employment.Activate` are not reachable from `PersonTick` or any unified system (call-graph test over the listed symbols) | D9.1, amendment §2 | exact |
| UN9-T10 | `Warehouse_LegacyOnly_Empty`: a unified recorded run writes zero rows to `fact_theft`, `fact_ownership_spell`, `fact_use_right`, `fact_inheritance` | QA-02 | exact |
| UN9-T11 | `Menu_Unified_Gate`: before the lead flips the flag "Living island (you as a god)" starts the existing god-only game; after, the unified game; Stage 3 entry unchanged | R79 | pass |
| UN9-T12 | `UN9_SaveLoadParity` and `Unified_Refuses_Legacy_Load` | U-P6 | exact |
| UN9-P | pins: U-P1, P1, P4, P8, Stage 3 hashes unchanged with `Unification.Enabled = 0`, including every legacy ownership, estate and theft test (`EstateTests`, `M5BarterTests`, `C1RunTests`, `C3RunTests`, `C6RunTests`) | all | exact |

D8 ids: U11 and U13/B11 are kept (reworded as above); U8 (planner) is superseded by EC-02; the D8 ownership-based B2 refusal reasons are superseded by UN3b (D9 acceptance table).

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN9|FullyQualifiedName~EP05|FullyQualifiedName~Unified_NoStage3|FullyQualifiedName~PolicyRunTests|FullyQualifiedName~Neutrality"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_start --seed 1 --days 10 --record full --sqlite --out <scratch>\un9
```
Then `SELECT code, COUNT(*) FROM event WHERE code IN (<OwnershipChanged>, <TheftCommitted>, <UseRightGranted>, <EstateTransfer>) GROUP BY 1;` (expect no rows) and the system list print of the unified sim. Report both host hashes.

## 15. Unity demonstration
Required (host and menu files change): compile check `AgentScripts/check-game-compile.ps1` 0 errors, then, holding the Unity token (H10.6–H11.1), batch smoke of both hosts in `wt-lrs-un9`: EditMode green, PlayMode boot and menu tests, `GodOfTheIsland` batch smokes `-batchmode -nographics`: Stage 3 `-godsmoke 60000 -godseed 2` hash `4bd54b271857a803`; unified `-godonly` with a unified setup boots and runs without exceptions; `unity close .`.

## 16. Performance considerations (D10)
The gates are branches on a bool read once per call (`Unified.On`); no new per-tick work. The unified path must not iterate legacy lists (estates, assets, organisations, hut rights): they are empty or absent. The compulsion driver steps toward a goal using the spatial index and its own remembered path; O(1) per compelled person per decision, no all-pairs scan, no allocation.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un9 -b unity/un9 <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un9\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un9 record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.
Package-specific: gate hunks are reviewed by the lead file by file against audit §8 (index §6 merge 7); the Unity batch smoke is run in this worktree by the package that holds the token; `Hosts_Equal_Hash` and `Stage3_Regression_Pins` are re-run by the lead on the merge result.

## 18. Risks
- **Silent title adapters** (the audit §1.4 warning): `OwnerRelation` returning allowed while `Held` and the M4/C6 filters still compare owners: each family gets its own unified branch and the static guard asserts no owner read; review for "holder owns" through `ToReceiver`.
- **Legacy hash drift** from an ungated hunk: every hunk is a unified-only branch; U-P1 and the Stage 3 pins on every commit.
- **Unity Safe Mode** from a host compile error (`Assets/God/Game` is not compiled by `dotnet`): the compile check before any editor run.
- **Compulsion becomes a hidden path oracle:** the driver only steps along what the person perceives; no `NodePather` truth; reviewed and recorded.
- **Sites missed by the audit** (the audit has an unverified list §9): the call-graph and grep guards fail on any unlisted reader found later; record any addition.
- **Hosts diverge:** the equal-hash test.

## 19. Definition of done
UN9-T1…T12 and UN9-P green under `dotnet test`; Unity batch smoke of both hosts green and recorded; static guard lists every gated site; compile check 0 errors; "UN9 record" in DEV_LOG lists every file and hunk, what ran and what did not, the added sites beyond the audit, and the defaults taken; handed back.

## 20. Smaller checkpoints
- **UN9a** (≈ 1.5 h): hosts, menu flag, Stage 3 regression pins, system-list and runtime guards (T1, T2, T5, T7, T11, T12).
- **UN9b** (≈ 1.5 h): call-site gates, estates and death conservation, prescriptive unreachability, compulsion driver and possession integration, warehouse labels (T3, T4, T6, T8, T9, T10). Cut plan: none; UN9 is on the UA-mech path.

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
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN9 record" under "Design decisions taken during implementation".
