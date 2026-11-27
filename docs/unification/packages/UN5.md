# UN5: Settlements and roads founded by NPCs (Catan rules, development areas, planned sites, travel speed)

Package spec for the Unification milestone (owner D8; owner brief §40 field set). Self-contained. Binding sources, in order: [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md) D8.10, D8.11, D8.17; [SHARED_ARCHITECTURAL_CONTRACTS.md](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md); [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72; [UNIFICATION_DESIGN.md](../UNIFICATION_DESIGN.md) §6, §7; [UNIFICATION_BASELINE.md](../UNIFICATION_BASELINE.md) §6, §7, R3, R10, R12; [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md); this file. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

| Field | Value |
|---|---|
| Package identifier | **UN5** (Tier B, Wave 1 start, Checkpoint UB; the development-area predicate is also used by UA) |
| Branch / worktree | `unity/un5` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un5`, from `unity/poc` after **UN0** merged (merge after UN1) |
| Effort | 5 agent-hours (UN5a development area and settlement rules 2 h; UN5b NPC founding and roads 3 h) |
| Status | Not started |
| Recommended agent | **Opus** (Catan rule fidelity, replaces the six HQ-circle semantics, new NPC acts). Never Fable |

## 1. Objective
A settlement is a valid hex-corner with Catan's distance rule and a **development area**; it owns nothing and is not a government. NPCs (and, through UN8, groups) can **propose and found** new settlements and **build roads** along valid Catan edges between settlements or toward a **planned** settlement site. Roads only make walking cheaper: no flags, carriers, handover points, road requirement or magical transfer. NPCs may always walk anywhere accessible.

## 2. User-facing outcome
New settlements appear at the edge of the known world because people decided to found them; paths between busy places become faster after someone proposes and others do the work. The overlay shows areas and roads (UN10b); nothing in normal play shows hexes or flags.

## 3. Relevant design documents
Design §6 (settlement record, distance rule, development area, founding, what may be built where, tests), §7 (road record, rules, the collective-action loop steps 1–6, tests), §14.1 U9 and U10; audit §6 (settlements, roads), §7 (the six circle checks), R3, R10, R12; owner brief §4 and §10; `docs/stage4/WORLD_ONTOLOGY.md` (hex and node layers, accessibility).

## 4. Relevant existing source files (verified)
- Territory: `Territory/TerritoryRules.cs` (`CheckHq` 15–24, `CheckHqSite` 26–36, `AddBuilding` 105–125, `CheckRoad` 244–275, `CompleteHq` ~52), `Territory/TerritoryState.cs` (`HqNearCorner` ~212–221, `IsBuildable` 192–198, `CornersAdjacent`, `CornerDistance`, `NodeRoad`), `Territory/NodeGrid.cs` (12 nodes per hex edge, `CircleRadiusSteps`), `Territory/TerritoryCommands.cs`, `Territory/TerritoryTuning.cs` (flag gap, `RoadStepTicks`), `Territory/GodState.Territory.cs`, `Territory/Faction.cs`, `Factions/FactionRules.cs:247-` (`StartCorners.Pick`), `Board/BoardTopology.cs` (corner and edge adjacency), `Board/Lattice.cs`.
- Walking: `Population/NodePather.cs` (path cost, road nodes), `Population/PopulationSystem.cs` (883–889: `RoadStepTicks 4` versus `StepTicks 6`; `Self.OnRoad` ~621), `Population/PopulationTuning.cs` (74–75).
- UN0 seams you fill: `DevelopmentArea.Allows` (UN0's stub), `Settlement` record and `Settlements` API (UN0), `ObjClass` 132 `Settlement` / 133 `Road`, `Verb` 134 `ProposeRoad`, 135 `RoadWork`, 136 `ProposeSettlement`, `NpcVerb` 221/222, `UnificationState.UN5.cs`, `UnificationTuning.UN5.cs`, `UnificationHooks.UN5.cs`, `UnificationMindHooks.UN5.cs`, `UnificationEvents.UN5.cs`, `UN5Mappings.cs`, `UnificationSystem.DayStartUN5`.
- UN1's `Board.HexType`, land/sea and shore API; UN4b's founding settlements (soft: use fixtures).
- Tests to model on: Territory placement tests in `Assets/God/Tests/EditMode`, `DotNet/God.Sim.Tests`, `Support/UnifiedFixtures.cs`.

## 5. Allowed scope
**UN5a:**
1. **`DevelopmentArea.Allows` body** (replacing the UN0 stub, design §6.3): a node belongs to the nearest settlement's area within `AreaRadiusNodes` (default 24; ties to the older settlement); basic or temporary building kinds (`BuildingDef.SettlementOnly = false`: level-1 collection shelters, the optional hut row) may stand anywhere accessible, level 1 only outside every area (refusal `OutsideArea` otherwise); everything else only inside an area. Also routes the **founder-placement** and **source** callers (the six circle checks, UN0 seam) so that no unified code uses an HQ circle. Pure query; no state change.
2. **Settlement rules** (design §6.1–6.2): the Catan **distance rule** (no two settlements on corners sharing an edge, using `Topology.CornersAdjacent` and `t.HqNearCorner`, alias reason `SettlementTooClose`), the corner must be land with ≥ `MinLandHexes` (default 2) land hexes touching, within `MaxSiteDistance` (default 3 hexes) of an existing area (no isolated sites across the sea), no existing settlement there. `Settlements.CheckSite(s, corner)` returns an explicit reason. A settlement stores nothing and appears in no `Owner` (`Settlement_OwnsNothing`).
3. **Planned sites**: `PlannedSite { Id, Corner, Proposer, Tick, State }` (a *proposal* for a corner: planned sites also obey the distance rule against settlements and other planned sites) held in `UnificationState.Settlements`; a road may end at a planned site (D8.11).
4. **Stage 3 isolation**: no `Hq`, no flag, no `FlagTooClose` check, no HQ build circle is created or consulted in unified mode; the Stage 3 `Territory` rules remain untouched and used only by Stage 3 scenarios.

**UN5b:**
5. **Founding act** (`ProposeSettlement` verb 136, `NpcVerb.Propose` with `Detail` = settlement): an NPC knowing the founding knowledge (UN4a bit) may propose a corner passing the rules; the proposal opens a **founding project** `{ Corner, FoundWork (default 600 ticks), FoundWood (default 4 `Log`; 0 allowed by tuning), Progress, Materials, State }`. Any NPC who chooses may contribute labour (`FoundWork` act at the site, at `Work.Speed`) and materials (`Goods.Consume`, reason `Built`); a lone proposer can finish alone, slowly. On completion the `Settlement` exists, its area opens, events fire. Nothing triggers a proposal automatically: the **candidate** (`ProposeSettlement`, within the Unification quota, learned like any verb) appears only when the NPC perceives or remembers an unsettled corner that would be near unused raw nodes or forage and the settlement rules pass in its beliefs. UN6's goal `Settlement` and UN8's group objective attach to the same project record.
6. **Road model** (design §7.1): `Road { Id, FromCorner, ToCorner (or planned-site id), Nodes[] (lattice path ≤ 64 along valid hex edges), Progress, Needed (`RoadPerNodeWork` default 40 ticks per node), State (Planned, Building, Done, Abandoned), Proposer, Project }`; edges are valid hex edges; the from-end is a settlement or planned site; roads may overlap and share nodes; **no flags, no segments, no carrier, no handover**; a road is public by default (no owner).
7. **Walking effect**: completed road nodes are marked in `NodeRoad` so `NodePather` and the step cost use `RoadStepTicks` (4 versus 6); only the built prefix of a partial road is faster; **no goods ever move along a road by themselves**; NPCs may walk anywhere accessible without a road (asserted).
8. **`TravelLog`** (per NPC, 8 entries, in `NpcMind.Unif`: node pairs and tick costs of recent journeys) feeding the trigger of design §7.2 step 2: an NPC whose recent journeys on one corridor exceed `RoadProposeJourneys` (default 6 in 3 days) and whose learned association "walking this corridor costs time" is strong may choose `ProposeRoad` (verb 134), creating a road project; contributors do `RoadWork` (verb 135) at the next unfinished node at their `Work.Speed`; materials optional (planks later, by data). Stall: `RoadStallDays` (default 6) without progress marks it `Abandoned` (the built prefix stays). UN8 wraps projects in groups with invitations, joining, withdrawing and free-riding; UN5 supports **solo and uncoordinated contribution** (anyone may work an open project).
9. **Candidates** (`ProposeSettlement`, `ProposeRoad`, `ContinueProject`), `SelfFeature` feeding, `ObjClass` 132/133 perception (a project site is perceivable as a place), situation hints. Registers the planner goal rules `Road` and `Settlement` in `Unification/Plan/GoalRules.UN5.cs` (UN6's `GoalRules` registry has a `RegisterUN5` slot; if UN6 has not merged yet, ship the rules as data and a test against the registry interface UN0 declared, and the lead wires them when UN6 lands).
10. **Events** (design §13.6): 1250 `SettlementProposed`, 1251 `SettlementFounded`, 1252 `SettlementFailed`, 1253 `RoadProposed`, 1254 `RoadWorked` (HV), 1255 `RoadCompleted`, 1256 `RoadStalled`, 1257 `RoadAbandoned`.

## 6. Explicitly excluded scope
Groups, invitations, joining, withdrawing, free-riding records (UN8); goal planning (UN6); the six founding settlements and the hall (UN4b); buildings and their costs (UN3); tolls, road ownership, road maintenance decay, dispute machinery beyond C1/C9 existing (design §7.2 step 6: first milestone has none); boats and sea roads; removing Stage 3 systems (UN9); views of roads and areas (UN10b).

## 7. Dependencies
Hard: **UN0**. Soft: UN1 (unified board for the land/sea tests: merge after UN1; fixtures before), UN3 (buildings use `Allows`), UN4b (founding settlements), UN6 and UN8 (callers). Downstream: UN8, UN10b, UN11b.

## 8. Shared contracts
**Read:** `Topology`, `NodeGrid`, `NodePather`, `NodeRoad` (existing structures), `Goods` (frozen), `Work.Speed`, UN0 ranges. **Modify (own files and additive switch-gated edits):** `UnificationState` UN5 partial, tuning partial, catalogue array; additive edits to `Population/NodePather.cs` and `Territory/TerritoryState.cs` (road node marking and a `NodeRoad` write API for unified roads, gated) listed in the hand-back for the lead. **Never** edit `TerritoryRules.cs` semantics, `Goods.cs`, enum files, `GodSim`, `SaveFile`.

## 9. Files and modules owned
`Unification/Settlements/SettlementRules.cs`, `Settlements/DevelopmentAreaImpl.cs`, `Settlements/Founding.cs`, `Unification/Roads/Road.cs`, `Roads/RoadRules.cs`, `Roads/RoadWork.cs`, `Roads/TravelLog.cs`, `Unification/Plan/GoalRules.UN5.cs`, `Unification/UnificationState.UN5.cs`, `UnificationTuning.UN5.cs`, `UnificationHooks.UN5.cs`, `UnificationMindHooks.UN5.cs`, `UnificationEvents.UN5.cs`, `DotNet/God.Runner/Warehouse/UN5Mappings.cs`, `DotNet/God.Runner/Scenarios/unif_roads.scenario.json`, `Assets/God/Tests/EditMode/Unification/UN5*.cs` (commit `.meta`), `DotNet/God.Runner.Tests/UN5*.cs`.

## 10. Required domain events
`1250 SettlementProposed { Corner, Proposer, Project }`; `1251 SettlementFounded { Settlement, Corner, Node, Founder, Contributors }`; `1252 SettlementFailed { Corner, Reason }`; `1253 RoadProposed { Road, From, To, Proposer }`; `1254 RoadWorked (HV) { Road, Worker, Node, Progress, Control }`; `1255 RoadCompleted { Road, Nodes, Contributors }`; `1256 RoadStalled { Road }`; `1257 RoadAbandoned { Road, BuiltNodes }`.

## 11. Save/load implications
Saved: settlements (UN0), planned sites, founding projects, roads with progress and built prefix, `NodeRoad` marks, per-NPC `TravelLog`. Test `UN5_SaveLoadParity`: save mid-road (half the nodes built, two contributors) and mid-founding (materials in, work half done); load; continue equals the straight run at every later checkpoint.

## 12. Warehouse implications
Mapper `UN5Mappings.cs` for 1250–1257; `UN5_EventsRoundTrip`. UN11 defines `settlement`, `road`, `road_work` tables.

## 13. Acceptance tests
| Id | Test (behavioural) | Pass |
|---|---|---|
| UN5-T1 | `Unification_Off_Reproduces_Base` (U-P1), `Territory_Stage3_Unchanged` | Stage 3 placement tests and the three Stage 3 smoke hashes unchanged with switches 0; with `Settlements.Enabled = 1` in a Stage 3 faction game Stage 3 parts unchanged |
| UN5-T2 | `Settlement_DistanceRule` | no two settlements on corners sharing an edge; all corner/neighbour combinations on the unified board checked exhaustively; refusal reasons exact |
| UN5-T3 | `Settlement_OwnsNothing` | no `Owner` of any asset, lot or right is ever a settlement; deleting a settlement changes no ownership |
| UN5-T4 | `Outside_OnlyBasic`, `Area_Nearest` | level-1 shelter allowed anywhere accessible; a level-2 building or workshop refused with `OutsideArea` outside every area; area membership by nearest settlement, ties to the older |
| UN5-T5 | `Settlement_NewOnlyByNpcWork` (U10) | an NPC with knowledge proposes a legal corner and completes the project (materials consumed with reason `Built`, work ticks equal `FoundWork`); the new area allows a non-basic building; an illegal corner (adjacent, sea, isolated) fails with a reason; no settlement ever appears without NPC work (static + run probe) |
| UN5-T6 | `Road_NoFlagsNoCarriers` | zero flags, zero carriers or segment structures exist in unified mode; goods are never moved by a road (ledger and movement events unchanged by completing a road) |
| UN5-T7 | `Road_OnlySpeedsWalking` | path ticks over a completed road equal 4 per step versus 6 off it; an unbuilt prefix gives no benefit; an NPC with no road walks any accessible route |
| UN5-T8 | `Road_PlannedSiteAllowed`, `Road_EdgesValid` | a road ending at a planned site is accepted and routes along valid hex edges only; a road with an illegal edge is refused |
| UN5-T9 | `Road_NeedsContributions` | with no contributor progress stays 0 and the road stalls to `Abandoned` after `RoadStallDays`; one contributor finishes it alone in `Needed / speed` ticks |
| UN5-T10 | `Road_Proposed_FromExperience` (Mech) | an NPC that walked a corridor ≥ `RoadProposeJourneys` times in 3 days (scripted fixture) and holds the learned cost association chooses `ProposeRoad` within 3 days on ≥ 6 of 10 seeds; none proposes without the journeys |
| UN5-T11 | `UN5_SaveLoadParity`, `UN5_EventsRoundTrip` | as sections 11 and 12 |

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN5|FullyQualifiedName~Neutrality|FullyQualifiedName~Territory"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_roads --seed 3 --days 15 --record full --sqlite --out <scratch>\un5
```
Report settlements and roads over time, contributors per project, path-cost change on the road, and the corridor journey counts that triggered proposals.

## 15. Unity demonstration
None here (UN10b draws areas and roads; the dev overlay may be used). **Unity compile check required** if any type read by `God.Game` changed (`Territory` state, `NodeRoad`): `check-game-compile.ps1` 0 errors.

## 16. Performance considerations
`DevelopmentArea.Allows` is on the placement path: O(settlements) per call with a node-to-settlement cache refreshed when a settlement is added; road projects ≤ `MaxRoads` (default 16). Report `bench_npc_500` with `Roads.Enabled = 1`.

## 17. Integration procedure
1. The lead creates the worktree: `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un5 -b unity/un5 unity/poc` (after UN0 merged), then `robocopy C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs\Library C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un5\Library /E /MT:16` (0–7 = success; source editor closed).
2. Sim and EditMode tests first. Hand back **UN5a** early (the predicate is needed by UN3b's upgrade rule and UN4b).
3. Before hand-back `git merge unity/poc`; conflicts only in your own files.
4. Neutrality pins that must hold: **P1, P4, P8**, U-P1, `Territory_Stage3_Unchanged`, the three Stage 3 smoke hashes of `PolicyRunTests.cs:27`.
5. Full `dotnet test DotNet -c Release` green.
6. `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` 0 errors before any Unity run.
7. "UN5 record" in `docs/stage4/DEV_LOG.md` on the branch. Hand back; never push; never merge into `unity/poc`.

## 18. Risks
- **R3 inversion**: the HQ circle was the opposite of the development area; any caller of `IsBuildable` left reachable in unified mode silently re-imposes it. The static test `Unified_NoHqCircleReads` (guard from UN0) must pass.
- Catan edges versus the fine lattice: the road path must be a lattice path along hex edges; make the path finder deterministic and bounded (≤ 64 nodes).
- Free corners may be rare on a 30-hex island with six start settlements: UN11 must report how many legal corners exist (audit §14 could not establish it); if none, the founding goal can never fire and the finding is escalated, not hidden.
- Proposals must stay candidates, not scripts: the learned association is the only driver; test T10's negative case.
- Stage 3 `TerritoryState` additions must not change any Stage 3 hash.

## 19. Definition of done
UN5-T1…T11 green; pins and Stage 3 hashes unchanged; compile check 0 errors; "UN5 record" lists the flagged edits, the legal-corner count on seeds 1–20, what ran and the defaults taken; UN5a handed back early; handed back.

## 20. Smaller checkpoints
- **UN5a** (≈2 h): `DevelopmentArea` body, settlement rules, planned sites, T1–T4.
- **UN5b** (≈3 h): founding act, road model and work, `TravelLog`, candidates, events, T5–T11.

## 21. Recommended agent
**Opus.**

## 22. Used later by industry (keep the interfaces general)
Rail, power lines and ports (I4) reuse `Road`-style projects and the planned-site endpoint; keep project records generic over "kind" and work-act so later link types append rows.

## 23. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded/keyed `Rng` with new labels unique per call site, no wall clock, no unordered enumeration; bots and people use the same commands); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `docs/stage4.zip` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done; never run the built player windowed.
- **Before any Unity open or test run:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives an interruption; never bare `git stash`, use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`.
- Report exactly which tests and runs happened (counts, hashes) and plainly what did not run or did not pass.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with the contracts or the code: stop and ask the lead for an amendment row; never edit a frozen file or work around a missing seam. Minor reversible choices: take a configurable default and record it in "UN5 record".
