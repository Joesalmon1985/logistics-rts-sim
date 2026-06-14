# Trial Military Game Requirements

Binding contract for the **Trial Military Game** — a standalone RTS base-building and combat trial. Direct unit control is allowed here; the Hybrid Game restricts this (see `04_hybrid_game_requirements.md`).

---

## 1. Purpose

Deliver a headless, deterministic RTS trial with resource harvesting, base building, power management, production queues, combat, intel/fog, repair, tech, skirmish setup, and defeat conditions. Uses credits-based production (trial mode) without economy logistics.

This engine must be **deeper than a small RTS demo**. It is the complete abstract military layer for the final hybrid game.

---

## 2. OpenRA-like RTS Coverage Target

> **OpenRA-like RTS mechanic coverage, not OpenRA feature parity.**

The Trial Military Game should cover major OpenRA-like RTS mechanic families in simplified, original, deterministic, testable form. It should **not** attempt OpenRA feature parity or copy OpenRA content.

Major families to cover (simplified originals):

| Family | Notes |
|--------|-------|
| Resource fields / harvesting | Abstract resource nodes; collector units |
| Base construction | Placement, footprint, construction queue, build radius |
| Power supply / demand | Generators, underpowered behaviour |
| Production queues | Building construction + unit production |
| Tech tree / prerequisites | Research lab unlocks buildings and units |
| Radar / fog / shroud / vision | Per-player intel layers for bots and Godot |
| Unit movement / pathfinding | Terrain modifiers, obstacles |
| Direct unit orders (trial only) | Move, attack, stop, attack-move, patrol, guard |
| Combat | Range, cooldown, projectile or instant-hit abstraction |
| Damage / armour / target types | Type modifiers (infantry, vehicle, structure, etc.) |
| Repairs | Repair depot heals units and buildings |
| Static defences | Turret targeting and range |
| Skirmish / map generation hooks | Seeded procedural map parameters |
| Replay / snapshot / diagnostics | Deterministic verification and failure analysis |
| Optional: capture/sabotage, veterancy, special powers | Simplified; may defer to later runs |

A mechanic may be **simple**, but it must **not be a placeholder**. It must affect state, emit events where relevant, appear in diagnostics, and have tests.

---

## 3. Non-Goals

- Economy ware chains, carriers, or road logistics (see `02_trial_economy_game_requirements.md`)
- Hybrid logistics-fed production (see `04_`)
- Godot rendering (RUN_05)
- Delegated-only military control (that is hybrid scope)
- Copying OpenRA, C&C, or Red Alert units, names, traits, YAML, maps, missions, UI, or exact balance
- Dozens of factions, units, or campaign missions (content breadth is RUN_11)

---

## 4. Originality Rules

Original placeholder names from `05_units_resources_buildings_catalogue.md`. Genre RTS conventions OK; protected identity is not.

---

## 5. Simulation Authority

All military rules execute in the authoritative **GDScript simulation core**. Commands validated centrally. Events for every state change. Snapshots for the Godot visual client. Authoritative state uses integers / fixed-point (no floats in hashed state; DDR-027). The military trial uses the **shared axial hex map** from the RUN_01 kernel — no separate square grid or parallel passability/map system (DDR-029); passability, range, vision, placement, attack range, movement, and pathfinding all derive from the shared hex map.

---

## 6. Determinism

- **MIL-DET-001:** Seeded RNG in state for combat variance (if any)
- **MIL-DET-002:** Same seed + commands → identical combat outcomes and replay hash

---

## 7. Resource Fields and Harvesting (MIL-ECON)

- **MIL-ECON-001:** Map contains resource fields or resource nodes (abstract ore/mineral patches; original naming)
- **MIL-ECON-002:** Resource nodes deplete over time when harvested (configurable rate)
- **MIL-ECON-003:** Collector unit (harvester equivalent) gathers from adjacent resource node
- **MIL-ECON-004:** Collector returns cargo to resource processor; emits harvest/delivery events
- **MIL-ECON-005:** Resource processor converts delivered resource to credits (trial simplification)
- **MIL-ECON-006:** Without processor + collector loop, credits stall; diagnostics flag stalled economy

