# Tutorial Legibility — Visual Review

See `docs/audit/screenshots/human_understandable_visual_review.json` for per-shot probe results.

## Before

- `docs/audit/screenshots/legibility_before_tutorial_start.png`
- `docs/audit/screenshots/legibility_before_review.json`

## After (required shots)

| File | Context |
|------|---------|
| `human_understandable_00_main_menu.png` | Main menu buttons |
| `human_understandable_01_tutorial_start.png` | Tutorial welcome visible |
| `human_understandable_02_hq_selected_inspector.png` | HQ inspector plain English |
| `human_understandable_03_build_palette_visible.png` | Build menu visible |
| `human_understandable_04_road_mode_instruction.png` | Road mode banner |
| `human_understandable_05_woodcutter_build_mode.png` | Build mode banner |
| `human_understandable_06_construction_site_inspector.png` | Construction inspector |
| `human_understandable_07_carrier_with_ware.png` | Carrier / economy |
| `human_understandable_08_watch_economy_running.png` | Running simulation |

## Strict visibility gate

A UI element counts as visible only when all hold:

- `visible == true`, in tree, intersects 1920×1080 viewport
- Minimum rect size, non-transparent text, not clipped or fully covered

Node probes are authoritative; headless PNG captures may be fallback-sized.
