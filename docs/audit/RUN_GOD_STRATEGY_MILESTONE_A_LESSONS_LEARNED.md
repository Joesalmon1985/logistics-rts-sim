# RUN — God-Strategy Milestone A Hardening — Lessons Learned

**Date:** 2026-06-30  
**Branch:** `feature/god-strategy-milestone-a-hardening`

## What worked

- **Micro-scenario ladder** (`GOD_A_013`–`GOD_A_016`) isolated road, logistics, and production failures in &lt; 5 min each; integration tests only ran after micro green.
- **Pre-mortem audit** (`PRE_MORTEM_MILESTONE_A.md`, layout sweep) caught overlapping footprints and corridor ownership before bot tuning.
- **`p1_only_scripted` regression** removed multi-bot carrier contention; eval and integration tests now agree on harness shape.
- **Phase telemetry** (`bot_phase_progression.json`) made disguised stalls obvious (`furthest_phase`, milestones).
- **Eval `evaluate_pass()`** correctly accepted legitimate tick-cap (unit produced + attack issued) while rejecting road_ownership stalls.

## What broke

- **`on_command_applied` early return** dropped skippable road errors → infinite `ROAD_EXISTS` / `NOT_OWNER` loops.
- **HQ `inventory_add` silent fail** at default capacity 100 when scenario `starting_wares` exceeded capacity — looked like bot stall.
- **Construction-site logistics** used site hex as road anchor on forest tiles → materials never delivered.
- **`_build_plan()` shrink** left `plan_index` past end → perpetual `wait_production`.
- **Hybrid unit queue** required HQ wares while logistics had moved rations to building stores.
- **Multi-spawn prep** overwrote ownership; **HQ hex** blocked road start without `_resolve_road_start`.

## Test / verification lessons

- Wrap long Godot runs in `timeout 2400s`; `GOD_A_011` ~33 min, eval domination_regression ~32 min @ 3000 ticks.
- `grep`-filtered tails hide `PASS`/`FAIL`; capture full log (`tee`) for long integration runs.
- `verify_failure_modes.gd` and `GOD_A_007` are fast sanity checks before multi-hour suites.
- Update `local_verification.md` when tick budgets change (3000 proof cap, 2400s eval timeout).

## Risks for next runs

- **Domination** not proven in regression eval — tick-cap win only; enemy hub survives 3000 ticks.
- **CanonicalJson float warnings** during replay hash — noisy logs; fix floats in authoritative state separately.
- **Ratchet** `PROOF_TICK_CAP` down only after confirming `furthest_phase` still climbs in last 500 ticks.
- Do not start Milestone C+ until domination or explicit combat-speed variant is scoped.

## Prompt improvements

- Always specify `p1_only_scripted` for regression/eval harnesses.
- Require micro ladder green before integration @ 3000 ticks.
- List HQ capacity and `starting_wares` when adding scenario prep.
