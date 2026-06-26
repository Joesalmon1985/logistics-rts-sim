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
TEST_FILTER=settler timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_settler_mvp_smoke.gd
```

Full closure: prior kernel/economy/viewer smokes + `verify_failure_modes.gd` per `docs/verification/local_verification.md`.
