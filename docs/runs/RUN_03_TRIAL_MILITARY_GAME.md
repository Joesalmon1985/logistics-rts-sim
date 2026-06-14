# RUN_03 — Trial Military Game

## Purpose

Implement standalone RTS trial per `docs/design/03_trial_military_game_requirements.md` with direct unit control.

Target: **OpenRA-like RTS mechanic coverage, not OpenRA feature parity.**

## Branch Name

`feature/run-03-trial-military-game`

## Prerequisite Documents

- `docs/design/03_trial_military_game_requirements.md`
- `docs/design/01_mechanic_coverage_contract.md`
- `docs/design/05_units_resources_buildings_catalogue.md`
- `docs/design/10_glossary.md`
- `docs/architecture/01_python_sim_authoritative_core.md`
- `docs/testing/01_rule_contract_test_matrix.md` (MIL-* rows)
- `docs/runs/RUN_01_CORE_SIMULATION_KERNEL.md` (complete)

## In-Scope Work

All MIL-* systems: resource fields, harvester/collector loop, buildings, construction queue, power, credits, unit/build queues, prerequisites/tech, infantry/vehicles, movement, combat, fog/shroud/vision, repair, trial unit orders (move, attack, attack-move, patrol, guard), skirmish setup, map generation hooks, commands, events, diagnostics, snapshot, replay.

Direct unit commands allowed (trial only).

## Out-of-Scope Work

- Economy ware logistics (RUN_02 integration is RUN_07)
- Hybrid delegated control (RUN_07)
- Godot (RUN_05)
- Campaign content breadth (RUN_11)

## Test-First Requirements

All MIL-* rows in test matrix. MIL-SMOKE-001 through MIL-SMOKE-009.

## Implementation Requirements

- `military_trial` variant
- Movement/pathfinding built on the **shared RUN_01 hex map** (terrain/height as passability inputs), not a parallel grid — see DDR-029
- Deterministic combat resolution with cooldown and type modifiers
- Fog/shroud/vision in snapshot for bots and Godot
- Seeded map generation hooks for skirmish
- Data-driven config for buildings, units, weapons, tech tree

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_03 section.

## Acceptance Checklist

- [ ] OpenRA-like coverage families in `03_` §7–§22 implemented (not OpenRA parity)
- [ ] Resource harvest → processor → credits loop works
- [ ] Construction queue and unit production queues with prerequisites
- [ ] Power gating and underpowered behaviour correct
- [ ] Fog/shroud/vision exported per player
- [ ] Direct MoveUnitCommand, AttackMoveCommand, PatrolCommand, GuardCommand work
- [ ] Combat with range, cooldown, armour/type modifiers
- [ ] Static defence turret auto-targeting works
- [ ] Repair depot functional
- [ ] Skirmish fixtures and seeded map generation hooks present
- [ ] MIL-SMOKE-001–009 pass
- [ ] Replay hash and diagnostics export verified
- [ ] Definition of done in `03_` §31 satisfied

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
