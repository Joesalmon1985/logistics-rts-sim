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

## Replay File and Import (RUN_04)

RUN_01 established the replay **hash** and scenario `expected_hash`. RUN_04 adds a portable
**replay file** plus an **import/replay** path so a recorded session can be re-executed and
verified (REP-SMOKE-001: export → import → re-run → identical hash).

A replay file is JSON-compatible and contains exactly the inputs needed to reproduce a run from
tick 0 (no derived state, so the file cannot drift from the simulation):

```
{
  "schema_version": <int>,
  "variant": "economy_trial" | "military_trial" | "hybrid",   // registered variant id
  "scenario": "<scenario path or embedded scenario object>",
  "seed": <int>,
  "tick_count": <int>,                 // ticks executed when recorded
  "commands": [                        // ordered command log, tick 0..T
    { "tick": <int>, "command": { ...validated command payload... } },
    ...
  ],
  "expected_hash": "sha256:<hex>"      // hash of final state after tick_count ticks
}
```

Rules:

- **Inputs only.** The file stores `seed` + ordered validated commands + scenario ref, never
  derived/hashed state. Replay re-derives all state by re-running the command log.
- **Same command path.** Import feeds each recorded command back through the **same**
  `CommandValidator` used by humans/bots/tests — no privileged replay-only mutation path.
- **Integer/fixed-point only** in every recorded command payload that reaches hashed state
  (DDR-027); float creep makes replays non-reproducible across machines.
- **Verification.** After import + re-run for `tick_count` ticks, `ReplayHash.compute_state_hash`
  must equal `expected_hash`. A mismatch is a failure (never re-pin to make it pass; re-pin only
  with triple-run evidence when behaviour legitimately changed).
- **Determinism order.** Commands replay in recorded `(tick, order)` sequence; event IDs and
  subsystem tick order follow the "Event Ordering" section above.

Implementation lives in the simulation core / tools (e.g. `sim/core/` + `sim/tools/`), reusing
`sim/core/replay_hash.gd`; it must not depend on scene/render state.

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
