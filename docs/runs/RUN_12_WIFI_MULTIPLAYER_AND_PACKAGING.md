# RUN_12 — Wi-Fi Multiplayer and Packaging

## Purpose

Local Wi-Fi multiplayer (**host-authoritative first**, DDR-030), desync handling, reconnect, and mobile packaging.

The host runs the authoritative GDScript simulation core; clients submit commands and receive snapshots/events/results. Clients do **not** run independent authoritative simulations. Command handling reuses the single-player validation pipeline; fog/intel per-client derives from the host's authoritative snapshot.

## Branch Name

`feature/run-12-wifi-multiplayer-packaging`

## Prerequisite Documents

- `docs/design/09_wifi_multiplayer_design.md`
- `docs/testing/05_multiplayer_test_plan.md`
- `docs/runs/RUN_10_HUMAN_PLAYABLE_HYBRID.md` (complete)

## In-Scope Work

- Host-authoritative GDScript simulation core over LAN
- Command relay and host-side validation (same pipeline as single-player)
- Host broadcasts authoritative snapshots/events; clients reconcile to host (not lockstep)
- Desync detection and pause
- Reconnect flow
- Per-client fog/intel from the host's authoritative snapshot (NET-SMOKE-003)
- Android export preset (minimum), shipping the in-process core (no Python runtime)
- NET-SMOKE-*, PKG-SMOKE-001

## Out-of-Scope Work

- Internet matchmaking
- Cross-platform PC client

## Test-First Requirements

Per `docs/testing/05_multiplayer_test_plan.md`.

## Implementation Requirements

- Host-authoritative: one authoritative GDScript core on the host; clients are non-authoritative (DDR-030)
- Same command validation as single-player
- Periodic host↔client snapshot-hash compare for desync detection (not dependent on lockstep determinism across clients)
- Replay determinism compatible with MIL-REPLAY / HYB replay requirements
- Lockstep is explicitly out of scope (possible future optimisation only)

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_12 section.

## Acceptance Checklist

- [ ] Host-authoritative: clients hold no independent authoritative sim (DDR-030)
- [ ] Two clients match the host's authoritative snapshot hash at tick 100
- [ ] Command validation identical to single-player pipeline
- [ ] Per-client fog/intel consistent with authoritative snapshot
- [ ] Desync test harness triggers pause
- [ ] Reconnect restores consistent state
- [ ] Godot export builds for Android
- [ ] NET-SMOKE-001–003, PKG-SMOKE-001 pass

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