---

## 8. Credits / Resource Stock (MIL-CRED)

- **MIL-CRED-001:** Players hold credits for trial-mode production and research
- **MIL-CRED-002:** Credits consumed by construction, production, research, and repair
- **MIL-CRED-003:** Credit balance exported per player in snapshot

---

## 9. Base Construction and Buildability (MIL-BUILD)

- **MIL-BUILD-001:** Player builds structures on buildable map tiles
- **MIL-BUILD-002:** Structures have placement rules, footprints, health, and owner
- **MIL-BUILD-003:** Construction queue: placed buildings enter queue; complete over ticks for credits
- **MIL-BUILD-004:** Building prerequisites: some structures require tech unlock or existing building
- **MIL-BUILD-005:** Construction radius: new buildings must be within range of command hub or powered structure (configurable)
- **MIL-BUILD-006:** Overlapping footprints and invalid terrain rejected with reason
- **MIL-BUILD-007:** Cancel construction returns partial credits (configurable)

---

## 10. Required Buildings

| Building | Rule ID | Role |
|----------|---------|------|
| Command hub | MIL-HUB-001 | Primary base; destruction = defeat |
| Power generator | MIL-POWER-001 | Supplies power capacity |
| Resource processor | MIL-PROC-001 | Converts harvested resource to credits |
| Barracks | MIL-PROD-001 | Produces infantry |
| Vehicle plant | MIL-PROD-002 | Produces vehicles |
| Radar tower | MIL-RADAR-001 | Extends vision/intel range |
| Research lab | MIL-TECH-001 | Unlocks tech |
| Repair depot | MIL-REPAIR-001 | Repairs units and buildings |
| Defence turret | MIL-TUR-001 | Static defence |

Placeholder names may change; IDs remain stable.

---

## 11. Power Supply and Demand (MIL-POWER)

- **MIL-POWER-002:** Each building has power demand; generators supply capacity
- **MIL-POWER-003:** Underpowered buildings cannot produce, attack, research, or repair
- **MIL-POWER-004:** Powered/unpowered state in snapshot and events
- **MIL-POWER-005:** Low power reported in diagnostics with affected building list

---

## 12. Production Queues (MIL-QUEUE)

- **MIL-QUEUE-001:** Barracks and vehicle plant maintain ordered unit production queues
- **MIL-QUEUE-002:** Queue items consume credits and ticks to complete
- **MIL-QUEUE-003:** Cancel queue item returns partial credit (configurable)
- **MIL-QUEUE-004:** Building construction queue separate from unit production queue
- **MIL-QUEUE-005:** Queue blocked when unpowered, missing prerequisites, or insufficient credits

---

## 13. Units and Target Types (MIL-UNIT, MIL-DAMAGE)

| Type | Rule ID | Target type | Notes |
|------|---------|-------------|-------|
| Rifle squad (infantry) | MIL-UNIT-001 | infantry | Foot movement, light armour |
| Rocket squad (infantry) | MIL-UNIT-002 | infantry | Anti-armour modifier |
| Scout | MIL-UNIT-003 | infantry | Extended sight range |
| Engineer | MIL-UNIT-004 | infantry | Repair/capture capable (optional capture) |
| Light vehicle | MIL-UNIT-005 | vehicle | Fast scout/combat |
| Tank | MIL-UNIT-006 | vehicle | Heavy armour |
| Artillery | MIL-UNIT-007 | vehicle | Long range, slow |
| Collector | MIL-UNIT-008 | vehicle | Harvests resource nodes |

Buildings use target type **structure**. Turrets use **structure** with weapon rules.

- **MIL-DAMAGE-001:** Weapons declare valid target types (infantry, vehicle, structure, air hook)
- **MIL-DAMAGE-002:** Armour class and damage type modifiers applied deterministically
- **MIL-DAMAGE-003:** Invalid target types not auto-selected unless weapon allows

