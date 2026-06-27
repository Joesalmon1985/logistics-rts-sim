# RUN_REALTIME_TUTORIALS_AND_3D — Phase 0 current state

Audit based on headless menu-route launches after product fixes on branch `feature/realtime-tutorials-and-3d-hybrid`.

| Mode | Launches from menu | Auto-running | Press Play required | Press Step required | Tick advances ≤3s | Tutorial clear | First action clear | Resources visible | First objective completable | Blocker (before fix) | Screenshot path |
|------|-------------------|--------------|---------------------|---------------------|-----------------|----------------|-------------------|-------------------|----------------------------|----------------------|-----------------|
| Settler Tutorial | yes | **yes (after fix)** | no | no | yes | yes | yes | yes (FOREST/STONE labels) | yes | was paused by default; play_time step | `docs/audit/screenshots/realtime_tutorials_and_3d/settler_tutorial_00_launch_running.png` |
| Skirmish Tutorial | yes | **yes (after fix)** | no | no | yes | yes | yes | N/A | yes | was paused by default | `docs/audit/screenshots/realtime_tutorials_and_3d/skirmish_tutorial_00_launch_running.png` |
| Hybrid Tutorial | yes | **yes (after fix)** | no | no | yes | **improved** | yes | **yes (after forest/rock seeding)** | partial — full chain 3–5 min | flatten_grass removed map resources | `docs/audit/screenshots/realtime_tutorials_and_3d/hybrid_tutorial_01_resources_visible.png` |
| Hybrid MVP | yes | **yes (after fix)** | no | no | yes | N/A | N/A | yes | N/A | was paused by default | `docs/audit/screenshots/realtime_tutorials_and_3d/hybrid_mvp_00_launch_running.png` |
| 3D Bot Spectator | yes | **yes (after fix)** | no | no | yes | N/A | N/A | yes (3D markers) | N/A | empty scene; autoplay off | `docs/audit/screenshots/realtime_tutorials_and_3d/hybrid_3d_03_buildings_and_units_visible.png` |

## Product decision applied

Enter mode → simulation already **Playing** → player may pause with Space or Pause button. Step remains debug-only.

## Key code changes

- `settler_mvp_controller.gd`, `military_mvp_controller.gd`, `hybrid_mvp_controller.gd`: `paused = false` default
- `hybrid_3d_spectator_controller.gd`: `autoplay_enabled = true` default; warm-up bot steps on reset
- `HybridTestHelper.prepare_playable_map`: forest/rock pockets instead of `flatten_grass` for hybrid MVP
- Tutorial JSON: removed “Press Play/Step” instructions; hybrid tutorial expanded to economy→military chain
- `hybrid_3d_world_renderer.gd`: rock terrain, tree/resource 3D markers

## Verification method

Headless menu-route tests (`TEST_FILTER=realtime`, `tutorial_completable`, `3d_spectator`) and screenshot harness `capture_realtime_tutorials_and_3d_screenshots.gd`.

Real GUI checklist: **pending** — run `./scripts/run_game.sh` when `$DISPLAY` available.
