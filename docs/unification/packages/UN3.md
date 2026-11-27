# UN3: Building architecture and the six collection shelters (capabilities, levels, collection, sleeping, rights, upgrades)

Package spec for the Unification milestone (owner D8; owner brief §40 field set). Self-contained. Binding sources, in order: [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md) D8.5–D8.8, D8.17; [SHARED_ARCHITECTURAL_CONTRACTS.md](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) §7, §14, §22.3; [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R20, R58, R72, R73, R74; [UNIFICATION_DESIGN.md](../UNIFICATION_DESIGN.md) §5, §11.2; [UNIFICATION_BASELINE.md](../UNIFICATION_BASELINE.md) §3.1, §7, R7, R11; [docs/civdiv/DEVELOPMENT_PACKAGES/C1.md](../../civdiv/DEVELOPMENT_PACKAGES/C1.md); [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md); this file. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

| Field | Value |
|---|---|
| Package identifier | **UN3** (Tier A, Wave 1, Checkpoint UA; the heart of the milestone) |
| Branch / worktree | `unity/un3` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un3`, from `unity/poc` after **UN0** merged |
| Effort | 6 agent-hours (UN3a 3.5 h; UN3b 2.5 h) |
| Status | Not started |
| Recommended agent | **Opus** (architecture on top of C1, rights semantics, cross-package seams). Never Fable |

## 1. Objective
One building architecture. A building is a C1 productive asset with a data row of capabilities (shelter, storage, collection, production, employment) and sequential levels. The six collection shelters are rows of that one table. Raw materials can be collected only through a finished, operational collection building the NPC may use, carried by the NPC and deposited in that building's finite store. Every collection building is also a sleeping shelter. Level 1 costs labour and time only; levels 2 and 3 cost materials and improve storage, beds, shelter quality, collection speed and worker slots, never the natural supply. Building ownership, collection right, sleeping right, storage access and ownership of collected goods stay five distinct things.

## 2. User-facing outcome
A person founds a woodcutter's shelter by working on it for a while, then walks to remembered wood, collects, carries it back and deposits it; sleeps there; later upgrades it with wood they collected; lets a neighbour sleep or collect, or sells them wood. The person card's Economy section shows the building, its level, store, beds and rights (data supplied through `CivCard`; drawing is UN10).

## 3. Relevant design documents
Design §5 (all), §11.2 (exchange reuses M5 and C3), §6.3 (where basic buildings may stand), §14.2 B2–B9, Unif-4…Unif-7, Unif-9; Part B of the owner brief (verbatim in `docs/unification/OWNER_BRIEF_2026-11-28_UNIFICATION.md`); audit §3.1 (parallel building representations), §7, R7, R11, R13; `docs/stage4/ECONOMY_DESIGN.md` §3.5 (property records, use rights); C1 spec §5 and §22.

## 4. Relevant existing source files (verified)
- C1 (the base): `Civ/Assets.cs` (`AssetKind`, `AssetDef`, `ProductiveAsset`, `UseRight`, `UseKind`, `Assets.Create/Transfer/GrantUse/EndUse/Destroy/Abandon`), `Civ/Work.cs` (`FoundSite`, `CanFound` with the HQ-circle check at line 68, `CarriesMaterial`, `Build`, `Operate`, `Haul`, `TakeOut`, `Speed`), `Civ/Ownership.cs` (`Permits`, theft classification), `Civ/Employment.cs` (C3), `Civ/CivState.C1.cs`, `Civ/CivMindHooks.cs` (build and work candidates), `Civ/CivTuning.C1.cs`, `Civ/CivEvents.C1.cs` (270–275), `Civ/CivSystem.cs`, `Civ/SkillStore.cs`.
- Huts and sleep: `Population/PopulationState.cs` (`Hut`: `Built`, `LogsIn`, `LogsNeeded`, `Beds`, `Rights`, `Contributors`, `Lodgers`, `RightSource`), `Population/Households/Households.cs` (`HutRights`, `StoreAccess`), `Population/PopulationSystem.cs` (sleep seam, hut founding at ~658–668, hand gathering ~966–1000), `Population/PopulationTuning.cs` (106–108 hut costs), `Population/LotBin.cs` (`BinKind.AssetStore`, `Owner`), `Population/Goods.cs`, `Population/Reach.cs`.
- Cognition: `Cognition/Thinker.cs`, `Cognition/Mind.cs`, `Cognition/Affordances.cs`, `Learning/Verbs.cs`, `Learning/EpisodeKey.cs`; social: `Social/Valuation.cs`, `Social/SocialSim.cs`.
- UN0 seams you fill: `Buildings/BuildingDef.cs`, `BuildingTable.cs` (`AddRowsUN3`), `DevelopmentArea.Allows`, `UseKind.Sleep`, `ObjClass` 136–149, `UnificationState.UN3.cs`, `UnificationTuning.UN3.cs`, `UnificationHooks.UN3.cs`, `UnificationMindHooks.UN3.cs`, `UnificationEvents.UN3.cs`, `ValuationPriors.UN3.cs`, `UN3Mappings.cs`; UN2's `RawNodes.Take`.
- Tests to model on: `Assets/God/Tests/EditMode/Civ/C1*.cs`, `DotNet/God.Runner.Tests/C1RunTests.cs`, `M5BarterTests.cs`, `Support/GoodsTrace.cs`.

## 5. Allowed scope
**UN3a (enough for UN4b, UN6, UN7 and UN10a to start):**
1. **Data.** Fill `BuildingTable` rows for kinds 10–15 (woodcutter's, stonecutter's, clay gatherer's, shepherd's, hay gatherer's, fisher's shelter) and `Levels[1..3]` with the design §5.2 defaults (L1 labour 400 ticks, storage 10, beds 2, quality Basic, workers 1, collection 1000; L2 10 `Log`, 800 ticks, 30, 4, Good, 2, 1500; L3 20 `Log` + 10 `Stone`, 1500 ticks, 60, 6, Excellent, 3, 2000, one production slot), all in `UnificationTuning.UN3` (nothing literal in code). `CollectsFrom`, `CollectWare`, `CollectRadiusNodes` 18, `PlacementRadius` 12, `CollectTicksPerUnit` 60, `ReserveTicks` 600. The existing C1 rows Field, Workshop, Store (kinds 1–3) become rows of the same table unchanged. **No row for Desert.**
2. **Extending `ProductiveAsset`** (flagged, additive, lead-reviewed edits to `Civ/Assets.cs`): `Level`, `Workers` count, `Reservations` (≤ 8: collector, units, expiry tick), `Contributions` (≤ 8: `Npc`, `WorkTicks`, materials), `Enclosure` (`Sheep`, `Capacity`, `LastShearDay`), `UpgradeTarget`, `UpgradeWork`, `MaterialsIn[]`. Hashed only when non-default.
3. **Founding and level-1 construction** through the C1 build act (`Work.FoundSite`/`Work.Build`), unchanged in mechanism: empty cost list means `CarriesMaterial` is trivially true; labour adds `BuildWork` at `Work.Speed`; several NPCs may work one site jointly (a `Work` right is auto-granted to contributors); the building finishes at `BuildWork ≥ BuildTicks`; `ConsumeReason.Built` is **not** called for level 1. Knowledge gate: the row's `Knowledge` bit in `NpcMind.Unif.Known` (UN4a seeds it; fixtures set it). Placement: `DevelopmentArea.Allows(s, node, kind)` plus `NoResourceInReach` when no raw node of the right hex type lies within `PlacementRadius`; level 1 may stand outside any settlement area, upgrades need an area (design §6.3). Condition starts at 1000.
4. **Collection** (verb 128 `Collect`, `NpcVerb.Collect` 220): the act and every check of design §5.4 with refusal reasons `NoBuilding`, `NoCollectRight`, `OutOfZone`, `StorageFull`, `NoWorkerSlot`, `BuildingRuined`; reserve → walk to the raw node → take units through UN2's `RawNodes.Take` at `CollectTicksPerUnit × 1000 / CollectPermille × 1000 / Work.Speed` per unit → carry lot with `ProduceReason.Collected` owned by the collector (R74) → walk back → deposit into the `AssetStore` bin through `Goods.Move`. The environment never writes to a bin. A storage-full building refuses collection; the NPC may deposit elsewhere only into a bin it holds a Store right on, never the ground.
5. **Sheep**: collection by a shepherd's shelter moves heads from the raw node into `Enclosure`; shearing and slaughter acts are UN7 (recipe rows R5, R5b); UN3 provides the enclosure register and `Sheep` ledger as a living stock.
6. **Sleeping**: `ShelterIndex` (derived, `[JsonIgnore]`, rebuilt in `FromState`) answering "beds I may use and their quality" from assets in unified mode and from `Huts` otherwise; `PopulationSystem`'s sleep seam routed through it; beds counted (child = half a bed, existing rule); quality recovery table `ShelterRecoveryPermille` (Basic 1000, Good 1250, Excellent 1500); `Exposure` only outdoors. `UseRight` kind `Sleep`.
7. **Candidates** (UN3's share of `Unification` quota 4): `Collect` (target building + raw node), `Deposit` (to a building store), `FoundShelter` (single-step, so the milestone works before UN6 exists; UN6 supersedes it with goals), `ContinueSite`. `SelfFeature` 88–90 fed. `ObjClass` from rows (136–149); `SenseExtra` reports buildings with level, store fill (quantised) and bed use.
8. **Events** (design §13.6): 1220–1225 (founded, worked HV, completed, collection started/deposited HV/refused) and 1230 `BuildingSlept` (HV); C1's 270/272/273 reused.

**UN3b:**
9. **Upgrades** (design §5.8): `Upgrade` verb 137 on an owned or permitted building, materials hauled in (`Goods.Consume` with `ConsumeReason.Built`), `UpgradeWork` to the next level's `BuildTicks`, `UpgradeDisruptionPermille` 500 reduction of workers and beds while operating (min 1), atomic completion, stall rule `UpgradeStallDays` 5 lifts the penalty and keeps progress; levels sequential. Events 1226–1229. Upgrades never touch `RawNode`, `HexSupply` or dice.
10. **Rights and ownership** (design §5.6): five distinct things implemented and mutation-tested; `UseKind.Sleep` grants (owner household, labour contributors in order while beds are free, invited); Store right with separate deposit and withdraw bits in `Terms`; collection `Terms` `SharePermille` (collector/owner split, default collector 1000); unauthorised sleeping classified analytics-only (`UseTheft`); abandonment, sale, gift, inheritance and destruction via C1 (`Assets.Abandon/Transfer/Destroy`, `Estates`); repair verb 138 with wood (`Condition` decay `ConditionLossPerDay`; below `ConditionNoUse` no quality bonus, below `ConditionRuin` unusable; events 1231, 1232).
11. **Retiring hand-gathered logs for D8 games**: with `Buildings.Enabled`, hand gathering of materials and the M3/M4 log-cost hut path are disabled; `Hut` perception stays for legacy; the plain **`Hut` row (kind 20) exists in the table but is disabled by default (`Buildings.HutRow = 0`, R73)**: people sleep in collection shelters and the communal hall; the row exists so the owner can enable it (owner question). Legacy `Hut` objects remain for `Unification.Enabled = 0`.
12. **Employment and exchange hooks**: C3 employment works unchanged at collection buildings (a hired collector's lots follow the contract `Terms`); the valuation priors for raw wares; goods in a bin can be offered by M5 (`Offer/HandOver`) with no change to M5; a `UseRight` grant can be offered as a "good" (an access grant with `Terms`) through the existing offer topic (design §11.2).
13. **`CivCard` fill**: buildings owned, levels, store fill, beds, rights held, believed owners (layer-tagged).

## 6. Explicitly excluded scope
Goal planning and multi-step plans (UN6; UN3 candidates are single-step); recipes, workshops beyond the production-slot data and the row stubs (UN7); settlement founding, roads (UN5); groups (UN8; a group *owner* is accepted as an `Owner` but UN3 does not create groups); the communal hall row and the founding group (UN4b); the unified start (UN4); removing Stage 3 systems (UN9); views (UN10); warehouse tables (UN11); wages beyond C3's existing behaviour; theft responses (C9).

## 7. Dependencies
Hard: **UN0**. Soft: UN2 (`RawNodes.Take`: develop against `UnifiedFixtures` raw nodes, merge after UN2), UN1 (hex types). Downstream: UN4b, UN6, UN7, UN8, UN10a, UN11.

## 8. Shared contracts
**Read:** `Goods` (frozen API), `Bodies`, `Reach`, the sense seam, `Control`, `Permits`, F1/UN0 id ranges, `RawNodes.Take` (UN2), `NpcMind.Unif.Known`. **Modify (own partials and flagged additive C1 edits only):** `UnificationState` UN3 partial, UN3 tuning partial and catalogue array, `Buildings/*`, `Civ/Assets.cs` (additive fields and `AssetDef` row loading), `Civ/Work.cs` (the unified `CanFound` branch calls `DevelopmentArea`; additive), `Civ/Ownership.cs` (Sleep and Store terms; additive), `Civ/CivMindHooks.cs` (call into `UnificationMindHooks`; additive). **Never** edit `Goods.cs`, `LotBin.cs`, enum files, `GodSim`, `SaveFile`, `Wares.cs`.

## 9. Files and modules owned
`Unification/Buildings/BuildingRows.UN3.cs`, `Collection.cs`, `Construction.cs`, `Upgrades.cs`, `ShelterIndex.cs`, `BuildingRights.cs`, `Enclosure.cs`, `Unification/UnificationState.UN3.cs`, `UnificationTuning.UN3.cs`, `UnificationHooks.UN3.cs`, `UnificationMindHooks.UN3.cs`, `UnificationEvents.UN3.cs`, `ValuationPriors.UN3.cs`, `Information/PeopleCards.Unification.cs` (the `CivCard` fill), `DotNet/God.Runner/Warehouse/UN3Mappings.cs`, `DotNet/God.Runner/Scenarios/unif_buildings.scenario.json`, `Assets/God/Tests/EditMode/Unification/UN3*.cs` (commit `.meta`), `DotNet/God.Runner.Tests/UN3*.cs`.

## 10. Required domain events
```
1220 BuildingFounded      { Asset, Kind, Node, Owner, Founder }
1221 BuildingWorked (HV)  { Asset, Worker, WorkTicks, Control }
1222 BuildingCompleted    { Asset, Kind, Level, Owner }               // C1 270 AssetBuilt also emitted
1223 CollectionStarted    { Asset, Collector, RawNode, Ware, Units }
1224 CollectionDeposited (HV) { Asset, Collector, Ware, Units, Owner, Control }
1225 CollectionRefused    { Asset, Collector, Reason }                // NoBuilding, NoCollectRight, StorageFull, OutOfZone, NoWorkerSlot, BuildingRuined
1226 UpgradeStarted / 1227 UpgradeMaterialAdded / 1228 UpgradeCompleted / 1229 UpgradeStalled   { Asset, Level, ... }
1230 BuildingSlept (HV)   { Asset, Npc, Beds, Quality }
1231 BuildingRuined / 1232 BuildingAbandoned   { Asset, Reason }
```
Codes from design §13.6 (1220–1249); C1's 270, 272 (`UseRightGranted`), 273 are reused. Every action event under control carries `Control`.

## 11. Save/load implications
Saved: the extended `ProductiveAsset` fields (level, reservations, contributions, enclosure, upgrade state), `UseRight` Sleep rows, the `Terms` fields. `ShelterIndex` is derived. Test `UN3_SaveLoadParity` saves at: an open build (half the work done by two contributors), an open collection (a carry lot mid-walk with an active reservation), an open upgrade (materials in, work half done), a sleeper in a building; load → continue equals the straight run at every later checkpoint.

## 12. Warehouse implications
Mapper `UN3Mappings.cs` for 1220–1232; `UN3_EventsRoundTrip`. UN11 defines `buildings`, `building_events` and the collection chain table and reconciles `Σ collected` against UN2's `Taken`.

## 13. Acceptance tests
| Id | Test (behavioural) | Pass |
|---|---|---|
| UN3-T1 | `Unification_Off_Reproduces_Base` (U-P1) and `Buildings_Off_C1Unchanged` | every existing C1 test (`C1-T1`…`T12`) and hash unchanged with `Buildings.Enabled = 0`; `civ_assets` scenario behaves as before |
| UN3-T2 | `Collect_RefusedWithout` (B2) | refused with `NoBuilding` (none), `NoCollectRight` (another person), `BuildingRuined`, `OutOfZone`, `NoWorkerSlot`; a hand pick-up of a raw node is refused (UN2) |
| UN3-T3 | `Level1_NoMaterials` (B3) | an NPC with no goods completes a level-1 shelter; zero `ConsumeReason.Built` units; materials ledger unchanged |
| UN3-T4 | `Level1_LabourAndTime` (B4) | `BuildWork == BuildTicks` at completion; elapsed ticks ≥ `BuildTicks / workers`; three joint workers finish in about a third of the time |
| UN3-T5 | `Collect_CarryDeposit` (B5) | event chain `CollectionStarted → CollectionDeposited` spans at least the walking time to the node and back; lots pass carry then bin; lot owner = collector; `RawNodes.Take` matches |
| UN3-T6 | `Collect_StorageCap` (B6) | at bin free capacity 0 collection is refused (`StorageFull`); with a reservation a second collector is refused until released or expired; the bin never exceeds capacity (Unif-5) |
| UN3-T7 | `Shelter_Sleeping` (B7) | an NPC with a Sleep right sleeps in the building; sleepers ≤ beds (Unif-6); children use half beds; quality changes recovery by the table; an NPC without a right sleeps outdoors or, if a bed is free, as classified `UseTheft` |
| UN3-T8 | `Upgrade_Costs` (B8) | `ConsumeReason.Built` units equal the upgrade cost; `UpgradeWork` equals the next `BuildTicks`; levels cannot be skipped; disruption penalty applies and lifts on a stall |
| UN3-T9 | `Upgrade_Benefit`, `Upgrade_DoesNotChangeSupply` (B9) | at level 1 versus 2, the same NPC and node setup collects in the ratio of `CollectPermille` (within per-unit rounding); storage, beds and quality change as tabled; `HexSupply`/`RawNode` totals unaffected |
| UN3-T10 | `Rights_FiveDistinct` (Unif-7) | a mutation test changes owner, collection right, sleep right, storage access and lot owner one at a time and checks the others unchanged; a collector keeps ownership of lots deposited in another's bin; only the owner (or a withdraw grant) may withdraw others' lots |
| UN3-T11 | `Economy_ThreeIndividuals_Fixture` (B10 part) | a fixture with a wood collector A, a stone collector B and a forager C: A and B barter wood for stone by M5 hand-over, A upgrades with wood, C trades food for wood; every transfer is a physical hand-over; no faction object exists |
| UN3-T12 | `Hut_Row_DisabledByDefault` (R73) | with `Buildings.HutRow = 0` no hut can be founded and no code path produces a unified `Hut`; with 1 the row works as design §5.5 |
| UN3-T13 | `Building_NoOwnerKindBranch` | static test: `Unification/Buildings` and `Civ/Work.cs` contain no branch on `OwnerKind` |
| UN3-T14 | `UN3_SaveLoadParity`, `UN3_EventsRoundTrip`, `UN3_GroupOwnerAccepted` | as sections 11 and 12; an asset whose owner is `Owner.Group(id)` (fixture group) works, shares `Terms` honoured |

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN3|FullyQualifiedName~C1|FullyQualifiedName~Neutrality|FullyQualifiedName~M5Barter"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_buildings --seed 3 --days 12 --record full --sqlite --out <scratch>\un3
dotnet run --project DotNet/God.Runner -c Release -- verify --scenario unif_buildings --seed 3 --ticks 14000 --save-mid --record-matrix
```
Report from the log: per building the level history, units collected and deposited per day, bed use, upgrades with their materials, refusal counts by reason, and the share of NPC work ticks on construction, collection and foraging.

## 15. Unity demonstration
None in this package (UN10a draws shelters). **Unity compile check required** (`CivCard`, `ObjClass`, asset records are read by `God.Game`): `check-game-compile.ps1` 0 errors, then batch EditMode green if you hold the Unity token.

## 16. Performance considerations
Collection candidates are bounded by the quota (4) and by perceived raw nodes (≤ `PerceiveMax` 16); reservations ≤ 8 per asset; `ShelterIndex` rebuilt only on asset or right change (event-driven), O(assets). Report `bench_npc_500` with `Buildings.Enabled = 1` and the unified fixture population, and bytes per NPC unchanged.

## 17. Integration procedure
1. The lead creates the worktree: `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un3 -b unity/un3 unity/poc` (after UN0 merged), then `robocopy C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un3\Library /E /MT:16` (0–7 = success; source editor closed).
2. Sim and EditMode tests first. **Hand back UN3a** (items 1–8, tests T1–T7, T13) as soon as green so the lead merges it and launches UN4b, UN6, UN7 and UN10a; then continue to UN3b on the same worktree (new branch `unity/un3b` cut from `unity/poc` after UN3a merged).
3. Before hand-back `git merge unity/poc`; conflicts only in your own files; the flagged additive C1 edits are listed in the hand-back for the lead's diff review.
4. Neutrality pins that must hold: **P1, P4, P8**, P8 `Civ_Off_Reproduces_Base`, `C1-T1` (`Civ_Idle_Neutral`), U-P1, U-P4 form, the three Stage 3 smoke hashes of `PolicyRunTests.cs:27`.
5. Full `dotnet test DotNet -c Release` green, including all C1/C3/M5 tests.
6. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` 0 errors before any Unity run; Unity only if you hold the token, otherwise the lead runs it at the checkpoint.
7. "UN3 record" in `docs/stage4/DEV_LOG.md` on the branch. Hand back; never push; never merge into `unity/poc`.

## 18. Risks
- **Scope**: this is the largest package; keep UN3a minimal and merge it early.
- **C1 regressions**: the additive `ProductiveAsset` fields and the `CanFound` branch must be inert with `Buildings.Enabled = 0` (T1 is a gate, not a nicety).
- **Wood bootstrap**: with hand gathering retired, level 2 and 3 require wood from a woodcutter's shelter: the first upgrade is the first test of the whole economy; do not paper over a stall by adding a manual route (D8.5).
- **Hut ambiguity** (design §5.5/§10.7 versus R73): follow R73 (row disabled); the hall (UN4b) has 8 beds for 12 people (7 adults + 5 children at half a bed = 9.5), a deliberate pressure to build, to be confirmed by UN11 V1.
- **Rights model**: five independent things is easy to collapse by accident; T10 is the guard.
- Collection productivity formula rounding: keep integer permille and report per-unit rounding.

## 19. Definition of done
UN3-T1…T14 green under `dotnet test`; C1 tests and pins unchanged; compile check 0 errors; EditMode green; "UN3 record" lists the flagged C1 edits, what ran, and the defaults taken (including the Hut decision); UN3a handed back before UN3b; handed back.

## 20. Smaller checkpoints
- **UN3a** (≈3.5 h): data rows, asset extension, level-1 construction, collection and deposit, sheep register, sleeping and `ShelterIndex`, single-step candidates, events, T1–T7, T13.
- **UN3b** (≈2.5 h): upgrades, rights and ownership separation, condition and repair, retiring hand gathering, employment and exchange hooks, `CivCard`, T8–T12, T14.

## 21. Recommended agent
**Opus.**

## 22. Used later by industry (keep the interfaces general)
Later buildings (hospital, refinery, barracks, factory, power station) are rows combining the same capability flags; never allocate ids outside `Assets.Create`; keep the collection act generic over `CollectsFrom` so a deposit (I1) can reuse the zone and storage checks.

## 23. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded/keyed `Rng` with new labels unique per call site, no wall clock, no unordered enumeration; bots and people use the same commands); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `docs/stage4.zip` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done; never run the built player windowed.
- **Before any Unity open or test run:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives an interruption; never bare `git stash`, use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`.
- Report exactly which tests and runs happened (counts, hashes) and plainly what did not run or did not pass.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with the contracts or the code: stop and ask the lead for an amendment row; never edit a frozen file or work around a missing seam. Minor reversible choices: take a configurable default and record it in "UN3 record".
