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
| `construction_sites[]` | Progress % → `construction_progress_25/50/75` |
| `carriers[].ware_type` / `ware_amount` | `carrier_carrying_loop` |
| `ware_routes[].ware_type` | log/plank/stone/generic ware loops |
| `event_log` / events | Event log panel, pulses |
| `blocked_job_markers` | `warning_blocked_loop` |
| `diagnostics.ware_totals` delta | `storage_activity_pulse` |

Each sprite state includes `grounding` metadata (RUN_05C.2).

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

## RUN_05C.3 viewer additions (non-authoritative)

| Consumer | Render/event source |
|----------|---------------------|
| Active chain trace panel | `chain_fingerprint`, `buildings`, `building_stores`, `carriers`, `ware_routes`, `delivery_jobs` |
| Forest source highlight | `tree_density[]` — highest density hex |
| Route src/dst hints | `ware_routes[].points`, `delivery_jobs` endpoints |
| Destination building pulse | `effect_events` from `ware_delivered` / delivery payload `building_id` |
| Activity score | Grounded composite of fingerprint + sprite count (viewer metric only) |

Generated capture outputs: `training_runs/visual_review/run_05c_economy_spectator/` — not committed unless repo policy changes.
