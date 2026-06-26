# Settler MVP — Play Guide

## Launch (live GUI)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
"$GODOT" --path godot_project
```

Or use the helper script:

```bash
./scripts/launch_settler_mvp_gui.sh
```

From the main menu choose **New Settler MVP Game**.

Alternative: `res://client/scenes/settler_mvp_game.tscn` directly.

The game starts **paused** so you can inspect the map first. Press **Play** or **Space** to start the simulation; you should see **Tick** increase within two seconds.

## Screenshot review (headless / agent)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_settler_mvp_screenshot.gd
timeout 90s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_settler_mvp_interaction_screenshots.gd
```

Outputs under `docs/audit/screenshots/`:

- `settler_mvp_initial.png` + `settler_mvp_initial_review.json` / `.md`
- `settler_mvp_00_initial.png` … `settler_mvp_04_build_mode.png` + per-step JSON

**Git note:** PNGs are committed for this debugging run. If screenshot churn grows, keep JSON/MD committed and gitignore regenerated PNGs in a later cleanup.

If no display session is available for manual play, report: *Live GUI window test was not run because no display session was available. In-engine screenshot capture was run instead.*

## Objective

Build a working starter economy:

- Complete **Woodcutter Lodge**, **Forester Camp**, **Sawmill**, and **Quarry**
- Connect each to **Headquarters** by roads
- Store at least **10 planks** and **10 stone** in your buildings (HQ counts)

The objective panel tracks each requirement. Victory appears when all are met.

## Time controls

HUD shows: `Simulation: Paused/Playing | Speed: 1x/2x/4x | Tick: N`

| Control | Action |
|---------|--------|
| **Play** / **Pause** | Run or stop simulation ticks |
| **Step** | Advance one tick while paused |
| **1x / 2x / 4x** buttons | Game speed |
| **Space** | Toggle Play/Pause |
| **.** or **Enter** | Step one tick (while paused) |
| **F1 / F2 / F3** | Speed 1x / 2x / 4x |

Press Space or Play to start the settlement simulation. You can inspect, pan, zoom and place plans while paused.

## Map navigation

| Input | Action |
|-------|--------|
| Right-drag | Pan map |
| Mouse wheel | Zoom |
| **WASD** / arrow keys | Pan map |
| **+** / **-** | Zoom in/out |
| Left-click | Inspect hex / place building or road |

## Placing buildings

1. Click a build button or press **1–4** (Woodcutter, Forester, Sawmill, Quarry).
2. Move the cursor over the map — green = legal, red = illegal (reason shown).
3. Click a valid hex to place a construction site.
4. Press **Cancel** or **Esc** to exit build mode.

Terrain hints:

- Woodcutter / Forester: forest hexes with trees
- Sawmill: grass
- Quarry: rock hexes with revealed deposit

## Placing roads

1. Click **Road** or press **R**.
2. Click a start hex on your existing road network (near HQ).
3. Click a destination hex — the game auto-routes along existing roads and places only missing segments.
4. **Esc** cancels road mode.

## Reading progress

- **Stock** — timber, planks, stone, tools in your economy
- **Warnings** — blocked construction, missing inputs, delivery issues
- **Inspector** — click any hex for details
- **Event log** — recent actions and messages
- **H** — toggle help panel

## Blocked buildings

Common messages:

- `Waiting for planks/stone/tools` — construction materials still delivering
- `No road connection to headquarters` — extend roads to the site
- `Sawmill waiting for timber` — woodcutter not producing or not connected
- `Quarry needs rock or quarryable terrain nearby` — wrong placement

## Victory

When the objective panel shows all checks complete, a **Victory** banner appears. There is no failure timer — take your time.

## Fast verification

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
timeout 900s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_settler_mvp_smoke.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_settler_mvp_screenshot.gd
timeout 90s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_settler_mvp_interaction_screenshots.gd
```

Filter settler tests only:

```bash
TEST_FILTER=settler timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
```

## Manual acceptance checklist

| Check | Result |
|-------|--------|
| Can launch main menu | |
| Can start Settler MVP | |
| Screenshot capture works | |
| Screenshot review shows map/HUD/objective/help | |
| Controls/help visible | |
| Mouse wheel zooms map | |
| Keyboard +/- zooms map | |
| Mouse drag pans map | |
| Keyboard WASD/arrows pan map | |
| Left click selects/inspects hex while paused | |
| Selection inspector updates | |
| No yellow focus on decorative title when clicking map | |
| Space toggles pause/play | |
| Play mouse click starts ticking (Tick increases within 2s) | |
| Number keys enter build modes | |
| Build menu enters placement mode | |
| Placement preview appears | |
| Valid building placement creates construction site | |
| Invalid placement shows readable reason | |
| Road mode start/end clicks work | |
| Roads render after placement | |
| Objective panel updates | |
| Known issues | |

## Debug viewers

From the main menu, **Open Debug Viewers** launches the economy command-list harness (`economy_viewer_demo.tscn`) for developers.
