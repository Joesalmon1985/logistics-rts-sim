# Trial Economy Game Requirements

Binding contract for the **Trial Economy Game** — the permanent abstract economy engine for this project. Not a throwaway prototype.

---

## 1. Purpose

Deliver a headless, deterministic hex-map settlement logistics simulation where players build roads, place buildings, run production chains, and satisfy construction goals using workers, carriers, and ware routing.

The Trial Economy Game is the **foundation** for the Hybrid Game. Military construction and production in hybrid mode will consume wares and logistics rules defined here.

This engine must be **deeper than a small logistics demo**. It is the complete abstract economy layer for the final hybrid game.

---

## 2. Coverage Target

> **Widelands-like mechanic coverage, not Widelands feature parity.**

The Trial Economy Game must include **all major settlement-logistics economy mechanic families** in simplified, original, testable form — the same *kind* of systems Widelands uses for economy (roads, road nodes / player-facing “flags”, carriers, chains, workers, mines, food support, forest management, upgrades), but:

> **Logistics foundation (ratified DDR-LOG-001–012):** Road **nodes** are authoritative simulation entities; **flags** are player-facing aliases, not separate entities. Building access uses configurable **`ports[]`** (default `[[0, 0]]`). Carriers are assigned **per delivery job**. Specialist workers are **abstract slots** in v1. Construction: **material delivery then abstract `builder_work`**. Ware transit is **carrier-held** in v1; road-node buffer staging is reserved/future. See `15_worker_carrier_road_node_logistics_foundation_RATIFIED.md`.

- Original placeholder names, data, and balance
- No copied Widelands tribes, campaigns, maps, UI, art, or identity
- No requirement to match Widelands content breadth (tribes, ports, military territory, etc.)

A mechanic may be **simple**, but it must **not be a placeholder**. It must affect state, emit events where relevant, appear in diagnostics, and have tests.

---

## 3. Non-Goals (Trial Economy)

The Trial Economy Game does **not** need:

- Multiple Widelands-style tribes
- Campaigns or map editor
- Multiplayer
- Ports, ships, overseas trade, inter-player trade
- Military, combat, unit attack logic, territory conquest via military buildings
- Neural-network training (RUN_06)
- Godot scenes or final art (RUN_05)
- Copied Widelands economy data, names, balance, UI, or art

Also excluded (other documents / runs):

- Hybrid integration rules (`04_hybrid_game_requirements.md`)
- Military trial systems (`03_trial_military_game_requirements.md`)

Data and schema **hooks** must remain so military, hybrid, multiplayer, and content expansion can attach later without rewriting the economy core.

---

## 4. Originality Rules

- Use original placeholder building and ware names from `05_units_resources_buildings_catalogue.md` and `10_glossary.md`
- Do not copy protected names, exact chains, or iconic identity from Widelands or other reference games
- Rules may follow **genre conventions** (logistics settlement games); data and naming must be **original**

---

## 5. Simulation Authority

- All economy rules execute in the authoritative **GDScript simulation core** only
- The Godot visual client/UI displays snapshots; it does not compute production, pathfinding, or ware movement
- Commands are validated and applied in the core; events record all state changes
- Authoritative state uses integers / fixed-point (no floats in hashed state; DDR-027)
- The economy uses the **shared axial hex map** from the RUN_01 kernel (DDR-029)
- See `docs/architecture/01_simulation_core_authoritative.md`

---

## 6. Determinism

- **ECON-DET-001:** All randomness uses seeded RNG stored in simulation state
- **ECON-DET-002:** Same seed + same command log → identical events and snapshot hash
- **ECON-DET-003:** Replay re-execution must pass hash verification

---

## 7. Tick Loop

- **ECON-TICK-001:** Simulation advances in discrete ticks
- **ECON-TICK-002:** Each tick processes: command queue → economy subsystems → event emission
- **ECON-TICK-003:** Tick number is monotonic and included in events and snapshots
- Subsystem order within a tick is fixed and documented (forest → deposits → worker requests → production → construction → upgrades → carriers → diagnostics → statistics)

---

## 8. Success and Failure States

| State | Condition | Rule ID |
|-------|-----------|---------|
| Economy success | Player completes configured victory building or ware quota | ECON-WIN-001 |
| Economy stalled | No progress possible for N ticks (configurable) with diagnostic reason | ECON-STALL-001 |
| Scenario timeout | Max ticks reached without success | ECON-TIME-001 |

