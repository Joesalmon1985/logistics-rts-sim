# Multiplayer Test Plan

Scope: RUN_12 — Wi-Fi multiplayer and packaging.

## Command Sync (NET-CMD-001)

- Two clients submit commands same tick → host orders deterministically
- Rejected command on client B does not advance host state incorrectly

## Deterministic Sync (NET-SYNC-001)

- Host + one client: at tick 100, snapshot hashes match
- Host + two clients: all three hashes match

## Desync Detection (NET-DESYNC-001)

- Inject hash mismatch in test harness → desync handler fires
- Game pauses; diagnostic dump includes last commands

## Reconnect (NET-REC-001)

- Client disconnect at tick 50, reconnect at tick 80
- Client receives snapshot + command catch-up
- Client hash matches host at tick 80

## Latency / Ordering

- Out-of-order command arrival sorted by (tick, sequence)
- Late command for past tick rejected with reason

## Packaging (NET-PKG-001)

- Godot export preset exists for Android (minimum)
- Python sim accessible to packaged build (embedded or host mode)

## Test Environment

- Local loopback or LAN in CI optional
- Headless host sim + two Python test clients sufficient for sync tests

## Pass Criteria

- [ ] NET-SMOKE-001, NET-SMOKE-002 pass
- [ ] PKG-SMOKE-001 export succeeds

See `docs/design/09_wifi_multiplayer_design.md`.
