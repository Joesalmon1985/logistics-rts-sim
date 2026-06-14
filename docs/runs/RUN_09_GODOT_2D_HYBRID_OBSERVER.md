# RUN_09 — Godot 2D Hybrid Observer

## Purpose

Godot observer for hybrid game — combined economy and military layers with delegated command UI and fog/shroud display.

## Branch Name

`feature/run-09-godot-2d-hybrid-observer`

## Prerequisite Documents

- `docs/design/06_mobile_2d_visual_design.md`
- `docs/design/04_hybrid_game_requirements.md`
- `docs/architecture/03_godot_adapter_contract.md`
- `docs/testing/03_godot_viewer_test_plan.md`
- `docs/runs/RUN_07_HEADLESS_HYBRID_GAME.md` (complete)
- `docs/runs/RUN_05_GODOT_2D_VIEWERS_FOR_TRIAL_GAMES.md` (complete)

## In-Scope Work

- Hybrid snapshot rendering (economy + military + fog/shroud/intel layers)
- Delegated command UI (stance, rally, delegated attack, patrol zone, guard policy, repair priority)
- Reject direct unit move UI in hybrid mode
- Spectator/replay snapshot sequence display
- GOD-HYB-001, GOD-HYB-002, GOD-HYB-003

## Out-of-Scope Work

- Full tutorial (RUN_10)
- Mobile export (RUN_12)

## Test-First Requirements

VIS-* hybrid tests in Godot test plan.

## Implementation Requirements

- Reuse adapter from RUN_05 where possible
- Diagnostics overlay for hybrid stalls, power, ware shortages
- Fog/shroud rendered from snapshot only (no local inference)

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_09 section.

## Acceptance Checklist

- [ ] Hybrid snapshot renders all combined layers including fog/shroud
- [ ] Delegated attack, patrol zone, guard, repair priority round-trip works
- [ ] Direct unit move not exposed in UI
- [ ] Replay/spectator mode displays snapshot sequence without local rule inference
- [ ] GOD-HYB-001–003 pass

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