Diagnostics must report stall reasons including: missing input, blocked output, no path, storage full, worker shortage, tool shortage, food shortage, forest depletion, deposit depletion, road congestion.

---

## 9. Hex Map (Axial Coordinates)

- **ECON-MAP-001:** Map uses axial coordinates `(q, r)` with standard hex neighbour definitions
- **ECON-MAP-002:** Map bounds are rectangular in axial space; out-of-bounds queries fail gracefully
- **ECON-MAP-003:** Each hex has terrain, height, tree density (where applicable), owner, and optional road/building references
- **ECON-MAP-004:** Snapshots export full map dimensions and per-hex display data

---

## 10. Terrain

- **ECON-TERR-001:** Terrain types include at minimum: grass, water, rock, forest, fertile
- **ECON-TERR-002:** Terrain affects buildability, road legality, and movement cost
- **ECON-TERR-003:** Water hexes are not buildable except for designated water-source buildings (well)

---

## 11. Height and Slope

- **ECON-SLOPE-001:** Each hex has integer height
- **ECON-SLOPE-002:** Slope between adjacent hexes = `abs(height_a - height_b)`
- **ECON-SLOPE-003:** Slope above threshold blocks road placement or increases path cost (configurable)
- **ECON-SLOPE-004:** Height exported in snapshot for 2D shading in Godot

---

## 12. Buildability

- **ECON-BUILD-001:** Placement checks terrain, slope, footprint overlap, and ownership
- **ECON-BUILD-002:** Failed placement returns structured rejection reason in command result

---

## 13. Display / Godot-Readiness (Snapshot Layers)

RUN_02 is headless, but snapshots must export enough display state for RUN_05 to visualise without reverse-engineering the sim. Godot **must not** own any of these rules — it only displays exported state.

| Layer | Snapshot field / content |
|-------|--------------------------|
| Hex terrain | terrain type per hex |
| Height / slope / buildability overlay | height, slope, buildable flag |
| Resource overlay | deposits, depletion state |
| Tree-density overlay | tree_density per forest hex |
| Ownership / build-radius overlay | player ownership, buildable territory |
| Road layer | road segments, quality if applicable |
| Road node layer | node positions; optional inventory capacity (reserved schema; active staging not v1) |
| Building layer | completed buildings |
| Construction site layer | plans, progress, missing wares |
| Worker / carrier markers | positions, assignments, load |
| Ware movement markers | in-transit jobs |
| Blocked job markers | blocked delivery/construction |
| Diagnostics overlay | stall flags, reasons |
| Optional hex grid | toggle metadata (Godot default: hidden on mobile) |
| Selected entity details | entity id, status, diagnostics (on request) |

- **ECON-SNAP-001:** Snapshot is JSON-compatible with `schema_version`
- **ECON-SNAP-002:** All layers above populated from sim state (empty arrays where none)
- **ECON-SNAP-003:** Optional hex grid overlay toggled in Godot only

See `06_mobile_2d_visual_design.md` for rendering order.

---

## 14. Player Ownership

- **ECON-OWN-001:** Each hex, building, and road belongs to at most one player
- **ECON-OWN-002:** Commands affect only the issuing player's entities unless scenario rules say otherwise

---

## 15. Resources and Wares

- **ECON-WARE-001:** Wares are typed, countable, and transportable
- **ECON-WARE-002:** Wares exist in building/site storage, **in transit on carriers** (v1 authoritative transit), or consumed by production/construction. Road-node inventory capacity may exist as **reserved schema**; active ware staging in node buffers is **not** v1 behaviour (DDR-LOG-006)

**Minimum ware set (Run 02):**

| Ware | Role |
|------|------|
| timber | Raw wood |
| planks | Processed wood |
| stone | Construction |
| grain, water | Food chain inputs |
| bread | Basic food product |
| rations | Basic mine food support |
| meals | Upgraded mine food support |
| coal, iron_ore, iron_ingots | Metal chain |
| tools | Generic tools (Run 02); schema supports tool categories later |
| builder_work | Construction labour |

- **ECON-DEP-001:** Deposits (forest, rock, coal, iron, fertile, water source) are map features with extraction and depletion rules

---

## 16. Required Production Chains

