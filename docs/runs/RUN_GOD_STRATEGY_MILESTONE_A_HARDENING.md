# RUN — God-Strategy Milestone A Hardening

**Branch:** `feature/god-strategy-milestone-a-hardening`  
**Parent:** `feature/god-strategy-overnight`  
**Scope:** Milestone A proof only (no Milestone C+)

## Summary

Hardening run fixing the scripted-bot road stall (`NOT_OWNER` / `ROAD_EXISTS` loops), adding phase-progression telemetry, a fast 16×16 regression scenario (`seed` 99001), a micro-scenario ladder (`GOD_A_013`–`GOD_A_016`), and eval gates that reject disguised tick-cap wins.

### Root causes (confirmed)

| Issue | Symptom | Fix |
|-------|---------|-----|
| `on_command_applied` early return | Skippable `PlaceRoadCommand` errors never popped queue | Handle skippable errors before early return |
| Greedy roads / ownership wedge | `NOT_OWNER` on legal-looking segments | `GodStrategyRoadPlanner` BFS + ownership-stable prep |
| HQ hex blocks roads | Planner must start from adjacent legal hex | `_resolve_road_start` |
| Prep re-queued existing roads | `ROAD_EXISTS` infinite loop | Skip segments where road exists; skip queue when `has_delivery_path` |
| Construction on non-road hex | Materials never delivered (`CONS_BLOCKED`) | `nearest_connected_road_node` for construction-site logistics |
| Shrinking `_build_plan()` | `plan_index` past end → `wait_production` stall | `_sync_plan_to_state()` each tick |
| Completed construction sites | `_has_pending_construction` stuck true | Only count `active` / `blocked` sites |
| Regression ran 4× scripted bots | Carrier / ownership contention | `run_scripted_regression` → p1 scripted vs passive |
| Overlapping build_slots + p2 HQ | Barracks `FOOTPRINT_OVERLAP` | Non-overlapping slots; p2 spawn moved to `(14,8)` |
| Enemy tiles on road corridor | Barracks pad unreachable | Force-claim corridor tiles in prep road placement |
| HQ capacity 100 | `rations` silently failed `inventory_add` | `_expand_regression_hq_stores` tops up starting wares @ capacity 400 |
| Hybrid queue HQ-only | Wares in building stores, queue rejected | `god_strategy_player_wide_wares` meta + player-wide spend |

### Deliverables

| Area | Files / artefacts |
|------|-------------------|
| Bot | `scripted_god_strategy_bot.gd`, `god_strategy_road_planner.gd`, `god_strategy_bot_telemetry.gd` |
| Map prep | `god_strategy_test_helper.gd`, `hybrid_map_setup.gd`, `god_strategy_map_viability.gd` |
| Economy | `economy_logistics_system.gd`, `economy_road_system.gd`, `hybrid_state.gd` |
| Eval | `god_strategy_eval_runner.gd`, `run_god_strategy_eval.gd`, `god_strategy_constants.gd` |
| Scenarios | `god_strategy_domination_regression.json`, `micro/*.json` (6) |
| Tests | `GOD_A_007` (tightened), `GOD_A_010`–`GOD_A_016` |
| Audit | `PRE_MORTEM_MILESTONE_A.md`, `map_layout_sweep.json`, eval artefacts under `docs/audit/god_strategy/eval/` |

### Regression scenario (`god_strategy_domination_regression.json`)

| Property | Value |
|----------|-------|
| Map | 16×16, seed 99001 |
| Players | p1 scripted vs p2–p4 passive |
| `PROOF_TICK_CAP` | 3000 |
| p1 `build_slots` | woodcutter `(1,10)`, sawmill `(6,8)`, barracks `(8,10)` |
| p2 | `(14,8)`, `hub_hp_override: 50` |

## Milestone A sign-off answers

