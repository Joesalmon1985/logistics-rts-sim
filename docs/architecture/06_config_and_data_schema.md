# Config and Data Schema

## File Locations

```
config/
  buildings.yaml
  recipes.yaml
  units.yaml
  terrain.yaml
  variants.yaml
scenarios/
  economy_tutorial.json
  military_skirmish.json
  hybrid_smoke.json
```

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

- JSON Schema or pydantic models in RUN_01 for config load
- Invalid config fails at startup, not mid-tick

## IP

All config content uses original placeholder IDs from `05_units_resources_buildings_catalogue.md`.
