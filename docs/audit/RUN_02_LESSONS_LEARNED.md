# RUN_02 Lessons Learned — Trial Economy Game

## What worked

- Test-first economy package under `godot_project/sim/economy/` integrated cleanly with RUN_01 kernel (command pipeline, snapshots, replay hash, variant registry).
- Headless harness scaled to **39 tests** (13 RUN_01 + 26 economy) with deterministic CLI pass/fail.
- ECON-SMOKE-001..005 and heuristic bot (ECON-SMOKE-005) validate real logistics-fed construction on `tiny_success_map`.
- Integer-only hashed state preserved; `EconomyConfig` kept out of `GameState.entities` meta.

## What broke / surprises

- **Paused production/buildings never resumed** until `_tick_building` allowed paused states to re-evaluate each tick.
- **Blocked construction sites stopped requesting materials** — must request for `CONS_BLOCKED` as well as `CONS_ACTIVE`.
- **Completed construction sites blocked footprints** — exclude `CONS_COMPLETED` / `CONS_CANCELLED` from overlap checks.
- **Bot axis-aligned road paths are invalid on hex grids** — must step through hex neighbours (greedy distance descent).
- **Build-radius + 2×2 footprints** — smelter/workshop anchors must keep all footprint hexes inside radius and avoid HQ/building overlap.
- **Stale delivery jobs** (`waiting_for_ware` with empty HQ source) blocked new jobs — source reassignment + `_has_pending_job` fix.
- **`economy_init.gd`** — `_create_hq` referenced `scenario` without parameter (compile error caught by smoke).

## economy_trial hash re-pin (deliberate)

| | Value |
|---|--------|
| Old (RUN_01 stub) | `sha256:4fe90e016f00e3b623f90ef1d984cc339d5dd696adfffd6eaf7efa6f64c0ca6a` |
| New (RUN_02 real economy) | `sha256:1d69994a924390478e89397dc3e0ff9b5d65118eeb2995fe1de8c53a5898332b` |
| Command | `run_smoke.gd --variant=economy_trial --ticks=10 --seed=1` on `scenarios/kernel_smoke.json` |
| Verified | Triple-run identical hash before pin |
| Reason | Real economy subsystems (init, tick, entities) replace stub variant behaviour |
| Untouched | `military_trial`, `hybrid` golden hashes |

## Verification commands (RUN_02)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=economy_trial --ticks=10 --seed=1
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=military_trial --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_military.json
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=hybrid --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_hybrid.json
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

## Risks for RUN_03+

- Bot/smoke runtime can grow with chain depth — keep timeout wrappers generous (ECON-SMOKE-005 uses up to 20000 bot ticks).
- Per-tick cost rises with delivery-job churn; prune finished jobs and avoid doomed job creation.
- Scenario coverage uses one bundled map-load test; individual per-scenario behaviour tests remain optional hardening.

## Prompt improvements

- Always validate hex road paths with neighbour steps, not axial q/r Manhattan paths.
- When placing multi-hex buildings, check **every** footprint hex against HQ footprint, build radius, and completed sites.
- After pausing a building, ensure the next tick can resume when inputs/workers return.
