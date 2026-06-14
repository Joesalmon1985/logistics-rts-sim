# RUN_05 — Godot 2D Viewers for Trial Games

## Purpose

Godot **visual client** viewers (`godot_project/client/`) for economy and military trials — snapshot display and command submission only. The authoritative GDScript simulation core (`sim/`) already exists from RUN_01–03; the viewer renders its snapshots in-process. No Python runtime is involved.

## Branch Name

`feature/run-05-godot-2d-viewers-trial-games`

## Prerequisite Documents

- `docs/design/06_mobile_2d_visual_design.md`
- `docs/architecture/03_godot_adapter_contract.md`
- `docs/testing/03_godot_viewer_test_plan.md`
- `docs/runs/RUN_02_TRIAL_ECONOMY_GAME.md` (complete)
- `docs/runs/RUN_03_TRIAL_MILITARY_GAME.md` (complete)

## In-Scope Work

- `godot_project/client/` viewer (scenes/renderers/adapters; may split economy/military scenes)
- Snapshot loader, layer renderer, command submitter (adapters submit commands to the in-process `sim/` core)
- Placeholder art
- GOD-SMOKE-001, GOD-SMOKE-002

**RUN_02 carryover:** Economy snapshot display layers and per-building diagnostics exported by RUN_02 (`ECON-SNAP-002`, `ECON-DIAG-001`) must render from snapshot JSON only — do not re-derive logistics rules in `client/`.

## Out-of-Scope Work

- Authoritative GDScript gameplay rules
- Hybrid viewer (RUN_09)
- Human tutorial polish (RUN_10)
- Mobile export packaging (RUN_12)

## Test-First Requirements

VIS-SNAP-001, VIS-CMD-001 tests per Godot test plan.

## Implementation Requirements

- The authoritative GDScript simulation core runs in-process; the visual client consumes its snapshots and submits commands (no Python, no subprocess, no socket on device)
- Visual client must not implement rules or hold authoritative state
- Optional hex grid toggle (off by default)

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_05 section.

## Acceptance Checklist

- [ ] Economy snapshot renders all layers
- [ ] Military snapshot renders units and intel
- [ ] One command round-trip each variant
- [ ] No gameplay logic in the visual client beyond display (rules stay in `sim/`)
- [ ] GOD-SMOKE-001–002 pass

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
