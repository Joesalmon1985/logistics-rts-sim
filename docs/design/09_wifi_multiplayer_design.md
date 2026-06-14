# Wi-Fi Multiplayer Design

Scope: RUN_12 — local Wi-Fi multiplayer and mobile packaging.

---

## Architecture

```
Player A Godot ──┐
                 ├──► Host Python Sim (authoritative) ◄── LAN
Player B Godot ──┘
```

- **One authoritative sim** per match (host device or dedicated headless host)
- Clients submit commands over LAN; host validates and steps sim
- Clients receive snapshots/events for display

Alternative: lockstep command relay with deterministic sim on all peers — choose in RUN_12 implementation; both require identical command validation.

---

## Command Relay

- **NET-CMD-001:** Commands timestamped with tick intent; host orders by tick then sequence
- **NET-CMD-002:** Rejected commands return reason to client
- **NET-CMD-003:** Same validation as single-player pipeline

---

## Deterministic Sync

- **NET-SYNC-001:** Shared seed agreed at match start
- **NET-SYNC-002:** All peers apply same command stream
- **NET-SYNC-003:** Periodic snapshot hash compare for desync detection

---

## Desync Detection

- **NET-DESYNC-001:** Hash mismatch triggers pause and diagnostic dump
- **NET-DESYNC-002:** Log last N commands and event IDs for recovery analysis

---

## Reconnect

- **NET-REC-001:** Late joiner receives full snapshot + command history since snapshot tick
- **NET-REC-002:** Disconnect timeout configurable

---

## Packaging

- **NET-PKG-001:** Mobile export preset exists (Android minimum) and produces a build that can reach the authoritative sim
- Android/iOS export via Godot
- Python sim bundled (embedded) or host-only mode **TBD in RUN_12** — this transport choice is an open project-level decision; see **DDR-026** in `docs/design/11_design_decision_register.md`
- No proprietary assets in repo

---

## Tests

See `docs/testing/05_multiplayer_test_plan.md`.

---

## Rule IDs Summary

NET-CMD-*, NET-SYNC-*, NET-DESYNC-*, NET-REC-*, NET-PKG-*
