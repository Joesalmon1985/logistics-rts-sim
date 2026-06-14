# Human Play Design

Human-playable hybrid game (RUN_10). Economy and military trials get minimal debug UI in RUN_05.

---

## Design Principles

1. **Delegated control** — player sets policies and orders; sim executes unit behaviour
2. **Logistics visibility** — player sees ware flow, stalls, and priorities clearly
3. **Mobile-first** — touch targets, readable at phone scale, optional grid off by default

---

## Economy UI

| Action | Command |
|--------|---------|
| Place road | PlaceRoadCommand |
| Place building plan | PlaceBuildingPlanCommand |
| Set construction priority | SetConstructionPriorityCommand |
| Set production priority | SetProductionPriorityCommand |
| Enable/disable building | Enable/DisableBuildingCommand |

Visual feedback: missing wares, blocked paths, storage full from diagnostics overlay.

---

## Hybrid Military UI

| Action | Command |
|--------|---------|
| Set building stance | SetBuildingStanceCommand |
| Set defence priority | SetDefencePriorityCommand |
| Set rally / garrison | SetRallyPointCommand, SetGarrisonPreferenceCommand |
| Delegated attack | DelegatedAttackCommand (tap visible hostile target) |
| Cancel / recall | CancelDelegatedOrderCommand, RecallUnitsCommand |

**No** unit selection box or move commands in hybrid mode.

---

## Tutorial (RUN_10)

1. Place roads and woodcutter
2. Complete timber → planks chain
3. Build storage and second building
4. (Hybrid) Build barracks via construction wares
5. (Hybrid) Queue unit production; set rally
6. (Hybrid) Issue delegated attack on visible target

---

## Diagnostics for Players

- Stall panel: list buildings with missing inputs or blocked outputs
- Power panel: supply vs demand (hybrid)
- Production panel: queue status and ware delivery progress

---

## Rule IDs

| ID | Requirement |
|----|-------------|
| HUM-UX-001 | Touch camera and selection |
| HUM-CTRL-001 | Delegated military UI only in hybrid |
| HUM-ECON-001 | Economy priority UI |
| HUM-TUT-001 | Tutorial scenario completable |
| HUM-DIAG-001 | Player-readable diagnostics |
