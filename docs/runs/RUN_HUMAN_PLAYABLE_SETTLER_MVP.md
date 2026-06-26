# RUN_HUMAN_PLAYABLE_SETTLER_MVP

Status: Implementation run for [PRD — Human-Playable Settler MVP](PRD%20%E2%80%94%20Human-Playable%20Settler%20MVP.md)  
Branch: `feature/human-playable-settler-mvp`  
Baseline: [RUN_PLAYABLE_SETTLER_MVP](RUN_PLAYABLE_SETTLER_MVP.md)

## Delivered

- **Generated sprites** — `tools/generate_settler_placeholder_sprites.py` + committed PNGs under `godot_project/client/assets/settler_mvp/generated/`
- **Asset registry** — `asset_registry.json`, `SettlerAssetRegistry`, `SettlerSpriteLayer` map overlay
- **Plain-English inspector** — `SelectionFormatter`, `BuildingCatalog`, selection priority (carrier → building → site → road → terrain)
- **Build UI** — `BuildPalette` (icons, tooltips, active states), `ModeBanner`, placement preview label
- **Tutorial** — main menu entry, `SettlerTutorialController`, 11 steps in `settler_tutorial_steps.json`
- **Launch** — `scripts/run_game.sh`, `scripts/run_tutorial.sh`, `scripts/create_desktop_launcher.sh`
- **Tests** — 14 `test_human_mvp_*` viewer tests + settler regression preserved
- **Screenshot suite** — `capture_human_mvp_screenshots.gd`, `HumanMvpReviewUtil`, visibility gate at 1920×1080

Kenney medieval assets in `DOWNLOADED/` remain **unused** (documented in asset inventory).

## Acceptance checklist

- [x] Full 11-step tutorial JSON + controller; launch from menu
- [x] Generated-only sprites committed and mapped
- [x] Plain-English inspector (HQ, terrain, building, construction site)
- [x] Build palette with icons, mode banner, road/build feedback
- [x] `./scripts/run_game.sh` opens main menu (Tutorial / Free Play / Debug / Quit)
- [x] `test_human_mvp_*` (14) pass headless
- [x] `test_settler_mvp_*` regression pass
- [x] Screenshot capture tool + `test_human_mvp_screenshot_capture.gd`
- [x] Asset inventory + play guide + lessons learned

## Human-understanding bar

1. Launch with `./scripts/run_game.sh`
2. Choose **Tutorial** — on-screen instruction panel visible
3. Inspect HQ / terrain / building via map click — plain-English inspector
4. Enter build or road mode — mode banner + preview label visible
5. Press **Play** — simulation ticks advance

## Verification

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

TEST_FILTER=human_mvp timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=settler_mvp timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_settler_mvp_smoke.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_human_mvp_screenshots.gd
```

See [RUN_HUMAN_PLAYABLE_SETTLER_PLAY_GUIDE.md](../audit/RUN_HUMAN_PLAYABLE_SETTLER_PLAY_GUIDE.md).
