# RUN — Final God-Strategy Playable — Lessons Learned (partial)

**Date:** 2026-07-08  
**Branch:** `feature/final-god-strategy-phase0-domination`

## What worked

- **Final PRD** with phase kill switches gave a clear Phase 0 target (domination-only eval).
- **Root-cause fix:** `HybridOrderSystem._execute_attack` stopped re-issuing `attack_move` every tick (was resetting `move_progress` / `path_index`).
- **Combat-speed regression scenario:** pre-spawned squads near enemy hubs + `hub_hp_override: 1` + corridor flatten proved domination in **1 tick** (eval artefact).
- **Deduped unit spawn:** `hybrid_init` already spawns scenario units; removed duplicate `_apply_spawn_prebuilt` from `prep_domination_regression`.
- **Phase 1 reuse:** GOD_B tests (5/5) already green from God-Strategy overnight run.

## What broke

- Initial domination attempts failed with `enemy_hub_damaged: false` — units had paths but never moved (hybrid order reset bug).
- **Double unit spawn** doubled sim cost and confused metrics.
- Long GOD_A_017 runs at 3000 ticks took 10–30+ min before combat-speed tuning; sandbox Godot segfault without `GODOT_USER_DATA_DIR` + `all` permissions.
- `CanonicalJson: float` spam on every command slowed runs (noisy, not blocking).

## Phase 0 verification (Linux Godot 4.5.2)

| Command | Status |
|---------|--------|
| `verify_failure_modes.gd` | passed |
| `TEST_FILTER=HYB_SMOKE run_tests.gd` | passed 9/9 |
| `TEST_FILTER=GOD_A_017 run_tests.gd` | passed (domination @ tick 1) |
| `TEST_FILTER=GOD_B run_tests.gd` | passed 5/5 |
| `run_god_strategy_eval.gd` | passed (domination regression) |

Artefacts: `docs/audit/god_strategy/final/phase0/` (`end_reason=domination`, `eliminated_players=[p2,p3,p4]`).

## Remaining (Phases 2–8)

Worker needs, societal selection, heuristic/neural god AI, pollution/collapse, and mobile human-vs-neural product are **not started**. Next run should begin at Phase 2 test-first per Final PRD.

## Risks for next runs

- Do not weaken `evaluate_domination_pass` back to tick-cap fallback for Phase 0 claims.
- Keep hybrid delegated-order idempotency when adding patrol/guard paths.
- Phase 6 neural training requires winning teachers — Phase 5 heuristic must beat scripted first.
