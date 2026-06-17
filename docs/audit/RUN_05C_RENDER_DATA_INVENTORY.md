# RUN_05C — Economy Spectator Render Data Inventory

Date: 2026-06-17

Inventory of snapshot/render-data fields consumed by the economy spectator visual layer. All values come from the Godot/GDScript simulation core via `SnapshotExporter` — no scene-tree or float-animation authority.

## Pipeline

```
GameState → SnapshotExporter → EconomySpectatorRenderBridge → LayerRenderer / EconomySpriteStateSelector → MapViewport + EconomySpriteLayer
```

## Render data keys (from `LayerRenderer` / bridge)

| Key | Used for | Sprite / overlay |
|-----|----------|------------------|
| `terrain` | Hex terrain fill | MapViewport base layer |
| `buildings` | HQ, woodcutter, sawmill, etc. | Building sprites, working/idle |
| `roads` | Road segments | Road overlay, `road_flow_loop` |
| `carriers` / `carrier_markers` | Worker carriers | `carrier_move_loop`, `carrier_idle` |
| `ware_routes` | Log/plank routes | `ware_log_move_loop`, `ware_generic_move_loop` |
| `delivery_jobs` | Active deliveries | Route animation selection |
| `construction_sites` | Sites in progress | `construction_*` animations |
| `blocked_job_markers` | Stalled jobs | `warning_blocked_loop` |
| `forest_density` | Tree density overlay | Chain fingerprint / D metric |
| `ownership` | Player tint (BvB manual view) | Ownership overlay toggle |
| `diagnostics` | Economy scores, stalls | Summary / bot status text |

## Snapshot fields for sprite selection

| Field | Selector use |
|-------|--------------|
| `buildings[].production_progress` | `building_working_loop` vs idle |
| `buildings[].production_activity` | Activity pulse |
| `construction_sites[]` | Progress %, materials delivered |
| `event_log` / events | Event log panel, `events_grew` metric |
| `tick` | Status label |

## Chain fingerprint (activity metrics)

`EconomySpectatorMetrics.chain_fingerprint()` tracks:

- `forest_density`, `woodcutter_timber`, `sawmill_timber`, `sawmill_planks`
- `carrier_moving`, `ware_routes`, `carriers`, `construction_sites`, `delivery_jobs`
- `carrier_ware`

Used for `chain_fingerprint_changed` start→end delta.

## Entity category counts

`LayerRenderer.count_entities(render_data)` — roads, buildings, carriers, construction_sites, etc. Used for debug label and `entity_categories_changed` signal.

## Explicit non-sources

- Godot `Node2D` positions (presentation only)
- Wall-clock / `_process` delta
- Bot decision heuristics (dual-bot manual view only; not automated path)
