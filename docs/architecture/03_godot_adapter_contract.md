# Godot Adapter Contract

## Role

Godot is a **non-authoritative** mobile client. It displays snapshots and submits commands.

## Directory Layout (RUN_05+)

```
godot_viewer/
  project.godot
  adapter/
    snapshot_loader.gd
    command_submitter.gd
    layer_renderer.gd
  scenes/
  assets/
```

Separate viewers for economy trial, military trial, hybrid — may share adapter code.

## Snapshot Loading

1. Receive JSON string (file, stdin, or network)
2. Parse into typed dictionary
3. Diff against previous snapshot; update nodes
4. Never infer rules not present in snapshot (e.g. do not compute production locally)

## Command Submission

1. UI collects intent → build command JSON matching `02_command_event_snapshot_contract.md`
2. Send to Python via subprocess pipe, socket, or embedded library — the on-device transport for packaged mobile builds is an **open project-level decision** (see **DDR-026** in `docs/design/11_design_decision_register.md`); confirm before RUN_05
3. Display `CommandResult` reason on rejection
4. Wait for next snapshot before updating authoritative display

## Layer Rendering

Implement layers per `06_mobile_2d_visual_design.md`. Layer renderer reads snapshot fields only.

## Replay Mode

- Load replay file
- Step commands tick-by-tick via Python replay runner
- Display resulting snapshots; no local stepping

## Forbidden

- GDScript pathfinding for gameplay
- Local ware/production counters
- `randf()` for gameplay outcomes

## Testing

See `docs/testing/03_godot_viewer_test_plan.md`.
