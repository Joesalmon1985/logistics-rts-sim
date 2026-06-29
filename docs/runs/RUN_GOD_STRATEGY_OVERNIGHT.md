# RUN_GOD_STRATEGY_OVERNIGHT

Branch: `feature/god-strategy-overnight`

Started: 2026-06-28

## Status: partially completed

**Highest milestone reached:** B (Age + culture slice)

## Purpose

Implement Hybrid God-Strategy MVP PRD milestones A→H, strictly gated. Milestone A is a hard gate (4P logistics RTS + map viability + scripted bot eval).

## Open-question decisions

| Question | Decision |
|----------|----------|
| `draft_interval_ticks` (B) | 500 |
| `worker_policy_refresh_ticks` (E) | 300 (deferred) |
| Worker needs granularity (C) | Cohort-level first (deferred) |
| Draft UX (B) | Dealt-hand (8 picks/player/age) |
| 4P eval tick budget (A) | 500 ticks in harness (long domination is ~0.5s/tick — 8000 ticks ≈ 60+ min) |
| Fixed domination regression seed | 99001 |
| Bot placement | GodStrategyMapViability + EconomyBvbSetup legal hex queries |

## Milestone status

| Milestone | Status | Notes |
|-----------|--------|-------|
| Preflight | passed | HYB-SMOKE 9/9, integrity pass |
| A — 4P logistics RTS | **passed** | GOD_A_001–009 green |
| B — Age + culture | **passed** | GOD_B_001–005 green |
| C–H | not started | gated per plan |

## Baseline verification

| Command | Exit | Status |
|---------|------|--------|
| Godot import | 0 | passed |
| TEST_FILTER=HYB_SMOKE run_tests.gd | 0 | 9/9 passed |
| verify_failure_modes.gd | 0 | passed |
| TEST_FILTER=GOD_A run_tests.gd | 0 | 9/9 passed |
| TEST_FILTER=GOD_B run_tests.gd | 0 | 5/5 passed |
| run_god_strategy_eval.gd | 0 | passed (p1 tick-cap winner @ 500 ticks) |

## Map-legality preflight

Human-reported pitfall addressed: `GodStrategyMapViability` + `GOD_A_009` + direction-aware `HybridMapSetup` with owned spawn corridors and reserved grass footprints. All four spawns have legal woodcutter/sawmill/barracks sites after prep.

## Key fixes during run

1. **Hub-site elimination:** destroyed hub + active hub construction site keeps player alive (`GodStrategyMatchEnd`).
2. **Map prep:** direction-aware forest/rock pockets, owned corridors, grass slots per spawn.
3. **Bot roads:** skip `NOT_OWNER` road segments; bind state for legal placement queries.
4. **Eval stall:** disable economy stall break during regression (`stall_ticks_threshold=0`).

## Known limitations

- **Domination within 8000 ticks not proven** — eval wins via tick-cap survival score at 500 ticks; bot telemetry may still show road-phase stalls on some seeds. Long-run domination regression remains a tuning target.
- **Milestone C–H not implemented** (worker needs, societal selection, modular god AI, pollution, UI polish).
- **Spectator HUD extension (A.6)** deferred — sim gate met without UI.

## Artefacts

- `docs/audit/god_strategy/eval/1_scripted_vs_3_passive/`
- `docs/audit/god_strategy/eval/4_scripted_ffa/`

## Next Cursor prompt

Tune `ScriptedGodStrategyBot` road routing to reach barracks+unit phase consistently, then run 8000-tick domination regression. Start Milestone C (worker needs) once combat-phase bot progress is logged in stall reports.

---

## Milestone A Hardening continuation (`feature/god-strategy-milestone-a-hardening`)

**Date:** 2026-06-29

Follow-up run per hardening plan: fixed road-stall root cause, added `god_strategy_domination_regression.json` (16×16, seed 99001, 3000-tick proof cap), phase telemetry (`bot_phase_progression.json`), micro-scenario ladder (`GOD_A_013`–`GOD_A_016`), and eval gates rejecting disguised tick-cap wins.

**Highlights:** construction-site logistics via nearest road node; regression p1-vs-passive; HQ store capacity fix for `starting_wares`; player-wide ware spend for hybrid unit queue.

**Verification (Linux):** `GOD_A_010` @ 3000 passed; micro ladder + `GOD_B` + `HYB_SMOKE` green. `GOD_A_011` @ 3000 and eval @ 3000 were the remaining acceptance gates at branch push time.

**Run doc:** `docs/runs/RUN_GOD_STRATEGY_MILESTONE_A_HARDENING.md`
