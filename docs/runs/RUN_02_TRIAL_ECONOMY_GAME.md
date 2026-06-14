# RUN_02 — Trial Economy Game

## Purpose

Implement the permanent abstract economy engine per `docs/design/02_trial_economy_game_requirements.md`.

## Branch Name

`feature/run-02-trial-economy-game`

## Prerequisite Documents

- `docs/design/02_trial_economy_game_requirements.md`
- `docs/design/05_units_resources_buildings_catalogue.md`
- `docs/architecture/01_python_sim_authoritative_core.md`
- `docs/testing/01_rule_contract_test_matrix.md` (ECON-* rows)
- `docs/runs/RUN_01_CORE_SIMULATION_KERNEL.md` (complete)

## In-Scope Work

All ECON-* mechanic families: hex map, terrain, height/slope, buildability, deposits, ownership, roads, road nodes, pathfinding, wares, workers, carriers, storage, recipes, chains, construction, priorities, diagnostics, commands, events, snapshot fields, replay.

All 11 production chains. All required commands and event categories.

## Out-of-Scope Work

- Military rules (RUN_03)
- Hybrid rules (RUN_07)
- Godot (RUN_05)
- Bots (RUN_04)

## Test-First Requirements

Implement tests from test matrix ECON-* rows before or with each mechanic.
Minimum: all ECON-CHAIN-* scenario tests, ECON-DET-002, ECON-SMOKE-*.

## Implementation Requirements

- `economy_trial` variant fully functional headlessly
- Config-driven buildings and recipes
- Diagnostics for stall reasons

## Smoke-Test Requirements

ECON-SMOKE-001 through ECON-SMOKE-004 per `docs/testing/02_smoke_test_matrix.md`.

## Acceptance Checklist

- [ ] All mechanic families in `02_` exist in state
- [ ] All ECON-CMD-* commands work
- [ ] All ECON-EVT-* categories emitted in tests
- [ ] Full building completion chain passes integration test
- [ ] Replay hash test passes
- [ ] Definition of done in `02_` §38 satisfied
- [ ] No military or hybrid code paths active in economy_trial

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
