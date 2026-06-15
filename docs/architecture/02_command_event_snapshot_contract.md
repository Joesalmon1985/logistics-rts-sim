# Command, Event, and Snapshot Contract

## Command Envelope

```json
{
  "type": "PlaceRoadCommand",
  "player_id": "p1",
  "tick": 42,
  "payload": {
    "hex_a": {"q": 1, "r": 2},
    "hex_b": {"q": 2, "r": 2}
  }
}
```

## Command Result

```json
{
  "accepted": true,
  "reason": null,
  "events": ["evt_001", "evt_002"]
}
```

Rejected commands: `accepted: false`, `reason` human-readable code (e.g. `TERRAIN_BLOCKED`).

## Base Command Structures (RUN_01, GDScript)

- `Command` — base GDScript class/resource with `type`, `player_id`, `tick`, `validate(state)`, `apply(state)`
- `CommandRegistry` — maps type string → command class/resource
- `CommandValidator` — runs `validate`; returns `CommandResult`

These live in the authoritative simulation core (`sim/core`), not in UI scene code.

## Event Envelope

```json
{
  "id": "evt_001",
  "type": "ware_delivered",
  "tick": 42,
  "payload": {
    "job_id": "job_7",
    "ware": "planks",
    "quantity": 1,
    "destination": "bld_sawmill_1"
  }
}
```

Events are append-only. IDs monotonic per game.

## Event Categories

See requirement docs:
- Economy: `02_trial_economy_game_requirements.md` §33
- Military: `03_trial_military_game_requirements.md` §25
- Hybrid: `04_hybrid_game_requirements.md` §18

## Snapshot Schema (versioned)

```json
{
  "schema_version": 1,
  "variant": "economy_trial",
  "tick": 42,
  "seed": 12345,
  "rng_state": "...",
  "map": { "width": 32, "height": 32, "hexes": [] },
  "roads": [],
  "road_nodes": [],
  "buildings": [],
  "entities": [],
  "diagnostics": {},
  "players": []
}
```

The Godot visual client reads snapshots only; it never writes back except via new commands.

## Available Commands (optional, non-authoritative — RUN_05)

A snapshot **may** carry an optional `available_commands` array so that a debug/visual client can
present legal actions to a human without deriving legality itself. This is provided by the
**sim-owned command availability provider** (see `03_godot_adapter_contract.md`); the client only
displays/submits.

```json
{
  "available_commands": [
    {
      "id": "build_lumber_yard_3_4",
      "label": "Build Lumber Yard at 3,4",
      "type": "build",
      "actor_id": "p1",
      "target": {"q": 3, "r": 4},
      "payload": {},
      "enabled": true,
      "disabled_reason": null,
      "source": "sim"
    }
  ]
}
```

Field notes:
- Minimum requirement: list **legal** commands only. `enabled: false` + `disabled_reason` for
  explainable-but-invalid commands is optional.
- `source` documents where the entry came from (`sim`).

### Non-authoritative guardrail (must-have)

`available_commands[]` is a **client/debug/export affordance, not authoritative state.** It must
**not** be included in replay-hashed state unless deliberately and explicitly approved. If
`SnapshotExporter` output feeds `ReplayHash`, either exclude `available_commands[]` from the hashed
snapshot or expose it via a separate client/debug export path. **Do not re-pin replay hashes**
merely because command availability was added to a viewer-facing snapshot. It also does not affect
`schema_version` for hashing purposes when kept out of hashed state.

## Replay Format

```json
{
  "initial_state": { ... },
  "commands": [ ... ],
  "expected_hash": "sha256:..."
}
```

Replay runner: load initial → apply commands in order → compute hash → compare.

## Validation Pipeline

```
Command (UI dict / bot / peer) → Deserialize → Schema check → validate(state) → Queue → apply on tick
```

All clients use the identical pipeline code path in the GDScript simulation core. (Commands may originate as JSON over LAN or as in-process GDScript objects from the local UI; both converge on the same validator.)

## Versioning

- `schema_version` increment on breaking snapshot/event changes
- Config data version separate (see `06_config_and_data_schema.md`)
