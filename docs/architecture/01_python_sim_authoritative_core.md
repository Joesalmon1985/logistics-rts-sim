# Python Sim Authoritative Core

## Principles

- All gameplay rules execute in Python
- Single process sim for single-player; host sim for multiplayer (RUN_12)
- No duplicate state in clients

## Package Layout (RUN_01)

```
sim/
  __init__.py
  kernel/          # tick, rng, entity ids, variant registry
  commands/        # base classes, validator, result types
  events/          # base classes, event log
  map/             # hex, terrain, height
  economy/         # RUN_02
  military/        # RUN_03
  hybrid/          # RUN_07
  snapshot/        # exporter
  replay/          # hash, load/save
  diagnostics/
tests/
scenarios/
config/
```

## Tick Loop

```python
def tick(state, rng):
    process_command_queue(state)
    for subsystem in ordered_subsystems(state.variant):
        subsystem.step(state, rng)
    emit_tick_advanced(state)
    return state
```

Subsystem order is fixed per variant and documented in variant config.

## State Model

- `GameState`: tick, seed, rng_state, variant, players, map, entities, event_log
- Entities indexed by `EntityId` (string or int UUID stable across ticks)
- All mutations through commands or subsystem steps during tick

## Variant Modules

| Variant | Modules loaded |
|---------|----------------|
| economy_trial | map, economy, diagnostics |
| military_trial | map, military, diagnostics |
| hybrid | map, economy, military, hybrid, diagnostics |

### Shared map (anti-divergence)

All variants load the **same RUN_01 `map` primitives** (axial hex, terrain, height). The military trial builds its passability/pathfinding on top of these shared primitives (terrain/height as passability inputs) rather than a parallel grid, so the hybrid variant composes economy and military without reconciling two map systems. See **DDR-029** in `docs/design/11_design_decision_register.md`.

## Entity IDs

- Assigned at creation; never reused in same match
- Included in events and snapshots for client diffing

## Seeded RNG

- `RandomState` stored in `GameState`
- All stochastic draws use this RNG only
- RNG state serialized in snapshot and save files

## Extension Points

- New commands: register in variant command registry
- New events: register event type + schema version
- New subsystems: register in variant ordered list

See `02_command_event_snapshot_contract.md` for wire formats.
