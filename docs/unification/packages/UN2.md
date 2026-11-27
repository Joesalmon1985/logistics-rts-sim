# UN2: Raw-material materialisation (dice, allowance, gradual physical lots, perception)

Package spec for the Unification milestone (owner D8; owner brief §40 field set). Self-contained. Binding sources, in order: [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md) D8.4, D8.5; [SHARED_ARCHITECTURAL_CONTRACTS.md](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md); [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72; [UNIFICATION_DESIGN.md](../UNIFICATION_DESIGN.md) §4; [UNIFICATION_BASELINE.md](../UNIFICATION_BASELINE.md) §3 (S15), §4.4, R2; [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md); this file. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

| Field | Value |
|---|---|
| Package identifier | **UN2** (Tier A, Wave 1, Checkpoint UA) |
| Branch / worktree | `unity/un2` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un2`, from `unity/poc` after **UN0** merged (merge after UN1) |
| Effort | 3 agent-hours |
| Status | Not started |
| Recommended agent | **Sonnet** (bounded: one pipeline, exact books). Never Fable |

## 1. Objective
A successful Catan production roll creates a **production allowance** in its hex that **materialises gradually** as physical, perceivable raw material at nodes of that hex. Nothing goes into any inventory; nobody can pick it up by hand (collection needs a building, UN3). Every unit is conserved from roll to node to (later) lot.

## 2. User-facing outcome
In a unified game the dice roll as today, but instead of a pile appearing at once, wood, stone, clay, hay, sheep and fish become available node by node over the following ticks; an NPC that looks around perceives "a few" or "many" of a material at a place and remembers it. Presentation of the nodes is UN10a.

## 3. Relevant design documents
Design §4 (pipeline, `HexSupply`, `RawNode`, rates, perception, desert and sheep, invariants Geo-1…Geo-5), §14.1 U3, §14.2 B2; audit §3 S15, §4.4, R2 (shared dice piles), R5; `docs/stage4/ECONOMY_DESIGN.md` (physical resources), `docs/stage4/WORLD_ONTOLOGY.md` (perception, accessibility).

## 4. Relevant existing source files (verified)
- Dice: `Territory/TerritorySystems.cs` (`DiceSystem`, order 10, lines 43–72; `Rng.Fork("dice", roll)` at line ~50; `PileAdded`/`PileWasted` events; `Piles[]`), `Territory/TerritoryTuning.cs` (`DiceIntervalTicks 60`), `Territory/GodState.Territory.cs`, `Economy/EconomyTuning.cs` (`WaresPerPile`), `Board/Board.cs` (hex numbers), `Board/HexCoord.cs`.
- Perception and memory: `Population/PopulationExtensions.cs` (`SenseExtra`), `Cognition/Thinker.cs` (`Sensed`, `ObjClass`), `Cognition/Mind.cs` (`BeliefKind`, `BeliefStore`, `RecallNodes 40`), `Cognition/People.cs`.
- Pile hand-gather path to switch off in unified mode: `Population/PopulationSystem.cs:497-515` and `:1196-1209`, `PopulationTuning.cs:127` (`HandGatherPiles`).
- UN0 seams you fill: `UnificationHooks.OnDiceMatch`, `UnificationSystem.TickUN2`, `UnificationState.UN2.cs` (`Geo`), `UnificationTuning.UN2.cs`, `UnificationEvents.UN2.cs`, `UN2Mappings.cs`, `ValuationPriors.UN2.cs`; UN1's `Board.HexType`/`ShoreNodes`.
- Tests to model on: `Assets/God/Tests/EditMode/Support/UnifiedFixtures.cs` (UN0), the dice tests in `Assets/God/Tests/EditMode` and `DotNet/God.Sim.Tests`, `F0GoodsConservationTests.cs`.

## 5. Allowed scope
1. **State** (`GodState.Unification.Geo`): `HexSupply { Hex, Type, Pending, Wasted, IssuedEver, MaterialisedEver, CollectedEver }` (one per raw hex, ascending hex id) and `RawNode { Id, Hex, Node, Ware, Count, Cap, ArrivedEver, TakenEver }` (sorted by id; id from `NextRawNodeId`).
2. **Allowance.** `UnificationHooks.OnDiceMatch` (called by `DiceSystem` for every hex whose number matches): when `Materialise.Enabled`, adds `AllowancePerRoll` (default 6; sheep 2 head) to `Pending` up to `AllowanceCap`, excess to `Wasted`; the Stage 3 pile is **not** placed in unified mode (the base dice, its keyed stream and the `DiceRolled` event are unchanged). Hexes that do not yield (Desert) do nothing.
3. **Materialisation.** `TickUN2` every `MaterialiseIntervalTicks` (default 30) per hex, in ascending hex id: moves `MaterialiseUnits` (default 1) from `Pending` to the `RawNode` of that hex with the lowest `Count` (ties lowest id); a full node (`Cap`, default 12) is skipped; if all are full the unit stays pending.
4. **Placement** (deterministic, at start): `NodesPerHex` (default 4) land nodes in each raw hex interior, at least `RawNodeSpacing` (4) nodes apart, not within 2 nodes of a settlement or building, `Rng.Fork("rawnodes", hex)`; Sea hexes use shore nodes adjacent to the hex (UN1's `ShoreNodes`); Desert none. Where UN4/UN5 settlements do not exist yet the rule "not near a settlement" is evaluated lazily at placement time via `Settlements.NearestArea` (empty list is fine).
5. **Sheep** are a living resource: a Pastures roll adds head; the node holds a `Count` of `Ware.Sheep` (79); it is never an `ItemLot`. (The enclosure that receives sheep is UN3's.)
6. **Not pickable.** A hand `PickUp` of a raw node is refused with reason `NoBuilding` (a new `Reasons` row; the `Reasons` table is extended as an additive edit); in unified mode `Verb.PickUp` of `Log` piles is disabled and `HandGatherPiles` is forced 0.
7. **Perception and memory.** `ObjClass.RawNode` (130) as a `Sensed` with a quantised state (none, few, many); the exact count only within `Reach.Touch`; remembered through the existing place memory (`RecallNodes`), aged like any belief; a remembered node yields a MoveTo-then-act candidate (UN3 supplies the act), never a direct act. A rumour is ordinary M5 social knowledge (no new system).
8. **Books** (design §4.6): `Geo-1` per hex `IssuedEver = Pending + Wasted + MaterialisedEver`; `Geo-2` per raw ware `ΣMaterialised = ΣRawNode.Count + ΣTaken`; `Geo-3` `ΣTaken = Σ lots produced with reason Collected` (checked once UN3 lands: a hook `RawNodes.Take(s, ev, node, n, collector)` is the only way to reduce `Count` and is UN2's API for UN3 to call); `Geo-5` determinism. An invariant check `Geo_Invariants` is added to `SimInvariants` through `UnificationModule.Check`.
9. **Events:** 1201 `AllowanceGranted`, 1202 `AllowanceWasted`, 1203 `ResourceMaterialised` (HV), and the raw/pending columns of 1205 `UnifiedDay` (UN1 created the event; UN2 adds fields, bumping `Version`).
10. **Valuation priors** rows for `Log`, `Stone`, `Clay`, `Hay`, `Fish` (raw) from collection effort; raw nodes carry no price (they are not goods).

## 6. Explicitly excluded scope
Collection, deposit, storage, buildings, rights (UN3); any change to Stage 3 piles or harvesters beyond the seam; forage (UN1); sheep enclosure, shearing and slaughter (UN3/UN7); views (UN10); warehouse tables (UN11); tuning of allowance and rates (UN11 V4).

## 7. Dependencies
Hard: **UN0**. Soft: UN1 (real unified board: merge after UN1; develop against `UnifiedFixtures`). Downstream: UN3 (the `RawNodes.Take` API and `RawNode` state), UN6, UN10a, UN11.

## 8. Shared contracts
**Read:** `Goods` (frozen), `Reach`, sense seam, `Rng` keyed forks, UN0 hooks, UN1 board API. **Modify (own partials only):** `UnificationState` UN2 partial, `UnificationTuning` UN2 partial, UN2 catalogue array, `ValuationPriors.UN2.cs`. **Never** edit `TerritorySystems.cs` (the seam is UN0's), `Wares.cs`, `Goods.cs`, `SaveFile.cs`.

## 9. Files and modules owned
`Unification/Geo/HexSupply.cs`, `Geo/RawNodes.cs`, `Geo/Materialise.cs`, `Geo/GeoInvariants.cs`, `Unification/UnificationState.UN2.cs`, `UnificationTuning.UN2.cs`, `UnificationHooks.UN2.cs`, `UnificationMindHooks.UN2.cs`, `UnificationEvents.UN2.cs`, `ValuationPriors.UN2.cs`, `DotNet/God.Runner/Warehouse/UN2Mappings.cs`, `DotNet/God.Runner/Scenarios/unif_geo.scenario.json`, `Assets/God/Tests/EditMode/Unification/UN2*.cs` (commit `.meta`), `DotNet/God.Runner.Tests/UN2*.cs`.

## 10. Required domain events
`1201 AllowanceGranted { Hex, Units, DieTotal, Pending }`; `1202 AllowanceWasted { Hex, Units }`; `1203 ResourceMaterialised { Hex, Node, Ware, Count }` (HV); `1205 UnifiedDay` extended with `{ Pending[hex type], NodeCount[ware], Issued, Materialised }`.

## 11. Save/load implications
Saved: `HexSupply` and `RawNode` lists, `NextRawNodeId`, the next materialisation tick per hex (public field). Test `UN2_SaveLoadParity`: save mid-allowance (pending units, a half-filled node) → load → continue equals the straight run at every later checkpoint.

## 12. Warehouse implications
Mapper `UN2Mappings.cs` maps 1201–1203/1205; `UN2_EventsRoundTrip`. UN11 defines `hex_supply` and reconciles `Geo-1`/`Geo-2` against events.

## 13. Acceptance tests
| Id | Test (behavioural) | Pass |
|---|---|---|
| UN2-T1 | `Unification_Off_Reproduces_Base` (U-P1), `Dice_Stage3_Unchanged` | with switches 0, and with `Unification.Enabled = 1` but `Materialise.Enabled = 0`, piles and every Stage 3 hash are unchanged |
| UN2-T2 | `Allowance_FromRollThenGradual` (design U3) | after a forced success at tick t, `Pending` rises by `AllowancePerRoll` at t; nodes of that hex gain one unit each `MaterialiseIntervalTicks` until spent; `Geo-1` and `Geo-2` exact at every tick of the timeline |
| UN2-T3 | `Allowance_Cap` | rolls beyond `AllowanceCap` put the excess in `Wasted` and emit 1202; full nodes leave units pending |
| UN2-T4 | `RawNode_NotPickable` (B2 part) | a hand `PickUp` of a node is refused with `NoBuilding`; no lot is produced; no inventory contains a raw lot after 20 days with no building |
| UN2-T5 | `Materialise_SpreadsToEmptiestNode` | four nodes fill evenly (±1); ties by id |
| UN2-T6 | `Sheep_AreHeadsNotLots`, `Sea_FishAtShoreOnly`, `Desert_NoRaw` | sheep counts conserve and no sheep lot exists; fish nodes are shore nodes; Desert has no `HexSupply` |
| UN2-T7 | `Perception_RawNode_QuantisedAndRemembered` | an NPC within sight senses "few"/"many"; after leaving, its belief persists and ages; with no building the NPC does not try to take it |
| UN2-T8 | `Geo_Deterministic` | same seed, same `RawNode` set and same roll-to-node timeline over 5 sim days on seeds 1–3; two runs hash equal |
| UN2-T9 | `Geo_Invariants_OnAllSeeds` | `Geo-1`, `Geo-2` hold at day ends over 10 days on seeds 1–10; `Geo-3` checked by a fixture that calls `RawNodes.Take` and the ledger |
| UN2-T10 | `UN2_SaveLoadParity`, `UN2_EventsRoundTrip` | as sections 11 and 12 |

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN2|FullyQualifiedName~Neutrality"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_geo --seed 3 --days 5 --record full --sqlite --out <scratch>\un2
```
Print the timeline for one raw hex: roll tick, pending after, each materialisation tick and node, and the end-of-day books.

## 15. Unity demonstration
None here (UN10a draws raw lots). Compile check required if `ObjClass` or `Reasons` types read by `God.Game` changed: `check-game-compile.ps1` 0 errors.

## 16. Performance considerations
Materialisation touches one hex per interval: O(raw hexes) per 30 ticks, trivial; `Sense` adds at most `RawNode` rows within sight (use the existing `PerceiveMax = 16` budget, nearest first). Report `bench_npc_500` with `Materialise.Enabled=1`.

## 17. Integration procedure
1. The lead creates the worktree: `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un2 -b unity/un2 unity/poc` (after UN0 merged), then `robocopy C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un2\Library /E /MT:16` (0–7 = success; source editor closed).
2. Sim and EditMode tests first; mapper after the books are green.
3. Before hand-back `git merge unity/poc` (and UN1 if merged); conflicts only in your own files.
4. Neutrality pins that must hold: **P1, P4, P8**, U-P1, `Dice_Stage3_Unchanged`, the three Stage 3 smoke hashes of `PolicyRunTests.cs:27`.
5. Full `dotnet test DotNet -c Release` green.
6. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` 0 errors before any Unity run; Unity only if you hold the token, otherwise hand back for the lead's checkpoint run.
7. "UN2 record" in `docs/stage4/DEV_LOG.md` on the branch. Hand back; never push; never merge into `unity/poc`.

## 18. Risks
- The dice seam is the shared pile path (audit R2): the Stage 3 pile and the allowance must never both fire for one roll; the test `Dice_Stage3_Unchanged` guards it.
- Rates are generous by design (design §4.3); do not tune. The capital bottleneck is meant to be labour (UN11 V4 reports).
- Raw nodes must not sit inside future settlement areas: placement near settlements is evaluated when settlements exist; a node placed before a settlement may later be inside its area, which is allowed.
- `RawNode` count display for sheep (heads) versus items: keep `Ware.Sheep` out of every `Goods` call.

## 19. Definition of done
UN2-T1…T10 green; pins and Stage 3 hashes unchanged; compile check 0 errors; "UN2 record" lists what ran and the defaults taken; handed back.

## 20. Smaller checkpoints
- **UN2a** (≈1.5 h): state, allowance, materialisation, books, `RawNodes.Take` API, T1–T3, T5, T8, T9.
- **UN2b** (≈1.5 h): placement on the unified board, sheep and sea cases, perception and memory, priors, events, T4, T6, T7, T10.

## 21. Recommended agent
**Sonnet.**

## 22. Used later by industry (keep the interfaces general)
`RawNode`/`HexSupply` are surface (renewable) materials; geological deposits (I1 `Deposit`) are separate and finite. Keep `RawNodes.Take` the only reducer so extraction can reuse the pattern.

## 23. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded/keyed `Rng` with new labels unique per call site, no wall clock, no unordered enumeration; bots and people use the same commands); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `docs/stage4.zip` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done; never run the built player windowed.
- **Before any Unity open or test run:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives an interruption; never bare `git stash`, use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`.
- Report exactly which tests and runs happened (counts, hashes) and plainly what did not run or did not pass.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with the contracts or the code: stop and ask the lead for an amendment row; never edit a frozen file or work around a missing seam. Minor reversible choices: take a configurable default and record it in "UN2 record".
