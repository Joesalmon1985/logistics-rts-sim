# UN0: Unification foundation (behaviour-neutral shared surface)

Package spec for the Unification milestone (owner D8; owner brief §40 field set). Self-contained. Binding sources, in order: [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md) D8.1–D8.18, [SHARED_ARCHITECTURAL_CONTRACTS.md](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) (esp. §14, §15, §16, §17, §22), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) (R72, R73, R74), [UNIFICATION_DESIGN.md](../UNIFICATION_DESIGN.md) ("the design"), [UNIFICATION_BASELINE.md](../UNIFICATION_BASELINE.md) ("the audit"), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md), this file. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

| Field | Value |
|---|---|
| Package identifier | **UN0** (Tier A foundation, Wave 0, first package of Checkpoint UA) |
| Branch / worktree | `unity/un0` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un0`, from `unity/poc` (head `b297cb0` or its green successor) |
| Effort | 4 agent-hours (checkpoints UN0a 2 h: switches, tuning, state shell, hash part, ledger and ware widening, pins; UN0b 2 h: id ranges, dispatch, seams, fixtures, events, mapping stubs) |
| Status | Not started |
| Recommended agent | **Opus** (frozen shared surface; hash-layout widening). Never Fable |

## 1. Objective
Land, in one behaviour-neutral commit series, every shared number, slot, seam and test helper that the other eleven unification packages need, exactly as F1 did for Civ and Divinity, so that UN1–UN11 each change only their own files and run in parallel worktrees without moving a hash. With every unification switch at 0, every part hash of every existing scenario equals the base head's.

## 2. User-facing outcome
None visible. A developer can set `tuning.Unification.Enabled = 1` and get an (empty) unified module that does nothing yet; `Unification.Enabled = 0` is the default and reproduces the current game bit for bit. The package's real outcome is that nine later packages can be built and merged independently.

## 3. Design anchors
- Design §0.1, §1.1 rule U-1, §2.2 (`Board.HexType`), §4.2 (state), §5.1 (`BuildingDef`, `RecipeDef` shapes), §5.5 (`UseKind.Sleep`), §6.1 (`Settlement` record), §10.2 (`NpcMind.Plan`), §13.2 (switches and pins U-P1..U-P6), §13.3 (tuning block), §13.4 (goods and ledger), §13.5 (id ranges), §13.6 (events), §15 (open questions 1 and 2).
- Audit §3 (S1–S16), §7 (the six HQ-circle checks, R3), §12 (R1, R2, R4, R9, R13), §13 reuse map.
- Contracts §14 (id ranges), §15.1 (event codes), §16 (switches, pins), §17 (save), §22.0–22.5, §22.8 (the I1a layout this package must stay compatible with); rulings R72, R73, R74, R20.

## 4. Relevant existing source files (verified)
- Goods and wares: `Economy/Wares.cs` (`PersonalCount = 40` at line 86), `Population/PopulationState.cs` (`ProduceReason` 0–5, `ConsumeReason` 0–7, `GoodsLedger` with `ProduceReasons = ConsumeReasons = 8` at line 99), `Population/PopulationModule.cs` (`HashPopulation`, lines ~420–430, the F1.17 "old order then extras only when non-zero" rule), `Population/Goods.cs`, `Population/LotBin.cs`.
- Cognition and learning: `Learning/Verbs.cs` (`Max = 128` at line 23), `Learning/EpisodeKey.cs`, `Learning/Features.cs`, `Cognition/Thinker.cs` (`MaxCand = 36` at line 95), `Cognition/Mind.cs` (`NpcMind` slots, "hashed only when present"), `Cognition/Extensions.cs`, `Cognition/Affordances.cs`, `Policy/PolicyTypes.cs` (`NpcVerb`).
- Dispatch pattern to copy: `Civ/CivModule.cs`, `Civ/CivHooks.cs`, `Civ/CivMindHooks.cs`, `Civ/CivSystem.cs` (257), `Civ/CivTuning.cs`, `Civ/CivEvents.cs`, `Orgs/OrgModule.cs`, `Population/PopulationExtensions.cs`, `Core/GodSim.cs` (`StartGame` calls 107–116, `InstallModules` 123–135, `StateHashParts` 134–170), `Core/GodState.cs`, `Core/SimTuning.cs` (`IOmitWhenDefault` roots), `Core/SimBuilder.cs`, `Core/GameSetup.cs`.
- Territory (dice seam): `Territory/TerritorySystems.cs` (`DiceSystem` order 10, lines 43–72: pile on a matching hex).
- The six HQ-circle checks (audit §7): `Civ/Work.cs:68` (`CanFound`), `Population/PopulationSystem.cs:658-668` (hut `CanFound`), `Population/Households/Founders.cs:364` (`FreeNode`), `Population/PopulationModule.cs:325` (`PlaceSources`) and `:131` (`FounderNode`), and the Stage 3 `Territory/TerritoryState.cs:192-198` (`IsBuildable`, stays Stage 3 only).
- Telemetry and warehouse: `Telemetry/DomainEvent.cs` (`TelemetryDomain`, `RangeOf`), `DotNet/God.Runner/Warehouse/*Mappings.cs` (pattern: `C1Mappings.cs`, `CivMappings.cs`), `Save/SaveFile.cs` (lead only).
- Tests to model on: `DotNet/God.Runner.Tests/F1NeutralityTests.cs`, `F0GoodsConservationTests.cs`, `Assets/God/Tests/EditMode/Support/GoodsTrace.cs`, `DotNet/God.Runner.Tests/C1RunTests.cs`.

## 5. Allowed scope
1. **Switches and tuning.** `SimTuning.Unification` (root, `IOmitWhenDefault`, written only when changed, like `Civ`/`Orgs`/`Divinity`): `Enabled = 0`; sub-switches `Forage.Enabled`, `Forage.SharedThroughput.Enabled`, `Materialise.Enabled`, `Buildings.Enabled`, `Buildings.HutRow` (default **0**, R73), `Planning.Enabled`, `Groups.Enabled`, `Roads.Enabled`, `Settlements.Enabled`, `StartKind` (0 = Unified six-tribe start, 1 = Legacy, for pin U-P2). A helper `UnifiedSwitch.On(tuning, sub)` implements "`Enabled = 1` turns every sub-switch on unless the scenario sets it explicitly" (design §13.2). Empty nested classes `Geo`, `Forage`, `Buildings`, `Settlements`, `Roads`, `Recipes`, `Start`, `Planning`, `Groups` (each in its own partial file, owned by its package after UN0: `UnificationTuning.UN1.cs` …). `SimTuning.TrySet` paths must work for every field (the word "seed" is forbidden in a path, R54).
2. **State slot.** `GodState.Unification` (`UnificationState`, partial, null unless `Unification.Enabled` and the game has a population), with per-package partial files `UnificationState.UN1.cs` … `UN8.cs` (empty), sub-objects hashed in a fixed order (Geo, Buildings, Settlements, Roads, Recipes, Groups) each only when non-null. Counters: `NextSettlementId`, `NextRoadId`, `NextGroupId`, `NextRawNodeId` (int, from 1, never reused). **Hash part `"unification"`** appended to `GodSim.AllHashPartNames` after `"divine"`, present only when `Population != null` and `GodState.Unification != null`; `PartNamesOf`/`PartsOf` extended. (If I1a later appends `"industry"`, it appends after `"unification"`: an amendment the lead records.)
3. **Widening of the goods surface, compatible with contracts §22.3.** I1a has **not** landed (it merges only after the Tier 1 set and, per D8.18, after Checkpoint UB), so UN0 lands the part of I1a.1 and I1a.2 that unification needs, in the exact layout §22.3 prescribes, so I1a later adds only content:
   - `Wares.PersonalCount` 40 → **96**; the existing 0–39 keep their ids. Ware ids 40–71 are **left empty and reserved for I1a** (industrial block, §22.3; stage 3 books never hold them). **New unification wares take 72–86**: 72 `Mushroom`, 73 `WildEgg`, 74 `Rabbit`, 75 `WildVeg`, 76 `CactusFruit`, 77 `Seaweed`, 78 `Hay`, 79 `Sheep` (animal register only, never a lot), 80 `Wool`, 81 `Meat`, 82 `Hide`, 83 `BuildingBlock`, 84 `Bedding`, 85 `PreparedFood`, 86 `Hammer`; 87–95 spare. (The design §13.4 proposed 40–54; that collides with §22.3 and is **renumbered here**, ruling to be recorded by the lead as R75.) Existing wares reused: `Log` 1, `Clay` 2, `Stone` 5, `Plank` 7, `Brick` 9, `Pottery` 14, `Cloth` 17, `Axe` 20, `Roots` 29, `Fish` 30.
   - Helpers: `Wares.IsFood` extended (new foods; `Fish` handling is the open item of section 21 and is switchable), `IsRaw`, `IsLiving` (`Sheep`), weight column rows for the new wares.
   - Hashing by the F1.17 rule: base order (wares 0–31, then 32–39 only when non-zero as today), then wares 40–95 only when any of their ledger entries or holdings is non-zero. With no new ware, every `population` part equals the base (`Wares_LayoutNeutral`).
   - `GoodsLedger.ProduceReasons = ConsumeReasons = 16`; slots 8–15 hashed only when non-zero (`Ledger_WideningNeutral`). **`ProduceReason.Collected = 10`** (design §13.4 said 6; §22.3 reserves 6 `Extracted`, 7 `Processed`, 8 `Discarded`, 9 `Salvaged` for industry, so 10 is the first free slot; the lead amends §22.3's "reserved 10–15" to "10 = Collected, 11–15 reserved"). `ConsumeReason` unchanged (`Built` covers building and upgrade materials). Internal array layout may change; the hasher iterates the base order explicitly.
4. **Id ranges** (R72, with the corrections above and in section 21):
   - Learning `Verb`: `Verb.Max` 128 → **160**; rows 128 `Collect`, 129 `AdoptGoal` (internal), 130 `ProposeGroup`, 131 `JoinGroup`, 132 `ContributeGroup`, 133 `LeaveGroup`, 134 `ProposeRoad`, 135 `RoadWork`, 136 `ProposeSettlement`, 137 `Upgrade`, 138 `Repair`, 139 spare, 140–159 reserved. All per-verb arrays (association stores, `VerbInfo`, instincts) sized by `Max` must be audited; widened arrays hashed in the F1.17 way (rows ≥128 only when non-zero).
   - `NpcVerb` 220 `Collect`, 221 `RoadWork`, 222 `Propose` (`Detail` = group, road, settlement), 223 `JoinGroup`, 224 `Contribute`, sentinel `UnifReserved = 229`.
   - `ObjClass` 130 `RawNode`, 131 `ForagePlace`, 132 `Settlement`, 133 `Road`, 134 `GroupSite`, 135 `Goal` (candidate class), 136–149 reserved for unified building rows (assets take their `ObjClass` from their `BuildingDef` row).
   - `SelfFeature` 88 `Unsheltered`, 89 `StoreFull`, 90 `HasCollectRight`, 91 `InGroup`, 92–95 spare. `SituationKind` 152 `NoShelter`, 153 `BuildingNear`, 154 `GroupInvite`, 155–159 spare. Episode-key kinds 48 goal, 49 building investment, 50 collection route, 51 group contribution, 52–55 spare. `UseKind.Sleep = 5` appended to Civ's enum (`Civ/Assets.cs`, additive). `ThinkScratch.MaxCand` 36 → **40** (Unification quota 4; if I1a has landed by then the lead sets the value to 44 and records it).
   - Telemetry domains `Unification = 80`, `Buildings = 81`, `Planning = 82`, `Groups = 83` → `RangeOf` **1200–1299**.
5. **Mind slots.** `NpcMind.Plan` (`PlanState`, fixed-size, null unless `Planning.Enabled`; shell only: fields are UN6's) and `NpcMind.Unif` (`UnifMind`: `ulong Known` knowledge bitset, `ushort` counters; created lazily on first write so enabling the module without seeded content changes no mind hash). Both hashed only when present, after the F1 mind slots.
6. **Dispatch.** `UnificationModule.StartGame/Install/NewMind/OnDeath/Check` called after `DivinityModule` at every F1.5 site (no-ops while the state is null); `UnificationHooks` and `UnificationMindHooks` dispatched **after Divinity** (and after Industry when it exists) at every existing site: `SenseExtra`, `AddCandidates` (quota 4), act `Start`/`Act`/`End` for the new `NpcVerb`s, `ActionEnded`, the day-start and per-tick slots, and the `Kill` order (before M3 cleanup). Per-package partial files `UnificationHooks.UN1.cs` … with fixed call order UN1 → UN2 → UN3 → UN4 → UN5 → UN6 → UN7 → UN8. `UnificationSystem` registered at order **258** (after `CivSystem` 257, before `OrganisationSystem` 260) with partial slots `TickUN2` (materialise), `DayStartUN3`, `DayStartUN5`, `DayStartUN8`; it returns at once while the state is null.
7. **Neutral seams (each returns the base behaviour while `Unification.Enabled = 0`).**
   - `DiceSystem` success branch calls `UnificationHooks.OnDiceMatch(s, ev, hex, total)`; when `Materialise.Enabled` the pile is not placed (UN2 fills the body; UN0 body: always "not handled", so piles remain).
   - **Development-area predicate** `DevelopmentArea.Allows(s, node, kind)` and `Allows` calls replacing the six HQ-circle checks of section 4: with the switch off they evaluate exactly the old rule; with `Buildings.Enabled` on they call the (UN0 stub: "any land node not on a source") predicate that UN4/UN5 later implement from settlements (design §6.3, audit R3).
   - `Valuation` priors table seam: `ValuationPriors.Rows` (append-only data keyed by ware; empty), read by `Social/Valuation.cs` and the instinct priors; packages append rows in their own partial (`ValuationPriors.UN1.cs` …). Neutral while the table is empty.
   - `Board.HexType` (`byte[]`, null by default, hashed only when present) and `HexType` enum (1 Forest, 2 Mountain, 3 Wetland, 4 Pastures, 5 Fields, 6 Desert, 7 Sea), `Board.IsLandHex/IsSeaHex/ShoreNode` seams (default: derive from `Terrain`, unchanged for Stage 3).
   - `BuildingDef`, `BuildingLevel`, `CapabilityFlags`, `RecipeDef` data classes (design §5.1, §8.1: pure data, no logic) and empty `BuildingTable`/`RecipeTable` registries with partial slots `AddRowsUN3`, `AddRowsUN4`, `AddRowsUN7`; kind-id constants 10–15 (collection shelters), 20 (hut), 21 (communal hall), 30–36 (workshops).
   - `Settlement` record (design §6.1: `Id, Corner, Node, FoundedTick, FoundedByGroup, Founder, Area`) with `Settlements.Find/NearestArea/All` read API over `UnificationState.Settlements`; `Group` and `Goal` shells are owned by UN8/UN6 and are **not** landed here.
   - `GoalRules` registry shell (`Unification/Plan/GoalRules.cs`: an empty partial class with `RegisterUN5`, `RegisterUN7`, `RegisterUN8` partial-method slots and the rule interface `IGoalRule`), so UN5, UN7 and UN8 can register planner goal rules without waiting for UN6 (UN6 owns the bodies of the registry and `GoalRules.UN6.cs`).
   - `Founders.StartGame` generator **2** ("Tribal") stub: falls back to generator 1 until UN4a lands; `GameSetup.Unified` flag (default false) added.
8. **Events and catalogue.** `UnificationEvents.Catalog` as per-package partial arrays `UN1` … `UN8` (empty) plus the code table of design §13.6 recorded as constants and a doc comment; `EventCatalog.All` extended; `EventMapRegistry` calls `UnificationMappings.Register(r)` (stubs `UN1Mappings` … `UN8Mappings`). **No migration** (UN11 writes `0044_unification.sql`, number allocated by the lead; contracts §22.7 lists 0044–0049 free).
9. **Test support** (landed for everyone): `UnifiedFixtures` (EditMode `Support/` and a DotNet mirror): builds a minimal sim with `Unification.Enabled = 1`, a hand-placed board (named hex types, shore nodes), settlements, raw nodes, assets and NPCs at chosen nodes, so UN1–UN8 test against fixtures without waiting for each other; `UnifiedProbe` (collects unification events); static-test guards in "guarding form" that pass vacuously until code exists: `Unified_NoStage3EconomySystem` (UN9), `Plan_NoTruthReads` (UN6), `Unif_NoFactionReads`, `NoProfessionField` (UN6/UN11), `Production_DoesNotBranchOnOwnerKind` extended to `Sim/Unification`.
10. **Pins.** U-P1 (real) and guarding forms of U-P2…U-P6 (design §13.2), `Wares_LayoutNeutral`, `Ledger_WideningNeutral`, `VerbMax_Neutral` (association hashes equal with `Verb.Max = 160`), `MaxCand_Neutral`.
11. **Documentation.** Contracts **§23 "Unification addendum (owner D8, 28 Nov 2026)"** (docs edit, lead-reviewed) recording every number in this section and the corrections; DEV_LOG "UN0 record" with the final id table.
12. **Save.** `SaveFile.FromJson` accepts null `Unification`, null `NpcMind.Plan/Unif`, null `Board.HexType` (the lead owns `SaveFile.cs` and the version bump; this package supplies the diff for the lead to apply at the next checkpoint).

## 6. Explicitly excluded scope
Any behaviour: no forage, no materialisation, no building logic, no planning, no groups, no roads, no start generator body, no removal of Stage 3 systems (UN9), no views (UN10), no warehouse tables (UN11). Do not edit `Civ/Assets.cs` beyond appending `UseKind.Sleep`. Do not renumber any existing id.

## 7. Dependencies
Prerequisite: `unity/poc` green at `b297cb0` or its successor (baseline `dotnet test`, `check-game-compile.ps1`, Stage 3 hashes unchanged). Downstream: every other UN package.

## 8. Shared contracts
**This package is the one permitted to modify frozen surfaces** (with the lead): `Wares.cs`, `PopulationState.cs` (ledger), `PopulationModule.cs` (`HashPopulation`), `Verbs.cs`, `Thinker.cs` (`MaxCand`), `Mind.cs`, `Extensions.cs`, `PopulationExtensions.cs`, `PopulationSystem.cs` (dispatch and the circle check), `Founders.cs`, `Work.cs` (circle check only), `GodSim.cs`, `GodState.cs`, `SimTuning.cs`, `SimBuilder.cs` (module slot only), `GameSetup.cs`, `DomainEvent.cs`, the enum files named in section 5, `TerritorySystems.cs` (the dice seam only). Reads: everything else. After UN0 merges, no other UN package edits any of these; a missing seam is an amendment request to the lead.

## 9. Files and modules owned
`Unification/UnificationState.cs` (+ partial stubs), `UnificationModule.cs`, `UnificationHooks.cs`, `UnificationMindHooks.cs`, `UnificationSystem.cs`, `UnificationTuning.cs` (+ partial stubs), `UnificationEvents.cs` (+ partial stubs), `UnifiedSwitch.cs`, `HexType.cs`, `DevelopmentArea.cs`, `Buildings/BuildingDef.cs`, `Buildings/BuildingTable.cs`, `Recipes/RecipeDef.cs`, `Recipes/RecipeTable.cs`, `Settlements/Settlement.cs`, `Plan/GoalRules.cs`, `ValuationPriors.cs`, `UnifMind.cs`; the frozen-file edits of section 8; `DotNet/God.Runner/Warehouse/UnificationMappings.cs` and `UN1Mappings.cs` … `UN8Mappings.cs` (stubs); `Assets/God/Tests/EditMode/Support/UnifiedFixtures.cs`, `UnifiedProbe.cs`; `Assets/God/Tests/EditMode/Unification/UN0*.cs` (commit the `.meta` files); `DotNet/God.Runner.Tests/UN0*.cs`; the contracts §23 text.

## 10. Required domain events
None emitted. The catalogue constants of design §13.6 (codes 1200–1299) are declared so that later packages append events to their own partial arrays without allocating codes.

## 11. Save/load implications
New optional saved shapes (all null/absent at default): `GodState.Unification`, `NpcMind.Plan`, `NpcMind.Unif`, `Board.HexType`, widened ledger arrays, widened verb arrays. Test `UN0_SaveLoadParity` (a unified-enabled empty module with a lazily created `UnifMind`) and `UN0_OldSaveLoads` (a v-current save from before UN0 loads and hashes identically). The lead bumps `SaveFile.CurrentVersion` at the next checkpoint; UN0 does not.

## 12. Warehouse implications
Mapping stubs only; `EventMapRegistry` still builds; `UN0_Warehouse_StubsRegister` asserts the registry constructs and a run with the switch on records zero unification rows. UN11 owns the migration.

## 13. Acceptance tests
| Id | Test (behavioural) | Pass |
|---|---|---|
| UN0-T1 | `Unification_Off_Reproduces_Base` (U-P1) | with every unification switch 0, every part hash for `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, `civ_assets`, seeds 1–3, every checkpoint, equals the base head's; the three Stage 3 smoke hashes of `PolicyRunTests.cs:27` unchanged; population smoke hash unchanged |
| UN0-T2 | `Wares_LayoutNeutral`, `Ledger_WideningNeutral` | `population` parts equal the base for scenarios holding no ware ≥ 40 and no ledger slot ≥ 8; a lot of `Hay` or reason `Collected` makes the part differ (the extra is hashed) |
| UN0-T3 | `VerbMax_Neutral`, `MaxCand_Neutral`, `NpcMindSlots_Neutral` | association/mind/candidate hashes equal the base with the widened bounds |
| UN0-T4 | `Unification_Idle_Neutral` (U-P2 form) | `Unification.Enabled = 1`, `StartKind = Legacy`, no content: every part except `unification` equals UN0-T1 |
| UN0-T5 | `DevelopmentArea_Off_IsOldRule` | with the switch off the predicate returns exactly `Work.CanFound`'s, the hut `CanFound`'s, `Founders.FreeNode`'s and `PlaceSources`'s old results at 500 sampled nodes |
| UN0-T6 | `Dice_Seam_Neutral` | with `Materialise.Enabled = 0` and 1 and the UN0 stub body, piles are placed exactly as before and `DiceRolled` events are identical |
| UN0-T7 | `Fixtures_BuildAndRun` | `UnifiedFixtures` builds a board with named hex types, shore nodes, a settlement, a raw node, an asset and two NPCs and ticks 600 ticks deterministically (same hash twice) |
| UN0-T8 | `Switch_Matrix_Stub` (U-P6 form) | each sub-switch alone, pairwise, all on: runs, no exception, and with no content every part except `unification` equals UN0-T1 |
| UN0-T9 | `Unif_GuardsPass_Vacuously` | the static guards of section 5 item 9 exist, pass, and fail on a planted violation in a scratch fixture (mutation test) |
| UN0-T10 | `UN0_SaveLoadParity`, `UN0_OldSaveLoads`, `UN0_Warehouse_StubsRegister` | as sections 11 and 12 |

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN0|FullyQualifiedName~Neutrality|FullyQualifiedName~PolicyRun"
dotnet run --project DotNet/God.Runner -c Release -- verify --scenario band_6x20 --seed 2 --ticks 12000 --save-mid --record-matrix
dotnet run --project DotNet/God.Runner -c Release -- verify --scenario band_6x20 --seed 2 --ticks 12000 --set tuning.Unification.Enabled=1 --set tuning.Unification.StartKind=1
```
(the last run must print the same part hashes as the first except `unification`).

## 15. Unity demonstration
None. **Unity compile check required** (the `Ware` enum, `GodState`, `Board`, `Verb` and `ObjClass` are read by `God.Game`): `check-game-compile.ps1` 0 errors, then batch EditMode green (section 17 step 7).

## 16. Performance considerations
Widened ledger and verb arrays must not slow the tick measurably: run `bench --cases bench_npc_500` before and after on the same machine and report both (budget: within 3 percent). No new per-tick work while the state is null.

## 17. Integration procedure
1. The lead creates the worktree: `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un0 -b unity/un0 unity/poc`, then `robocopy C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un0\Library /E /MT:16` (exit codes 0–7 are success; the source editor must be closed).
2. Sim and EditMode tests first; the pins before anything else is built on them. Hand back an interim **UN0a** (items 1–3, 10, pin tests) as soon as it is green so the lead can review the ledger and ware diff early, then continue to UN0b on the same branch.
3. Before hand-back: `git merge unity/poc`; conflicts only in your own files; a conflict in a frozen file you did not edit means stop and report.
4. Neutrality pins that must hold: **P1, P4, P8**, U-P1, the three Stage 3 smoke hashes, and every pin already on `unity/poc` (P2, P3, P5, P9 where present).
5. Full `dotnet test DotNet -c Release` green.
6. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors.
7. Then open the editor in batch mode in `wt-lrs-un0` (only you hold the Unity token for this package), `unity status` ready, 0 `error CS`, EditMode run green, `unity close .`.
8. Write "UN0 record" in `docs/stage4/DEV_LOG.md` on the branch (the final id table, the defaults taken, what ran and what did not). Hand back to the lead. **Never push; never merge into `unity/poc`.**

## 18. Risks
- The widened verb array moves association hashes if any array is hashed whole: audit every use of `Verb.Max`; use the F1.17 rule.
- Ledger reason slot collision with I1a (section 21 item 1): the lead must record R75 and amend §22.3 before UN0 merges, otherwise I1a's later landing would collide.
- The six circle checks are in hot paths: the switch test must be one cheap branch.
- `GodSim` hash-part order: the position of `"unification"` relative to `"industry"` must be stated in the contracts addendum.
- Over-building: UN0 must not contain behaviour; every body is a no-op or a pass-through.

## 19. Definition of done
UN0-T1…T10 green under `dotnet test`; pins P1/P4/P8 and the Stage 3 hashes unchanged; compile check 0 errors; Unity EditMode green; the contracts §23 text and the "UN0 record" written and listing every id; the interim UN0a handed back before UN0b; no behaviour added.

## 20. Smaller checkpoints
- **UN0a** (≈2 h): switches, tuning, state slot and hash part, ware and ledger widening, `Verb.Max`/`MaxCand`, pins T1–T3.
- **UN0b** (≈2 h): dispatch, seams (dice, development area, valuation, `HexType`, data shapes), fixtures and guards, events and mapping stubs, contracts §23, T4–T10.

## 21. Recommended agent and notes for the lead
**Opus.** Items the lead decides before UN0 merges (all with the default UN0 takes):
1. **Ware and ledger collision with the industry addendum** (design §13.4 versus contracts §22.3): default as in section 5 item 3 (unification wares 72–86; `Collected` = 10; `PersonalCount` 96; I1a keeps 40–71 and its own ledger slots 6–9).
2. **`Fish` as food.** The M3 model treats `Fish` (30) as food; D8.2/D8.12 make raw fish a raw material that becomes prepared food. Default: `Wares.IsFood(Fish)` stays true in the base; UN1 adds tuning `Forage.RawFishNutrition` (default 0 = not edible raw) read in unified mode only.
3. **`MaxCand`** is 36 in code, not 40 as the design assumed: UN0 sets 40.

## 22. Used later by industry (keep the interfaces general)
I1a lands on top of this foundation: its remaining items are the ware enum values 40–71 (no resize), ledger slots 6–9 and 11–13, and the rest of §22.1. `BuildingDef` and `RecipeDef` are the tables that industrial recipes and facilities append rows to (design §13.7); do not hard-code the six collection kinds anywhere outside table rows.

## 23. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded/keyed `Rng` with new labels unique per call site, no wall clock, no unordered enumeration; bots and people use the same commands); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `docs/stage4.zip` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done; never run the built player windowed.
- **Before any Unity open or test run:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors. Only then open the editor.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives an interruption; never bare `git stash` (shared stash stack), use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`.
- Report exactly which tests and runs happened (counts, hashes) and plainly what did not run or did not pass.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with the contracts or the code: stop and ask the lead for an amendment row; never work around a missing seam. Minor reversible choices: take a configurable default and record it in "UN0 record" under "Design decisions taken during implementation".
