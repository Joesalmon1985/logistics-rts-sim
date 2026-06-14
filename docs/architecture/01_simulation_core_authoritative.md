# Simulation Core (Authoritative, GDScript)

## Principles

- All gameplay rules execute in the **Godot/GDScript simulation core** (pure scripts/resources/classes).
- The core is **headless-capable** (runs under Godot headless with no rendered scene).
- The authoritative core is **separate from UI scenes and rendering**; UI is non-authoritative.
- No duplicate authoritative state in the UI client.
- Single authoritative core per match; for multiplayer the host runs it (host-authoritative, RUN_12).

> "Godot/GDScript simulation core is authoritative" — never "Godot is authoritative." Scenes/rendering/UI never own rules.

## Project Layout (RUN_01)

```
godot_project/
  sim/                      # AUTHORITATIVE simulation core (pure GDScript)
    core/                   # tick loop, seeded RNG, entity ids, fixed-point helpers, hashing
    variants/               # variant registry + per-variant rule sets
      economy_trial/        # RUN_02
      military_trial/       # RUN_03
      hybrid/               # RUN_07
    data/                   # config resources (buildings, recipes, units, terrain, variants)
    tests/                  # headless GDScript tests for the core (see DDR-032)
    tools/                  # headless CLI entry points (create/tick/snapshot/replay/smoke)
  client/                   # NON-AUTHORITATIVE Godot UI/visual client
    scenes/
    ui/
    renderers/
    adapters/               # snapshot loader, command submitter

python_tools/               # Python is TOOLING/TRAINING ONLY (never shipped as runtime)
  training/
  analysis/
  tournament_analysis/
```

The authoritative `sim/` core must not reference anything under `client/`. The `client/` may read snapshots and submit commands but must not implement rules.

## Tick Loop (conceptual)

```
func tick(state, rng):
    process_command_queue(state)
    for subsystem in ordered_subsystems(state.variant):
        subsystem.step(state, rng)
    emit_tick_advanced(state)
    return state
```

Subsystem order is fixed per variant and documented in variant config. The tick loop must not read frame delta, wall-clock, or scene state.

## Core must NOT depend on

- Scene tree, Node or sprite positions
- Animation timing, `_process`/`_physics_process` delta, or wall-clock time
- Physics engine side effects, cameras, UI nodes, or input events

## Allowed in the core

- Deterministic tick loop
- Seeded RNG stored in simulation state
- Integer / fixed-point authoritative state (no floats in hashed state; see `05_determinism_replay_save_load.md`)
- Command validation/execution
- Event log
- Snapshot export
- Replay hash
- JSON/CSV diagnostics

## State Model

- `GameState`: tick, seed, rng_state, variant, players, map, entities, event_log
- Entities indexed by `EntityId` (stable across ticks)
- All mutations through commands or subsystem steps during a tick

## Shared Map (anti-divergence — DDR-029)

All variants load the **same shared axial hex-map primitives** created in RUN_01 (`sim/core` map types: axial hex, terrain, height). The military trial derives passability, range, vision, building placement, attack range, movement, and pathfinding from this shared hex map — it must **not** create a separate square grid or parallel map system. The hybrid composes economy + military without reconciling different map models.

## Variant Modules

| Variant | Modules loaded |
|---------|----------------|
| economy_trial | map, economy, diagnostics |
| military_trial | map, military, diagnostics |
| hybrid | map, economy, military, hybrid, diagnostics |

## Seeded RNG

- RNG object/state stored in `GameState`
- All stochastic draws use this RNG only
- RNG state serialized in snapshot and save files

## Extension Points

- New commands: register in variant command registry
- New events: register event type + schema version
- New subsystems: register in variant ordered list

## Python role

Python (`python_tools/`) is for training, analysis, and tooling only. It reaches the authoritative core by running Godot headless and exchanging commands/snapshots/replays (development-time transport, see `04_bot_and_training_architecture.md` and DDR-033). Python is never embedded in the shipped mobile app.

See `02_command_event_snapshot_contract.md` for wire formats.
