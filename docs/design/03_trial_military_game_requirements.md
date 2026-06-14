# Trial Military Game Requirements

Binding contract for the **Trial Military Game** — a standalone RTS base-building and combat trial. Direct unit control is allowed here; the Hybrid Game restricts this (see `04_hybrid_game_requirements.md`).

---

## 1. Purpose

Deliver a headless, deterministic RTS trial with base building, power management, unit production, combat, intel, repair, tech, and defeat conditions. Uses credits-based production (trial mode) without economy logistics.

---

## 2. Non-Goals

- Economy ware chains, carriers, or road logistics (see `02_trial_economy_game_requirements.md`)
- Hybrid logistics-fed production (see `04_`)
- Godot rendering (RUN_05)
- Delegated-only military control (that is hybrid scope)
- Copying OpenRA, C&C, or Red Alert units, names, or exact balance

---

## 3. Originality Rules

Original placeholder names from `05_units_resources_buildings_catalogue.md`. Genre RTS conventions OK; protected identity is not.

---

## 4. Python Simulation Authority

All military rules in Python. Commands validated centrally. Events for every state change. Snapshots for Godot.

---

## 5. Determinism

- **MIL-DET-001:** Seeded RNG in state for combat variance (if any)
- **MIL-DET-002:** Same seed + commands → identical combat outcomes and replay hash

---

## 6. Generic RTS Base-Building

- **MIL-BASE-001:** Player builds structures on buildable map tiles
- **MIL-BASE-002:** Structures have placement rules, footprints, health, and owner

---

## 7. Required Buildings

| Building | Rule ID | Role |
|----------|---------|------|
| Command hub | MIL-HUB-001 | Primary base; destruction = defeat |
| Power generator | MIL-POWER-001 | Supplies power capacity |
| Resource processor | MIL-PROC-001 | Converts abstract resources to credits (trial) |
| Barracks | MIL-PROD-001 | Produces infantry |
| Vehicle plant | MIL-PROD-002 | Produces vehicles |
| Radar tower | MIL-RADAR-001 | Extends vision/intel range |
| Research lab | MIL-TECH-001 | Unlocks tech |
| Repair depot | MIL-REP-001 | Repairs units and buildings |
| Defence turret | MIL-TUR-001 | Static defence |

---

## 8. Credits / Resource Stock

- **MIL-CRED-001:** Players hold credits for trial-mode production
- **MIL-CRED-002:** Resource processor generates credits from abstract processed resource (trial simplification)

---

## 9. Power Supply and Demand

- **MIL-POWER-002:** Each building has power demand; generators supply capacity
- **MIL-POWER-003:** Unpowered buildings cannot produce, attack, or research
- **MIL-POWER-004:** Powered/unpowered state in snapshot and events

---

## 10. Production Queues

- **MIL-QUEUE-001:** Barracks and vehicle plant maintain ordered production queues
- **MIL-QUEUE-002:** Queue items consume credits and ticks to complete
- **MIL-QUEUE-003:** Cancel queue item returns partial credit (configurable)

---

## 11. Units

| Type | Rule ID | Notes |
|------|---------|-------|
| Infantry | MIL-UNIT-001 | Foot movement, light armour |
| Vehicle | MIL-UNIT-002 | Faster, heavier armour, may require vehicle plant |

Placeholder unit names in catalogue; no reference-game names.

---

## 12. Movement and Pathfinding

- **MIL-MOVE-001:** Units pathfind on passability grid (may share hex map with economy later)
- **MIL-MOVE-002:** Direct move/attack commands allowed in trial mode
- **MIL-MOVE-003:** Movement cost respects terrain and obstacles

---

## 13. Combat

- **MIL-COMBAT-001:** Units have range, damage, rate of fire, and hit points
- **MIL-COMBAT-002:** Armour/type modifiers (infantry vs vehicle, etc.)
- **MIL-COMBAT-003:** Target selection: nearest hostile in range unless commanded
- **MIL-COMBAT-004:** Building damage and destruction with events
- **MIL-COMBAT-005:** Unit destruction removes entity and emits event

---

