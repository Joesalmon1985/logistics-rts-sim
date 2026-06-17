# RUN_05C — Placeholder Economy Spritesheet Spec

Date: 2026-06-17

Original placeholder art for economy spectator visual groundwork. Not final art direction.

## Assets

| File | Purpose |
|------|---------|
| `godot_project/client/art/mock_economy_sprites.png` | 16×16 cell spritesheet |
| `godot_project/client/art/mock_economy_sprites.json` | Frame rects + animation definitions |
| `godot_project/client/adapters/mock_economy_atlas.gd` | Loads PNG + JSON into `SpriteFrames` |
| `godot_project/client/tools/generate_mock_economy_atlas.gd` | Regenerates PNG/JSON (dev tool) |

## Animation catalogue (15+ required names)

| Animation | Loop | Visual intent | State source |
|-----------|------|---------------|--------------|
| `carrier_idle` | yes | Amber carrier stationary | Carrier state idle |
| `carrier_move_loop` | yes | Amber carrier motion | Carrier moving |
| `ware_log_move_loop` | yes | Brown log on route | Ware route type log |
| `ware_generic_move_loop` | yes | Generic ware on route | Other ware routes |
| `road_flow_loop` | yes | Road traffic hint | Active route on road |
| `building_idle` | yes | Grey building | No production |
| `building_working_loop` | yes | Green pulse | `production_progress` active |
| `building_activity_pulse` | no | Single activity flash | Production event |
| `building_blocked_loop` | yes | Red blocked | Blocked production |
| `construction_idle` | yes | Grey site | Site placed, no delivery |
| `construction_progress_loop` | yes | Scaffold pulse | Delivery % increasing |
| `construction_complete_pulse` | no | Completion flash | Site complete event |
| `storage_idle` | yes | Storehouse idle | Store present |
| `storage_activity_pulse` | no | Store intake flash | Ware total delta |
| `warning_blocked_loop` | yes | Red warning | `blocked_job_markers` |

Additional names may exist in JSON; VIS-SPECTATOR-003 requires ≥15 loadable animations including the table above.

## Color coding (scene legend)

- Amber — logistics (carriers, road flow)
- Brown — timber/ware
- Green — working buildings
- Red — blocked/warning
- Grey — idle

## Grounding rules

1. No sprite without a corresponding render-data or snapshot marker.
2. Animation name chosen by `EconomySpriteStateSelector` from authoritative fields only.
3. Sprite layer applies states from controller; does not infer economy rules.

## Regeneration

```bash
$GODOT --headless --path godot_project --script res://client/tools/generate_mock_economy_atlas.gd
```

Commit regenerated PNG/JSON only when placeholder layout changes.
