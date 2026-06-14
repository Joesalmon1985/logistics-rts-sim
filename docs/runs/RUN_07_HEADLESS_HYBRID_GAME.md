# RUN_07 — Headless Hybrid Game

## Purpose

Compose economy + military with hybrid integration rules per `docs/design/04_hybrid_game_requirements.md`.

## Branch Name

`feature/run-07-headless-hybrid-game`

## Prerequisite Documents

- `docs/design/04_hybrid_game_requirements.md`
- `docs/design/02_trial_economy_game_requirements.md`
- `docs/design/03_trial_military_game_requirements.md`
- `docs/testing/01_rule_contract_test_matrix.md` (HYB-* rows)
- `docs/runs/RUN_02_TRIAL_ECONOMY_GAME.md` (complete)
- `docs/runs/RUN_03_TRIAL_MILITARY_GAME.md` (complete)

## In-Scope Work

- `hybrid` variant composing modules
- Logistics-fed construction and production
- Delegated military commands; reject direct unit commands from human player
- Power/radar/research integration
- HYB-SMOKE-001 through HYB-SMOKE-005

## Out-of-Scope Work

- Godot (RUN_09)
- Hybrid bots (RUN_08)
- Human UX (RUN_10)

## Test-First Requirements

All HYB-* rows in test matrix.

## Implementation Requirements

- No instant sidebar production
- Sim AI executes unit behaviour for delegated orders

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_07 section.

## Acceptance Checklist

- [ ] Hybrid variant loads economy + military without duplication
- [ ] Military buildings require ware construction
- [ ] Unit production requires ware delivery
- [ ] Direct MoveUnitCommand rejected for human player
- [ ] DelegatedAttackCommand works on visible targets
- [ ] HYB-SMOKE-001–005 pass
- [ ] Definition of done RUN_07 items in `04_` §23 satisfied

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
