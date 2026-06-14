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
    ports: [road_node_south]
    enable_on_complete: true
```

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
- Invalid config fails at startup, not mid-tick
- All numeric authoritative fields are integers or fixed-point (no floats; DDR-027)

## IP

All config content uses original placeholder IDs from `05_units_resources_buildings_catalogue.md`.
