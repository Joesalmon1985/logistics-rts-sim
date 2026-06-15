# RUN_05 Lessons Learned

## What was implemented

- Sim-owned `CommandAvailabilityProvider` with economy/military candidate enumeration; legality via `command.validate()` only; deterministic DTO ordering; `id` is UI/debug-only.
- `SnapshotExporter.export_debug_snapshot()` adds `available_commands[]`; base `export_snapshot()` unchanged; replay hash path unchanged (`state.to_hash_dict()`).
- `godot_project/client/` debug viewers: adapters, `ViewerController`, `ViewerRoot` + `MapViewport`, economy/military demo scenes at 1920×1080 layout.
- Curated demos: `EconomyDemo`, `MilitaryDemo`.
- Tests: VIS-SNAP-001, VIS-CMD-001, VIS-AVAIL-001 (151 total in harness).
- Smokes: GOD-SMOKE-001/002 via `run_viewer_smoke.gd`; invalid smoke id in failure-mode integrity.

## Tests/smokes added

| ID | Runner |
|----|--------|
| VIS-SNAP-001 | `run_tests.gd` |
| VIS-CMD-001 | `run_tests.gd` |
| VIS-AVAIL-001 | `run_tests.gd` |
| GOD-SMOKE-001/002 | `run_viewer_smoke.gd` |

## Viewer launch commands

Windowed (human review):

```powershell
$env:GODOT = "C:\Users\joesa\Downloads\Godot_v4.5.1-stable_win64.exe\Godot_v4.5.1-stable_win64_console.exe"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"
& $env:GODOT --path godot_project --resolution 1920x1080 res://client/scenes/economy_viewer_demo.tscn
& $env:GODOT --path godot_project --resolution 1920x1080 res://client/scenes/military_viewer_demo.tscn
```

Screenshot capture (SubViewport, non-headless):

```powershell
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=economy
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=military
```

## Screenshot paths

- Convention: `user://screenshots/run_05_economy_demo.png`, `user://screenshots/run_05_military_demo.png`
- Windows capture run saved under OS app user data when `GODOT_USER_DATA_DIR` not applied to non-headless child process: `%APPDATA%\Godot\app_userdata\WideRTS Simulation\screenshots\`

## Windows / Godot version

- Godot **4.5.1.stable** console exe on Windows 10.
- Full headless suite: **151/151** tests, all RUN_01–04 smokes, viewer smokes, failure-mode integrity — exit 0, zero `SCRIPT ERROR` after fixes.

## Process issues

- Headless dummy renderer cannot capture SubViewport textures; screenshot runner requires non-headless.
- `Theme.default_font` removed in 4.5; use `ThemeDB.fallback_font`.
- Windowed Godot launches may leave processes; inspect with `Get-Process *Godot*` before re-run.

## Limitations

- Debug UI is functional, not polished (no animation, minimal zoom/pan).
- Layer toggles implemented for roads/grid; full layer panel is minimal.
- Automated SubViewport capture substitutes for agent-environment windowed review; human windowed pass still recommended on Linux reference machine.

## Deferred

- Hybrid viewer (RUN_09), tutorial polish (RUN_10), final art (RUN_11), mobile export (RUN_12), neural training (RUN_06).