| Chain | Rule ID |
|-------|---------|
| forest/tree density → woodcutter → timber | ECON-CHAIN-001 |
| timber → sawmill → planks | ECON-CHAIN-002 |
| rock/deposit → quarry → stone | ECON-CHAIN-003 |
| fertile land → farm → grain | ECON-CHAIN-004 |
| water source → well → water | ECON-CHAIN-005 |
| grain + water → bakery → bread | ECON-CHAIN-006 |
| bread (or grain+water) → rations processing → rations | ECON-CHAIN-012 |
| rations + additional inputs → meals | ECON-CHAIN-013 |
| coal deposit → coal mine → coal | ECON-CHAIN-007 |
| iron deposit → iron mine → iron ore | ECON-CHAIN-008 |
| iron ore + coal → smelter → iron ingots | ECON-CHAIN-009 |
| iron ingots + planks → workshop → tools | ECON-CHAIN-010 |
| planks + stone + tools + builder work → completed building | ECON-CHAIN-011 |

Each chain must be testable in isolation and as an integrated scenario.

**Modularity (DDR-LOG-007):** Wares, buildings, and recipes are config-driven. RUN_02.7 must prove at least one **config-only** new ware/recipe/building chain (e.g. bricks) without simulation-code ware enum changes.

---

## 17. Buildings

- **ECON-BLD-001:** Buildings have footprints, configurable **`ports[]`** entrance offsets (road node links at each port; default `[[0, 0]]` when omitted), worker slots, enable/disable state, and optional upgrade targets. RUN_02.7 implements full `ports[]` routing
- **ECON-BLD-002:** Building types match catalogue in `05_units_resources_buildings_catalogue.md`

**Minimum building set includes:** Headquarters, Storehouse, Woodcutter, Forester, Sawmill, Quarry, Farm, Well, Bakery, Rations kitchen, Meal hall, Coal mine, Iron mine, Deep coal mine, Deep iron mine, Smelter, Workshop, Storage yard.

---

## 18. Building Placement

- **ECON-PLACE-001:** `PlaceBuildingPlanCommand` creates a construction site at valid footprint
- **ECON-PLACE-002:** Plan shows required wares before construction starts

---

## 19. Warehouse / HQ Worker Provisioning

Workers are **not** freely assignable without supply. Simplified original rules:

- **ECON-WORKER-001:** Production buildings request required worker types (e.g. woodcutter, miner, carrier operator)
- **ECON-WORKER-002:** Workers are supplied from **Headquarters** or **Storehouses** connected by road network
- **ECON-WORKER-003:** Missing workers pause production with clear diagnostic
- **ECON-WORKER-004:** Some worker types require tools before assignment (see §20)
- **ECON-WORKER-005:** Worker assignment, release, and shortage emit events and appear in diagnostics

Player commands may influence policy (`AssignWorkerCommand`, `SetWorkerPolicyCommand`) but cannot bypass availability rules.

---

## 20. Tool-Based Worker Availability

Run 02 uses generic `tools` ware. Schema must support future tool categories without rewrite:

- `cutting_tool`, `digging_tool`, `building_tool`, `farming_tool`, `mining_tool`, `smithing_tool` (future)

- **ECON-TOOL-001:** Specialist workers may require tools (configurable per worker type)
- **ECON-TOOL-002:** Missing tools block worker creation or assignment
- **ECON-TOOL-003:** Tool requirements are configurable by worker type in data files
- **ECON-TOOL-004:** Tool shortages appear in diagnostics and emit events

Run 02 minimum: assigning a miner or woodcutter consumes or reserves `tools` per config.

---

## 21. Forest Management

Forest is dynamic, not a static infinite deposit.

- **ECON-FOREST-001:** Forest hexes have `tree_density` (integer 0–100 or equivalent)
- **ECON-FOREST-002:** Woodcutters reduce tree density on worked hexes when producing timber
- **ECON-FOREST-003:** Foresters increase tree density over time on adjacent forest hexes
- **ECON-FOREST-004:** Low tree density reduces or blocks timber production
- **ECON-FOREST-005:** Forest depletion and renewal emit events and appear in diagnostics/overlays

---

## 22. Food Tiers and Miner Support

Food is functional production support, not decorative output.

- **ECON-FOOD-001:** Food wares (`bread`, `rations`, `meals`) can be consumed as production support
- **ECON-FOOD-002:** Mines require food delivery to operate
- **ECON-FOOD-003:** Basic mines accept `rations`; upgraded/deep mines require `meals` (configurable)
- **ECON-FOOD-004:** Missing food pauses mine production
- **ECON-FOOD-005:** Food shortages appear in diagnostics

