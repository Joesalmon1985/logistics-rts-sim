# UB1: Settlements and roads (built by people, between existing settlements only)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

**Later-stage spec (Tier UB).** Shorter than the UA specs; every owner-brief §40 field is present. UB does not start in this run unless UA-learned is green before H14 (index §8). Re-check this spec against merged UN4, UN5 and the executor before launching.

| Field | Value |
|---|---|
| Package identifier | **UB1** (Tier UB; Checkpoint UB) |
| Branch / worktree | `unity/ub1` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-ub1`, from `unity/poc` after **UN4 and UN5** merged |
| Effort | 3.0 agent-hours |
| Status | Not started. Replaces the D8 UN5 "settlement and road geometry/planner" content |
| Recommended agent | **Opus** (placement geometry and physical construction rules). Never Fable |

## 1. Objective
Let people found new settlements and build roads **as construction sites with real materials and labour**, using the UN4 pattern (`USE` with a known design on a valid placement, deliver by `USE`, labour by `USE`, completion only after both). Roads connect **existing** settlements only; a planned route or planned settlement is a gesture or a claim, never a site or a finished road to nowhere (amendment §2).

## 2. User-facing outcome
A person (or several) can found a settlement on a legal Catan vertex, which opens a development area where non-basic buildings may be placed; people can then lay a road edge by edge between two existing settlements, each completed edge speeding walking at once. The player watches them do it; nothing is built by a flag, carrier or faction.

## 3. Design anchors
Amendment §2 (upgrades, roads and settlements use the same pattern with Catan placement rules and real work; no road flags or abstract carriers; planned routes may be proposals), §D9.5 G11 (costs and rules), G1 (sea ring, coast nodes), §D9.4 c (site rules), §D9.3 (`Settlements`, `Roads` switches, default 0 until UB), §D9.6 (scale: any radius, multiple settlement corners per tribe), §23.4 (`ObjClass` 136 `Settlement`, 137 `Road`), §23.7 (`EffectKind` `PlaceSite`, `Contribute`, `Labour`), §23.8 (`ConstructionSite`, `Building`), §23.12 (events 1340–1359), R82, index §2 row UB1, D8 U9 and U10 (reworded).

## 4. Relevant existing source files (verified)
`Assets/God/Sim/Board/BoardTopology.cs` (`SegmentsPerHexEdge = 3` at `:44`, `VertexHexes`, `VertexEdges`, `VertexNeighbours`, `HexEdgeCount`), `Board/Lattice.cs`, `Board/Board.cs`, `Board/BoardGenerator.cs`, `Board/HexCoord.cs`; legacy Stage 3 roads and flags (`Economy/*`, `Core/Reasons.cs`, `Ai/AiBrain.cs`, `Civ/Work.cs` road references) are **reference only and never reused** (unified mode has no flags or carriers). Unified code from UN0/UN2/UN3b/UN4/UN5 (not present at spec time): `Unified/Construction/*` (`Sites.Place`, `Contribute`, `Labour`), `Unified/Buildings/*`, `Unified/Motor/*` (step durations `StepTicks` 6, road 4 at §D9.4 a), `Unified/Start/*` (settlement corners and development areas), `Unified/Catalogue/Generated/*`. Tests to model on: `Assets/God/Tests/EditMode/BoardTopologyTests.cs`, UN4's `UN4*`/`EC01*` tests, `DotNet/God.Runner.Tests/IslandPopulationTests.cs`.

## 5. Allowed scope
1. **Settlement site:** Catan vertex with the distance rule (no settlement on a neighbouring vertex), land, not coast-forbidden per the row; cost **2 wood + 2 clay + 1 sheep + 2 hay + 1,200 ticks of labour** (G11, all configurable); completion creates a `Settlement` record and a **development area of 3 node rings**; the six founding settlements of UN5 are the same record type.
2. **Development area rule (UN4's rule, now live):** production buildings and L2+ upgrades are placeable only inside a settlement's development area; refused with `BadSite` outside.
3. **Road site:** per Catan hex-edge segment **1 wood + 1 clay + 300 ticks**; placeable only on a Catan edge path **between two existing settlements**, adjacent to one of them or to a completed road edge; each completed edge sets walking cost at once (movement lattice edge duration 4 instead of 6, UN2 reads the road table); partial roads are valid, half-built edges are sites.
4. **Planned routes and settlements as claims:** a gesture with referents (place, person) or a `Claim`; nothing in the physics treats a plan as a site; no planner, flags or carriers.
5. Interruption, abandonment, resumption, destruction and contributor provenance exactly as UN4 (anyone may contribute; contributors are provenance, not title).
6. Events 1340–1359 (`SettlementFounded`, `RoadSitePlaced`, `RoadEdgeCompleted`, `RoadDecayed` if decay is implemented (L: off), mapper stub), tuning `Unification.Settlements`, `Unification.Roads` (default 0; 1 in UB scenarios).

## 6. Explicitly excluded scope
Roads to nowhere, road flags, carriers, walkers, faction road building, automatic road networks, city planning AI, title or rights over settlements or roads, tolls, groups (UN8b), settlement-level institutions, industry, trained use of these (UN6t2), views (UN10b).

## 7. Dependencies
Hard: **UN4** (sites, contribution), **UN5** (founding settlements, start generator), transitively UN0, UN2, UN3b. Merge-after: none. Downstream: UN8b, UN6t2, UN10b, UN11b, UC1.

## 8. Shared contracts
R: §23.7, §23.8 records, `Sites.Place`/`Contribute`/`Labour` APIs of UN4, `BoardTopology`. M (own partials): `UnifiedState.UB1.cs`, `EffectHandlers.UB1.cs`, events 1340–1359, `UB1Mappings.cs`. Never edit frozen files; the road speed table seam is an amendment request to UN2's owner via the lead if missing.

## 9. Files and modules owned
`Unified/Settlements/*`, `Unified/Roads/*`, `Unified/UnifiedState.UB1.cs`, `Unified/Interactions/EffectHandlers.UB1.cs`, `DotNet/God.Runner/Warehouse/UB1Mappings.cs` (stub; UN11b completes with migration 0047), tests `DotNet/God.Runner.Tests/UB1*.cs`, scenario `unif_settle.scenario.json`.

## 10. Required domain events
`SettlementFounded` 1340, `SettlementSitePlaced` 1341, `RoadSitePlaced` 1342, `RoadEdgeCompleted` 1343, `RoadSiteAbandoned` 1344 (L), each with `Origin`; contributions reuse UN4's 1242 and 1243.

## 11. Save/load implications
Settlement and road records, sites in progress, road table (edge → completed) saved and hashed in `unification`; `UB1_SaveLoadParity` (U-P6) with a road edge half built. The save version bump (7 → 8) happens only if UB state shapes change (lead, §23.14).

## 12. Warehouse implications
Mapper stub; `0047` (UN11b) holds settlements, roads (edge facts), no ownership facts; reconciliation: materials delivered equal consumed at completion.

## 13. Acceptance tests
| Id | Test | Maps to | Pass |
|---|---|---|---|
| UB1-T1 | `Settlement_Founded` (D8 U10): a settlement is founded on a legal vertex by real materials and labour from one or several persons; the distance rule refuses a neighbour; the development area opens; a non-basic building is placed inside and refused outside | U10 | exact |
| UB1-T2 | `Road_BetweenExisting` (D8 U9 mechanical): a road site is accepted only between two existing settlements or adjacent to a completed edge; a "road to nowhere" and a road toward a planned (claimed) settlement are refused (`BadSite`) | U9, amendment §2 | exact |
| UB1-T3 | `Road_Speed`: a person's step on a completed edge takes the road duration, on others the base duration; a half-built road changes only completed edges | MOTOR | exact |
| UB1-T4 | `Settlement_Road_Conservation`: materials delivered = materials consumed at completion; interruption, abandonment, resumption and destruction conserve goods; no title, flag, carrier or walker exists | EP-05, U11 | exact |
| UB1-T5 | `Plans_Are_Claims`: a communicated plan creates only a claim record; nothing physical changes | amendment §2, §3 | exact |
| UB1-T6 | `Scale_Geometry`: generation and placement work at radius 4 and 6 with 3–4 settlement corners per tribe | D10 | exact |
| UB1-T7 | `UB1_SaveLoadParity`, `UB1_EventsRoundTrip`, U-P3 static clean | U-P6 | exact |
| UB1-P | pins U-P1, P1, P4, P8, Stage 3 hashes with the switch off | all | exact |

## 14. Headless demonstration
`dotnet test DotNet -c Release --filter "FullyQualifiedName~UB1|FullyQualifiedName~Neutrality"`; `run --scenario unif_settle --seed 1 --days 30 --record full --sqlite`; report settlements founded, road edges completed, contributors per site (fixture-driven; labelled mechanical).

## 15. Unity demonstration
Compile check `AgentScripts/check-game-compile.ps1` 0 errors (types read by `God.Game`). Views are UN10b; no Unity run.

## 16. Performance considerations (D10)
Only active sites are processed; the road table is a bitset over edges; placement validity uses topology adjacency lists (no scans of all settlements); cost O(active sites + changed edges) per tick; works at radius 6 and 12 tribes.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-ub1 -b unity/ub1 <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-ub1\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "ub1 record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.

## 18. Risks
Road legality edge cases at the sea ring and coast; a planned route accidentally acting as a site; step-duration change moving hashes (only when `Roads = 1`); settlement distance rule on enlarged boards.

## 19. Definition of done
UB1-T1…T7 and UB1-P green; compile check 0 errors; "UB1 record" in DEV_LOG with what ran and defaults (costs, ticks, ring size); handed back.

## 20. Smaller checkpoints
**UB1a** settlements (T1, T4, T6); **UB1b** roads and speed (T2, T3, T5, T7).

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
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UB1 record" under "Design decisions taken during implementation".
