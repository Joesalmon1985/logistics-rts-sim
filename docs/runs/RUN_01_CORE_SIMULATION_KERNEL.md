# RUN_01 — Core Simulation Kernel

## Purpose

Deliver real simulation infrastructure so RUN_02 and RUN_03 build without rewriting foundations.

## Branch Name

`feature/run-01-core-simulation-kernel`

## Prerequisite Documents

- `docs/design/00_project_brief.md`
- `docs/design/10_glossary.md`
- `docs/architecture/00_system_architecture.md`
- `docs/architecture/01_python_sim_authoritative_core.md`
- `docs/architecture/02_command_event_snapshot_contract.md`
- `docs/architecture/05_determinism_replay_save_load.md`
- `docs/architecture/06_config_and_data_schema.md`
- `docs/testing/00_test_first_policy.md`
- `docs/testing/02_smoke_test_matrix.md` (KERN-SMOKE rows)
- `docs/design/11_design_decision_register.md` (confirm DDR-027 float policy and DDR-029 shared map before fixing hash/math conventions)

## In-Scope Work

- Package layout (`sim/`, `tests/`, `scenarios/`, `config/`)
- pytest scaffold and one passing smoke test
- Command base classes and `CommandResult` shape
- Event base classes and event log
- Seeded RNG wrapper stored in state
- Map primitives (axial hex, terrain, height)
- Entity ID assignment
- Snapshot exporter (JSON, schema_version 1)
- Replay hash/check mechanism
- Variant registry (economy_trial, military_trial, hybrid stubs)
- Smoke runner skeleton (CLI or pytest entry)
- Config loader with validation
- Minimal scenario fixture

## Out-of-Scope Work

- Economy chains, carriers, production (RUN_02)
- Military combat, units (RUN_03)
- Godot project (RUN_05)
- Bots beyond stub (RUN_04)

## Test-First Requirements

- Write tests for: RNG determinism, hex neighbours, command validate/reject, snapshot export, replay hash
- KERN-SMOKE-001 through KERN-SMOKE-004

## Implementation Requirements

- Headless CLI: create game, tick, export snapshot, replay
- No gameplay rules beyond map + tick + noop commands
- Integer math preferred

## Smoke-Test Requirements

| ID | Pass |
|----|------|
| KERN-SMOKE-001 | game_created |
| KERN-SMOKE-002 | 10 ticks |
| KERN-SMOKE-003 | valid snapshot JSON |
| KERN-SMOKE-004 | replay hash match |

## Acceptance Checklist

- [ ] `sim/` package importable
- [ ] pytest passes all kernel tests
- [ ] Command registry accepts registered command types
- [ ] Events append with monotonic IDs
- [ ] Snapshot exports map + tick + seed
- [ ] Replay hash stable across two runs
- [ ] Variant registry loads three variant names
- [ ] Smoke runner executes scenario fixture
- [ ] No Godot code

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