Food delivery uses the normal carrier logistics system.

---

## 23. Mine Depth / Mine Upgrade Tiers

- **ECON-MINE-001:** Mines require matching revealed deposits on map hexes
- **ECON-MINE-002:** Mines consume food support while producing
- **ECON-MINE-003:** Deposits deplete over time; output reduces or stops when exhausted
- **ECON-MINE-004:** Mines have at least **basic** and **upgraded/deep** tier (via upgrade or separate building type)
- **ECON-MINE-005:** Higher-tier mines require higher-tier food and/or more inputs; produce more efficiently or access deeper deposit reserves
- **ECON-MINE-006:** Depletion and missing support appear in diagnostics

---

## 24. Building Upgrades / Enhancements

- **ECON-UPGRADE-001:** Some completed buildings have configured upgrade targets (e.g. sawmill → enhanced sawmill, coal mine → deep coal mine)
- **ECON-UPGRADE-002:** Upgrades require delivered materials and builder work
- **ECON-UPGRADE-003:** Upgrades use construction/logistics system (construction site at same footprint)
- **ECON-UPGRADE-004:** Upgraded buildings expose changed capabilities (speed, storage, slots, recipes)
- **ECON-UPGRADE-005:** Upgrade progress and completion emit events

Command: `UpgradeBuildingCommand` (target building id, upgrade target type).

---

## 25. Roads, Slope, and Capacity

Roads are not a thin connected/not-connected graph only.

- **ECON-ROAD-001:** Roads connect adjacent legal hexes over hex paths/edges
- **ECON-ROAD-002:** Terrain affects road legality
- **ECON-ROAD-003:** Slope affects road legality or movement cost
- **ECON-ROAD-004:** Road nodes expose inventory **capacity schema** for future junction staging; **v1 logistics does not actively stage wares in node buffers** — transit is carrier-held (DDR-LOG-006). Tests may verify capacity API separately from live logistics staging
- **ECON-ROAD-005:** Carriers transport wares along road network edges node-by-node on assigned routes
- **ECON-ROAD-006:** Road capacity or congestion is represented (at minimum in diagnostics; may limit concurrent carriers per segment)
- **ECON-ROAD-007:** Blocked routes appear in diagnostics with reason

`RemoveRoadCommand` remains valid where no blocking constraints exist.

Road nodes (**ECON-NODE-001**): at least one road node at each configured building **port** (entrance); network must be connected for delivery. Large footprints use `ports[]` offsets from anchor (DDR-LOG-002).

Pathfinding (**ECON-PATH-001**–**003**): carriers pathfind on road-node graph; specialist workers use reachability only (no physical pathing in v1); slope may affect cost; blocked paths emit events.

Carriers (**ECON-CARR-001**–**002**, DDR-LOG-003): assigned **per delivery job** in v1; one ware type per carrier; configurable edge speed.

Delivery jobs (**ECON-JOB-001**–**003**): explicit entities with source, destination, ware, quantity, status, `block_reason`.

---

## 26. Logistics and Delivery Jobs

- **ECON-JOB-001:** When a site needs wares, a delivery job is created
- **ECON-JOB-002:** Jobs assigned to idle carriers by priority
- **ECON-JOB-003:** Job lifecycle: created → assigned → in_transit → delivered | blocked

---

## 27. Carriers

- **ECON-CARR-001:** Carriers carry one ware type at a time (simplified)
- **ECON-CARR-002:** Carriers move along road edges per tick (configurable speed)

---

## 28. Storage

- **ECON-STOR-001:** Storehouses and HQ hold wares and available workers up to capacity
- **ECON-STOR-002:** Storage full emits event and blocks input delivery

---

## 29. Construction Sites

- **ECON-CONS-001:** Construction requires **delivered wares first**; then abstract **builder work** ticks until complete (DDR-LOG-005). No site-assigned builder worker entities in v1
- **ECON-CONS-002:** Progress events emitted each tick while active
- **ECON-CONS-003:** Completion spawns functional building entity

---

## 30. Economy Priorities

- **ECON-PRI-001:** `SetConstructionPriorityCommand` orders competing construction sites
- **ECON-PRI-002:** `SetProductionPriorityCommand` orders production at buildings
- **ECON-PRI-003:** `SetWarePriorityCommand` orders ware routing when carriers are scarce

