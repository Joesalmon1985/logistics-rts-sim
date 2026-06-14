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
- `docs/architecture/01_simulation_core_authoritative.md`
- `docs/testing/01_rule_contract_test_matrix.md` (MIL-* rows)
- `docs/runs/RUN_01_CORE_SIMULATION_KERNEL.md` (complete)
- `docs/audit/RUN_01_LESSONS_LEARNED.md`
- `docs/audit/RUN_02_LESSONS_LEARNED.md`

Depends on the **shared axial hex map** primitives from RUN_01 (DDR-029) and the GDScript-authoritative core (DDR-026); military code is pure simulation-core GDScript, headless. Trial military may use credits (hybrid removes them — DDR-031).

## Pre-flight (before any RUN_03 implementation)

Before implementing new mechanics (see `.cursor/rules/10-run-verification-and-logging.mdc`):

1. Read `docs/audit/RUN_01_LESSONS_LEARNED.md` and `docs/audit/RUN_02_LESSONS_LEARNED.md`.
2. Confirm RUN_01 kernel regression per `docs/verification/local_verification.md`:
   - `run_tests.gd` — 48/48 pass (13 RUN_01 + 35 RUN_02), exit 0
   - three variant smokes — exit 0 each
   - `run_economy_smoke.gd` — exit 0
   - `verify_failure_modes.gd` — exit 0
3. Use `timeout` wrappers; export `GODOT_USER_DATA_DIR`; `--import` after fresh checkout or new `class_name` types.
4. Kill only confirmed stale project Godot processes; never broad `pkill -f Godot`.
5. Confirm exit code `0` and the expected summary on each command.
6. Do not count aborted, timed-out, errored, or not-run commands as passes. If verification fails, **stop and report** before editing gameplay systems.

## Carryover from RUN_02

Apply these RUN_02 implementation lessons when building MIL-* pathfinding, placement, and diagnostics (see `docs/audit/RUN_02_LESSONS_LEARNED.md`):

- **Hex-neighbour pathing only** — validate movement and bot paths via hex neighbour steps (greedy distance descent), not axial q/r Manhattan shortcuts.
- **Multi-hex footprints** — when placing buildings, check **every** footprint hex against build radius, HQ footprint, and overlap with completed/cancelled construction sites.
- **Job/task churn** — prune finished jobs and avoid creating doomed jobs; per-tick cost rises with stale pending work.
- **Integer-only hashed state** — credits, combat values, timers, and cooldowns in replay-hashed state must use integers or fixed-point scaled integers, never floats.

## In-Scope Work

All MIL-* systems: resource fields, harvester/collector loop, buildings, construction queue, power, credits, unit/build queues, prerequisites/tech, infantry/vehicles, movement, combat, fog/shroud/vision, repair, trial unit orders (move, attack, attack-move, patrol, guard), skirmish setup, map generation hooks, commands, events, diagnostics, snapshot, replay.

Direct unit commands allowed (trial only).

## Out-of-Scope Work

- Economy ware logistics (RUN_02 integration is RUN_07)
- Hybrid delegated control (RUN_07)
- Godot visual client / UI scenes / rendering (RUN_05)
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
- [ ] Movement/range/vision/placement/pathfinding derive from the shared RUN_01 hex map; no parallel grid (DDR-029)
- [ ] Resource harvest → processor → credits loop works (trial credits only)
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
