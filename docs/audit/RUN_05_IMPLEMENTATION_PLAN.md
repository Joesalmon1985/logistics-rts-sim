# RUN_05 Implementation Plan

## 1. Branch and starting commit

- Branch: `feature/run-05-godot-2d-viewers-trial-games`
- Starting commit: `c9524f5` (docs: expand RUN_05 prestart plan)
- Pre-flight (2026-06-15, Windows Godot 4.5.1 console): 148/148 tests, all RUN_01–04 smokes, failure-mode integrity — exit 0, zero `SCRIPT ERROR`

## 2. Files expected to change

### Sim (authoritative)
- `godot_project/sim/core/command_availability_provider.gd` (new)
- `godot_project/sim/core/command_availability_dto.gd` (new)
- `godot_project/sim/core/snapshot_exporter.gd` — `export_debug_snapshot()`
- `godot_project/sim/economy/economy_command_availability.gd` (new)
- `godot_project/sim/military/military_command_availability.gd` (new)
- `godot_project/sim/core/sim_bootstrap.gd` — register new scripts
- `godot_project/sim/tests/viewer/` — VIS-SNAP/CMD/AVAIL tests
- `godot_project/sim/tools/run_tests.gd` — register viewer tests (148 → 151)
- `godot_project/sim/tools/run_viewer_smoke.gd` (new)
- `godot_project/sim/tools/verify_failure_modes.gd` — invalid viewer smoke id

### Client (non-authoritative)
- `godot_project/client/adapters/` — snapshot_loader, layer_renderer, command_submitter, command_availability
- `godot_project/client/core/viewer_controller.gd`
- `godot_project/client/demo/` — economy_demo, military_demo
- `godot_project/client/ui/` — ViewerRoot, MapViewport, panels
- `godot_project/client/scenes/` — economy/military demo scenes

### Docs
- This file, run doc, viewer test plan, smoke matrix, local_verification, lessons learned

## 3. Implementation sequence

1. Sim-side command availability provider + DTO (validate-only legality, deterministic sort)
2. `export_debug_snapshot()` (depends on `export_snapshot`, not reverse)
3. Curated economy/military demo states
4. Client adapters (snapshot load, render data, submit, availability bridge)
5. ViewerController + UI panels + map render
6. Demo scenes (1920×1080 layout)
7. Headless tests (VIS-*) and smokes (GOD-SMOKE-*)
8. Full verification + windowed review + screenshots

## 4. Tests/smokes to add

| ID | File |
|----|------|
| VIS-SNAP-001 | `sim/tests/viewer/test_vis_snap_001_render_data_from_snapshot.gd` |
| VIS-CMD-001 | `sim/tests/viewer/test_vis_cmd_001_command_roundtrip.gd` |
| VIS-AVAIL-001 | `sim/tests/viewer/test_vis_avail_001_availability_bridge.gd` |
| GOD-SMOKE-001 | `run_viewer_smoke.gd` — render data from debug snapshots |
| GOD-SMOKE-002 | `run_viewer_smoke.gd` — availability + command round-trip |

Expected test count: **148 → 151** (update docs only after verified run).

## 5. Headless verification commands (Windows)

```powershell
$env:GODOT = "C:\Users\joesa\Downloads\Godot_v4.5.1-stable_win64.exe\Godot_v4.5.1-stable_win64_console.exe"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"
& $env:GODOT --headless --path godot_project --import
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=economy_trial --ticks=10 --seed=1
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=military_trial --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_military.json
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=hybrid --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_hybrid.json
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_military_smoke.gd -- --smoke=all
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_bot_smoke.gd -- --smoke=all
& $env:GODOT --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd -- --smoke=all
```

## 6. Windowed visual review commands

```powershell
& $env:GODOT --path godot_project --resolution 1920x1080 res://client/scenes/economy_viewer_demo.tscn
& $env:GODOT --path godot_project --resolution 1920x1080 res://client/scenes/military_viewer_demo.tscn
```

## 7. Screenshot path convention

- `user://screenshots/run_05_economy_demo.png`
- `user://screenshots/run_05_military_demo.png`

## 8. Known risks

- UI tests avoid scene tree; adapters tested on plain dicts
- Provider bounded enumeration only; legality via `validate()` only
- Field names from exporters (`anchor_q`/`anchor_r`)
- Windowed runs may leave stale Godot processes

## 9. Out of scope

Hybrid viewer, neural training, mobile layout, final art, packaging, client-side legality, merge to main.
