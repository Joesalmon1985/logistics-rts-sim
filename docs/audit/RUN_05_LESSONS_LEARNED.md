# RUN_05 Lessons Learned

**Status:** complete — merged to `main` at `cce553f`. Formal closure: [`RUN_05_FINAL_REPORT.md`](RUN_05_FINAL_REPORT.md).

## What was implemented

- Sim-owned `CommandAvailabilityProvider` with economy/military candidate enumeration; legality via `command.validate()` only; deterministic DTO ordering; `id` is UI/debug-only.
- `SnapshotExporter.export_debug_snapshot()` adds `available_commands[]`; base `export_snapshot()` unchanged; replay hash path unchanged (`state.to_hash_dict()`).
- `godot_project/client/` debug viewers: adapters, `ViewerController`, `ViewerRoot` + `MapViewport`, economy/military demo scenes at 1920×1080 layout.
- Curated demos: `EconomyDemo` (production chain default + build tutorial), `MilitaryDemo`.
- Tests: VIS-SNAP-001, VIS-CMD-001, VIS-RENDER-001, VIS-AVAIL-001, VIS-ECON-CHAIN-001, VIS-ECON-BUILD-001, VIS-LAYOUT-001, VIS-PIX-CHAIN-001, VIS-PIX-BUILD-001 (**154** total in harness).
- Smokes: GOD-SMOKE-001/002/003 via `run_viewer_smoke.gd` (headless); GOD-SMOKE-004 via `run_viewer_pixel_smoke.gd` (non-headless); invalid smoke id in failure-mode integrity.

## Avoiding a non-working visual product

Headless render-data tests on `LayerRenderer` dictionaries can pass while the SubViewport/window shows a **blank map**. This happened when `fit_to_content()` skipped layout because `MapViewport.size < 10` before first paint, with no resize retry.

### Anti-patterns (do not rely on these alone)

1. **Never treat render-data tests as viewport proof** — VIS-ECON-*, GOD-SMOKE-003 operate on dictionaries; blank-map regressions slip through without GOD-SMOKE-004.
2. **Do not merge viewer milestones on Track 1 only** — headless contract smokes prove snapshot/command wiring, not that a human sees a map.

### Required gates (three tracks)

| Track | What it proves | Runners |
|-------|----------------|---------|
| **1 Headless** | Snapshot load, render dicts, command round-trip, logistics chain deltas | `run_tests.gd`, `run_viewer_smoke.gd --smoke=all` |
| **1.5 Pixel** | SubViewport has non-zero size, non-blank pixels, step/command visible changes | `run_viewer_pixel_smoke.gd`, `run_viewer_capture.gd --assert` |
| **2 Human** | 1920×1080 readability, legend, panels, debug loop at desktop scale | Windowed demo launch + checklist in `local_verification.md` |

**Track 1 alone does not substitute for Tracks 1.5 + 2.** All three together gate RUN_05-style viewer milestones.

### Layout-before-paint fix

- `MapViewport`: `NOTIFICATION_RESIZED` handler; min size 640×480 on map panel.
- `ViewerRoot`: deferred `_refresh_map_layers()` after scene ready; `map_title` declared so layout resolves.
- See `godot_project/client/ui/map_viewport.gd`, `godot_project/client/ui/viewer_root.gd`.

### SubViewport capture is non-headless

Headless Godot returns null viewport textures. Use `run_viewer_capture.gd` **without** `--headless`. Add `--assert` for automated grass/forest/entity color checks (GOD-CAPTURE-001).

### Demo UX must match docs

When demo modes change (production chain vs build tutorial), update human playbooks in the same commit. The economy viewer no longer pre-places a storehouse at 5,8 — that was superseded by the build tutorial flow.

### Honest logistics visualization

Workers do not move on the map; they are assigned to buildings in sim. Show tree density, building stores, carrier routes (with `edge_progress`), delivery jobs, and production activity from snapshot only.

## Interactive / observable-state closure pass

Root cause of "cannot test economy/combat loops": `layer_renderer.gd` initially ignored `construction_sites`, `carrier_markers`, and `worker_markers`; military availability omitted attack commands and demo used invalid type names (`power_plant`, `infantry`).

