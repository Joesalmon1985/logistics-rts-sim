# RUN_05 — Godot 2D Viewers for Trial Games

## Purpose

Godot viewers for economy and military trials — snapshot display and command submission only.

## Branch Name

`feature/run-05-godot-2d-viewers-trial-games`

## Prerequisite Documents

- `docs/design/06_mobile_2d_visual_design.md`
- `docs/architecture/03_godot_adapter_contract.md`
- `docs/testing/03_godot_viewer_test_plan.md`
- `docs/runs/RUN_02_TRIAL_ECONOMY_GAME.md` (complete)
- `docs/runs/RUN_03_TRIAL_MILITARY_GAME.md` (complete)

## In-Scope Work

- `godot_viewer/` project (or split economy/military scenes)
- Snapshot loader, layer renderer, command submitter
- Placeholder art
- GOD-SMOKE-001, GOD-SMOKE-002

## Out-of-Scope Work

- Authoritative GDScript gameplay rules
- Hybrid viewer (RUN_09)
- Human tutorial polish (RUN_10)
- Mobile export packaging (RUN_12)

## Test-First Requirements

VIS-SNAP-001, VIS-CMD-001 tests per Godot test plan.

## Implementation Requirements

- Python sim runs separately; Godot consumes snapshots
- Optional hex grid toggle (off by default)

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_05 section.

## Acceptance Checklist

- [ ] Economy snapshot renders all layers
- [ ] Military snapshot renders units and intel
- [ ] One command round-trip each variant
- [ ] No gameplay logic in GDScript beyond display
- [ ] GOD-SMOKE-001–002 pass

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
