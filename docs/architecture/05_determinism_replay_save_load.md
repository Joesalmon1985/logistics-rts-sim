# Determinism, Replay, Save, and Load

## Determinism Contract

Given:
- Initial state (including seed and rng_state)
- Ordered command list

Then:
- Event sequence is identical
- Final snapshot hash is identical

## Seeded RNG

- Stored in `GameState.rng_state` (an explicit seeded RNG in the GDScript core)
- Serialized in saves and snapshots
- No ad-hoc/unseeded RNG calls (`randi()`/`randf()`/global RNG) outside the seeded RNG wrapper
- Draw integers from the seeded RNG; never use float RNG for authoritative outcomes

## Replay Hash Mechanism (RUN_01)

```
func compute_state_hash(state) -> String:
    var canonical = canonicalize(state)   # stable key order, exclude event log
    return sha256_hex(to_canonical_json(canonical))
```

Canonicalisation must be stable across platforms (sorted keys, integer/fixed-point values only). After a replay run, compare the hash to `expected_hash` in the scenario fixture.

## Event Ordering

- Events emitted in subsystem order within tick
- Tick order monotonic
- Event IDs assigned sequentially

## Save / Load

Save file contains:
- Full game state at tick T
- Command log from tick 0..T (optional embedded)
- Schema version

Load restores state + RNG + resumes command processing.

## Replay Verification Tests

Every scenario fixture includes `expected_hash`. The headless test harness (and CI where available) runs replay on each change touching `sim/` core code.

## Floating Point (DDR-027 — Accepted)

All authoritative state that contributes to the replay/state hash **must** use **integers or fixed-point scaled integers** — never floats. This guarantees identical hashes across machines, platforms, and GDScript builds (required for cross-device replay verification and any future lockstep option).

- Positions use hex coordinates and integer progress; movement progress uses integer ticks or integer progress units.
- Production, cooldowns, health, damage, ware/resource quantities, visibility timers, construction/repair/research progress are integers.
- Fractional rates use fixed-point scaled integers or integer "per N ticks" rates.
- Godot visual animation may use floats, but those are **presentation-only** and must never enter authoritative state, the replay hash, command validation, combat, pathfinding, production, or logistics.

Ratified in `docs/design/11_design_decision_register.md` (DDR-027).

## Related

- `02_command_event_snapshot_contract.md`
- Economy: ECON-DET-*
- Military: MIL-DET-*
