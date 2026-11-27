# I1a: Industrial and knowledge foundation (behaviour-neutral shared surface)

Package spec for the Civilisation and Divinity run, industrial workstream (owner brief §40; owner decisions D6.1–D6.19). Self-contained. Binding sources, in order: [OWNER_DECISION_REGISTER.md](../OWNER_DECISION_REGISTER.md) (set D6), [SHARED_ARCHITECTURAL_CONTRACTS.md](../SHARED_ARCHITECTURAL_CONTRACTS.md) ("the contracts", especially **§22 Industry addendum**), [LEAD_RULINGS.md](../LEAD_RULINGS.md) (R1–R37), [DEVELOPMENT_PACKAGE_INDEX.md](../DEVELOPMENT_PACKAGE_INDEX.md) §2 "I1a", [DEVELOPMENT_DEPENDENCY_GRAPH.md](../DEVELOPMENT_DEPENDENCY_GRAPH.md), this file. Where a design document differs from contracts §22, §22 wins. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/`, `AgentScripts/` or `docs/`.

| Field | Value |
|---|---|
| Package identifier | **I1a** (Tier 2, first industrial merge, wave 4i; prerequisite of Checkpoint G "Industrial slice") |
| Branch / worktree | `unity/i1a` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-i1a`, cut from `unity/poc` after **F1b and C1** have merged; **merged only after the Tier 1 set has merged** (contracts §22.0) |
| Effort | 1.5 agent-hours (plus the wait for the Tier 1 set and one rebase) |
| Status | Not started |

## 1. Objective
Land, in **one behaviour-neutral, lead-reviewed commit series**, every shared surface the industrial and knowledge packages need (contracts §22.1 items I1a.1–I1a.16): the ware block 40–71, the widened goods ledger, the goods-API amendments (`BinTransfer`, `SupplyAgreement`, `Paid`, `Goods.MoveBin`, `Wares.IsUnique`), the `GodState.Industry` slot and `industry` hash part, `CivState.K1/K2`, the switches, module entry points, dispatch after Divinity, the three movement sites, the id ranges, `MaxCand = 40`, the lazy mind slots, `DesignId` on C1 assets, the system shells 262 and 264, telemetry domains 70–79, catalogue and mapping stubs, the save null-acceptance and the pins **P10–P13**. Separate worktrees do not make shared-contract changes safe (contracts §22.0): after I1a, I1–I6, K1 and K2 touch only their own files.

## 2. User-facing outcome
None visible, by design. Every existing game (stage 3, population, god-only Living Island) plays and hashes exactly as before; switching `Industry.Enabled`, `Civ.Knowledge.Enabled` or `Civ.Money.Enabled` on creates only empty state. What it buys the owner: the industrial packages can run in parallel (I2 ∥ I4, K1 ∥ K2a) without breaking each other or moving hashes at merge.

