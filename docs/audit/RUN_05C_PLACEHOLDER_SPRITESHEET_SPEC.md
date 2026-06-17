# RUN_05C — Placeholder Economy Spritesheet Spec

Date: 2026-06-17 (RUN_05C.2)

Original placeholder art for economy spectator. **32×32** frames, procedural generation only.

## Assets

| File | Purpose |
|------|---------|
| `godot_project/client/art/mock_economy_sprites.png` | 32×32 cell spritesheet |
| `godot_project/client/art/mock_economy_sprites.json` | Frame rects + 21 animations |
| `godot_project/client/adapters/mock_economy_atlas.gd` | Loads PNG + JSON |
| `godot_project/client/tools/generate_mock_economy_atlas.gd` | Regenerates assets |

## Animation contract (21 names)

| Family | Animations |
|--------|------------|
| Logistics | `carrier_idle`, `carrier_move_loop`, `carrier_carrying_loop`, `road_flow_loop` |
| Wares | `ware_log_move_loop`, `ware_plank_move_loop`, `ware_stone_move_loop`, `ware_generic_move_loop` |
| Buildings | `building_idle`, `building_working_loop`, `building_blocked_loop`, `building_activity_pulse` |
| Construction | `construction_idle`, `construction_progress_25`, `construction_progress_50`, `construction_progress_75`, `construction_progress_loop`, `construction_complete_pulse` |
| Storage/warnings | `storage_idle`, `storage_activity_pulse`, `warning_blocked_loop` |

## Grounding

Every sprite state includes `grounding.reason`, `field`, `entity_key`, `value`, optional `prev`/`current`. See [`economy_sprite_state_selector.gd`](../../godot_project/client/adapters/economy_sprite_state_selector.gd).

## Regeneration

```bash
$GODOT --headless --path godot_project --script res://client/tools/generate_mock_economy_atlas.gd
```

Commit PNG/JSON when placeholder layout changes.