---

## 31. Economy Statistics and Diagnostics

Required diagnostic fields (snapshot + exportable):

| Category | Content |
|----------|---------|
| Ware totals | by type, by storage location |
| Production | building status, paused reasons |
| Construction | active sites, blocked sites, missing wares |
| Delivery | active jobs, blocked jobs |
| Utilisation | carrier utilisation, worker utilisation |
| Network | road congestion, connectivity gaps |
| Resources | deposit depletion, forest tree density summary |
| Shortages | food, tools, workers |
| Routing | no-route problems |
| Progress | economy progress score toward victory |
| Stall | primary stall reason |

- **ECON-DIAG-001:** Per-building and per-site diagnostic records
- **ECON-DIAG-002:** Global economy health summary in snapshot
- **ECON-DIAG-003:** Diagnostics update when state changes
- **ECON-DIAG-004:** Statistics exportable to CSV (see §36)

---

## 32. Required Commands

| Command | Rule ID | Description |
|---------|---------|-------------|
| PlaceRoadCommand | ECON-CMD-ROAD-001 | Place road segment |
| RemoveRoadCommand | ECON-CMD-ROAD-002 | Remove road segment |
| PlaceBuildingPlanCommand | ECON-CMD-BLD-001 | Place building plan |
| CancelConstructionCommand | ECON-CMD-BLD-002 | Cancel construction |
| UpgradeBuildingCommand | ECON-CMD-BLD-003 | Start building upgrade |
| EnableBuildingCommand | ECON-CMD-BLD-004 | Enable production |
| DisableBuildingCommand | ECON-CMD-BLD-005 | Disable production |
| SetConstructionPriorityCommand | ECON-CMD-PRI-001 | Construction priority |
| SetProductionPriorityCommand | ECON-CMD-PRI-002 | Production priority |
| SetWarePriorityCommand | ECON-CMD-PRI-003 | Ware routing priority |
| AssignWorkerCommand | ECON-CMD-WORK-001 | Request/policy for worker assignment |
| SetWorkerPolicyCommand | ECON-CMD-WORK-002 | Auto/manual worker provisioning policy |

All commands return: `{ accepted, reason, events[] }`.

---

## 33. Required Event Categories

| Category | Rule ID |
|----------|---------|
| game_created | ECON-EVT-001 |
| tick_advanced | ECON-EVT-002 |
| road_placed / removed / blocked / congested | ECON-EVT-003 |
| building_plan_placed | ECON-EVT-004 |
| construction_started / progressed / completed | ECON-EVT-005 |
| upgrade_started / progressed / completed | ECON-EVT-017 |
| delivery_job_created / assigned / blocked | ECON-EVT-006 |
| ware_reserved / picked_up / moved / delivered / consumed | ECON-EVT-007 |
| worker_assigned / released / unavailable | ECON-EVT-008 |
| production_started / paused / completed | ECON-EVT-009 |
| resource_extracted / depleted / renewed | ECON-EVT-010 |
| tree_density_changed / forest_depleted / forest_renewed | ECON-EVT-018 |
| food_consumed / food_missing | ECON-EVT-019 |
| tool_consumed / tool_missing | ECON-EVT-020 |
| storage_full | ECON-EVT-011 |
| input_missing | ECON-EVT-012 |
| output_blocked | ECON-EVT-013 |
| economy_stalled | ECON-EVT-014 |
| economy_success_achieved | ECON-EVT-015 |
| snapshot_exported | ECON-EVT-016 |

---

## 34. Bots (Run 02 minimum)

- **ECON-BOT-001:** Heuristic economy bot uses same command API as humans
- **ECON-BOT-002:** Bot must complete ECON-SMOKE-005 (workshop via full logistics chain)

Formal bot framework expansion is RUN_04; Run 02 includes minimal heuristic bot for smoke proof.

---

## 35. Tests

| Category | Rule ID | Examples |
|----------|---------|----------|
| Unit | ECON-TEST-UNIT | `test_ECON_ROAD_001_roads_connect_adjacent_hexes` |
| Integration | ECON-TEST-INT | `test_ECON_WORKER_003_missing_worker_pauses_production` |
| Scenario | ECON-TEST-SCEN | One test per bundled scenario map (§37) |
| Replay hash | ECON-TEST-REP | Same seed+log → same hash |

