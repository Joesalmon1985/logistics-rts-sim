# Tutorial Legibility — Play Guide

## Launch

```bash
./scripts/run_game.sh
```

For tutorial-focused launch (opens main menu):

```bash
./scripts/run_tutorial.sh
```

Choose **Tutorial** from the main menu.

## What you should see (1920×1080)

- **Left/center:** map with buildings, roads, carriers
- **Right panel:** tutorial instructions, objective checklist, stockpile, build menu, inspector, help
- **Top bar:** Play/Pause, speed, tick, current mode

## Tutorial flow (steps 1–11)

1. Welcome and objective
2. Pan/zoom the map
3. Click Headquarters — read inspector
4. Press Play
5. Enter Road mode and extend roads
6. Build Woodcutter, Forester, Sawmill, Quarry (guided)
7. Watch carriers deliver goods

## Controls

- **Space** — Play/Pause
- **.** / **Enter** — Step one tick while paused
- **R** — Road mode
- **1–4** — Woodcutter / Forester / Sawmill / Quarry build modes
- **Esc** — Cancel mode
- **H** — Toggle help panel

## Screenshot verification

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_human_understandable_screenshots.gd
```

Outputs land in `docs/audit/screenshots/human_understandable_*.png`.
