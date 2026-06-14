# Trial Economy Game Requirements

Binding contract for the **Trial Economy Game** — the permanent abstract economy engine for this project. Not a throwaway prototype.

---

## 1. Purpose

Deliver a headless, deterministic hex-map settlement logistics simulation where players build roads, place buildings, run production chains, and satisfy construction goals using workers, carriers, and ware routing.

The Trial Economy Game is the **foundation** for the Hybrid Game. Military construction and production in hybrid mode will consume wares defined here.

---

## 2. Non-Goals

- Military units, combat, power, or intel (see `03_trial_military_game_requirements.md`)
- Hybrid integration rules (see `04_hybrid_game_requirements.md`)
- Godot rendering (see `06_mobile_2d_visual_design.md` and RUN_05)
- Neural training (RUN_06)
- Multiplayer (RUN_12)
- Copying Widelands economy chains, names, or balance

---

## 3. Originality Rules

- Use original placeholder building and ware names from `05_units_resources_buildings_catalogue.md` and `10_glossary.md`
- Do not copy protected names, exact chains, or iconic identity from Widelands or other reference games
- Rules may follow genre conventions; data and naming must be original

---

## 4. Python Simulation Authority

- All economy rules execute in Python only
- Godot displays snapshots; it does not compute production, pathfinding, or ware movement
- Commands are validated and applied in the sim; events record all state changes
- See `docs/architecture/01_python_sim_authoritative_core.md`

---

## 5. Determinism

- **ECON-DET-001:** All randomness uses seeded RNG stored in simulation state
- **ECON-DET-002:** Same seed + same command log → identical events and snapshot hash
- **ECON-DET-003:** Replay re-execution must pass hash verification

---

## 6. Tick Loop

- **ECON-TICK-001:** Simulation advances in discrete ticks
- **ECON-TICK-002:** Each tick processes: command queue → economy subsystems → event emission
- **ECON-TICK-003:** Tick number is monotonic and included in events and snapshots
- Subsystem order within a tick is fixed and documented (deposits → production → construction → carriers → diagnostics)

---

## 7. Success and Failure States

| State | Condition | Rule ID |
|-------|-----------|---------|
| Economy success | Player completes configured victory building or ware quota | ECON-WIN-001 |
| Economy stalled | No progress possible for N ticks (configurable) with diagnostic reason | ECON-STALL-001 |
| Scenario timeout | Max ticks reached without success | ECON-TIME-001 |

Diagnostics must report stall reasons: missing input, blocked output, no path, storage full, no workers.

---

## 8. Hex Map (Axial Coordinates)

- **ECON-MAP-001:** Map uses axial coordinates `(q, r)` with standard hex neighbour definitions
- **ECON-MAP-002:** Map bounds are rectangular in axial space; out-of-bounds queries fail gracefully
- **ECON-MAP-003:** Each hex has terrain, height, owner, and optional road/building references
- **ECON-MAP-004:** Snapshots export full map dimensions and per-hex display data

---

## 9. Terrain

- **ECON-TERR-001:** Terrain types include at minimum: grass, water, rock, forest, fertile
- **ECON-TERR-002:** Terrain affects buildability and movement cost
- **ECON-TERR-003:** Water hexes are not buildable except for designated water-source buildings (well)

---

## 10. Height and Slope

- **ECON-SLOPE-001:** Each hex has integer height
- **ECON-SLOPE-002:** Slope between adjacent hexes = `abs(height_a - height_b)`
- **ECON-SLOPE-003:** Slope above threshold blocks road placement or increases path cost (configurable)
- **ECON-SLOPE-004:** Height exported in snapshot for 2D shading in Godot

---

## 11. Buildability

- **ECON-BUILD-001:** Placement checks terrain, slope, footprint overlap, and ownership
- **ECON-BUILD-002:** Failed placement returns structured rejection reason in command result

---

## 12. Display / Snapshot Requirements (2D Godot Viewer)

Snapshots must include layers Godot renders without sim logic:

| Layer | Content |
|-------|---------|
| terrain | terrain type, height |
| roads | road segments per hex edge |
| road_nodes | node positions and connections |
| buildings | plan, construction progress, completed |
| wares | in-transit and stored (aggregated per node where needed) |
| workers / carriers | position and assignment |
| ownership | per-hex or per-building player colour |
| diagnostics | stall flags, blocked jobs |

- **ECON-SNAP-001:** Snapshot is JSON-compatible
- **ECON-SNAP-002:** Optional hex grid overlay toggled in Godot (hidden by default on mobile)

---

## 13. Visual Layers (Future Godot)

See `06_mobile_2d_visual_design.md`. Economy trial requires snapshot data for all layers above; art assets are RUN_05 scope.

