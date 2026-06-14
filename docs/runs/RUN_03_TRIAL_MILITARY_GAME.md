# RUN_03 — Trial Military Game

## Purpose

Implement standalone RTS trial per `docs/design/03_trial_military_game_requirements.md` with direct unit control.

## Branch Name

`feature/run-03-trial-military-game`

## Prerequisite Documents

- `docs/design/03_trial_military_game_requirements.md`
- `docs/design/05_units_resources_buildings_catalogue.md`
- `docs/testing/01_rule_contract_test_matrix.md` (MIL-* rows)
- `docs/runs/RUN_01_CORE_SIMULATION_KERNEL.md` (complete)

## In-Scope Work

All MIL-* systems: buildings, power, credits, queues, infantry, vehicles, movement, combat, intel, repair, tech, defeat, commands, events, diagnostics, snapshot, replay.

Direct unit commands allowed.

## Out-of-Scope Work

- Economy ware logistics (RUN_02 integration is RUN_07)
- Hybrid delegated control (RUN_07)
- Godot (RUN_05)

## Test-First Requirements

MIL-* rows in test matrix. MIL-SMOKE-001 through MIL-SMOKE-005.

## Implementation Requirements

- `military_trial` variant
- Deterministic combat resolution
- Fog of war in snapshot

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_03 section.

## Acceptance Checklist

- [ ] All buildings functional with power gating
- [ ] Production queues for infantry and vehicles
- [ ] Combat with armour modifiers
- [ ] Intel/fog correct
- [ ] Defeat on hub destruction
- [ ] Direct MoveUnitCommand and AttackUnitCommand work
- [ ] MIL-SMOKE-001–005 pass
- [ ] Definition of done in `03_` §25 satisfied

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