Placeholder unit names in catalogue; no reference-game names.

---

## 14. Movement and Pathfinding (MIL-MOVE)

- **MIL-MOVE-001:** Units pathfind on the **shared axial hex map** from the RUN_01 kernel (terrain/height as passability inputs); no separate or parallel grid (DDR-029)
- **MIL-MOVE-002:** Direct move/attack commands allowed in trial mode
- **MIL-MOVE-003:** Movement cost respects terrain modifiers and obstacles
- **MIL-MOVE-004:** Impassable terrain blocks movement; diagnostics on path failure
- **MIL-MOVE-005:** Collectors path to nearest resource node and return to processor

---

## 15. Unit Orders — Trial Direct Control (MIL-ORDER)

Direct unit micro is **required in trial mode** and **forbidden for human players in hybrid mode**.

| Order | Rule ID | Behaviour |
|-------|---------|-----------|
| MoveUnitCommand | MIL-ORDER-001 | Move to hex; pathfind |
| AttackUnitCommand | MIL-ORDER-002 | Attack specific target |
| AttackMoveCommand | MIL-ORDER-003 | Move to hex; engage hostiles en route and at destination |
| PatrolCommand | MIL-ORDER-004 | Loop between two waypoints; engage hostiles |
| GuardCommand | MIL-ORDER-005 | Stay near guard target; engage nearby hostiles |
| StopUnitCommand | MIL-ORDER-006 | Halt current order |

- **MIL-ORDER-007:** Order queue per unit (optional single active order for trial simplification)
- **MIL-ORDER-008:** Invalid orders rejected (target out of intel, dead unit, etc.)

---

## 16. Combat (MIL-COMBAT)

- **MIL-COMBAT-001:** Units and turrets have weapon range, damage, rate of fire (cooldown), and hit points
- **MIL-COMBAT-002:** Projectile or instant-hit abstraction configurable per weapon (no Godot physics)
- **MIL-COMBAT-003:** Cooldown prevents firing until elapsed ticks complete
- **MIL-COMBAT-004:** Target selection: nearest valid hostile in range unless commanded
- **MIL-COMBAT-005:** Building damage and destruction with events
- **MIL-COMBAT-006:** Unit destruction removes entity and emits event
- **MIL-COMBAT-007:** Static defence turret auto-targets by priority policy (nearest valid in range)

---

## 17. Fog, Shroud, Vision, and Scouting (MIL-FOG, MIL-VISION)

- **MIL-FOG-001:** Fog of war: unexplored, explored (shroud), visible layers per player
- **MIL-FOG-002:** Shroud hides enemy units/buildings outside current vision
- **MIL-FOG-003:** Explored terrain remains visible as shroud (no unit info) after leaving sight
- **MIL-FOG-004:** Intel state exported per player in snapshot for bots and Godot display
- **MIL-VISION-001:** Units reveal area based on sight stat
- **MIL-VISION-002:** Radar tower extends sight range for owner
- **MIL-VISION-003:** Scout units have bonus sight range
- **MIL-VISION-004:** Fog/shroud settings configurable per scenario (enabled/disabled for tests)

---

## 18. Repair (MIL-REPAIR)

- **MIL-REPAIR-002:** Repair depot heals nearby units and buildings for credits over ticks
- **MIL-REPAIR-003:** Repair emits progress events
- **MIL-REPAIR-004:** Repair requires power and credits; stalls when either missing
- **MIL-REPAIR-005:** Engineer unit may repair in field (simplified; optional in RUN_03)

---

## 19. Tech Unlocks and Prerequisites (MIL-TECH)

- **MIL-TECH-002:** Research lab consumes credits/ticks to unlock tech entries
- **MIL-TECH-003:** Unlocks gate unit types, building types, or stat upgrades
- **MIL-TECH-004:** Building and unit production require prerequisite tech or structures
- **MIL-TECH-005:** Missing prerequisite rejects queue/place with reason

