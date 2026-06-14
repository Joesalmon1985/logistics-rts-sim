# Wi-Fi Multiplayer Design

Scope: RUN_12 — local Wi-Fi multiplayer and mobile packaging.

---

## Architecture (host-authoritative first — DDR-030)

```
Player A Godot client ──┐
                        ├──► Host GDScript simulation core (authoritative) ◄── LAN
Player B Godot client ──┘
```

- **One authoritative GDScript simulation core** per match, on the host device (or a dedicated headless host running Godot)
- Clients submit commands over LAN; the host validates and steps the core
- Clients receive snapshots/events/results for display; clients do **not** run independent authoritative simulations

This host-authoritative model is the **first** multiplayer architecture. Lockstep (deterministic core on all peers) remains a possible future optimisation only after deterministic replay is proven robust; it is out of scope for the first RUN_12 implementation.

---

## Command Relay

- **NET-CMD-001:** Commands timestamped with tick intent; host orders by tick then sequence
- **NET-CMD-002:** Rejected commands return reason to client
- **NET-CMD-003:** Same validation as single-player pipeline

---

## State Sync (host-authoritative)

- **NET-SYNC-001:** Shared seed agreed at match start
- **NET-SYNC-002:** The host is the single source of truth; it applies the ordered command stream and broadcasts authoritative snapshots/events to clients
- **NET-SYNC-003:** Periodic snapshot hash compare between host and clients for desync detection (clients reconcile to the host snapshot rather than relying on lockstep determinism)

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

- **NET-PKG-001:** Mobile export preset exists (Android minimum) and produces a build containing the in-process authoritative GDScript simulation core
- Android/iOS export via Godot
- The authoritative core ships **in-process** inside the Godot app — no embedded Python and no external Python process for gameplay (DDR-026)
- No proprietary assets in repo

---

## Tests

See `docs/testing/05_multiplayer_test_plan.md`.

---

## Rule IDs Summary

NET-CMD-*, NET-SYNC-*, NET-DESYNC-*, NET-REC-*, NET-PKG-*
