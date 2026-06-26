# RUN_PLAYABLE_SETTLER_MVP

Status: Implementation run for [RUN_PLAYABLE_SETTLER_MVP_PRD.md](RUN_PLAYABLE_SETTLER_MVP_PRD.md)  
Branch: `feature/playable-settler-mvp`

## Delivered

- Main menu (`main_menu.tscn`) with New Settler MVP Game, Debug Viewers, Quit
- Playable scene (`settler_mvp_game.tscn`) with map-driven build/road placement
- `SettlerMvpController` wrapping `ViewerController` + `CommandSubmitter`
- Deterministic scenario `settler_mvp.json` + map `settler_mvp_map.json`
- Authoritative composite objective (`settler_objective.gd`) exported in snapshots
- Starting roads via `EconomyRoadSystem.place_road` in `economy_init.gd`
- Tests and headless smoke (`run_settler_mvp_smoke.gd`)
- Keyboard fallback controls (WASD pan, +/- zoom, Space play/pause, 1–4 build, R road, F1/F2/F3 speed)
- Screenshot capture tools + review JSON/MD under `docs/audit/screenshots/`
- 13 viewer tests for input, time controls, screenshots, and HUD focus

## Acceptance (blocking)

- Human can press **Play** (mouse) or **Space** (keyboard) and see **Tick** increase within two seconds
- At 1920×1080 capture: map, objective, stockpile/status, inspector, controls/help, and warnings are on-screen
- Map clicks do not focus decorative labels; inspector updates while paused

## Launch

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
"$GODOT" --path godot_project --resolution 1920x1080
```

Or directly:

```bash
"$GODOT" --path godot_project res://client/scenes/settler_mvp_game.tscn
```

## Objective

Build Woodcutter, Forester, Sawmill, and Quarry (each road-connected to HQ) and store at least 10 planks and 10 stone.

## Verification

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
TEST_FILTER=settler timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_settler_mvp_smoke.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_settler_mvp_screenshot.gd
timeout 90s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_settler_mvp_interaction_screenshots.gd
```

Full closure: prior kernel/economy/viewer smokes + `verify_failure_modes.gd` per `docs/verification/local_verification.md`.

See [RUN_PLAYABLE_SETTLER_MVP_PLAY_GUIDE.md](../audit/RUN_PLAYABLE_SETTLER_MVP_PLAY_GUIDE.md) for live launch, keyboard map, and manual checklist.
