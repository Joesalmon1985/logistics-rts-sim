# Hybrid Game Requirements

Binding contract for the **Hybrid Game**. This is **not** a third unrelated game.

---

## 1. Composition Model

```
Hybrid Game = Trial Economy Game + Trial Military Game + Hybrid Integration Rules
```

| Layer | Source document |
|-------|-----------------|
| Economy rules | `docs/design/02_trial_economy_game_requirements.md` |
| Military rules | `docs/design/03_trial_military_game_requirements.md` |
| Integration rules | This document |

Inherited rules apply unless explicitly overridden below.

> **RUN_07 dependency (DDR-LOG-008):** Hybrid military/economy integration (**RUN_07**) must not proceed until **RUN_02.7** economy logistics foundation hardening is complete and acceptance criteria in `15_worker_carrier_road_node_logistics_foundation_RATIFIED.md` are met. Hybrid buildings use the same economy construction and logistics foundation: building `ports[]` / entrances, delivery jobs, carriers per job, diagnostics, and config-driven wares/recipes where appropriate.

---

## 2. Purpose

Combine settlement logistics with RTS military gameplay so that military buildings and units require ware delivery from the economy, and players control military through delegated building/priority commands — not individual unit micro.

---

## 3. Non-Goals

- Instant sidebar unit production (explicitly forbidden)
- Direct unit selection/micro as primary control (trial military direct commands are disabled in hybrid variant)
- New economy or military mechanic families not in `02_` or `03_`
- Godot human UX polish (RUN_10)

---

## 4. What Changes in Hybrid Mode

| Aspect | Trial Economy | Trial Military | Hybrid |
|--------|---------------|----------------|--------|
| Unit production cost | N/A | Credits | Wares + builder work |
| Building construction | Wares | Credits/ instant | Wares via construction sites |
| Unit control | N/A | Direct commands | Delegated orders only |
| Power | N/A | Generator supply | Integrated; economy buildings may need power |
| Credits | N/A | Trial currency | **Removed** — hybrid is ware/logistics only |

> **Ratified (DDR-031):** The Hybrid Game has **no credits**. All hybrid construction, unit production, repair, and research are ware/logistics based. The Trial Military Game may still use credits as a standalone simplified mechanic, but the hybrid variant must not. If a future "command capacity"/"permission" abstraction is added, it must not replace ware delivery.

---

## 5. Inherited Economy Rules

All `ECON-*` rules from `02_` apply unless overridden:

- Hex map, terrain, roads, carriers, wares, production chains, construction sites, priorities, diagnostics
- Military building **plans** are placed via `PlaceBuildingPlanCommand` and consume planks, stone, tools, builder work

**HYB-ECON-001:** Economy engine is not duplicated; hybrid extends variant registry.

**HYB-ECON-004:** Hybrid military construction, production, and repair use economy **delivery jobs**, **carriers assigned per job**, and building **port/entrance** road nodes — same foundation as trial economy (DDR-LOG-002, DDR-LOG-003). No hybrid-specific logistics duplicate.

**HYB-ECON-002:** There is **no credit loop** in hybrid. Military buildings/units are paid for entirely in wares via the economy. Where the trial military used a collector→processor→credits loop, hybrid uses economy deposit extraction and ware logistics instead; trial collector micro is not exposed to the player.

**HYB-ECON-003:** No credits as a build, production, repair, or research resource anywhere in the hybrid variant (DDR-031).

---

## 6. Inherited Military Rules

All `MIL-*` rules from `03_` apply except:

- **HYB-MIL-001:** `QueueUnitCommand` requires **ware inputs only** — credits are not used in hybrid
- **HYB-MIL-002:** `MoveUnitCommand`, `AttackUnitCommand`, `AttackMoveCommand`, `PatrolCommand`, `GuardCommand` are **not** accepted from human players
- **HYB-MIL-003:** AI/bot delegated logic may still issue internal unit intents via sim AI layer (not player commands)
- **HYB-MIL-004:** Trial collector/harvester→credits loop is removed; replaced by economy deposit extraction and ware logistics feeding military production/construction/repair/research
- **HYB-MIL-005:** Any `MIL-*` rule that consumes credits (construction, production, repair, research) is overridden to consume wares in hybrid

---

## 7. No Instant Sidebar Production

- **HYB-NOSIDE-001:** Players cannot spawn units from UI without queued production at a building
- **HYB-NOSIDE-002:** Production progress requires ware delivery to building road nodes
- **HYB-NOSIDE-003:** Missing wares stall production with `input_missing` event

---

## 8. Logistics-Fed Military Construction

- **HYB-LOGCON-001:** Military buildings use economy construction site flow
- **HYB-LOGCON-002:** Footprints and ware requirements defined in hybrid config data
- **HYB-LOGCON-003:** Carriers deliver construction wares to military site road nodes at configured **ports/entrances**

