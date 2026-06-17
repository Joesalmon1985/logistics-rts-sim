# RUN_02.8c — Test Runtime Report

Date: 2026-06-16  
Branch: `feature/run-02-8c-test-runtime-efficiency`  
Base: `main` @ `ae78996`

Environment:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
```

Stale process noted (not killed): PID 16910 — `Godot ... --path godot_project --script res://sim/tools/run_economy_bvb_chain_debug.gd`

---

## Phase A — Baseline (pre-edit)

| Command | Status | Exit | Wall (s) | Notes |
|---------|--------|------|----------|-------|
| `timeout 580s ... run_tests.gd` | **timed out** | 124 | 580 | 160/188 tests reached before timeout |
| (smokes/integrity) | not run | — | — | Blocked after test timeout |

Baseline test log highlights:

- `FAIL test_ECON_BOT_002_dual_bot_expansion_metrics` (pre-existing on `main`)
- No `SCRIPT ERROR` lines in captured output
- 580s timeout insufficient for full 188-test suite on reference machine

---

## Final verification (post-change)

### New runner features

| Check | Exit | Notes |
|-------|------|-------|
| `--manifest-check` | 0 | `MANIFEST OK: 188 manifest entries match 188 discovered test scripts` |
| `TEST_FILTER=definitely_no_such_test` | 1 | `ERROR: zero tests selected (mode=filter)` |
| `TEST_FILTERS=econ_bot_b001,econ_bot_c001,econ_bot_d001,econ_bot_e001` | 0 | 4/4 pass, **3.89s** total (early exit) |
| `TEST_FILTER=econ_bot_b001` | 0 | 1/1 pass, **0.64s** |

### Full serial suite

```bash
timeout 900s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd \
  -- --timing-json=training_runs/test_timings/run_02_8c_serial_final.json
```

| Metric | Value |
|--------|-------|
| Exit | 1 (ECON_BOT_002 fail — pre-existing) |
| Wall | 556 s |
| Results | 188/188 ran, 187 passed, 1 failed, 0 errored |
| SCRIPT ERROR | none |
| TIMING total | 554.65s |

**Slowest 20 (final serial):**

| Duration | Test |
|----------|------|
| 180.75s | test_ECON_BOT_003_dual_replay_hash |
| 91.16s | test_ECON_BOT_002_dual_bot_expansion_metrics |
| 61.82s | test_POLICY_CONTRACT_002_episode_result |
| 58.50s | test_ECON_BOT_G001_road_cap |
| 45.58s | test_ECON_BOT_F001_dual_symmetry_minimal |
| 41.72s | test_BOT_DET_001_bot_rollout_deterministic |
| 26.13s | test_ECON_BOT_004_observation_boundary |
| 13.59s | test_TRN_ENV_003_replay_round_trip |
| 10.84s | test_VIS_ECON_BUILD_001_build_tutorial_flow |
| 2.05s | test_VIS_ECON_CHAIN_001_production_chain_delta |
| 1.78s | test_ECON_BOT_E001_completes_woodcutter |
| 1.24s | test_MIL_REPLAY_002_skirmish_replay_hash |
| 0.92s | test_ECON_BOT_D001_site_receives_delivery |
| 0.77s | test_MIL_SMOKE_008_win_loss_triggers |
| 0.64s | test_MIL_VICTORY_001_hub_destroyed |
| 0.63s | test_MIL_SKIRMISH_001_seeded_setup_deterministic |
| 0.63s | test_ECON_BOT_B001_single_building_plan |
| 0.63s | test_MIL_MAPGEN_002_seeded_map_deterministic |
| 0.60s | test_REP_SMOKE_001_export_import_identical_hash |
| 0.57s | test_MIL_HUB_001_hub_destruction_defeats_player |

**Early-exit improvement (B–E slice):** same four tests now complete in **3.89s** vs burning full tick caps per test before.

### Smokes and integrity

| Command | Exit | Wall (s) | Notes |
|---------|------|----------|-------|
| `run_economy_smoke.gd` | 1 | 11 | ECON-SMOKE-006 hash mismatch (pre-existing on `main`) |
| `run_bot_smoke.gd` | 1 | 57 | REP-SMOKE-001 hash_mismatch (pre-existing on `main`; verified with main script) |
| `run_viewer_smoke.gd` | 0 | 21 | VIEWER SMOKE PASS |
| `verify_failure_modes.gd` | 0 | 17 | INTEGRITY PASS |

### Phase H — Parallel wrapper (experimental, opt-in)

```bash
python3 python_tools/testing/parallel_godot_tests.py \
  --godot "$GODOT" --project godot_project --parallel 2 \
  --timeout-per-worker 900 \
  --timing-json training_runs/test_timings/parallel_run_02_8c_v2.json
```

| Metric | Value |
|--------|-------|
| Exit | 1 (worker 01: ECON_BOT_002 fail) |
| Wall | **353 s** (concurrent workers) vs **556 s** serial |
| Aggregate | 188 selected, 188 unique ran, no duplicates, no missing |
| Worker logs/timing JSON | under `training_runs/test_timings/` work dir |

Parallel mode is **not** the authoritative merge gate.

---

## What changed

- [`godot_project/sim/tools/test_runner_support.gd`](../../godot_project/sim/tools/test_runner_support.gd) — selection, manifest integrity, timing export
- [`godot_project/sim/tools/run_tests.gd`](../../godot_project/sim/tools/run_tests.gd) — timing, filters, `--list`, `--manifest-check`, shard inputs, zero-match fail
- [`godot_project/sim/tests/sim_test_loop.gd`](../../godot_project/sim/tests/sim_test_loop.gd) — early-exit helpers
- Econ bot B/C/D/E tests — condition-based early exit (same MAX_TICKS caps)
- [`godot_project/sim/tools/run_bot_smoke.gd`](../../godot_project/sim/tools/run_bot_smoke.gd) — export opt-in; internal temp for REP-SMOKE-003
- [`python_tools/testing/parallel_godot_tests.py`](../../python_tools/testing/parallel_godot_tests.py) — external 2-worker prototype
- [`docs/verification/local_verification.md`](../verification/local_verification.md) — tiered workflows, 188 count in active guidance

## What was deliberately not changed

- No tests deleted or assertions weakened
- No changes to ECON_BOT_002/003/F/G dual-match hash contracts
- No changes to exact-tick scenario/hash tests
- No changes to `verify_failure_modes.gd` or deliberate-fail fixture
- Historical audit reports not rewritten for count updates
- No gameplay/bot logic fixes for pre-existing failures

## Remaining slow hotspots

1. `test_ECON_BOT_003_dual_replay_hash` (~181s) — two full 800-tick dual matches
2. `test_ECON_BOT_002` (~91s) — dual match + **currently failing** on `main`
3. `test_POLICY_CONTRACT_002_episode_result` (~62s)
4. `test_ECON_BOT_G001_road_cap` / `test_ECON_BOT_F001` (~46–59s)
5. `test_BOT_DET_001` (~42s) — 3000-tick rollout ×2

## Pre-existing failures blocking full closure

| Item | Symptom | Verified on `main`? |
|------|---------|---------------------|
| ECON_BOT_002 | buildings threshold not met | yes (baseline + final) |
| ECON-SMOKE-006 | pinned hash mismatch | yes (not re-pinned in this run) |
| REP-SMOKE-001 | replay hash_mismatch | yes (main `run_bot_smoke.gd`) |

These are out of scope for RUN_02.8c (infrastructure only) but block a clean Tier 2 gate on current `main`.