---

## 14. Player Ownership

- **ECON-OWN-001:** Each hex, building, and road belongs to at most one player
- **ECON-OWN-002:** Commands affect only the issuing player's entities unless scenario rules say otherwise

---

## 15. Resources and Wares

- **ECON-WARE-001:** Wares are typed, countable, and transportable (timber, planks, stone, grain, water, bread, coal, iron_ore, iron_ingots, tools, builder_work)
- **ECON-WARE-002:** Wares exist in storage, at road nodes, in transit on carriers, or consumed by production/construction
- **ECON-DEP-001:** Deposits (forest, rock, coal, iron, fertile, water source) are map features with extraction or renewal rules

---

## 16. Required Production Chains

| Chain | Rule ID |
|-------|---------|
| forest/trees → woodcutter → timber | ECON-CHAIN-001 |
| timber → sawmill → planks | ECON-CHAIN-002 |
| rock/deposit → quarry → stone | ECON-CHAIN-003 |
| fertile land → farm → grain | ECON-CHAIN-004 |
| water source → well → water | ECON-CHAIN-005 |
| grain + water → bakery → bread | ECON-CHAIN-006 |
| coal deposit → coal mine → coal | ECON-CHAIN-007 |
| iron deposit → iron mine → iron ore | ECON-CHAIN-008 |
| iron ore + coal → smelter → iron ingots | ECON-CHAIN-009 |
| iron ingots + planks → workshop → tools | ECON-CHAIN-010 |
| planks + stone + tools + builder work → completed building | ECON-CHAIN-011 |

Each chain must be testable in isolation and as an integrated scenario.

---

## 17. Buildings

- **ECON-BLD-001:** Buildings have footprints, input/output ports (road node links), worker slots, and enable/disable state
- **ECON-BLD-002:** Building types match catalogue in `05_units_resources_buildings_catalogue.md`

---

## 18. Building Placement

- **ECON-PLACE-001:** `PlaceBuildingPlanCommand` creates a construction site at valid footprint
- **ECON-PLACE-002:** Plan shows required wares before construction starts

---

## 19. Workers

- **ECON-WORK-001:** Workers are assigned to buildings or construction sites
- **ECON-WORK-002:** Production and construction progress require assigned workers (simplified: 1 worker = 1 work unit per tick unless configured otherwise)
- **ECON-WORK-003:** `AssignWorkerCommand` or `SetWorkerPolicyCommand` controls assignment

---

## 20. Roads and Road Nodes

- **ECON-ROAD-001:** Roads connect adjacent hexes; placement requires valid terrain and ownership
- **ECON-ROAD-002:** `RemoveRoadCommand` removes road if no blocking constraints
- **ECON-NODE-001:** Road nodes are junctions for carrier pickup/delivery; at least one node adjacent to each building port
- **ECON-NODE-002:** Roads must form a connected network for carriers to reach destinations

---

## 21. Pathfinding

- **ECON-PATH-001:** Carriers pathfind on the road network graph (nodes and edges)
- **ECON-PATH-002:** Blocked paths emit events and diagnostics with failure reason
- **ECON-PATH-003:** Path cost may incorporate slope at hex endpoints

---

## 22. Logistics and Delivery Jobs

- **ECON-JOB-001:** When a site needs wares, a delivery job is created
- **ECON-JOB-002:** Jobs are assigned to idle carriers by priority
- **ECON-JOB-003:** Job lifecycle: created → assigned → in_transit → delivered | blocked

---

## 23. Carriers

- **ECON-CARR-001:** Carriers carry one ware type at a time (simplified)
- **ECON-CARR-002:** Carriers move one edge per tick along computed path (configurable speed)

---

## 24. Storage

- **ECON-STOR-001:** Storage buildings hold wares up to capacity
- **ECON-STOR-002:** Storage full emits event and blocks input delivery

---

## 25. Construction Sites

- **ECON-CONS-001:** Construction consumes delivered wares and builder work over ticks
- **ECON-CONS-002:** Progress events emitted each tick while active
- **ECON-CONS-003:** Completion spawns functional building entity

---

## 26. Economy Priorities

- **ECON-PRI-001:** `SetConstructionPriorityCommand` orders competing construction sites
- **ECON-PRI-002:** `SetProductionPriorityCommand` orders production at buildings
- **ECON-PRI-003:** `SetWarePriorityCommand` orders ware routing when carriers are scarce

---

## 27. Required Commands