1. **Genuinely proven?** Micro ladder green + `GOD_A_010`/`GOD_A_011` @ 3000 ticks passed; eval @ 3000 is the remaining domination gate.
2. **p1 reach barracks?** Yes — `GOD_A_010` passed @ 3000 ticks (2026-06-29).
3. **p1 queue/produce rifle_squad?** Yes — `GOD_A_011` passed @ 3000 ticks (2026-06-29).
4. **Delegated attack?** `GOD_A_016_micro_attack` passes.
5. **Enemy hub damage?** Micro attack + eval telemetry (`enemy_hub_damaged` flag).
6. **Domination vs tick-cap?** Eval writes `match_end_reason.txt` (`domination` \| `tick_cap` \| `stall_break`).
7. **Legitimate vs disguised stall?** Eval `evaluate_pass()` requires `furthest_phase >= queue_unit` and `unit_produced` on tick-cap.
8. **Verification** — table below.
9. **Next:** Ratchet `PROOF_TICK_CAP` 3000→2500→2000 after `GOD_A_010`+`GOD_A_011` both green; refresh long-form `god_strategy_4p_mvp_a` eval separately.

## Verification results (Linux Godot 4.5.2, 2026-06-29)

| Command | Exit | Status | Notes |
|---------|------|--------|-------|
| `TEST_FILTER=GOD_A_009` | 0 | passed | Map viability (prior runs) |
| `TEST_FILTER=GOD_A_010` timeout 2400s | 0 | **passed** | ~11 min |
| `TEST_FILTER=GOD_A_011` timeout 2400s | 0 | **passed** | ~33 min (2026-06-29, post-push) |
| `TEST_FILTER=GOD_A_012` | 0 | passed | Determinism @ 800 ticks |
| `TEST_FILTER=GOD_A_013` | 0 | passed | Layout sweep |
| `TEST_FILTER=GOD_A_014` | 0 | passed | Resource audit |
| `TEST_FILTER=GOD_A_015` | 0 | passed | 4/4 micro (policy + roads) |
| `TEST_FILTER=GOD_A_016` | 0 | passed | 4/4 micro (sawmill, barracks, queue, attack) |
| `TEST_FILTER=GOD_B` | 0 | passed | 5/5 |
| `TEST_FILTER=HYB_SMOKE` | 0 | passed | 9/9 |
| `verify_failure_modes.gd` | 0 | passed | (prior batch) |
| `run_god_strategy_eval.gd` @ 3000 | — | not re-run this session | Re-run before merge |

## Verification commands

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
cd godot_project

TEST_FILTER=GOD_A_010 timeout 2400s $GODOT --headless --path . -s res://sim/tools/run_tests.gd
TEST_FILTER=GOD_A_011 timeout 2400s $GODOT --headless --path . -s res://sim/tools/run_tests.gd
TEST_FILTER=GOD_A timeout 3600s $GODOT --headless --path . -s res://sim/tools/run_tests.gd
TEST_FILTER=GOD_B timeout 180s $GODOT --headless --path . -s res://sim/tools/run_tests.gd
TEST_FILTER=HYB_SMOKE timeout 180s $GODOT --headless --path . -s res://sim/tools/run_tests.gd
timeout 2400s $GODOT --headless --path . -s res://sim/tools/run_god_strategy_eval.gd
timeout 120s $GODOT --headless --path . -s res://sim/tools/verify_failure_modes.gd
```

## Tick budget policy

| Constant | Value |
|----------|-------|
| `PROOF_TICK_CAP` | 3000 |
| `MICRO_TICK_CAP` | 800 |
| `SMOKE_TICK_CAP` | 500 |

Ratchet down after integration passes with `furthest_phase` still climbing in the last 500 ticks.

## Known issues / follow-ups

- `GOD_A_010`/`GOD_A_011` @ 3000 ticks are slow (~0.5 s/tick headless); wrap in `timeout 2400s`.
- Long-form `god_strategy_4p_mvp_a.json` @ 8000 ticks remains manual-only.
- `validate_full_chain_layout` still uses `has_road_path` (plan-only); runtime gate is `has_delivery_path` + `build_slots`.
- Do not claim full Milestone A sign-off until `GOD_A_011` @ 3000 and `run_god_strategy_eval.gd` @ 3000 both pass on CI/reviewer machine.

## Lessons learned

- **Always check HQ `inventory_free`** when scenario `starting_wares` exceed default capacity — silent add failures look like bot stalls.
- **Construction-site delivery** must use nearest connected road node, not anchor on forest/non-road hex.
- **Integration tests** must use p1-only scripted vs passive for regression, matching eval harness.
- **grep-filtered** Godot runs can appear hung while test completes; capture full log for long integration tests.
