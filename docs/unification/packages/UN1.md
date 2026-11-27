# UN1: Geography (seven hex types, sea ring) and infinite forage

Package spec for the Unification milestone (owner D8; owner brief §40 field set). Self-contained. Binding sources, in order: [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md) D8.2, D8.3, D8.17; [SHARED_ARCHITECTURAL_CONTRACTS.md](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md); [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72; [UNIFICATION_DESIGN.md](../UNIFICATION_DESIGN.md) §2, §3; [UNIFICATION_BASELINE.md](../UNIFICATION_BASELINE.md) §4; [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md); this file. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

| Field | Value |
|---|---|
| Package identifier | **UN1** (Tier A, Wave 1, Checkpoint UA) |
| Branch / worktree | `unity/un1` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un1`, from `unity/poc` after **UN0** merged |
| Effort | 3 agent-hours (UN1a board and hex types 1.5 h; UN1b forage 1.5 h) |
| Status | Not started |
| Recommended agent | **Sonnet** (bounded data and rules; the board change is the only delicate part: see risks). Never Fable |

## 1. Objective
The world has seven abstract hex types on one island ringed by sea, and every region offers an infinite, rate-limited, perishable, nutritious forage food that needs no building and no dice. Starvation can then arise only from a person's own time and choices, not from a finite depleting stock.

## 2. User-facing outcome
In a unified game (`Unification.Forage.Enabled = 1`) people walk to forests, mountains, wetlands, pastures, fields, deserts and the shore, spend time, and pick mushrooms, roots, eggs, rabbit, vegetables, cactus fruit and seaweed, then eat, carry, store, gift or trade them. Nothing ever runs out. The view work is UN10a.

## 3. Relevant design documents
Design §2 (hex types, terrain mapping, `UnifiedPool`, sea ring, shore nodes), §3 (forage principle, `ForageTable`, mechanics, replaces `ResourceSource`, M3 learning pins), §14.1 U2, §14.2 B1; audit §4 (terrain, wares, sources, gap table), §12 R8 and R9; `docs/stage4/ECONOMY_DESIGN.md` (food and spoilage), `docs/stage4/NPC_LEARNING_DESIGN.md` (forage act learning), `docs/stage4/WORLD_ONTOLOGY.md` (accessibility and perception).

## 4. Relevant existing source files (verified)
- Board: `Board/Board.cs`, `Board/BoardGenerator.cs` (`IslandTerrainPool`, `CreateIslandBoard`, number-token dealing lines 34–36 and 69–80), `Board/HexCoord.cs` (`Terrain` 0–6, `TerrainInfo.IsResource`), `Board/BoardTopology.cs`, `Board/Lattice.cs`, `Territory/NodeGrid.cs` (12 nodes per hex edge), `Territory/Faction.cs` (`RawKinds.OfTerrain`, lines 58–72).
- Forage and food: `Population/PopulationSystem.cs` (the `ForageAt` act, lines ~942–965; hand gathering ~966–1000), `Population/PopulationModule.cs` (`PlaceSources` ~279–306, `IsCoast` ~308–313, `SpoilDays`/`Nutrition` ~210–232), `Population/PopulationTuning.cs` (`EcologyTuning` 138–160, `ForageTicks 30`, `HungerPerDay 500`, carry limits 103–104), `Population/PopulationState.cs` (`ResourceSource`, `SourceKind`, `ProduceReason.Foraged`), `Population/Goods.cs` (`Produce`), `Economy/Wares.cs` (`IsFood`), `Learning/Verbs.cs` (`ForageAt` 10), `Cognition/Thinker.cs` and `Population/PopulationExtensions.cs` (`Sense`, `SenseExtra`), `Population/LearningFixtures.cs` (the M3 `learning_*` fixtures that must not change).
- UN0 seams you fill: `Unification/UnificationState.UN1.cs`, `UnificationTuning.UN1.cs`, `UnificationHooks.UN1.cs`, `UnificationMindHooks.UN1.cs`, `UnificationEvents.UN1.cs`, `ValuationPriors.UN1.cs`, `DotNet/God.Runner/Warehouse/UN1Mappings.cs`, the `HexType`/`Board.HexType` seams.
- Tests to model on: `Assets/God/Tests/EditMode/Support/UnifiedFixtures.cs` (UN0), `Support/GoodsTrace.cs`, `DotNet/God.Runner.Tests/F0GoodsConservationTests.cs`, the M3 learning tests that use `LearningFixtures`.

## 5. Allowed scope
1. **Hex types and the unified board.** `HexTypes.OfTerrain(Terrain)` (design §2.2: Forest, Mountain, Hills→Wetland, Fields, Pasture→Pastures, Desert, Meadow→Sea) and `BoardGenerator.UnifiedPool` (30 land hexes: 5 Forest, 5 Mountain, 5 Wetland, 6 Pastures, 6 Fields, 3 Desert by default, configurable in `Unification.Geo`) plus **one ring of Sea hexes (radius 4)**; `Board.HexType[]` filled; number tokens (2–12) dealt as today to every hex with a raw material (Desert none; Sea hexes get tokens too because they yield fish: default 12 tokens spread over the ring, configurable). `Board.HexTerrain[]` keeps Stage 3 values. Sea hexes are not walkable; **shore nodes** (land nodes adjacent to a Sea hex) are the access points; a land hex is coastal when any node is shore. Fairness check (design §15 item 7): every tribe start corner is within 2 hexes of at least four of the seven hex types, else re-deal with the next `Rng.Fork("unified-deal", attempt)`; the Stage 3 island is untouched when `Unification.Enabled = 0`.
2. **`ForageTable`** (data rows in `Population/ForageTable.cs`, shape per design §3.2: hex type, ware, nutrition, collect ticks, spoil days, access rule, `PoisonPermille`, skill) with the defaults of the design table; wares are UN0's 72–77 plus `Roots` 29 for Mountain. `Wares.IsFood` true for all seven; nutrition from the table (not `BerryNutrition`) in unified mode; spoilage via `ItemLot.SpoilDay` as today; `Forage.RawFishNutrition` (default 0): `Fish` is not eaten raw in unified mode (UN0 section 21 item 2).
3. **The forage act** (reuse `Verb.ForageAt` 10 and `NpcVerb.ForageAt`): walk to `Reach.Touch` of an accessible node of the hex type, work `CollectTicks` (adjusted by `SkillClass.Foraging`, at most `ForageSkillMaxPermille` default 300), then `Goods.Produce(..., ProduceReason.Foraged)` one lot into the carry (or eat). **No `ResourceSource` is touched or placed**: `Ecology.Place` skips and `PopulationState.Sources` is empty when `Forage.Enabled`; the hand-gathered `Wood` sources and log hand-gathering are disabled in unified mode (the replacement is UN3: until UN3 merges the unified world simply has no wood, which is correct).
4. **Perception.** `Sense` reports the nearest accessible node of each forage type within sight as `ObjClass.ForagePlace` (131) with no stock; place memory through the existing recall (`RecallNodes`); a remembered place yields a MoveTo-then-act candidate, never a direct act.
5. **Per-individual rate limit** is the collection time plus carry slots plus spoilage; `ForageDailyItemCap` (default 0 = off). **Optional shared throughput** (`Forage.SharedThroughput.Enabled`, default **0**): forage cells with refilling tokens (design §3.3.4); tokens never reach "gone for ever".
6. **Coastal access.** Seaweed only from shore nodes; `FishAccessRadius` recorded for UN3 (fisher's shelter). Boats are out of scope.
7. **Valuation priors** rows for the seven foods (value from nutrition per collect tick) in `ValuationPriors.UN1.cs`; instinct priors so a hungry NPC treats the new foods as food.
8. **Events** (UN1 partial, codes from design §13.6): 1200 `ForageCollected` (HV), 1204 `ForageThroughputWaited` (HV), 1205 `UnifiedDay` (daily snapshot, shared with UN2: UN1 writes the forage columns).

## 6. Explicitly excluded scope
Raw-material dice allowance and `RawNode` (UN2); any building, shelter or collection building (UN3); the start population (UN4); settlements and roads (UN5); presentation (UN10); changing any M3 learning scenario's food model (they keep `ResourceSource` with the switch off); a thirst or water drive (design §15 item 6).

## 7. Dependencies
Hard: **UN0**. Soft: UN2 (the Sea hexes' raw nodes use the shore nodes you define: publish `Board.ShoreNodes(hex)` in the first commit). Downstream: UN2, UN3, UN4b, UN9, UN10a, UN11.

## 8. Shared contracts
**Read:** `Goods` (frozen API: `Produce`, `Consume`, `Move`), `Bodies`, `Reach`, the sense seam, F1/UN0 id ranges (`ObjClass` 131, `ProduceReason.Foraged`), `SkillStore` accessor. **Modify (own partials only):** `UnificationState` UN1 partial, `UnificationTuning` UN1 partial, the UN1 catalogue array, `ValuationPriors.UN1.cs`. **Never** edit `Wares.cs`, `Goods.cs`, `PopulationState.cs`, enum files, `GodSim`, `SaveFile`. Existing files you must edit for the board and forage (flagged, lead-reviewed, additive and switch-gated): `Board/BoardGenerator.cs`, `Board/Board.cs`, `Population/PopulationModule.cs` (`PlaceSources` skip), `Population/PopulationSystem.cs` (forage act table lookup).

## 9. Files and modules owned
`Board/HexTypes.cs`, `Board/UnifiedBoard.cs`, `Population/ForageTable.cs`, `Population/Forage.cs` (act logic and cells), `Unification/UnificationState.UN1.cs`, `UnificationTuning.UN1.cs`, `UnificationHooks.UN1.cs`, `UnificationMindHooks.UN1.cs`, `UnificationEvents.UN1.cs`, `ValuationPriors.UN1.cs`, `DotNet/God.Runner/Warehouse/UN1Mappings.cs`, `DotNet/God.Runner/Scenarios/unif_forage.scenario.json`, `Assets/God/Tests/EditMode/Unification/UN1*.cs` (commit `.meta`), `DotNet/God.Runner.Tests/UN1*.cs`.

## 10. Required domain events
`1200 ForageCollected { Npc, Hex, HexType, Ware, Count, Node, Control }` (HV); `1204 ForageThroughputWaited { Npc, Cell }` (HV, only with shared throughput); `1205 UnifiedDay { Day, Foraged[ware], ... }` (the UN2 columns are added by UN2: a field added bumps `Version`). Every action event started under control carries `Control` (contracts §15.1).

## 11. Save/load implications
Saved: `Board.HexType` (already optional from UN0), forage cell token state (only with shared throughput), nothing else (forage has no stock). Test `UN1_SaveLoadParity`: save mid-forage (an NPC half-way through a collection) and mid-throughput-wait, load, continue equals the straight run at every later checkpoint.

## 12. Warehouse implications
Mapper `UN1Mappings.cs` maps 1200/1204/1205 to rows UN11 defines (`fact_forage` , `fact_unified_day`); UN1 supplies a test `UN1_EventsRoundTrip` (every event emitted with all fields populated); the table and reconciliation are UN11's.

## 13. Acceptance tests
| Id | Test (behavioural) | Pass |
|---|---|---|
| UN1-T1 | `Unification_Off_Reproduces_Base` (U-P1) and `Forage_SubSwitch_Isolated` (U-P3) | Stage 3 and every existing scenario hash unchanged with switches 0; `Forage.Enabled` alone in a Stage 3 faction game leaves Stage 3 parts unchanged |
| UN1-T2 | `Board_SevenTypes_SeaRing` | the unified board has exactly the configured counts of the seven types (30 land + sea ring), every land hex adjacent to the ring has shore nodes, Sea nodes are unwalkable, tokens dealt only to raw-material hexes, same seed gives the same board, fairness holds on seeds 1–20 |
| UN1-T3 | `Forage_NoBuildingNeeded` (B1) | an NPC with no building anywhere forages and eats; hunger falls by the table's nutrition |
| UN1-T4 | `Forage_TakesTime` | ticks between act start and the lot equal `CollectTicks` ±1; skill 1000 shortens by the configured permille |
| UN1-T5 | `Forage_NeverDepletes` | 10 000 acts at one node change nothing but the forager's lots and the ledger; no `ResourceSource` exists |
| UN1-T6 | `Forage_SeaweedOnlyAtShore`, `Forage_SharedThroughput_RefillsAndNeverZeroForever` | seaweed refused inland; with throughput on and `Cap` 6, a crowd of 30 waits, tokens refill by `RefillPerDay`, a lone forager is never blocked for more than one refill interval |
| UN1-T7 | `Forage_LedgerBalances` | every food lot comes from reason `Foraged`; ledger identity holds over all wares; `Fish` is not eaten raw by default |
| UN1-T8 | `Forage_Survive_NoDice` (design U2) | on the unified board with `Materialise.Enabled = 0` and the UN0 fixture population of 72 placed one per two nodes near mixed hexes, all survive 20 days; every food lot is `Foraged` (the real six-tribe start is UN4b; this uses the fixture) |
| UN1-T9 | `M3_Learning_Unchanged` | the existing `learning_*` fixtures and pins P1–P9 pass untouched with `Unification.Enabled = 0` |
| UN1-T10 | `Forage_Learns_BestPerTick` (Mech) | from 5 seeds, a hungry NPC's learned preference for a high-nutrition-per-tick food over a low one rises over 10 days |
| UN1-T11 | `UN1_SaveLoadParity`, `UN1_EventsRoundTrip` | as sections 11 and 12 |

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN1|FullyQualifiedName~Neutrality|FullyQualifiedName~Learning"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_forage --seed 3 --days 20 --record full --sqlite --out <scratch>\un1
```
Report from the SQLite or event log: foraged items per food per day, mean hunger, the share of work ticks spent foraging, deaths (expect zero in the fixture), and the board's hex-type counts.

## 15. Unity demonstration
None in this package (UN10a draws the landscape). **Unity compile check required** if `Board` or any type `God.Game` reads changed (it does): `check-game-compile.ps1` 0 errors, then batch EditMode green.

## 16. Performance considerations
`Sense` adds at most one `ForagePlace` row per forage type (seven) within sight: bound by the existing `PerceiveMax = 16` budget; report ticks/s with `bench_npc_500` and `--set tuning.Unification.Forage.Enabled=1`. Forage cells (throughput) are a sorted array, updated at day start only.

## 17. Integration procedure
1. The lead creates the worktree: `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un1 -b unity/un1 unity/poc` (after UN0 merged), then `robocopy C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un1\Library /E /MT:16` (0–7 = success; source editor closed).
2. Sim and EditMode tests first. Hand back **UN1a** (board, hex types, shore nodes) early: UN2, UN4b and UN10a wait for it.
3. Before hand-back `git merge unity/poc`; conflicts only in your own files; a frozen-file conflict means stop and report.
4. Neutrality pins that must hold: **P1, P4, P8**, U-P1 (`Unification.Enabled = 0` reproduces the current hashes), U-P3, the three Stage 3 smoke hashes of `PolicyRunTests.cs:27`, the M3 learning pins.
5. Full `dotnet test DotNet -c Release` green.
6. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` 0 errors before any Unity run; then batch EditMode if you hold the Unity token, otherwise hand back and the lead runs Unity at the checkpoint.
7. "UN1 record" in `docs/stage4/DEV_LOG.md` on the branch. Hand back; never push; never merge into `unity/poc`.

## 18. Risks
- **The sea ring changes the board size** (30 → 54 hexes). `BoardTopology`, `NodeGrid`, `Lattice`, `Faction`/`StartCorners`, camera bounds and island views were written for the 30-hex island; the audit notes `Meadow` exists as "the rim of a larger board" but no run has exercised it [unverified]. Spike this first (a headless board with the ring, node counts, pathing) and report before building on it. A fallback (sea as a ring of unwalkable *nodes* inside the existing 30 hexes) needs a lead decision.
- Forage tuned so it is trivially sufficient or fatal: the defaults are the design's; report V1-style survival (UN11 will tune), do not tune here.
- Removing hand-gathered wood makes the unified world woodless until UN3: tests in this package must not depend on wood.
- M3 learning pins depend on `ResourceSource`: keep the old path byte-identical when the switch is off.

## 19. Definition of done
UN1-T1…T11 green; pins P1/P4/P8, U-P1, U-P3 and Stage 3 hashes unchanged; compile check 0 errors; the "UN1 record" lists what ran, the board spike result and the defaults taken; UN1a handed back early; handed back.

## 20. Smaller checkpoints
- **UN1a** (≈1.5 h): hex types, unified board, sea ring, shore nodes, fairness, UN1-T1 and T2.
- **UN1b** (≈1.5 h): forage table, act, perception, throughput option, priors, events, UN1-T3…T11.

## 21. Recommended agent
**Sonnet.** If the board spike (risk 1) shows the topology cannot take the ring without changes to frozen territory code, stop and hand the finding to the lead; the lead may reassign UN1a to Opus.

## 22. Used later by industry (keep the interfaces general)
`HexType` and the shore-node API are reused by boats and ports (I2, I4); the forage table is data, so later foods (cooked, farmed) append rows.

## 23. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded/keyed `Rng` with new labels unique per call site, no wall clock, no unordered enumeration; bots and people use the same commands); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `docs/stage4.zip` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done; never run the built player windowed.
- **Before any Unity open or test run:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives an interruption; never bare `git stash`, use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`.
- Report exactly which tests and runs happened (counts, hashes) and plainly what did not run or did not pass.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with the contracts or the code: stop and ask the lead for an amendment row; never edit a frozen file or work around a missing seam. Minor reversible choices: take a configurable default and record it in "UN1 record".
