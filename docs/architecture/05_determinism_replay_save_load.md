# Determinism, Replay, Save, and Load

## Determinism Contract

Given:
- Initial state (including seed and rng_state)
- Ordered command list

Then:
- Event sequence is identical
- Final snapshot hash is identical

## Seeded RNG

- Stored in `GameState.rng_state`
- Serialized in saves and snapshots
- No `random` module calls outside RNG wrapper

## Replay Hash Mechanism (RUN_01)

```python
def compute_state_hash(state) -> str:
    canonical = canonicalize(state)  # stable key order, exclude event log
    return sha256(json.dumps(canonical, sort_keys=True)).hexdigest()
```

After replay run, compare hash to `expected_hash` in scenario fixture.

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

Every scenario fixture includes `expected_hash`. CI runs replay on each PR touching sim code.

## Floating Point

All authoritative state that contributes to the replay/state hash must use **integers or fixed-point**, not floats, to guarantee identical hashes across machines and platforms (required for cross-device replay verification and the lockstep multiplayer option). Use integers for combat/economy math. This is the strict reading recommended by **DDR-027**; confirm/ratify it in `docs/design/11_design_decision_register.md` before RUN_01 fixes the hash and math conventions.

## Related

- `02_command_event_snapshot_contract.md`
- Economy: ECON-DET-*
- Military: MIL-DET-*