Fixes:
- Render construction sites (dashed hex + progress ring), carriers (via `road_nodes` lookup), worker badges (W#), unit/building HP bars.
- Military demo: `power_generator`, `rifle_squad`; nearby p2 rifle squad; attack/attack-move enumeration with prebuilt labels.
- UI: Step 10, Step Until Event (meaningful events only), summary panel, construction-site selection + progress inspector, attack-target highlight.
- Tests/smokes: before/after render deltas (VIS-CMD-001, GOD-SMOKE-002); VIS-RENDER-001 for site/carrier extraction.

## How to submit and observe commands

**Economy — Production chain (Load Production Demo):** Load demo → woodcutter on forest, sawmill on road network → Step 10/Until Event → watch forest density, store chips, amber carrier routes, summary wares, event log.

**Economy — Build tutorial (Load Build Tutorial):** Load demo → no pre-built site → pick storehouse placement → Submit → dashed site hex → road segment(s) → Step 10/Until Event → progress ring, carriers, checklist steps, completed STORE building.

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
| VIS-LAYOUT-001 | `run_viewer_pixel_smoke.gd` / `run_tests.gd` |
| VIS-PIX-CHAIN-001 | `run_viewer_pixel_smoke.gd` |
| VIS-PIX-BUILD-001 | `run_viewer_pixel_smoke.gd` |
| GOD-CAPTURE-001 | `run_viewer_capture.gd --assert` |
| GOD-SMOKE-001/002/003 | `run_viewer_smoke.gd` (headless) |
| GOD-SMOKE-004 | `run_viewer_pixel_smoke.gd` (non-headless) |

## Viewer launch commands

Windowed (human review, Track 2):

```powershell
$env:GODOT = "C:\Users\joesa\Downloads\Godot_v4.5.1-stable_win64.exe\Godot_v4.5.1-stable_win64_console.exe"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"
& $env:GODOT --path godot_project --resolution 1920x1080 res://client/scenes/economy_viewer_demo.tscn
& $env:GODOT --path godot_project --resolution 1920x1080 res://client/scenes/military_viewer_demo.tscn
```

Screenshot capture and pixel assert (Track 1.5 — **non-headless**):

```powershell
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_pixel_smoke.gd
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=economy --assert
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=military --assert
```

Headless Track 1:

```powershell
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd -- --smoke=all
```

## Screenshot paths

- Convention: `user://screenshots/run_05_economy_demo.png`, `user://screenshots/run_05_military_demo.png`
- Closure verification (2026-06-15, Windows Godot 4.5.1, default app user data when capture subprocess did not inherit `GODOT_USER_DATA_DIR`):
  - `C:/Users/joesa/AppData/Roaming/Godot/app_userdata/WideRTS Simulation/screenshots/run_05_economy_demo.png`
  - `C:/Users/joesa/AppData/Roaming/Godot/app_userdata/WideRTS Simulation/screenshots/run_05_military_demo.png`

## Closure verification results (2026-06-15, Windows Godot 4.5.1)

Platform: Windows 10, Godot **4.5.1.stable** console exe. No `SCRIPT ERROR` lines in any command.

| Command | Exit | Status |
|---------|------|--------|
| `run_tests.gd` | 0 | **passed** — 154/154 |
| `run_economy_smoke.gd` | 0 | **passed** |
| `run_military_smoke.gd` | 0 | **passed** |
| `run_bot_smoke.gd` | 0 | **passed** |
| `run_viewer_smoke.gd --smoke=all` | 0 | **passed** — GOD-SMOKE-001/002/003 |
| `verify_failure_modes.gd` | 0 | **passed** |
| `run_viewer_pixel_smoke.gd` | 0 | **passed** — GOD-SMOKE-004 |
| `run_viewer_capture.gd --variant=economy --assert` | 0 | **passed** — GOD-CAPTURE-001 |
| `run_viewer_capture.gd --variant=military --assert` | 0 | **passed** — GOD-CAPTURE-001 |

| Track | Status |
|-------|--------|
| 1 Headless | **passed** |
| 1.5 Pixel | **passed** |
| 2 Human | **complete** (user confirmed satisfied at 1920×1080) |

Linux/Godot 4.5.2 re-verification was not run in this closure pass; Windows pass is merge-supporting per `local_verification.md`.

## Known limitations

- Carrier position uses `edge_progress` interpolation between road nodes when in transit.
- Workers shown as per-building W# badge, not free movement on map.
- Attack/combat resolves over several ticks (range in `tick_combat`); use Step Until Event.
- Headless SubViewport screenshot capture fails (`texture_2d_get` null); use non-headless capture runner.
- Debug UI is functional, not polished (minimal zoom/pan, no animation).
- Anchor warnings on some Control nodes (`Nodes with non-equal opposite anchors`) — cosmetic; pixel smokes still pass.
- Pixel smokes (Track 1.5) require display; not CI-headless today — document as manual/non-headless gate.
- Neural training observations must come from **snapshots**, not viewport pixels (RUN_05 pixel tests are viewer gates only).

## Process issues

- `Theme.default_font` removed in 4.5; use `ThemeDB.fallback_font`.
- Economy construction site placement must avoid HQ/building footprints; build tutorial uses road-adjacent storehouse candidates from sim provider.
- Windowed Godot launches may leave processes; inspect with `Get-Process *Godot*` before re-run.

## Economy logistics visibility pass

Workers are **not** free-moving on the map — they are assigned to buildings in sim. Honest visualization uses snapshot-only layers: `tree_density`, `building_stores`, `ware_routes` (with `edge_progress` interpolation), `delivery_jobs`, `production_activity` pulse.

- **Production chain demo** (default): HQ + woodcutter + sawmill; no pre-placed site; tools-only starting wares; auto worker policy.
- **Build tutorial demo**: road stub toward placement hex; user places storehouse; checklist panel tracks site → road → deliver → complete.
- Road command enumeration expanded: road_nodes + construction_sites + HQ fallback; storehouse candidates labeled road-adjacent.
- Tests: VIS-ECON-CHAIN-001, VIS-ECON-BUILD-001 (**154** total in harness).
- Smokes: GOD-SMOKE-003 (logistics chain delta + build tutorial mini-sequence).

## Deferred

- Hybrid viewer (RUN_09), tutorial polish (RUN_10), final art (RUN_11), mobile export (RUN_12), neural training (RUN_06).
- CI wiring for pixel smokes (Track 1.5 remains manual/non-headless gate).
