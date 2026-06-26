# RUN — Tutorial Legibility and Watchable Settler MVP

Branch: `feature/tutorial-legibility-settler-mvp`
Base: `main`
Status: Complete

## Goal

Make the Settler MVP human-understandable from the screen: visible tutorial/objective/inspector/build/mode UI, plain-English selection guidance, readable sprites, and lightweight watchability animations.

## Hard priority

1. Visible tutorial/objective/inspector/build/mode UI
2. Plain-English selection and action guidance
3. Sprite readability
4. Animation/watchability (only after legibility gate passes)

## Verification

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_settler_mvp_smoke.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_human_understandable_screenshots.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_human_understandable_screen.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_human_understandable_flow.gd
```

## Acceptance

- Tutorial opens with readable welcome/objective/first-action text
- Inspector shows plain English for HQ, buildings, sites, terrain, carriers
- Build and road modes show visible banners and invalid reasons
- Sprites distinguish core buildings/terrain; carriers show ware icons when carrying
- Client-only animations for carriers and building status
- `verify_human_understandable_screen.gd` and `verify_human_understandable_flow.gd` exit 0
