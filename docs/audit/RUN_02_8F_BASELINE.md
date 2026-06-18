# RUN_02.8F — Baseline (Phase 0)

Date: 2026-06-17  
Branch: `feature/run-02-8f-granular-bot-proof-diagnostics`  
Starting commit: `02aac40` (branch created from `main`)

## Git state at start

```
Branch: feature/run-02-8f-granular-bot-proof-diagnostics (new from main)
Untracked: .godot_user_parallel/, various .uid files, sim/tools debug scripts
```

## Focused baseline commands

Environment:
```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$PWD/godot_project/.godot_user_parallel/worker_01"
```

| Command | Exit | Status | Notes |
|---------|------|--------|-------|
| `TEST_FILTER=test_econ_bot_002 timeout 300s ... run_tests.gd` | 1 | failed | Expected: p1/p2 bot-built buildings gate (204s wall) |
| `TEST_FILTER=econ_human_proof timeout 120s ... run_tests.gd` | 0 | passed | 4/4 |
| `timeout 120s ... run_economy_human_proof_smoke.gd -- --smoke=all` | 0 | passed | HUMAN PROOF SMOKE PASS |
| `timeout 60s ... verify_failure_modes.gd` | 0 | passed | INTEGRITY PASS |

## ECON_BOT_002 failure detail

```
FAIL test_ECON_BOT_002_dual_bot_expansion_metrics — p1 bot-built buildings; p2 bot-built buildings
TIMING: 204.10s
EXIT: 1 (fail)
```

Roads and deliveries gates not reported as failing in summary line (buildings gate only in failure message).

## Notes

- Initial `timeout 120s` with `TEST_FILTER=econ_bot_002` timed out (exit 124) — single test needs ~204s; use 300s+ for ECON_BOT_002.
- First sandbox run segfaulted (exit 139); full permissions required for Godot userdata path.
