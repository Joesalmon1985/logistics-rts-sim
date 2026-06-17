# RUN_05C.2 — Visual Polish Verification Report

Date: 2026-06-17  
Branch: `feature/run-05c-economy-spectator-visual-identity`  
Starting commit: `81c3cd7`  
Ending commit: `d56da7b` (docs commit follows)

## Scope

Visual polish and legibility only. `ECON_BOT_002` remains unresolved and untouched.

## Demo source

- `EconomyDemo.build_production_chain()` seed 42
- Automated path: production-chain only; dual-bot manual diagnostic

## Visual improvements (RUN_05C.2)

- 32×32 placeholder atlas with 21 animation states
- Grounding reasons on every sprite state (`grounding.reason`, `field`, `entity_key`, `prev`/`current`)
- Recent-changes activity panel (last 30 ticks)
- Sprite and route/flow overlay toggles
- Expanded animation-family legend
- Forest-density hex tint, route direction chevrons, ware labels on sprites
- Flexible visual-family smoke gate (no faking absent families)

## Authoritative gates (not pixel delta)

| Gate | Requirement |
|------|-------------|
| `meaningful_count` | >= 2 start→end deltas |
| `non_sprite_delta_count` | >= 1 |
| Visual families | All exposable families represented; >= 3 active only when demo exposes >= 3 |
| Grounding | Every sprite state has `grounding.reason` |
| Pixel delta | Sanity check only (tick0→tick30 > 5) |

## Sprite animation contract (21)

Logistics: `carrier_idle`, `carrier_move_loop`, `carrier_carrying_loop`, `road_flow_loop`  
Wares: `ware_log_move_loop`, `ware_plank_move_loop`, `ware_stone_move_loop`, `ware_generic_move_loop`  
Buildings: `building_idle`, `building_working_loop`, `building_blocked_loop`, `building_activity_pulse`  
Construction: `construction_idle`, `construction_progress_25/50/75`, `construction_progress_loop`, `construction_complete_pulse`  
Storage/warnings: `storage_idle`, `storage_activity_pulse`, `warning_blocked_loop`

Construction stage sprites required in atlas/resolver tests (synthetic fixtures). Production-chain smoke does **not** require construction sprites unless `construction_sites` present.

## Verification (Linux Godot 4.5.2)

| Command | Exit | Status |
|---------|------|--------|
| `TEST_FILTER=vis_spectator run_tests.gd` | 0 | 5/5 passed |
| `run_economy_spectator_smoke.gd` | 0 | `meaningful_count=5`, `families=3/3` |
| `run_economy_spectator_capture.gd -- --assert` | 0 | `pixel_delta_0_30=1298` |
| `run_viewer_smoke.gd` | 0 | pass |
| `verify_failure_modes.gd` | 0 | pass |

0 `SCRIPT ERROR` in focused runs.

## Capture outputs

`training_runs/visual_review/run_05c_economy_spectator/`:

- `spectator_tick0.png`, `spectator_tick10.png`, `spectator_tick30.png`, `spectator_tick60.png`
- `capture_manifest.json` (authoritative metrics per tick)
- `contact_sheet.html`

Not committed to git.

## Launch command

```bash
$GODOT --path godot_project res://client/scenes/economy_spectator_bot_demo.tscn
```

## Review readiness

**Visual groundwork only — Ready**

## Known unrelated blockers

- `ECON_BOT_002` unresolved
- `BOT-SMOKE-004` / `ECON-SMOKE-007` outstanding