---

## 9. Logistics-Fed Unit Production

- **HYB-LOGPROD-001:** Barracks/vehicle plant production recipes consume wares (e.g. tools, iron ingots, bread for crew)
- **HYB-LOGPROD-002:** Production queue shows ware requirements and delivery status
- **HYB-LOGPROD-003:** `SetProductionPriorityCommand` applies to military production buildings

---

## 9b. Logistics-Fed Repair (HYB-LOGREP)

- **HYB-LOGREP-001:** Repair depot consumes wares (e.g. iron ingots, tools) over ticks to heal targets
- **HYB-LOGREP-002:** Missing wares stall repair with `input_missing` event
- **HYB-LOGREP-003:** `SetRepairPriorityCommand` orders repair targets by policy

---

## 10. Power Integration

- **HYB-POWER-001:** Military buildings require power from generators
- **HYB-POWER-002:** Generators may require economy wares to build and maintain (configurable)
- **HYB-POWER-003:** Unpowered military buildings: no production, turrets inactive, radar offline

---

## 11. Radar / Intel Integration

- **HYB-INTEL-001:** Radar towers are economy-built structures with military intel behaviour
- **HYB-INTEL-002:** Intel snapshot merges economy visibility (explored terrain) with military sight

---

## 12. Research Integration

- **HYB-TECH-001:** Research lab requires wares and power to complete research
- **HYB-TECH-002:** Unlocks affect both economy and military recipes

---

## 13. Delegated Military Control

Players **must not** directly select or micro individual units in the main hybrid game.

### Trial → Hybrid translation

For each Trial Military direct-control family, the hybrid game provides a **delegated equivalent**:

| Trial Military (direct) | Hybrid (delegated) | Rule ID |
|-------------------------|-------------------|---------|
| `MoveUnitCommand` | Rally point + garrison preference + building stance | HYB-CTRL-003, HYB-CTRL-004, HYB-CTRL-002 |
| `AttackUnitCommand` | `DelegatedAttackCommand` on visible building/sector target | HYB-CTRL-006 |
| `AttackMoveCommand` | Delegated attack order or aggressive/pressure stance | HYB-CTRL-006, HYB-CTRL-002 |
| `PatrolCommand` | Patrol zone / defence policy on sector or route | HYB-CTRL-009 |
| `GuardCommand` | Defend building / road node / storage policy | HYB-CTRL-010 |
| `QueueUnitCommand` (credits) | Logistics-fed production priority at barracks/plant | HYB-LOGPROD-001, HYB-CTRL-001 |
| `PlaceBuildingCommand` (instant/credits) | `PlaceBuildingPlanCommand` + construction site wares | HYB-LOGCON-001 |
| Building construction queue | Economy construction site flow + priorities | HYB-LOGCON-001, ECON-PRI-* |
| `RepairTargetCommand` | Repair priority policy + logistics-fed repair jobs | HYB-CTRL-011, HYB-LOGREP-001 |
| Collector harvest loop | Economy ore/coal deposits + military processor (no direct collector micro) | HYB-ECON-002, HYB-MIL-004 |
| `StartResearchCommand` (credits only) | Research lab with ware + power inputs | HYB-TECH-001 |
| Scout / vision orders | Radar tower + economy exploration + delegated scout stance | HYB-INTEL-001, HYB-CTRL-012 |

Sim AI executes unit pathfinding, combat, and harvest-adjacent behaviour internally (`HYB-MIL-003`); players set policies and targets only.

### Allowed player controls

| Control | Rule ID | Description |
|---------|---------|-------------|
| Production priorities | HYB-CTRL-001 | Order ware-fed production queues |
| Building stances | HYB-CTRL-002 | Aggressive / defensive / passive / hold |
| Rally preferences | HYB-CTRL-003 | Where new units gather |
| Garrison preferences | HYB-CTRL-004 | Units stay near assigned building |
| Defence priorities | HYB-CTRL-005 | Target priorities for turrets and defensive buildings |
| Delegated attack orders | HYB-CTRL-006 | Attack visible hostile **target** (building/sector), not individual unit pathing |
| Patrol / defence zone | HYB-CTRL-009 | Assign patrol or defence sector; sim AI loops units |
| Guard policy | HYB-CTRL-010 | Defend assigned building, road hub, or storage |
| Repair priority | HYB-CTRL-011 | Set repair depot priority; wares feed repair jobs |
| Scout / intel stance | HYB-CTRL-012 | Emphasize vision sectors without direct unit selection |
| Cancellation / recall | HYB-CTRL-007 | Cancel delegated orders, recall to rally |
| Economy / logistics priorities | HYB-CTRL-008 | Existing economy priority commands |

### Forbidden player controls

