# RUN_REALTIME_TUTORIALS_AND_3D — Results

## Summary judgement

**Pass (headless gate)** — Player-facing modes now default to **Playing**; tutorials no longer instruct Press Play/Step; hybrid maps seed visible FOREST/STONE zones; 3D Bot Spectator autoplays a real bot-vs-bot hybrid match with hex board, entities, and HUD. Top-level QA script exits 0.

Real GUI manual checklist was **not run** in this session (`DISPLAY=:0` is available on the host — recommend a short `./scripts/run_game.sh` pass).

## Branch

`feature/realtime-tutorials-and-3d-hybrid` (based on `feature/manual-playability-qa-polish`)

## Commit

Uncommitted at report time (parent: `2546430`)

## Before / after

| Issue | Before | After |
|-------|--------|-------|
| Default sim state | Paused; Play required | Playing; Pause optional |
| Hybrid tutorial map | `flatten_grass` removed all resources | `prepare_playable_map` seeds forest/rock pockets |
| Tutorial copy | “Press Play / Step” | Real-time wording; hybrid 11-step economy→military chain |
| 3D spectator | Autoplay off; sparse scene | Autoplay on; warm-up ticks; terrain/rock/trees/buildings in 3D |
| Resource labels | Cryptic `D:80` only | FOREST / STONE labels on 2D map |

## Per-mode pass/fail (headless menu-route tests)

| Mode | Real-time autoplay | Tutorial journey | Resources visible |
|------|-------------------|------------------|-------------------|
| Settler Tutorial | PASS | PASS | PASS |
| Skirmish Tutorial | PASS | PASS | N/A |
| Hybrid Tutorial | PASS | PASS | PASS |
| Hybrid MVP | PASS | N/A | PASS |
| 3D Bot Spectator | PASS | N/A | PASS (snapshot + 3D markers) |

## Verification commands run

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

TEST_FILTER=realtime timeout 500s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
# 5/5 passed, exit 0

TEST_FILTER=tutorial_completable timeout 500s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
# 3/3 passed, exit 0

TEST_FILTER=3d_spectator timeout 500s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
# 21/21 passed, exit 0

timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_hybrid_bot_vs_bot_3d_smoke.gd
# PASS tick=132 buildings=8 units=1, exit 0

timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_realtime_tutorials_and_3d_screenshots.gd
# 20 PNGs + review JSON, exit 0

timeout 900s "$GODOT" --headless --path godot_project --script res://sim/tools/run_realtime_tutorials_and_3d_qa.gd
# REALTIME TUTORIALS AND 3D QA PASS, exit 0
```

## Screenshot table

20 PNGs under `docs/audit/screenshots/realtime_tutorials_and_3d/`; review JSON `realtime_tutorials_visual_review.json`. QA gate requires files on disk — all present after capture rerun.

## Known gaps

- Full hybrid tutorial completion in under 5 minutes by a human not verified in this run (headless proves navigation + resources + sim running).
- Settler Free Play / Skirmish MVP not given dedicated `test_realtime_*` entries (same controller defaults apply).
- Real GUI checklist not executed despite `$DISPLAY=:0`.
- 3D SubViewport capture logs headless texture warnings (non-fatal; PNGs still written).

## Suggested next run

Manual GUI verification pass, then commit/push `feature/realtime-tutorials-and-3d-hybrid` and open PR against `feature/manual-playability-qa-polish`.
