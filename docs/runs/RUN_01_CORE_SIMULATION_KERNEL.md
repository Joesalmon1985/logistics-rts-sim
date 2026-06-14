# RUN_01 — Godot/GDScript Core Simulation Kernel

## Purpose

Deliver the authoritative **Godot/GDScript simulation core** infrastructure so RUN_02 and RUN_03 build without rewriting foundations. The core is pure GDScript, headless-capable, and separate from any UI/rendering.

## Branch Name

`feature/run-01-core-simulation-kernel`

## Prerequisite Documents

- `docs/design/00_project_brief.md`
- `docs/design/10_glossary.md`
- `docs/architecture/00_system_architecture.md`
- `docs/architecture/01_simulation_core_authoritative.md`
- `docs/architecture/02_command_event_snapshot_contract.md`
- `docs/architecture/05_determinism_replay_save_load.md`
- `docs/architecture/06_config_and_data_schema.md`
- `docs/testing/00_test_first_policy.md`
- `docs/testing/02_smoke_test_matrix.md` (KERN-SMOKE rows)
- `docs/design/11_design_decision_register.md` (DDR-026 GDScript-authoritative, DDR-027 integer/fixed-point, DDR-029 shared hex map are Accepted; DDR-032 test-framework choice is decided at the start of this run)

## In-Scope Work

- Godot project skeleton with the pure-GDScript simulation-core package layout (`godot_project/sim/core`, `sim/variants/{economy_trial,military_trial,hybrid}`, `sim/data`, `sim/tests`, `sim/tools`) — see `docs/architecture/01_simulation_core_authoritative.md`
- Choose and stand up the **headless Godot test harness** (custom runner / GUT / gdUnit4 — DDR-032) with one passing smoke test; record the choice in the final report
- Deterministic tick loop (no frame delta / wall-clock)
- Integer / fixed-point policy and helpers (no floats in hashed state — DDR-027)
- Seeded RNG stored in simulation state
- **Shared axial hex map primitives** (axial hex, terrain, height) used by all variants (DDR-029)
- Command base structures and `CommandResult` shape
- Event structures and event log
- Entity ID assignment
- Snapshot exporter (JSON, schema_version 1)
- Replay hash/check mechanism (stable cross-platform canonicalisation)
- Variant registry (economy_trial, military_trial, hybrid stubs)
- Headless smoke runner (CLI entry under `sim/tools/`)
- Config loader with validation
- Minimal scenario fixture
- CLI documentation for running tests and smokes (Godot headless invocation)

## Out-of-Scope Work

- Economy chains, carriers, production (RUN_02)
- Military combat, units (RUN_03)
- Godot visual client / UI scenes / rendering (`client/`, RUN_05)
- Python tooling/training (RUN_06)
- Bots beyond stub (RUN_04; in-engine GDScript bots)
- Any gameplay mechanics beyond the minimal variant stubs needed to prove the kernel

## Test-First Requirements

- Write tests for: RNG determinism, hex neighbours, command validate/reject, snapshot export, replay hash
- KERN-SMOKE-001 through KERN-SMOKE-004, run via the headless test harness

## Implementation Requirements

- Pure GDScript simulation core: no dependence on scene tree, Node/sprite positions, animation, frame delta, physics, cameras, UI nodes, input events, or wall-clock time
- Headless CLI: create game, tick, export snapshot, replay
- No gameplay rules beyond map + tick + noop commands and minimal variant stubs
- Integer / fixed-point math for all authoritative hashed state

## Smoke-Test Requirements

| ID | Pass |
|----|------|
| KERN-SMOKE-001 | game_created |
| KERN-SMOKE-002 | 10 ticks |
| KERN-SMOKE-003 | valid snapshot JSON |
| KERN-SMOKE-004 | replay hash match |

## Acceptance Checklist

- [ ] `sim/` GDScript core loads and runs headlessly
- [ ] Headless test harness chosen (DDR-032), documented, and passes all kernel tests
- [ ] Command registry accepts registered command types
- [ ] Events append with monotonic IDs
- [ ] Snapshot exports map + tick + seed
- [ ] Replay hash stable across two runs (integer/fixed-point state only)
- [ ] Shared axial hex map primitives present and used by variant stubs (DDR-029)
- [ ] Variant registry loads three variant names
- [ ] Smoke runner executes scenario fixture (KERN-SMOKE-001–004)
- [ ] Core has no dependency on scene tree / rendering / UI / frame delta / wall-clock
- [ ] No Godot visual-client/UI/scene/rendering code (that is RUN_05)

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