- **HYB-CTRL-999:** Direct `MoveUnitCommand` / `AttackUnitCommand` from human player in hybrid variant

---

## 14. Valid Attack Targets

- **HYB-TARGET-001:** Delegated attacks require target visible in player's intel
- **HYB-TARGET-002:** Invalid or fogged targets rejected with reason
- **HYB-TARGET-003:** Sim AI executes unit movement/combat toward delegated target

---

## 15. Defence Priorities

- **HYB-DEF-001:** Turrets and defensive buildings select targets by priority: air > vehicles > infantry > nearest (configurable)
- **HYB-DEF-002:** `SetDefencePriorityCommand` sets policy per building

---

## 16. Garrison / Rally Behaviour

- **HYB-GARR-001:** Garrisoned units remain within radius of assigned building unless delegated order active
- **HYB-GARR-002:** Rally point receives newly produced units before assignment
- **HYB-GARR-003:** `SetGarrisonPreferenceCommand`, `SetRallyPointCommand`

---

## 17. Hybrid Commands

| Command | Rule ID |
|---------|---------|
| SetBuildingStanceCommand | HYB-CMD-001 |
| SetDefencePriorityCommand | HYB-CMD-002 |
| SetGarrisonPreferenceCommand | HYB-CMD-003 |
| SetRallyPointCommand | HYB-CMD-004 |
| DelegatedAttackCommand | HYB-CMD-005 |
| CancelDelegatedOrderCommand | HYB-CMD-006 |
| RecallUnitsCommand | HYB-CMD-007 |
| SetPatrolZoneCommand | HYB-CMD-008 |
| SetGuardPolicyCommand | HYB-CMD-009 |
| SetRepairPriorityCommand | HYB-CMD-010 |
| SetScoutStanceCommand | HYB-CMD-011 |
| (All economy commands from ECON-CMD-*) | HYB-CMD-ECON |
| (Military placement via economy construction) | HYB-CMD-BLD |

---

## 18. Hybrid Events

All economy and military events plus:

| Category | Rule ID |
|----------|---------|
| hybrid_game_created | HYB-EVT-001 |
| delegated_order_issued / cancelled | HYB-EVT-002 |
| stance_changed | HYB-EVT-003 |
| ware_delivered_to_military_production | HYB-EVT-004 |
| military_construction_completed | HYB-EVT-005 |
| hybrid_stalled | HYB-EVT-006 |
| hybrid_victory / hybrid_defeat | HYB-EVT-007 |

---

## 19. Hybrid Diagnostics

- **HYB-DIAG-001:** Combined economy stall + military idle/unpowered report
- **HYB-DIAG-002:** Per-building: ware inputs for production, delegated order status
- **HYB-DIAG-003:** Player-facing summary for Godot overlay (RUN_09+)

---

## 20. Hybrid Bots

- **HYB-BOT-001:** Bots use delegated commands + economy commands only
- **HYB-BOT-002:** Bot observes snapshot; no privileged intel (RUN_08)

---

## 21. Hybrid Tests

| Category | Examples |
|----------|----------|
| Integration | `test_HYB_LOGPROD_001_barracks_requires_tool_delivery` |
| Control | `test_HYB_CTRL_001_rejects_direct_move_unit_from_player` |
| Scenario | Build economy chain → build barracks → produce unit → delegated attack |

---

## 22. Hybrid Smoke Tests

| ID | Pass criteria |
|----|---------------|
| HYB-SMOKE-001 | Complete military building via ware construction |
| HYB-SMOKE-002 | Produce unit with ware-fed queue |
| HYB-SMOKE-003 | Power/radar/tech integration functional |
| HYB-SMOKE-004 | Delegated attack destroys hostile hub |
| HYB-SMOKE-005 | Patrol/defence zone or guard policy engages hostiles |
| HYB-SMOKE-006 | Repair priority heals damaged unit/building with wares |
| HYB-SMOKE-007 | Direct unit command rejected for human player |
| HYB-SMOKE-008 | Win/loss/stall detection on hybrid scenario |
| HYB-SMOKE-009 | Full scenario replay hash match |

---

## 23. Definition of Done (RUN_07 + RUN_08)

**RUN_07 (headless):**
- [ ] Variant `hybrid` composes economy + military modules
- [ ] Logistics-fed construction and production work
- [ ] Logistics-fed repair with priority policy works
- [ ] Delegated commands accepted; all direct unit commands rejected for humans
- [ ] Trial→hybrid translation table §13 satisfied
- [ ] HYB-SMOKE-001–009 pass headlessly

**RUN_08 (bots/balance):**
- [ ] Hybrid bot plays delegated scenario (attack, defence, repair priorities)
- [ ] Balance smoke scenarios complete without stall deadlocks
- [ ] Bot respects fog/intel (no privileged vision)

---

## 24. Cursor Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
