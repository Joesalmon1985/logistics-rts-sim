# RUN Human-Playable Settler MVP — Asset Inventory

## Existing repo art (pre-run)

| Asset | Location | Licence | Use in Human MVP |
|-------|----------|---------|------------------|
| Mock economy atlas | `godot_project/client/art/mock_economy_sprites.{json,png}` | Project original (RUN_05C) | Not used in Settler MVP; spectator demos only |
| Procedural map colors | `godot_project/client/ui/map_viewport.gd` | Code-generated | Fallback when registry texture missing |

## Downloaded reference (unused)

| Asset | Location | Licence | Decision |
|-------|----------|---------|----------|
| Kenney Medieval RTS | `DOWNLOADED/kenney_medieval-rts/` | CC0 | **Unused** — run uses generated-only placeholders per plan |

## Generated placeholders (this run)

All committed under `godot_project/client/assets/settler_mvp/generated/`, produced by `tools/generate_settler_placeholder_sprites.py` (deterministic seed 42).

| Concept | File | Size |
|---------|------|------|
| headquarters | `building_headquarters.png` | 96×96 |
| woodcutter | `building_woodcutter.png` | 96×96 |
| forester | `building_forester.png` | 96×96 |
| sawmill | `building_sawmill.png` | 96×96 |
| quarry | `building_quarry.png` | 96×96 |
| construction_site | `building_construction_site.png` | 96×96 |
| road | `logistics_road.png` | 64×64 |
| road_node | `logistics_road_node.png` | 48×48 |
| carrier | `logistics_carrier.png` | 48×48 |
| builder | `logistics_builder.png` | 48×48 |
| timber | `ware_timber.png` | 64×64 |
| planks | `ware_planks.png` | 64×64 |
| stone | `ware_stone.png` | 64×64 |
| builder_work | `ware_builder_work.png` | 64×64 |
| grass | `terrain_grass.png` | 64×64 |
| forest | `terrain_forest.png` | 64×64 |
| rock | `terrain_rock.png` | 64×64 |
| water | `terrain_water.png` | 64×64 |
| selection_highlight | `ui_selection_highlight.png` | 64×64 |
| placement_valid | `ui_placement_valid.png` | 64×64 |
| placement_invalid | `ui_placement_invalid.png` | 64×64 |
| HUD icons (64×64) | `icon_*.png` | 64×64 |

Mappings: `godot_project/client/assets/asset_registry.json`