## 14. Radar / Intel / Visibility

- **MIL-INTEL-001:** Fog of war: unexplored, explored, visible
- **MIL-INTEL-002:** Radar tower extends sight range
- **MIL-INTEL-003:** Units reveal area based on sight stat
- **MIL-INTEL-004:** Intel state exported per player in snapshot

---

## 15. Repair

- **MIL-REP-002:** Repair depot heals nearby units/buildings for credits over ticks
- **MIL-REP-003:** Repair emits progress events

---

## 16. Tech Unlocks

- **MIL-TECH-002:** Research lab consumes credits/ticks to unlock tech entries
- **MIL-TECH-003:** Unlocks gate unit types, building upgrades, or stats

---

## 17. Defeat Condition

- **MIL-DEFEAT-001:** Command hub destroyed → player defeated
- **MIL-DEFEAT-002:** Last player standing wins (multi-player trial scenarios)

---

## 18. Required Commands (Trial — Direct Control Allowed)

| Command | Rule ID |
|---------|---------|
| PlaceBuildingCommand | MIL-CMD-BLD-001 |
| CancelProductionCommand | MIL-CMD-PROD-001 |
| QueueUnitCommand | MIL-CMD-PROD-002 |
| MoveUnitCommand | MIL-CMD-UNIT-001 |
| AttackUnitCommand | MIL-CMD-UNIT-002 |
| AttackMoveCommand | MIL-CMD-UNIT-003 |
| StopUnitCommand | MIL-CMD-UNIT-004 |
| StartResearchCommand | MIL-CMD-TECH-001 |
| RepairTargetCommand | MIL-CMD-REP-001 |
| SellBuildingCommand | MIL-CMD-BLD-002 |

---

## 19. Required Event Categories

| Category | Rule ID |
|----------|---------|
| military_game_created | MIL-EVT-001 |
| tick_advanced | MIL-EVT-002 |
| building_placed / damaged / destroyed | MIL-EVT-003 |
| power_state_changed | MIL-EVT-004 |
| production_queued / started / completed / cancelled | MIL-EVT-005 |
| unit_spawned / moved / attacked / damaged / destroyed | MIL-EVT-006 |
| research_started / completed | MIL-EVT-007 |
| repair_started / completed | MIL-EVT-008 |
| intel_updated | MIL-EVT-009 |
| player_defeated | MIL-EVT-010 |
| snapshot_exported | MIL-EVT-011 |

---

## 20. Diagnostics

- **MIL-DIAG-001:** Unpowered buildings, stalled production, idle units flagged
- **MIL-DIAG-002:** Combat log summary optional in snapshot

---

## 21. Bots

- **MIL-BOT-001:** Bot submits same commands; minimal: build generator, barracks, queue units, attack (RUN_04)

---

## 22. Tests

| Category | Examples |
|----------|----------|
| Unit | `test_MIL_POWER_001_unpowered_barracks_cannot_produce` |
| Integration | `test_MIL_COMBAT_001_damage_applies_armour_modifier` |
| Scenario | Two-player skirmish hash test |

---

## 23. Smoke Tests

| ID | Pass criteria |
|----|---------------|
| MIL-SMOKE-001 | Build command hub + generator + barracks |
| MIL-SMOKE-002 | Produce infantry unit |
| MIL-SMOKE-003 | Unit destroys target building |
| MIL-SMOKE-004 | Command hub loss triggers defeat |
| MIL-SMOKE-005 | Replay hash match |

---

## 24. Replay and Snapshot Export

- **MIL-REP-001:** Full replay compatibility with kernel replay mechanism
- **MIL-SNAP-001:** Snapshot includes buildings, units, power, intel per player

---

## 25. Definition of Done (RUN_03)

- [ ] All buildings functional with power gating
- [ ] Production queues work for infantry and vehicles
- [ ] Combat resolves deterministically with modifiers
- [ ] Intel/fog updates correctly
- [ ] Defeat condition triggers
- [ ] Direct unit commands work
- [ ] Smoke tests MIL-SMOKE-001–005 pass
- [ ] Replay hash verified

---

## 26. Cursor Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
