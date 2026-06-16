# Config and Data Schema

## File Locations

Config and scenario data live under the simulation core's data folder (`godot_project/sim/data/`):

```
godot_project/sim/data/
  config/
    buildings.(yaml|tres|json)
    recipes.(yaml|tres|json)
    units.(yaml|tres|json)
    terrain.(yaml|tres|json)
    variants.(yaml|tres|json)
  scenarios/
    economy_tutorial.json
    military_skirmish.json
    hybrid_smoke.json
  maps/
    tutorial.json
```

Config may be authored as JSON/YAML or as Godot resources (`.tres`); the loader normalises to in-core data structures. The format is chosen in RUN_01; examples below use YAML for readability.

## buildings.yaml (example structure)

```yaml
schema_version: 1
buildings:
  sawmill:
    footprint: [[0,0], [1,0]]
    worker_slots: 2
    ports: [[0,0], [1,0]]   # [dq, dr] offsets from anchor; default [[0,0]] if omitted
    enable_on_complete: true
```

### Building `ports[]` (ratified — RUN_02.7 implementation)

| Field | Semantics |
|-------|-----------|
| `ports` | List of `[dq, dr]` axial offsets from building anchor (`anchor_q`, `anchor_r`). Each port defines a valid **building entrance** hex where a **road node** connects the footprint to the logistics network. |
| Default | If `ports` is omitted, treat as **`[[0, 0]]`** (anchor port only). Existing configs without `ports` remain valid (DDR-LOG-011). |
| Implementation | Planned/expected in **RUN_02.7** if not already present in loader/sim. |

### Road node inventory / buffer (ratified — DDR-LOG-006)

| Topic | Semantics |
|-------|-----------|
| v1 behaviour | **No active ware staging** in road-node buffers during logistics ticks. Wares in transit are **carrier-held**. |
| Schema | Road nodes may expose `inventory` with `capacity` from `road_node_buffer_capacity` — **reserved** for future junction staging. |
| Tests | ECON-ROAD-004 may verify capacity API separately from live logistics staging. |

### Config validation (RUN_02.7 — DDR-LOG-007)

- Lightweight validation on load: required fields, ware/recipe references resolve, `ports` offset format.
- Invalid config fails at startup, not mid-tick.
- Prove modularity with at least one config-only ware/recipe/building chain test (e.g. bricks).

## recipes.yaml

```yaml
schema_version: 1
recipes:
  planks_from_timber:
    building: sawmill
    inputs: { timber: 1 }
    outputs: { planks: 1 }
    ticks: 5
```

## variants.yaml

```yaml
schema_version: 1
variants:
  economy_trial:
    subsystems: [map, economy, diagnostics]
    commands: [PlaceRoadCommand, ...]
  military_trial:
    subsystems: [map, military, diagnostics]
    commands: [PlaceBuildingCommand, MoveUnitCommand, ...]
  hybrid:
    subsystems: [map, economy, military, hybrid, diagnostics]
    commands: [PlaceRoadCommand, SetBuildingStanceCommand, ...]
    forbidden_player_commands: [MoveUnitCommand, AttackUnitCommand]
```

## scenarios/*.json

```json
{
  "schema_version": 1,
  "variant": "economy_trial",
  "seed": 42,
  "map_file": "maps/tutorial.json",
  "players": ["p1"],
  "victory": { "type": "complete_building", "building": "storage_yard" },
  "expected_hash": null
}
```

`expected_hash` filled after first verified run.

## Map Files

- Axial hex grid JSON: terrain, height, deposits per hex
- Referenced by scenario, not embedded inline for large maps

## Versioning

- Increment `schema_version` on breaking changes
- Sim loader rejects unknown major versions with clear error

## Validation

- GDScript config loader validates schema on load in RUN_01 (typed parse + range/required checks)
- **RUN_02.7:** extend validation per DDR-LOG-007 (ware/recipe cross-refs, `ports[]` shape)
- Invalid config fails at startup, not mid-tick
- All numeric authoritative fields are integers or fixed-point (no floats; DDR-027)

## IP

All config content uses original placeholder IDs from `05_units_resources_buildings_catalogue.md`.
