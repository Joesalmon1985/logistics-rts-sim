# RUN_10 — Human Playable Hybrid

## Purpose

Make hybrid game human-playable on mobile with touch UX, tutorial, and player diagnostics.

Delegated control only — no unit micro UI.

## Branch Name

`feature/run-10-human-playable-hybrid`

## Prerequisite Documents

- `docs/design/07_human_play_design.md`
- `docs/design/04_hybrid_game_requirements.md`
- `docs/runs/RUN_09_GODOT_2D_HYBRID_OBSERVER.md` (complete)

## In-Scope Work

- Touch-optimized UI per human play design
- Tutorial scenario flow covering logistics-fed military, delegated attack, defence, repair
- Player-readable diagnostics panels (stall, power, production, repair, intel)
- HUM-SMOKE-001, HUM-SMOKE-002

## Out-of-Scope Work

- Campaign progression (RUN_11)
- Multiplayer (RUN_12)

## Test-First Requirements

Automated tests where possible; manual checklist for touch UX.

## Implementation Requirements

- Delegated control only; no unit micro UI
- Economy priority UI accessible
- Fog/shroud display matches the authoritative snapshot
- The authoritative GDScript simulation core runs **in-process** inside the app; the UI is non-authoritative and submits commands to it. No Python runtime, embedded interpreter, subprocess, or socket-to-Python for gameplay (DDR-026)

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_10 section.

## Acceptance Checklist

- [ ] Tutorial completable start to finish (economy → military → delegated attack → win)
- [ ] Touch targets meet minimum size (44pt equivalent)
- [ ] Stall/power/production/repair/intel panels readable
- [ ] HUM-UX-*, HUM-CTRL-*, HUM-TUT-* criteria met
- [ ] HUM-SMOKE-001–002 pass

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