## 3. Relevant design documents
- Contracts **§22.0** (why, cut and merge rules), **§22.1** (the sixteen items, files, owner after), **§22.2** IA-1…IA-9, **§22.3** (wares, ledger reasons, moves, owners, `Goods.MoveBin`), **§22.4** (state order, identity table, switches, mind slots, skill store rule, tick order, dispatch and `Kill` order, movement sites), **§22.5** (id ranges), **§22.6** (domains, ranges, code blocks), **§22.7** (migration numbers: I1a writes none), **§22.8** (P10–P13, save bump), **§22.10** (static tests in guarding form), **§22.11** (decisions), **§22.12** (who lands what). Also §0 (the F1 pattern this copies), §14, §15.1, §16, §17.
- `docs/stage4/INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md` §3.1 (ware table, weights), §3.2 (ledger reasons), §9.1–§9.2 (I1a proposal; superseded in detail by contracts §22), §12.
- `docs/stage4/VEHICLES_AND_HARVESTING_DESIGN.md` §3.4 (goods-API usage), §4.3 (movement sites), §9 (migration).
- `docs/stage4/ELECTRICITY_DESIGN.md` §4.2 (system 262), §9.
- `docs/stage4/KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md` §14 K1-R1…R12 (renumbered by contracts §22.11 #27).
- `docs/stage4/MONEY_AND_ACCOUNTING_DESIGN.md` §12 K2-R1…R9.
- `docs/stage4/WORLD_ONTOLOGY.md` §11.1 (the nine categories the slots carry).
- `docs/stage4/DATA_WAREHOUSE_DESIGN.md` §13.2 (domain names), §13.4 (migration plan; none is I1a's).
- LEAD_RULINGS **R15** (provisional numbers settled by §22), **R20** (`AssetId` is C1's), **R32**, **R33**, **R34**.

## 4. Relevant existing source files (verified on `unity/poc`, 27 Nov)
Values marked "today" are the base before F1; F1 changes several of them first. **Assert the post-F1 values at branch start** (contracts state `PersonalCount` 40 and `MaxCand` 36 as the starting point); if they differ, stop and ask the lead.
- `Economy/Wares.cs` — `enum Ware`; `Wares.PersonalCount = 32` today (F1 → 40; I1a → **72**); `Wares.Count = 28` (unchanged forever); weight column.
- `Population/PopulationModule.cs` — `HashPopulation(GodState)` (line ~359), `Weight`.
- `Population/PopulationState.cs` — `ProduceReason` 0–3 and `ConsumeReason` 0–4 today (F1 adds 4–5 and 5–7), `GoodsLedger`, `ItemLot`.
- `Population/Goods.cs` — `MoveReason` 1–9 today (F1/C1 add up to 12), `OwnershipBasis`, `Mergeable` (line ~473: `w != Ware.Axe`) and the ground merge.
- `Population/Reach.cs`, `Population/LotBin.cs` (`BinKind`, `OwnerKind`; F1 adds `AssetStore = 4`, `Org`).
- `Population/PopulationExtensions.cs`, `Population/PopulationSystem.cs` (the step and act loop), `Population/NodePather.cs` (**unchanged**; receives a cost profile).
- `Cognition/Thinker.cs` — `ThinkScratch` constants (`MaxCand = 24` today; F1 → 36; I1a → **40**), `Cognition/Extensions.cs` (hook dispatch), `Cognition/Mind.cs` (`NpcMind`).
- `Learning/Verbs.cs` (`static class Verb`), `Learning/EpisodeKey.cs`, `Learning/Features.cs` (`ObjClass`, `SituationKind`, `SelfFeature`, `NpcGroupValue` and the role values), `Comms/Message.cs` (`TopicKind` 0–6 today), `Policy/PolicyTypes.cs` (`NpcVerb`).
- `Telemetry/DomainEvent.cs` — `TelemetryDomain`, `EventCatalog` (line ~65), `RangeOf` (line ~174).
- `Core/GodSim.cs` (`AllHashPartNames` line ~35, `PartNamesOf`, `PartsOf`, system registration), `Core/GodState.cs`, `Core/SimTuning.cs` (`IOmitWhenDefault`), `Core/SimInvariants.cs`, `Save/SaveFile.cs` (**lead-owned**).
- F1/C1 files that must exist before the cut: `Divinity/DivineTypes.cs` (`DivineAct`), `Civ/CivState.cs`, `Civ/CivHooks.cs`, `Civ/CivMindHooks.cs`, `Civ/CivSystem.cs`, `Civ/Assets.cs`, `Civ/CivEvents.cs`, `Civ/CivTuning.cs` (none of these exists on `unity/poc` today; they are F1's and C1's).
- Warehouse: `DotNet/God.Runner/Warehouse/EventMapping.cs` (`EventMapRegistry.CreateDefault`, line ~53), `Tables.cs`, `Migrations/0001`–`0006` today.
- Scenario loader: `Assets/God/Recording/Scenario.cs` (`Scenario.FromJson`, `Tuning` as `long` paths, `ScheduledCommand`); see the contract gap in §5 item 17.
- Tests to model on: `DotNet/God.Runner.Tests/PolicyRunTests.cs` (stage 3 hash), `M5NeutralityTests.cs`, `F0GoodsConservationTests.cs`, `DeterminismTests.cs`, `ReplayTests.cs`; `Assets/God/Tests/EditMode/GoodsTests.cs`.
- Compile check: `AgentScripts/check-game-compile.ps1` building `DotNet/God.GameCompile` (`Ware`, `GodState`, `NpcMind` are used by `God.Game`).

## 5. Allowed scope (contracts §22.1; every item behaviour-neutral)
1. **I1a.1 Wares**: `Ware` 40–71 exactly as §22.3 (40 `IronOre` … 53 `Firearm`, 54–55 spare, 56–62 waste, 63 `Document`, 64 `Paper`, 65 `Coin`, 66–71 spare); `PersonalCount = 72`; helpers `IsIndustrial` (40–55), `IsWaste` (56–62), `IsLiquid` {43, 45, 46}, `IsFuel` {42, 45, 46, 52, 56}, `IsUnique` {`Axe`, `Document`}; weights; `WareRefValue` rows (analytics only); `HashPopulation` extended by the F1.17 rule (wares 40–71 hashed only when any ledger entry or holding is non-zero; the hasher iterates the base order explicitly).
2. **I1a.2 Ledger**: `GoodsLedger.ProduceReasons = ConsumeReasons = 16`; `ProduceReason` 6 `Extracted`, 7 `Processed`, 8 `Discarded`, 9 `Salvaged`; `ConsumeReason` 8 `ProcessInput`, 9 `Combusted`, 10 `Embodied`, 11 `Disposed`, 12 `Expended`, 13 `Research`; slots 8–15 hashed only when non-zero.
3. **I1a.3 Goods API**: `MoveReason.BinTransfer = 13` (actor within `Reach.Store` of **both** bins); `OwnershipBasis.SupplyAgreement = 25`, `Paid = 26`; `Goods.MoveBin(s, ev, bin, toNode, actor)` (relocates one `LotBin` to a lattice neighbour; lots keep holder and owner; emits `BinMoved` 1014; `RemoveBin` still throws on non-empty); `Wares.IsUnique(w)` replaces `w != Ware.Axe` in `Mergeable` and the ground merge.
4. **I1a.4 State slot**: `Core/GodState.Industry.cs`, `Industry/IndustryState.cs` with empty partials `IndustryState.I1.cs` … `I5.cs`, `IndustryState.Waste.cs`; sub-objects `Geology`, `Production`, `Vehicles`, `Trade`, `Grid`, `Military`, `Waste`, each null until its package fills it; hashed in that order.
5. **I1a.5 Civ sub-objects**: `CivState.K1`, `CivState.K2` partial stubs (null), hashed after C5 in the order C1, C2, C3, C5, K1, K2 (flagged additive edit to C1's `CivState.cs`).
6. **I1a.6 Hash part** `"industry"` appended after `"divine"` in `AllHashPartNames`, present only when `Population != null && GodState.Industry != null`; `PartNamesOf`/`PartsOf` extended.
7. **I1a.7 Tuning**: `SimTuning.Industry` (`IOmitWhenDefault`, `Enabled = 0`) with nested `Geology` (`Generate = 1`), `Production`, `Waste` (`Enabled = 0`), `Vehicles`, `Trade`, `Grid`, `Military` (empty classes in `Industry/IndustryTuning.cs`); `SimTuning.Civ.Knowledge` (`Enabled = 0`, empty `Designs`) in `Civ/CivTuning.K1.cs`; `SimTuning.Civ.Money` (`Enabled = 0`, `RecordPersonSales = 0`, `SliceCommodity = Bronze`) in `Civ/CivTuning.K2.cs`.
8. **I1a.8 Module entry points**: `Industry/IndustryModule.cs` with `StartGame/Install/NewMind/OnDeath/Check`, called after `DivinityModule` at every F1.5 site; `StartGame` creates `GodState.Industry` only when `Industry.Enabled = 1` and the game has a population; all other bodies no-ops.
9. **I1a.9 Dispatch**: `IndustryHooks`/`IndustryMindHooks` called **after Divinity** at every site (F1.6 sites and `SenseExtra` included); `Kill` order … → `DivinityHooks.OnDeath` → `IndustryHooks.OnDeath` → M3 cleanup → `ArchiveMind` → `Mind = null`; the movement sites `PopulationExtensions.StepTicks(s, npc, from, to, base)` (default `base`), `PathCosts(s, npc, ref profile)` (default unchanged), `Moved(s, ev, npc, from, to)` (default no-op) called from `PopulationSystem`; the K1/K2 partial-method slots in C1's `CivHooks`, `CivMindHooks`, `CivSystem` (flagged additive edits). Partial order inside Industry dispatch: I1 → I2 → I3 → I4 → I5.
10. **I1a.10 Id ranges** of §22.5 (learning verbs 21–31, 63, 102, 103, 127; `NpcVerb` 200–209 and 210–219; `ObjClass` 110–122; `TopicKind` 11–14; `SituationKind` 128–132 and reservations; `SelfFeature` 72–74; roles 11–13; `NpcGroup` 11; `DivineAct` 22 Board, 23 Alight; episode-key kinds 40–43 and 19–22); `ThinkScratch.MaxCand` 36 → **40** (Industry quota 4; K1/K2 use the Civ quota); `NpcMind.Industry` (`Industry/IndustryMind.cs`) and `NpcMind.Know` (`Civ/KnowledgeMind.cs`), **created lazily on first write**, hashed only when present, after the F1 mind slots, order `Industry`, `Know`.
11. **I1a.11 `DesignId`** (`ushort`, 0 = common) on C1's asset record (flagged additive edit to `Civ/Assets.cs`), exposed to the sense seam as an inspectable attribute (K1-R10).
12. **I1a.12 System shells**: `Industry/Grid/ElectricitySystem.cs` (order 262) and `Industry/IndustrySystem.cs` (order 264), registered in `GodSim`, each returning at once while `GodState.Industry == null`.
13. **I1a.13 Events**: `TelemetryDomain` 70 `Industry`, 71 `Vehicle`, 72 `Energy`, 73 `Waste`, 74 `Environment`, 75 `IndustrialMilitary` (→ 1000–1099), 76 `Knowledge`, 77 `Rights`, 78 `Auction`, 79 `Money` (→ 1100–1199); `Industry/IndustryEvents.cs` with partial arrays `IndustryEvents.I1.cs` … `I6.cs` (empty except **1014 `BinMoved`**); `CivEvents.K1`, `CivEvents.K2` partial arrays (empty); `EventCatalog.All += IndustryEvents.Catalog`.
14. **I1a.14 Warehouse stubs**: `EventMapRegistry.CreateDefault` calls `IndustryMappings.Register(r)` (→ empty `I1Mappings` … `I6Mappings`); `CivMappings` also calls empty `K1Mappings`, `K2Mappings`; `Tables.Industry.cs` stub. **No migration.**
15. **I1a.15 Save**: `SaveFile.FromJson` accepts null `Industry`, null `CivState.K1/K2`, null `NpcMind.Industry/Know` — written **on the lead's instruction**; the lead owns `SaveFile.cs` and the version.
16. **I1a.16 Tests and helpers**: pins P10–P13; `Wares_LayoutNeutral`; `Ledger_WideningNeutral`; the §22.10 static tests in guarding form (vacuous until code exists, then guarding); `IndustrialProbe` (collect industry events) in `Assets/God/Tests/EditMode/Support/`.
17. **Contract gap, needs a §20 amendment row before this item is touched — scenario setup seam.** Contracts §22.3/§22.4 assume a scenario `deposits` block (which suppresses generation), and GAMEPLAY_SCENARIOS §13 assumes Iron Coast seeds facilities, vehicles, organisations, contracts, agreements, knowledge and coin holdings "through the ordinary setup paths". No such path exists: `Assets/God/Recording/Scenario.cs` carries only `long` tuning paths and scheduled commands, and contracts §17 allows only the two F1 `GameSetup` fields. **Proposed default (for the lead to accept or replace):** I1a adds an optional, omitted-when-empty `industry` JSON block to `Scenario.FromJson`/`ToJson` that is passed unchanged to `SimTuning.Industry.Setup` (an `IOmitWhenDefault` object of typed lists: `Deposits`, `Facilities`, `Vehicles`, `Links`, `Agreements`, `Knowledge`, `Money`), read only by each owning package's `StartGame` body. Without the row, I1a does not touch `Scenario.cs`, and the I packages are limited to fixtures built in tests.
18. **Contract gap, needs a §20 amendment row — query stubs that I packages call before K1/K2 exist.** IA-3 requires the industrial runners to call a knowledge-and-skill check and `Rights.Permits` (for the `Licensed` † flag), but §22.1 lands no stub for either, and I1, I2, I4 merge before K1 in the planned order. **Proposed default:** I1a adds `Civ/KnowledgeQuery.cs` with `KnownAtSite(s, node, designId, use) => true` and `Civ/RightsQuery.cs` with `Permits(s, party, designId, use, node) => true` (bodies owned by K1 after I1a, same signatures), both returning `true` when `DesignId == 0` or K1 is absent or off.

## 6. Explicitly excluded scope
Every behaviour (no deposit, recipe, facility, vehicle, station, agreement, design, payment or book logic); every migration (`0031`–`0043` belong to their packages); every row in another package's owned table (contracts §22.9: `JobKind.Operate`, `DivineActs.Map` rows 22/23, C6 purposes, offices, policies, C2 obligation sources, C7 teach topic, C10 patent law, D7 `ArchivedLife.Know`, KPI rows) — those are added later by the requesting package; content (recipe rows, deposit tables, `DesignDef` rows, `VehicleDef`, `GeneratorDesign`); the save version bump (lead, at the next checkpoint); any new death cause, faction or person creation (IA-9); any edit to `NodePather`, `Duel`, stage 3 `EconomySystem` or walkers.

## 7. Dependencies
- **Hard (to cut):** F1b and C1 merged into `unity/poc` (F1's slot, dispatch and catalogue pattern; C1's `AssetId` allocator, asset record and Civ dispatcher bodies, R20).
- **Hard (to merge):** the Tier 1 set merged (D6.18: F0, F1, F2 experiments, C1, D1, D2, D3, package D6 commandments, U1a, U2a, U3a, Q1 Tier 1, Q3 harness).
- **Downstream:** I1, and through it I2, I3, I4, I5, I6; K1; K2a. No other I or K package branches before I1a merges.
- Timeline (graph doc §5): branch may be cut at ~H11.3 (C1 merged); work H13.5–H15.0; merged at H15.0 if its gate passes by H15.5, else first merge of the continuation.

## 8. Shared contracts
- **M (frozen files, lead-reviewed diff, this package only):** `Economy/Wares.cs`, `Population/PopulationState.cs`, `Population/Goods.cs`, `Population/Reach.cs`, `Population/PopulationModule.cs`, `Population/PopulationExtensions.cs`, `Population/PopulationSystem.cs`, `Cognition/Thinker.cs`, `Cognition/Extensions.cs`, `Cognition/Mind.cs`, `Learning/Verbs.cs`, `Learning/EpisodeKey.cs`, `Learning/Features.cs`, `Comms/Message.cs`, `Policy/PolicyTypes.cs`, `Divinity/DivineTypes.cs`, `Telemetry/DomainEvent.cs`, `Core/GodSim.cs`, `Core/SimTuning.cs`, `Core/SimInvariants.cs`; flagged additive edits to C1's `Civ/CivState.cs`, `Civ/CivHooks.cs`, `Civ/CivMindHooks.cs`, `Civ/CivSystem.cs`, `Civ/Assets.cs`, `Civ/CivEvents.cs`; `Save/SaveFile.cs` only as instructed by the lead.
- **R:** everything else (F1 command families, control seam, Mythic, Orgs, Divinity bodies).
- After merge each stub has exactly one owner (§22.1 column 4); any later shared change needs a §20 amendment row from the lead.

## 9. Files and modules owned
New: `Core/GodState.Industry.cs`, `Industry/IndustryState.cs` (+ empty partials `IndustryState.I1.cs`…`I5.cs`, `IndustryState.Waste.cs`), `Industry/IndustryModule.cs`, `Industry/IndustryHooks.cs`, `Industry/IndustryMindHooks.cs`, `Industry/IndustryMind.cs`, `Industry/IndustryTuning.cs`, `Industry/IndustrySystem.cs`, `Industry/Grid/ElectricitySystem.cs`, `Industry/IndustryEvents.cs` (+ `IndustryEvents.I1.cs`…`I6.cs`), `Civ/CivState.K1.cs`, `Civ/CivState.K2.cs`, `Civ/CivTuning.K1.cs`, `Civ/CivTuning.K2.cs`, `Civ/CivEvents.K1.cs`, `Civ/CivEvents.K2.cs`, `Civ/KnowledgeMind.cs`, (with amendment rows) `Civ/KnowledgeQuery.cs`, `Civ/RightsQuery.cs`; `DotNet/God.Runner/Warehouse/IndustryMappings.cs`, `I1Mappings.cs`…`I6Mappings.cs`, `K1Mappings.cs`, `K2Mappings.cs`, `Tables.Industry.cs`; tests `DotNet/God.Runner.Tests/I1a*.cs`, `Assets/God/Tests/EditMode/Industry/I1a*.cs` (new folder: commit its `.meta`), `Assets/God/Tests/EditMode/Support/IndustrialProbe.cs`. Plus the frozen edits of §8. Everything becomes frozen or passes to the named owner at merge.

## 10. Required domain events
```
// Telemetry/DomainEvent.cs — domains (contracts §22.6)
Industry = 70, Vehicle = 71, Energy = 72, Waste = 73, Environment = 74, IndustrialMilitary = 75   // RangeOf → 1000–1099
Knowledge = 76, Rights = 77, Auction = 78, Money = 79                                              // RangeOf → 1100–1199
// Industry/IndustryEvents.I1.cs — the only event row I1a lands
1014 BinMoved { Bin, From, To, Actor }          // emitted only by Goods.MoveBin; no caller exists until I2
```
All other codes stay unallocated until their package appends them (I1 1000–1019 and 1080–1094, I2 1020–1039, I3 1040–1049, I4 1050–1069, I5 1070–1079, I6 1095–1099, K1 1100–1169, K2a 1170–1179 and 1190–1199, K2b 1180–1189). Existing ranges are unchanged.

## 11. Save/load implications
New optional slots: `GodState.Industry` (null when off), `CivState.K1/K2` (null), `NpcMind.Industry/Know` (null until first write), tuning roots (`IOmitWhenDefault`). The lead writes the `FromJson` null-acceptance and bumps `SaveFile.CurrentVersion` in the merge commit of the next checkpoint after I1a (contracts §22.8: **F → 10** if I1a merges before the freeze, otherwise **G → 11**). Tests: `I1a_Save_RoundTrip_AllSwitchesOff` (second save byte-identical to the first and to the pre-I1a save shape apart from omitted defaults), `I1a_Save_RoundTrip_EmptyIndustryOn` (each of the three switches on alone, empty state round-trips; continue = straight run).

## 12. Warehouse implications
No migration (contracts §22.1 I1a.14). The registry calls the empty mapping stubs; `Tables.Industry.cs` declares nothing yet. Test `I1a_Recording_Neutral`: Off, Summary and Full give one hash with all three switches on and empty state (P7 pattern on domains 70–79); `I1a_MappingStubs_Registered`: the registry builds, and an unknown code in 1000–1199 is skipped exactly as other unmapped codes are today.

## 13. Acceptance tests (behavioural or pinned; never "a class exists")
| Id | Test | Pass |
|---|---|---|
| I1a-P10 | `Industry_Off_Reproduces_Base` | every D5 switch, `Industry.Enabled`, `Civ.Knowledge.Enabled`, `Civ.Money.Enabled` = 0: every part hash of the P1 scenario set (`band_6x20`, `m4_families`, `ibs_families`, `watch6`, the `demog_*` set; seeds 1–3; every checkpoint) equals the pre-I1a base |
| I1a-P11 | `Industry_Idle_Is_Neutral` | `Industry.Enabled = 1`, no deposits, `Geology.Generate = 0`, `Waste.Enabled = 0`, no seeded assets: every part except `industry` equals P10 |
| I1a-P12 | `Knowledge_Idle_Is_Neutral` | `Civ.Enabled = 1`, `Civ.Knowledge.Enabled = 1`, empty `Designs`: every part except `civ` equals the same run with Knowledge off (mind hashes unchanged because `NpcMind.Know` is never created) |
| I1a-P13 | `Money_Idle_Is_Neutral` | `Civ.Enabled = 1`, `Civ.Money.Enabled = 1`, `RecordPersonSales = 0`, no seeded money: every part except `civ` equals the same run with Money off |
| I1a-T1 | `Wares_LayoutNeutral` | with no lot of 40–71, `population` part equals the base; a fixture that produces one `IronOre` lot changes the part and consuming it again restores conservation (C1 per ware) |
| I1a-T2 | `Ledger_WideningNeutral` | with reasons 8–15 all zero the ledger hash equals the base; producing with `Extracted` once makes slot 6 non-zero and hashed; C1 holds over all 16 reasons in both directions |
| I1a-T3 | `Goods_IsUnique_NoBehaviourChange` | the existing axe merge tests and M5 barter tests pass unchanged; two `Document` lots never merge in carry, bin or ground; other wares merge as before |
| I1a-T4 | `MoveBin_RelocatesWithoutTransfer` | fixture: a bin with three lots at node A, actor at A; `MoveBin` to a neighbour B: bin node = B, every lot's holder still `Bin(id)` and owner unchanged, `NextLotId` unchanged, one `BinMoved`; a non-neighbour target is refused; `RemoveBin` of a non-empty bin still throws |
| I1a-T5 | `BinTransfer_ReachBothBins` | a `Move(..., BinTransfer)` with the actor within `Reach.Store` of both bins succeeds; with either bin out of reach it fails `OutOfReach` and nothing moves |
| I1a-T6 | `MovementSites_DefaultNeutral` | with `GodState.Industry` null or empty: `StepTicks` returns `base` for 10 000 sampled steps, `PathCosts` leaves the profile unchanged, `Moved` emits nothing; every path in `band_6x20` seed 1 equals the base |
| I1a-T7 | `Dispatch_IndustryAfterDivinity` | a probe records hook call order at every site: M3 → M4 → M5 → Civ (C1, C2, C3, C5, K1, K2) → Orgs → Divinity → Industry; `Kill` calls `IndustryHooks.OnDeath` after `DivinityHooks.OnDeath` and before `ArchiveMind` |
| I1a-T8 | `IdRanges_Reserved_NoCollision` | every value of §22.5 is defined once; no learning verb, `NpcVerb`, `ObjClass`, `TopicKind`, `SituationKind`, `SelfFeature`, role, group, `DivineAct` or episode kind collides with a value already on `unity/poc` (reflection over the enum and constant classes) |
| I1a-T9 | `Systems_262_264_Shells` | both systems are registered at 262 and 264; with Industry null they return before touching state (no event, no allocation, hash equal) |
| I1a-T10 | `HashPart_Industry_OnlyWhenPresent` | `PartNamesOf` lists `industry` after `divine` exactly when population and Industry state both exist |
| I1a-T11 | `MindSlots_Lazy` | with all three switches on, a 2-day run creates no `NpcMind.Industry` or `NpcMind.Know`; a fixture write creates one for that NPC only |
| I1a-T12 | Static tests in guarding form (contracts §22.10) | `Industry_NoMoneyApi`, `Grid_NoMoneyApi`, `NoExternalBuyer`, `MoneyWriters_OnlyK2`, `Industry_NoActorZeroGoodsCalls`, `Industry_NoOwnershipModelBranch`, `Rights_NeverGate`, `Vehicle_NoNpcRecord`, `MoveBin_OnlyFromIndustryHooks` all exist, scan the right folders, and pass (vacuously now) |
| I1a-T13 | Stage 3 and existing pins | `PolicyRunTests` hash `4bd54b271857a803`; P1, P4, P8 and every pin already landed (P2, P3, P5, P9) unchanged; the "no handler, refused" and person-loop static tests pass |
| I1a-T14 | `I1a_Save_*`, `I1a_Recording_Neutral` | as §11, §12 |

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~I1a|FullyQualifiedName~Neutrality|FullyQualifiedName~PolicyRun"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario band_6x20 --seed 1 --days 5 --set tuning.Industry.Enabled=1 --set tuning.Civ.Enabled=1 --set tuning.Civ.Knowledge.Enabled=1 --set tuning.Civ.Money.Enabled=1 --out <scratch>\i1a-on
dotnet run --project DotNet/God.Runner -c Release -- run --scenario band_6x20 --seed 1 --days 5 --out <scratch>\i1a-off
dotnet run --project DotNet/God.Runner -c Release -- verify --scenario m4_families --seed 1 --ticks 12000 --save-mid --record-matrix --set tuning.Industry.Enabled=1
```
Compare the per-part checkpoint hashes of the two `run` outputs: equal except `industry` and `civ`. (Use the exact tuning path spelling that `SimTuning.TrySet` accepts; record it in the I1a record.)

## 15. Unity demonstration
No view work. **Unity is part of the I1a gate** (like F1, contracts §22.1 gate): compile check first, then batch-mode editor with a clean console and Unity EditMode green, because `Ware`, `GodState` and `NpcMind` are used by `God.Game`.

## 16. Performance considerations
`MaxCand` 36 → 40 grows `ThinkScratch` only; wares 72 grow per-ware arrays (ledger 16 × 72 × 2 longs; per-NPC carry counts if sized by `PersonalCount`). Report `bench --cases bench_npc_500` decisions/s and bytes per NPC before and after (both runs on the same quiet machine state, never during a Q1 benchmark slot); a regression above 3% is reported to the lead, not hidden. Hashing skips all-zero blocks, so hash cost is unchanged without industry.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-i1a -b unity/i1a <unity/poc SHA after F1b and C1 merged>`; `robocopy <warm worktree>\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-i1a\Library /E /MT:16` (a warm worktree is one whose editor is closed and whose Library was last compiled, normally `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs`; robocopy exit 0–7 = success).
2. Order: assert the post-F1 constants → wares and ledger (I1a.1–2) with T1–T3 → goods API (I1a.3) with T4–T5 → state, tuning, module, dispatch, sites (I1a.4–9) with T6–T7, T9–T11 → ranges (I1a.10) with T8 → events and stubs (I1a.13–14) → pins and static tests (I1a.16) → save null-acceptance (lead).
3. Hold the branch until the Tier 1 set has merged; then `git merge unity/poc`, resolve only in your own files (a conflict in a frozen file → stop and ask the lead), **re-take every pin on the new base**.
4. Neutrality pins that must hold: **P1, P4, P8**, P2, P3, P5, P9 when present, **P10–P13** (new), stage 3 hash `4bd54b271857a803`. Every pinned scenario runs with `Industry.Enabled = 0`, `Civ.Knowledge.Enabled = 0`, `Civ.Money.Enabled = 0` unless the pin says otherwise.
5. Full `dotnet test DotNet -c Release` green.
6. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` → 0 errors; then the batch-mode editor in `wt-lrs-i1a` (`unity status` ready, 0 `error CS`), EditMode `unity command --project-path . test run --mode editor --async-tests true` (poll `Temp/pipeline_test_status.json`), `unity close .`. Take the Unity token from the lead (graph doc §4: at most one package agent runs Unity at a time).
7. "I1a record" in DEV_LOG on the branch (pins re-taken, the bench figures, every frozen-file diff listed for the lead's review); hand back. **Never push; never merge into `unity/poc`.**

## 18. Risks
- **Waiting for the Tier 1 set.** Cut early, finish early, then rebase; the pins must be re-taken on the rebased base, never copied.
- **Hidden hash movement** from array layout changes (ledger widening, `PersonalCount`): the hasher must iterate the base order explicitly; P10 is the proof.
- **Enum collisions** with packages merged while the branch waits (package D6 verbs 116–118, Orgs 114–115, C7, C10): T8 is re-run after the rebase.
- **`MaxCand` change** must not alter candidate choice for non-industrial NPCs (quota bookkeeping only); P10 catches it.
- **Contract gaps (items 17, 18):** without the setup seam, Iron Coast cannot be expressed as a scenario file; without the query stubs, I1/I2/I4 cannot compile their knowledge and licence calls before K1. Ask for both rows on day one.
- **Scope creep:** any behaviour or table row in I1a is refused at review; the industrial packages own them.

## 19. Definition of done
I1a-P10…P13 and T1…T14 green under `dotnet test`; every earlier pin and the stage 3 hash unchanged; compile check 0 errors; Unity EditMode green; the lead has reviewed every frozen-file diff; amendment rows for items 17 and 18 decided (accepted, replaced, or recorded as refused with the consequence); "I1a record" written; handed back; the lead's save bump scheduled for the next checkpoint.

## 20. Smaller checkpoints (minimal path first; all of I1a is on the minimal path to Checkpoint G)
- **I1a-1** (≈0.7 h): wares, ledger, goods API, `IsUnique`, `MoveBin`, `BinTransfer` with T1–T5 (the frozen-goods part, reviewed first).
- **I1a-2** (≈0.8 h): state slots, hash part, tuning, module calls, dispatch and movement sites, ranges and `MaxCand`, lazy mind slots, `DesignId`, system shells, domains, catalogue and mapping stubs, pins P10–P13, static tests, save null-acceptance (lead).

## 21. Recommended agent
**Opus**, run for the lead (frozen shared files; the lead reviews every frozen-file diff, as for F1). Never Fable.

## 22. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded/keyed `Rng` with new labels unique per call site, no wall clock, no unordered enumeration; bots and people use the same commands); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `Assets/InitTestScene*` or anything Sims-2-derived; no third-party art or audio; original names and data; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done; never run the built player windowed in automation.
- **Before any Unity open or test run** (CLAUDE.md): `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it builds `DotNet/God.GameCompile` against a warm Library; pass `-UnityLibraryDir` if your worktree's Library is cold). Only then open the editor (batch mode) and confirm a clean console.
- **Industry rules (contracts §22.2):** one economy (no industrial inventory, stockpile, credit balance or owner type); never call or create a money issuance or destruction API, and never add a type named or acting as an external buyer, market or sink (D6.2, D6.6); never pass `actor <= 0` to `Goods`; never branch on `OwnerKind` or `OwnershipModel` in `Sim/Industry`, `Sim/Civ/Knowledge*`, `Sim/Civ/Money*`; a right is never a truth gate (R33); the god acts only through existing divine mechanics; no new death cause, faction or person creation.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `git status` before every commit; other sessions write other files concurrently, so stage only your own paths.
- Commit as you go so work survives an interruption; never bare `git stash` (shared stash stack), use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`.
- Report exactly which tests and runs happened and plainly what did not.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with the contracts or the code: stop and ask the lead for a §20 amendment row; never work around a missing seam. Minor reversible choices: take a default and record it in "I1a record" under "Design decisions taken during implementation".