| Command | Rule ID | Description |
|---------|---------|-------------|
| PlaceRoadCommand | ECON-CMD-ROAD-001 | Place road on hex edge |
| RemoveRoadCommand | ECON-CMD-ROAD-002 | Remove road segment |
| PlaceBuildingPlanCommand | ECON-CMD-BLD-001 | Place building plan footprint |
| CancelConstructionCommand | ECON-CMD-BLD-002 | Cancel in-progress construction |
| SetConstructionPriorityCommand | ECON-CMD-PRI-001 | Set construction priority |
| SetProductionPriorityCommand | ECON-CMD-PRI-002 | Set production priority |
| SetWarePriorityCommand | ECON-CMD-PRI-003 | Set ware routing priority |
| EnableBuildingCommand | ECON-CMD-BLD-003 | Enable production at building |
| DisableBuildingCommand | ECON-CMD-BLD-004 | Disable production at building |
| AssignWorkerCommand | ECON-CMD-WORK-001 | Assign worker to building/site |
| SetWorkerPolicyCommand | ECON-CMD-WORK-002 | Alternative: auto/manual worker policy |

All commands return structured result: `{ accepted, reason, events[] }`.

---

## 28. Required Event Categories

| Category | Rule ID |
|----------|---------|
| game_created | ECON-EVT-001 |
| tick_advanced | ECON-EVT-002 |
| road_placed / removed / blocked | ECON-EVT-003 |
| building_plan_placed | ECON-EVT-004 |
| construction_started / progressed / completed | ECON-EVT-005 |
| delivery_job_created / assigned / blocked | ECON-EVT-006 |
| ware_reserved / picked_up / moved / delivered / consumed | ECON-EVT-007 |
| worker_assigned / moved | ECON-EVT-008 |
| production_started / paused / completed | ECON-EVT-009 |
| resource_extracted / depleted / renewed | ECON-EVT-010 |
| storage_full | ECON-EVT-011 |
| input_missing | ECON-EVT-012 |
| output_blocked | ECON-EVT-013 |
| economy_stalled | ECON-EVT-014 |
| economy_success_achieved | ECON-EVT-015 |
| snapshot_exported | ECON-EVT-016 |

---

## 29. Diagnostics

- **ECON-DIAG-001:** Per-building and per-site diagnostic records: status, missing inputs, blocked outputs, path failures
- **ECON-DIAG-002:** Global economy health summary in snapshot
- **ECON-DIAG-003:** Diagnostics update every tick when state changes

---

## 30. Bots

- **ECON-BOT-001:** Bots submit same commands as humans via Python API
- **ECON-BOT-002:** Minimal bot: place roads, building plan, set priorities (RUN_04)

---

## 31. Tests

| Category | Rule ID | Examples |
|----------|---------|----------|
| Unit | ECON-TEST-UNIT | `test_ECON_ROAD_001_roads_connect_adjacent_hexes` |
| Integration | ECON-TEST-INT | `test_ECON_PATH_003_blocked_path_returns_failure_reason` |
| Scenario | ECON-TEST-SCEN | Full chain: timber → planks → tools → building |
| Replay hash | ECON-TEST-REP | Same seed+log → same hash |

---

## 32. Smoke Tests

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| ECON-SMOKE-001 | Build road network between two nodes | Carriers can path |
| ECON-SMOKE-002 | Complete wood → planks chain | Planks in storage |
| ECON-SMOKE-003 | Complete full building construction | Building completed event |
| ECON-SMOKE-004 | Replay 500 ticks | Hash match |

---

## 33. Replay

- **ECON-REP-001:** Command log + initial state + seed replays identically
- **ECON-REP-002:** Replay export/import JSON format documented in architecture docs

---

## 34. Configuration / Data

- Buildings, recipes, terrain, map fixtures in YAML/JSON per `docs/architecture/06_config_and_data_schema.md`
- Scenario files define map, starting entities, victory conditions

---

## 35. Deterministic Scenarios

At least three bundled scenarios: small tutorial map, medium chain test, stall diagnostic test. Each has expected event sequence or hash.

---

## 36. Future Hybrid Compatibility

- Ware types and building footprints must be extensible for military buildings (RUN_07)
- Road network and carriers serve hybrid logistics-fed production
- No economy shortcuts that bypass ware delivery (reserved for hybrid rules in `04_`)

---

## 37. Performance Expectations

- 64×64 hex map, 50 carriers, 30 buildings: tick under 50ms on dev hardware
- Snapshot export under 100ms

---

## 38. Definition of Done (RUN_02)

- [ ] All mechanic families in Section coverage exist in state
- [ ] All required commands validated and tested
- [ ] All event categories emitted in appropriate scenarios
- [ ] All production chains completable in integration test
- [ ] Diagnostics report stall reasons
- [ ] Replay hash test passes
- [ ] Smoke tests ECON-SMOKE-001 through 004 pass
- [ ] No Godot code required for above

---

## 39. Cursor Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
