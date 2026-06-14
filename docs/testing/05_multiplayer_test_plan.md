# Multiplayer Test Plan

Scope: RUN_12 — Wi-Fi multiplayer and packaging.

## Command Sync (NET-CMD-001)

- Two clients submit commands same tick → host orders deterministically
- Rejected command on client B does not advance host state incorrectly

## State Sync (NET-SYNC-001, host-authoritative)

- Host + one client: at tick 100, the client's displayed snapshot matches the host's authoritative snapshot hash
- Host + two clients: both clients match the host snapshot (host is source of truth; not lockstep)

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
- The packaged build contains the in-process authoritative GDScript simulation core (no Python runtime; DDR-026)

## Test Environment

- Local loopback or LAN in CI optional
- Headless host core (Godot headless) + two headless test clients sufficient for sync tests

## Pass Criteria

- [ ] NET-SMOKE-001, NET-SMOKE-002 pass
- [ ] PKG-SMOKE-001 export succeeds

See `docs/design/09_wifi_multiplayer_design.md`.
