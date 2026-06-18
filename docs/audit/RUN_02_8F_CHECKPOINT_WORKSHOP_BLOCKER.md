# RUN_02.8F — Workshop Blocker Checkpoint

Date: 2026-06-17  
Branch: `feature/run-02-8f-granular-bot-proof-diagnostics`  
Base commit: `02aac40`

## Git state at checkpoint

```
Branch: feature/run-02-8f-granular-bot-proof-diagnostics
Modified: economy_expansion_bot.gd, economy_command_availability.gd, economy_bvb_setup.gd,
          economy_config.gd, economy_state.gd, sim_bootstrap.gd, run_tests.gd
Untracked: economy_bvb_ladder.gd, run_economy_bvb_ladder.gd, run_economy_bvb_chain_debug.gd,
           H002/H009/H010 tests, RUN_02_8F_BASELINE.md, .godot_user_parallel/
```

## Key changed files (WIP, uncommitted)

| File | Change summary |
|------|----------------|
| `godot_project/sim/bot/economy_expansion_bot.gd` | Shortlist-based placement, `last_noop_reason`, road discipline, chain order |
| `godot_project/sim/economy/economy_command_availability.gd` | Full `build_radius`, grass fallbacks, candidate caps |
| `godot_project/sim/economy/economy_bvb_setup.gd` | Diag profiles, scenario config attach, chain helpers |
| `godot_project/sim/economy/economy_bvb_ladder.gd` | Minimal ablation ladder runner |
| `godot_project/sim/economy/economy_config.gd` / `economy_state.gd` | Scenario-local config override |
| `godot_project/sim/core/sim_bootstrap.gd` | Loads ladder / human_proof / bot scripts |
| `godot_project/sim/tools/run_economy_bvb_chain_debug.gd` | End-state chain debug on PROFILE_PROOF |

## Current test results (pre-workshop-runner)

Environment:
```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$PWD/godot_project/.godot_user_parallel/worker_01"
```

| Command | Exit | Status | Notes |
|---------|------|--------|-------|
| `TEST_FILTER=test_econ_bot_h010 ... run_tests.gd` | 0 | passed | PROFILE_PROOF uses real costs |
| `TEST_FILTER=test_econ_bot_h002 ... run_tests.gd` | 0 | passed | Legal chain spaces at proof start |
| `TEST_FILTER=test_econ_bot_002 ... run_tests.gd` | 1 | failed | Buildings gate; ~204–631s wall |
| `run_economy_bvb_chain_debug.gd` (800 ticks, proof) | 0 | diagnostic | b≈4/4; workshop site **blocked** |

## Chain-debug end state (PROFILE_PROOF @ 800 ticks)

- Both players complete: woodcutter, storehouse, forester, sawmill (b≈4 each).
- Workshop construction site present, state **blocked**.
- Bot noop: `waiting_for_site_delivery`.
- Roads: ~36–47 segments per player; cap `MAX_TOTAL_ROAD_SEGMENTS = 45` often reached before workshop connects.
- Upstream producers show pauses: `paused_missing_input`, `output_blocked` / `paused_no_output_space`, `missing_worker`.
- ECON_BOT_002 gate requires b≥5 per player — still **red**.

## Explicit scope statement

Full diagnostic collector / CSV-JSONL export package is **deferred**. This checkpoint precedes the focused workshop-debug runner and targeted fix only.

## Post-fix results

### Workshop-debug classification (PROFILE_PROOF @ 800 ticks, fixed runner)

| Profile | p1 b | p2 b | Classification |
|---------|------|------|----------------|
| `diag_real_costs_abundant` | 2 | 2 | early chain stall (not workshop) |
| `diag_real_costs_exact` | — | — | timed out @720s (pre-recorder fix) |
| `proof` | 4 | 4 | **`workshop_no_source_wares`** |

Key proof-profile findings (pre-fix):
- Wares exist globally (planks/stone/tools) but **`workshop_route_from_store_exists=false`**
- **`workshop_road_adjacent=true` was a false positive** (neighbor had road; site anchor had no segment / empty `segment_ids`)
- **`delivery_request_exists=false`** for all missing wares (no route → no job)
- Milestones: workshop site created @ tick 91, blocked same tick; zero deliveries by tick 800

### Fixes applied

1. **`economy_road_system.gd`**: sync construction-site road nodes to adjacent segments (`_sync_node_adjacent_segments`)
2. **`economy_expansion_bot.gd`**: strict road adjacency (segment on hex); connect-first active-site roads; road reserve/bootstrap caps; workshop cap-bypass blocked; `get_current_goal()`
3. **`economy_command_availability.gd`**: prioritize construction-site connect roads in bot shortlist
4. **`run_economy_bvb_workshop_debug.gd`**: time-sequenced diagnostic runner (recorder cursor perf fix)

### Latest chain-debug (@800 ticks, post-fix)

```
p1 b=4 r=41 noop=road_cap_reached:workshop
p2 b=5 r=57 noop=chain_complete  (workshop building completed)
```

p2 reliably reaches **b=5**; p1 still **b=4** (workshop plan never placed / cap+shortlist stall).

### Re-verification (exact commands, Linux Godot 4.5.2)

Environment: `GODOT_USER_DATA_DIR=godot_project/.godot_user_parallel/worker_01`, `timeout 720s` for long runs.

| Command | Exit | Status |
|---------|------|--------|
| `run_economy_bvb_workshop_debug.gd -- --list-profiles` | 0 | passed |
| `run_economy_bvb_workshop_debug.gd -- --profile=proof --max-ticks=50` | 0 | passed (smoke) |
| `run_economy_bvb_workshop_debug.gd -- --profile=proof --max-ticks=800` | 0 | passed (diagnostic) |
| `TEST_FILTER=test_econ_bot_h010 run_tests.gd` | 0 | passed |
| `TEST_FILTER=test_econ_bot_002 run_tests.gd` (run 1) | 1 | failed — p1 bot-built buildings |
| `TEST_FILTER=test_econ_bot_002 run_tests.gd` (run 2) | 1 | failed — p1 bot-built buildings |
| `TEST_FILTER=test_econ_bot_g001 run_tests.gd` | 1 | failed — p2 roads within cap (57 > 45) |
| `run_bot_smoke.gd -- --smoke=004` | 124 | timed out |

### Verdict: **still-red**

ECON_BOT_002 remains red (p1 `< b=5`). Road-cap guard G001 regressed (p2 `r=57`). Do **not** merge.

### Suggested next step

Per plan oracle stop / mixed failure: run **H006 p1 five-building oracle** to separate p1 placement infeasibility from tick-budget; then narrow p1 workshop shortlist/placement vs road-cap enforcement without exceeding `MAX_TOTAL_ROAD_SEGMENTS=45`.
