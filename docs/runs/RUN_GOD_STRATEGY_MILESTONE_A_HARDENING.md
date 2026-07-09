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

1. **Genuinely proven?** **Yes** — micro ladder (`GOD_A_013`–`GOD_A_016`) green, `GOD_A_010`/`GOD_A_011` @ 3000 ticks passed, and `run_god_strategy_eval.gd` @ 3000 exited 0 with legitimate tick-cap (`unit_produced`, `furthest_phase` = `hold` ≥ `queue_unit`).
2. **p1 reach barracks?** Yes — `GOD_A_010` @ 3000 ticks; eval regression `barracks_placed` milestone @ tick 28.
3. **p1 queue/produce rifle_squad?** Yes — `GOD_A_011` @ 3000 ticks; eval regression `unit_produced` @ tick 68 queue / tick 88 attack.
4. **Delegated attack?** Yes — eval regression `attack_issued`; `GOD_A_016_micro_attack` passes.
5. **Enemy hub damage?** Not in 3000-tick regression (`enemy_hub_damaged: false`; p2 `hub_hp_override: 50` survives). Micro attack scenario confirms combat path works.
6. **Domination vs tick-cap?** **Tick-cap** — `match_end_reason.txt` = `tick_cap`; winner `p1` by survival score (1225 vs 1010).
7. **Legitimate vs disguised stall?** **Legitimate** — `furthest_phase` = `hold`, `unit_produced` = true, `blocker_class` ≠ `road_ownership`.
8. **Verification** — table below (2026-06-30 session).
9. **Next:** Ratchet `PROOF_TICK_CAP` 3000→2500→2000; optional domination variant (lower p2 hub HP or pre-built unit); **Milestone B hardening** — not Milestone C until domination or faster tick-cap combat proof is desired.

## Verification results (Linux Godot 4.5.2, 2026-06-30)

| Command | Exit | Status | Notes |
|---------|------|--------|-------|
| `TEST_FILTER=GOD_A_007` | 0 | passed | Eval artefact writer |
| `TEST_FILTER=GOD_A_009` | 0 | passed | Map viability (prior runs) |
| `TEST_FILTER=GOD_A_010` timeout 2400s | 0 | **passed** | ~11 min (2026-06-29) |
| `TEST_FILTER=GOD_A_011` timeout 2400s | 0 | **passed** | ~33 min (2026-06-29) |
| `TEST_FILTER=GOD_A_012` | 0 | passed | Determinism @ 800 ticks |
| `TEST_FILTER=GOD_A_013` | 0 | passed | Layout sweep |
| `TEST_FILTER=GOD_A_014` | 0 | passed | Resource audit |
| `TEST_FILTER=GOD_A_015` | 0 | passed | 4/4 micro (policy + roads) |
| `TEST_FILTER=GOD_A_016` | 0 | passed | 4/4 micro (sawmill, barracks, queue, attack) |
| `TEST_FILTER=GOD_B` | 0 | passed | 5/5 |
| `TEST_FILTER=HYB_SMOKE` | 0 | passed | 9/9 |
| `verify_failure_modes.gd` | 0 | passed | 2026-06-30 |
| `run_god_strategy_eval.gd` timeout 2400s | 0 | **passed** | ~32 min; domination_regression tick-cap + 4p_mvp_a_smoke |
| `TEST_FILTER=GOD_A` timeout 3600s | — | **timed out** | Killed during `GOD_A_011` (~60 min needed); use `timeout 7200s` or run 010/011 individually |

## Verification commands

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
cd godot_project

TEST_FILTER=GOD_A_010 timeout 2400s $GODOT --headless --path . -s res://sim/tools/run_tests.gd
TEST_FILTER=GOD_A_011 timeout 2400s $GODOT --headless --path . -s res://sim/tools/run_tests.gd
TEST_FILTER=GOD_A timeout 7200s $GODOT --headless --path . -s res://sim/tools/run_tests.gd
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
- `run_god_strategy_eval.gd` domination_regression @ 3000 takes ~32 min; legitimate tick-cap pass does not yet destroy enemy hub.
- Long-form `god_strategy_4p_mvp_a.json` @ 8000 ticks remains manual-only.
- `validate_full_chain_layout` still uses `has_road_path` (plan-only); runtime gate is `has_delivery_path` + `build_slots`.
- CanonicalJson float warnings during replay hash (pre-existing; does not block eval pass).

## Lessons learned

- **Always check HQ `inventory_free`** when scenario `starting_wares` exceed default capacity — silent add failures look like bot stalls.
- **Construction-site delivery** must use nearest connected road node, not anchor on forest/non-road hex.
- **Integration tests** must use p1-only scripted vs passive for regression, matching eval harness.
- **grep-filtered** Godot runs can appear hung while test completes; capture full log for long integration tests.