Every mechanic family (rule-ID prefix) in §19–§31 must have at least one representative row in `docs/testing/01_rule_contract_test_matrix.md`. During RUN_02, every individual rule ID in §19–§31 must additionally gain at least one test (added to the matrix and/or the test suite); the matrix lists representative rows and is expanded during implementation without removing existing rows.

---

## 36. Data Exports

In addition to snapshot JSON and event log (JSON or JSONL):

| Export | Content |
|--------|---------|
| production_status.csv | Per-building production state |
| construction_status.csv | Active/blocked construction sites |
| delivery_jobs.csv | Active/blocked jobs |
| resource_status.csv | Deposits, forest density summary |
| worker_carrier_status.csv | Utilisation, assignments |
| bot_decision.csv | Heuristic bot commands per tick (when bot active) |

---

## 37. Deterministic Scenarios (Required)

Each scenario is a bundled map fixture with seed, victory/stall config, and expected hash or event assertions:

| Scenario | Purpose |
|----------|---------|
| `tiny_success_map` | Minimal win path |
| `blocked_route_map` | No-route / blocked delivery diagnostics |
| `missing_resource_map` | Missing deposit/input stall |
| `priority_test_map` | Construction/ware priority ordering |
| `storage_full_map` | Storage full blocks chain |
| `pathfinding_test_map` | Multi-node path selection |
| `forest_depletion_regrowth_map` | ECON-FOREST-* behaviour |
| `mine_food_support_map` | ECON-FOOD-* and ECON-MINE-* |
| `tool_shortage_worker_block_map` | ECON-TOOL-* and ECON-WORKER-* |
| `building_upgrade_map` | ECON-UPGRADE-* |

---

## 38. Smoke Tests

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| ECON-SMOKE-001 | Road network with slope/terrain | Carriers path; ECON-ROAD-* satisfied |
| ECON-SMOKE-002 | Timber → planks chain | Planks in storage |
| ECON-SMOKE-003 | Full building construction | construction_completed |
| ECON-SMOKE-004 | 500-tick replay | Hash match |
| ECON-SMOKE-005 | Heuristic bot → completed **Workshop** | Full logistics chain (see below) |

**ECON-SMOKE-005** must require actual:

- Road placement and road nodes
- Carriers and ware movement
- Timber/planks, stone, iron/coal/ingots chains
- Tool production
- Construction site with delivered materials and builder work
- Worker/tool provisioning from HQ/Storehouse
- Diagnostics, event log, snapshot export

---

## 39. Replay

- **ECON-REP-001:** Command log + initial state + seed replays identically
- **ECON-REP-002:** Replay export/import JSON documented in architecture docs

---

## 40. Configuration / Data

- Buildings, recipes, worker types, tool requirements, upgrades, terrain, maps in YAML/JSON per `docs/architecture/06_config_and_data_schema.md`
- Tool category schema optional in Run 02; generic `tools` required
- Scenario files reference map fixtures in §37

---

## 41. Future Hybrid Compatibility

- Ware types, worker provisioning, and road logistics extensible for military buildings (RUN_07)
- No economy shortcuts that bypass ware delivery
- HQ/Storehouse model supports hybrid military construction queues

---

## 42. Performance Expectations

- 64×64 hex map, 50 carriers, 40 buildings: tick under 50ms on dev hardware
- Snapshot export under 100ms
- CSV exports under 200ms

---

## 43. Definition of Done (RUN_02)

> Run 02 is complete only when **every major economy mechanic family** is implemented in simplified, original, deterministic, testable form. A mechanic may be simple, but it must not be a placeholder. It must affect state, emit events where relevant, appear in diagnostics, and have tests.

Checklist:

- [ ] Widelands-like coverage families §19–§31 all implemented (not Widelands parity)
- [ ] All production chains §16 completable
- [ ] All commands §32 validated and tested
- [ ] All event categories §33 emitted in appropriate scenarios
- [ ] All ten scenarios §37 pass with deterministic hashes where configured
- [ ] All smoke tests §38 including ECON-SMOKE-005 pass
- [ ] Statistics/diagnostics §31 and exports §36 functional
- [ ] Snapshot layers §13 complete for Godot-readiness
- [ ] Replay hash verification passes
- [ ] No military, combat, multiplayer, campaigns, or ports in RUN_02
- [ ] No Godot visual-client/UI/scene/rendering code in RUN_02 (the economy is GDScript **simulation-core** code, headless; UI is RUN_05)

---

## 44. Cursor Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
