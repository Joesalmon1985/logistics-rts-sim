# RUN_12 — Wi-Fi Multiplayer and Packaging

## Purpose

Local Wi-Fi multiplayer, desync handling, reconnect, and mobile packaging.

Command sync must match single-player validation pipeline; fog/intel per-client from authoritative snapshot.

## Branch Name

`feature/run-12-wifi-multiplayer-packaging`

## Prerequisite Documents

- `docs/design/09_wifi_multiplayer_design.md`
- `docs/testing/05_multiplayer_test_plan.md`
- `docs/runs/RUN_10_HUMAN_PLAYABLE_HYBRID.md` (complete)

## In-Scope Work

- Host authoritative sim over LAN
- Command relay and sync (same validation as single-player)
- Desync detection and pause
- Reconnect flow
- Per-client fog/intel from authoritative snapshot (NET-SMOKE-003)
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
- Replay determinism compatible with MIL-REPLAY / HYB replay requirements

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_12 section.

## Acceptance Checklist

- [ ] Two clients sync to tick 100 with matching hash
- [ ] Command sync identical to single-player pipeline
- [ ] Per-client fog/intel consistent with authoritative snapshot
- [ ] Desync test harness triggers pause
- [ ] Reconnect restores consistent state
- [ ] Godot export builds for Android
- [ ] NET-SMOKE-001–003, PKG-SMOKE-001 pass

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
