# RUN_05 Lessons Learned

## What was implemented

- Sim-owned `CommandAvailabilityProvider` with economy/military candidate enumeration; legality via `command.validate()` only; deterministic DTO ordering; `id` is UI/debug-only.
- `SnapshotExporter.export_debug_snapshot()` adds `available_commands[]`; base `export_snapshot()` unchanged; replay hash path unchanged (`state.to_hash_dict()`).
- `godot_project/client/` debug viewers: adapters, `ViewerController`, `ViewerRoot` + `MapViewport`, economy/military demo scenes at 1920×1080 layout.
- Curated demos: `EconomyDemo`, `MilitaryDemo`.
- Tests: VIS-SNAP-001, VIS-CMD-001, VIS-RENDER-001, VIS-AVAIL-001, VIS-ECON-CHAIN-001, VIS-ECON-BUILD-001 (**154** total in harness).
- Smokes: GOD-SMOKE-001/002/003 via `run_viewer_smoke.gd`; invalid smoke id in failure-mode integrity.

## Interactive / observable-state closure pass

Root cause of "cannot test economy/combat loops": `layer_renderer.gd` ignored `construction_sites`, `carrier_markers`, and `worker_markers`; military availability omitted attack commands and demo used invalid type names (`power_plant`, `infantry`).

Fixes:
- Render construction sites (dashed hex + progress ring), carriers (via `road_nodes` lookup), worker badges (W#), unit/building HP bars.
- Economy demo: pre-placed storehouse site at 5,8 with road link; stone in starting wares; `verify_progression_within_ticks(50)` in smoke.
- Military demo: `power_generator`, `rifle_squad`; nearby p2 rifle squad; attack/attack-move enumeration with prebuilt labels.
- UI: Step 10, Step Until Event (meaningful events only), summary panel, construction-site selection + progress inspector, attack-target highlight.
- Tests/smokes: before/after render deltas (VIS-CMD-001, GOD-SMOKE-002); VIS-RENDER-001 for site/carrier extraction.

## How to submit and observe commands

**Economy:** Load demo → see dashed site south of sawmill → pick **Roads:** or **Buildings:** command → Submit → Step 10/Until Event → watch site progress ring, carrier dots, summary wares, event log.

**Military:** Load demo → pick **Attack: Rifle Squad attacks enemy …** → Submit → Step 10/Until Event → HP bars change, order/combat/damage in event log, red target ring on map.

## Tests/smokes added

| ID | Runner |
|----|--------|
| VIS-SNAP-001 | `run_tests.gd` |
| VIS-CMD-001 | `run_tests.gd` (before/after render delta) |
| VIS-RENDER-001 | `run_tests.gd` |
| VIS-AVAIL-001 | `run_tests.gd` |
| VIS-ECON-CHAIN-001 | `run_tests.gd` (30-tick production chain render delta) |
| VIS-ECON-BUILD-001 | `run_tests.gd` (build tutorial place → road → deliver) |
| GOD-SMOKE-001/002/003 | `run_viewer_smoke.gd` (progression + combat delta + logistics) |

## Viewer launch commands

Windowed (human review):

```powershell
$env:GODOT = "C:\Users\joesa\Downloads\Godot_v4.5.1-stable_win64.exe\Godot_v4.5.1-stable_win64_console.exe"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"
& $env:GODOT --path godot_project --resolution 1920x1080 res://client/scenes/economy_viewer_demo.tscn
& $env:GODOT --path godot_project --resolution 1920x1080 res://client/scenes/military_viewer_demo.tscn
```

Screenshot capture (SubViewport, **non-headless** — headless dummy renderer returns null texture):

```powershell
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=economy
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=military
```

## Screenshot paths

- Convention: `user://screenshots/run_05_economy_demo.png`, `user://screenshots/run_05_military_demo.png`
- Windows non-headless capture saved under `%APPDATA%\Godot\app_userdata\WideRTS Simulation\screenshots\` when `GODOT_USER_DATA_DIR` is not applied to the capture process.

## Windows / Godot version

- Godot **4.5.1.stable** console exe on Windows 10.
- Full headless suite after closure pass: **152/152** tests, all RUN_01–04 smokes, viewer smokes, failure-mode integrity — exit 0, zero `SCRIPT ERROR`.

## Known limitations

- Carrier position uses `edge_progress` interpolation between road nodes when in transit.
- Workers shown as per-building W# badge, not free movement on map.
- Attack/combat resolves over several ticks (range in `tick_combat`); use Step Until Event.
- Headless SubViewport screenshot capture fails (`texture_2d_get` null); use non-headless capture runner.
- Debug UI is functional, not polished (minimal zoom/pan, no animation).
- **Track 2 windowed human review** is required for merge readiness confirmation — automated tests do not substitute.

## Process issues

- `Theme.default_font` removed in 4.5; use `ThemeDB.fallback_font`.
- Economy construction site placement must avoid HQ/building footprints; demo site at 5,8 with road from sawmill.
- Windowed Godot launches may leave processes; inspect with `Get-Process *Godot*` before re-run.

## 2D observability closure (Track 1.5)

Headless render-data tests can pass while the hex map is blank in the actual viewport. Fixes and gates:

- **Layout fix:** `MapViewport` `NOTIFICATION_RESIZED` + deferred `_refresh_map_layers()` in `ViewerRoot`; `map_title` declared; min size 640×480 on map panel.
- **Pixel harness:** `client/tools/viewer_pixel_probe.gd`, `client/tools/viewer_test_harness.gd`.
- **Runners:** `run_viewer_pixel_smoke.gd` (GOD-SMOKE-004); `run_viewer_capture.gd --assert` (GOD-CAPTURE-001).
- **Tests:** VIS-LAYOUT-001, VIS-PIX-CHAIN-001, VIS-PIX-BUILD-001 (viewport pixels, not dictionaries only).

Report three statuses: headless / pixel smoke / human windowed review.

## Economy logistics visibility pass

Workers are **not** free-moving on the map — they are assigned to buildings in sim. Honest visualization uses snapshot-only layers: `tree_density`, `building_stores`, `ware_routes` (with `edge_progress` interpolation), `delivery_jobs`, `production_activity` pulse.

- **Production chain demo** (default): HQ + woodcutter + sawmill; no pre-placed site; tools-only starting wares; auto worker policy.
- **Build tutorial demo**: road stub to 6,2; user places storehouse; checklist panel tracks site → road → deliver → complete.
- Road command enumeration expanded: road_nodes + construction_sites + HQ fallback; storehouse candidates labeled road-adjacent.
- Tests: VIS-ECON-CHAIN-001, VIS-ECON-BUILD-001 (**154** total in harness).
- Smokes: GOD-SMOKE-003 (logistics chain delta + build tutorial mini-sequence).

## Deferred

- Hybrid viewer (RUN_09), tutorial polish (RUN_10), final art (RUN_11), mobile export (RUN_12), neural training (RUN_06).
