# Godot Visual Client / Adapter Contract

## Role

The Godot **visual client/UI** is **non-authoritative**. It displays snapshots from the authoritative GDScript simulation core and submits commands. It does not own rules.

> The simulation core is also GDScript, but it is a separate layer (`sim/`). The UI client (`client/`) renders and submits only.

## Directory Layout (RUN_05+)

```
godot_project/
  sim/                # authoritative core (RUN_01+); not part of the UI client
  client/
    scenes/
    ui/
    renderers/
    adapters/
      snapshot_loader.gd
      command_submitter.gd
      layer_renderer.gd
```

Separate viewer scenes for economy trial, military trial, and hybrid may share adapter/renderer code.

## Snapshot Access

1. Obtain a snapshot from the in-process simulation core (or, for tooling/replay, from a JSON file or LAN message)
2. Parse into a typed dictionary
3. Diff against previous snapshot; update nodes
4. Never infer rules not present in the snapshot (e.g. do not compute production locally)

## Command Submission

1. UI collects intent → builds a command matching `02_command_event_snapshot_contract.md`
2. Submit to the simulation core through the shared command pipeline:
   - **Single-player / mobile:** an in-process call into `sim/` (no Python, no subprocess, no socket)
   - **Multiplayer (RUN_12):** send to the host, which runs the authoritative core (host-authoritative)
3. Display `CommandResult` reason on rejection
4. Wait for the next snapshot before updating authoritative display

## Layer Rendering

Implement layers per `06_mobile_2d_visual_design.md`. The layer renderer reads snapshot fields only. Presentation-only float animation is allowed but must never feed back into authoritative state.

## Replay Mode

- Load a replay file
- Step commands tick-by-tick via the core's replay runner (Godot headless or in-process)
- Display resulting snapshots; no local stepping of rules

## Forbidden in the UI client

- Gameplay pathfinding/rules in scene or UI code
- Local ware/production counters that diverge from snapshots
- `randf()` / any RNG for gameplay outcomes
- Treating animation/presentation floats as authoritative state

## Testing

See `docs/testing/03_godot_viewer_test_plan.md`.