---

## 20. Optional Mechanics (MIL-CAPTURE, MIL-VET, MIL-SPECIAL)

Optional simplified mechanics — implement in RUN_03 if low cost, else defer to RUN_04/RUN_11:

- **MIL-CAPTURE-001:** Engineer may capture damaged hostile structure (simplified sabotage/capture)
- **MIL-VET-001:** Units gain veterancy levels after combat; minor stat bonus (configurable)
- **MIL-SPECIAL-001:** Schema hook for future original special-power building (not required RUN_03)

---

## 21. Defeat and Victory Conditions (MIL-DEFEAT)

- **MIL-DEFEAT-001:** Command hub destroyed → player defeated
- **MIL-DEFEAT-002:** Last player standing wins (multi-player skirmish scenarios)
- **MIL-DEFEAT-003:** Optional stall detection: no progress N ticks → draw/diagnostic (skirmish tests)

---

## 22. Skirmish Setup and Map Generation (MIL-MAPGEN)

- **MIL-MAPGEN-001:** Skirmish scenario config: players, starting positions, starting units/buildings
- **MIL-MAPGEN-002:** Seeded random map generation hooks: biome, resource field placement, player spawns (deterministic from seed)
- **MIL-MAPGEN-003:** Generated maps usable in skirmish smoke tests and bot scenarios
- **MIL-MAPGEN-004:** Fixed scenario fixtures also supported for regression tests

---

## 23. Configuration and Scenario Hooks (MIL-CONFIG, MIL-SCEN)

- **MIL-CONFIG-001:** Buildings, units, weapons, tech tree, terrain modifiers in YAML/JSON per `docs/architecture/06_config_and_data_schema.md`
- **MIL-CONFIG-002:** Mod/data pack hook: variant loads overlay config without code changes
- **MIL-SCEN-001:** Scenario files reference map fixtures and win/loss rules
- **MIL-SCEN-002:** Campaign mission hook schema (content in RUN_11; schema in RUN_03)

---

## 24. Required Commands (Trial — Direct Control Allowed)

| Command | Rule ID |
|---------|---------|
| PlaceBuildingCommand | MIL-CMD-BLD-001 |
| CancelConstructionCommand | MIL-CMD-BLD-002 |
| SellBuildingCommand | MIL-CMD-BLD-003 |
| QueueUnitCommand | MIL-CMD-PROD-001 |
| CancelProductionCommand | MIL-CMD-PROD-002 |
| MoveUnitCommand | MIL-CMD-UNIT-001 |
| AttackUnitCommand | MIL-CMD-UNIT-002 |
| AttackMoveCommand | MIL-CMD-UNIT-003 |
| PatrolCommand | MIL-CMD-UNIT-004 |
| GuardCommand | MIL-CMD-UNIT-005 |
| StopUnitCommand | MIL-CMD-UNIT-006 |
| StartResearchCommand | MIL-CMD-TECH-001 |
| RepairTargetCommand | MIL-CMD-REP-001 |

---

## 25. Required Event Categories

| Category | Rule ID |
|----------|---------|
| military_game_created | MIL-EVT-001 |
| tick_advanced | MIL-EVT-002 |
| building_placed / construction_started / completed / cancelled | MIL-EVT-003 |
| power_state_changed | MIL-EVT-004 |
| production_queued / started / completed / cancelled | MIL-EVT-005 |
| unit_spawned / moved / attacked / damaged / destroyed | MIL-EVT-006 |
| research_started / completed | MIL-EVT-007 |
| repair_started / completed | MIL-EVT-008 |
| intel_updated | MIL-EVT-009 |
| resource_harvested / delivered / node_depleted | MIL-EVT-010 |
| player_defeated / player_victory | MIL-EVT-011 |
| snapshot_exported | MIL-EVT-012 |
| capture_attempted / completed (optional) | MIL-EVT-013 |
| veterancy_gained (optional) | MIL-EVT-014 |

