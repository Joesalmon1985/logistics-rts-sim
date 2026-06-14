# RUN_12 — Wi-Fi Multiplayer and Packaging

## Purpose

Local Wi-Fi multiplayer, desync handling, reconnect, and mobile packaging.

## Branch Name

`feature/run-12-wifi-multiplayer-packaging`

## Prerequisite Documents

- `docs/design/09_wifi_multiplayer_design.md`
- `docs/testing/05_multiplayer_test_plan.md`
- `docs/runs/RUN_10_HUMAN_PLAYABLE_HYBRID.md` (complete)

## In-Scope Work

- Host authoritative sim over LAN
- Command relay and sync
- Desync detection and pause
- Reconnect flow
- Android export preset (minimum)
- NET-SMOKE-*, PKG-SMOKE-001

## Out-of-Scope Work

- Internet matchmaking
- Cross-platform PC client

## Test-First Requirements

Per `docs/testing/05_multiplayer_test_plan.md`.

## Implementation Requirements

- Same command validation as single-player
- Periodic hash compare

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_12 section.

## Acceptance Checklist

- [ ] Two clients sync to tick 100 with matching hash
- [ ] Desync test harness triggers pause
- [ ] Reconnect restores consistent state
- [ ] Godot export builds for Android
- [ ] NET-SMOKE-001–002, PKG-SMOKE-001 pass

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
