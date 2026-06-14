# RUN_07 — Headless Hybrid Game

## Purpose

Compose economy + military with hybrid integration rules per `docs/design/04_hybrid_game_requirements.md`.

Trial→hybrid delegation: no direct unit micro; logistics-fed construction, production, and repair.

## Branch Name

`feature/run-07-headless-hybrid-game`

## Prerequisite Documents

- `docs/design/04_hybrid_game_requirements.md`
- `docs/design/02_trial_economy_game_requirements.md`
- `docs/design/03_trial_military_game_requirements.md`
- `docs/testing/01_rule_contract_test_matrix.md` (HYB-* rows)
- `docs/runs/RUN_02_TRIAL_ECONOMY_GAME.md` (complete)
- `docs/runs/RUN_03_TRIAL_MILITARY_GAME.md` (complete)

## Pre-flight (before any RUN_07 implementation)

Before implementing new mechanics (see `.cursor/rules/10-run-verification-and-logging.mdc`):

1. Read `docs/audit/RUN_01_LESSONS_LEARNED.md` plus the RUN_02 and RUN_03 lessons-learned notes if present.
2. Confirm RUN_01 kernel regression per `docs/verification/local_verification.md`:
   - `run_tests.gd` — 13/13 pass, exit 0
   - three variant smokes — exit 0 each
   - `verify_failure_modes.gd` — exit 0
3. Rerun the RUN_02 economy and RUN_03 military test/smoke suites and confirm exit 0 before composing them.
4. Use `timeout` wrappers; export `GODOT_USER_DATA_DIR`; `--import` after fresh checkout or new `class_name` types.
5. Kill only confirmed stale project Godot processes; never broad `pkill -f Godot`.
6. Do not count aborted, timed-out, errored, or not-run commands as passes. If verification fails, **stop and report** before editing gameplay systems.

## In-Scope Work

- `hybrid` variant composing economy + military modules (shared hex map, no duplication)
- **No credits** — all hybrid construction, production, repair, and research are ware/logistics based (DDR-031)
- Logistics-fed construction, production, and repair
- Delegated military commands per translation table `04_` §13
- Reject all direct trial unit commands from human player
- Power/radar/tech integration
- Merged fog/intel with economy exploration
- HYB-SMOKE-001 through HYB-SMOKE-009

## Out-of-Scope Work

- Godot visual client / UI (RUN_09)
- Hybrid bots (RUN_08)
- Human UX (RUN_10)

## Test-First Requirements

All HYB-* rows in test matrix.

## Implementation Requirements

- No instant sidebar production
- No credits in hybrid: military costs are wares only; remove/override any credit-based cost from the military module (HYB-MIL-005)
- Sim AI executes unit behaviour for delegated orders, patrol zones, guard policies
- Economy deposits + ware logistics feed military production/construction/repair/research (no trial collector micro, no credit loop)

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_07 section.

## Acceptance Checklist

- [ ] Hybrid variant loads economy + military without duplication (shared hex map)
- [ ] No credits anywhere in hybrid; all costs are ware/logistics based (DDR-031)
- [ ] Military buildings require ware construction
- [ ] Unit production requires ware delivery
- [ ] Repair depot requires ware-fed repair jobs with priority policy
- [ ] All direct trial unit commands rejected for human player
- [ ] DelegatedAttackCommand, patrol zone, guard policy work on visible targets
- [ ] Power/radar/tech integration functional
- [ ] Merged fog/intel snapshot present
- [ ] HYB-SMOKE-001–009 pass
- [ ] Definition of done RUN_07 items in `04_` §23 satisfied

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