---

## 26. Diagnostics (MIL-DIAG)

- **MIL-DIAG-001:** Unpowered buildings, stalled production, idle units, stalled harvest loop flagged
- **MIL-DIAG-002:** Combat log summary optional in snapshot
- **MIL-DIAG-003:** Per-building: queue status, power state, prerequisite missing
- **MIL-DIAG-004:** Per-player: credits, harvest rate, military idle count
- **MIL-DIAG-005:** Fog/intel summary for debugging bot vision failures

---

## 27. Bots and Skirmish Smoke Tests (MIL-BOT)

- **MIL-BOT-001:** Bot submits same commands as human; minimal skirmish: build base, harvest, produce, scout, attack (RUN_04)
- **MIL-BOT-002:** Bot respects fog of war (no privileged intel)
- **MIL-BOT-003:** Two-bot skirmish completes with win/loss or stall diagnostic

---

## 28. Replay and Snapshot Export (MIL-REPLAY, MIL-SNAP)

- **MIL-REPLAY-001:** Full replay compatibility with kernel replay mechanism
- **MIL-REPLAY-002:** Replay hash verified after skirmish and smoke scenarios
- **MIL-SNAP-001:** Snapshot includes buildings, units, power, credits, queues, intel per player
- **MIL-SNAP-002:** Snapshot includes fog/shroud layers for Godot and bot observation
- **MIL-SNAP-003:** Spectator/replay viewer reads snapshot sequence only (no authority)

Multiplayer command sync implications: same command log + validation pipeline as single-player (see RUN_12, `NET-SYNC`).

---

## 29. Tests

| Category | Examples |
|----------|----------|
| Unit | `test_MIL_POWER_003_unpowered_barracks_cannot_produce` |
| Integration | `test_MIL_COMBAT_002_armour_modifier_applied` |
| Order | `test_MIL_ORDER_003_attack_move_engages_en_route` |
| Scenario | Two-player skirmish hash test |

See `docs/testing/01_rule_contract_test_matrix.md` for full MIL-* mapping.

---

## 30. Smoke Tests

| ID | Pass criteria |
|----|---------------|
| MIL-SMOKE-001 | Build command hub + generator + processor + barracks |
| MIL-SMOKE-002 | Harvest resource, process to credits |
| MIL-SMOKE-003 | Maintain power; unpowered production blocked |
| MIL-SMOKE-004 | Unlock tech prerequisite; produce advanced unit |
| MIL-SMOKE-005 | Scout reveals enemy; fog/shroud updates |
| MIL-SMOKE-006 | Attack-move or patrol destroys hostile unit |
| MIL-SMOKE-007 | Repair damaged building or unit |
| MIL-SMOKE-008 | Destroy hostile command hub; win/loss triggers |
| MIL-SMOKE-009 | Export replay and diagnostics; hash match |

---

## 31. Definition of Done (RUN_03)

> Run 03 is complete only when **every major OpenRA-like RTS mechanic family** is implemented in simplified, original, deterministic, testable form.

Checklist:

- [ ] OpenRA-like coverage families §7–§22 implemented (not OpenRA parity)
- [ ] Resource harvest → processor → credits loop works
- [ ] Construction queue and unit production queues work with prerequisites
- [ ] Power gating and underpowered behaviour correct
- [ ] Fog/shroud/vision exported for bots and Godot
- [ ] Direct unit commands including attack-move, patrol, guard work
- [ ] Combat with range, cooldown, armour/type modifiers
- [ ] Static defence targeting works
- [ ] Repair depot functional
- [ ] Skirmish setup and map generation hooks present
- [ ] All MIL-* tests in rule contract matrix pass
- [ ] MIL-SMOKE-001–009 pass
- [ ] Replay hash verified
- [ ] Optional capture/veterancy implemented or explicitly deferred with schema hooks

---

## 32. Cursor Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
