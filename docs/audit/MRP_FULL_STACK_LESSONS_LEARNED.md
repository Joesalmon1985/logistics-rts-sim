# MRP Full-Stack Autonomous Run — Lessons Learned

Date: 2026-06-26  
Branch: `feature/mrp-full-stack-autonomous`

## What worked

- Cloning Settler MVP / economy spectator patterns accelerated military and hybrid client work.
- Parallel subagent tracks (military spectator, RUN_07 hybrid sim, skirmish MVP, hybrid client, 3D spectator) kept momentum.
- Headless SubViewport verification (`verify_*_screen.gd`, `capture_*_screenshots.gd`) provides human-legibility gates without manual supervision.
- HYB-SMOKE-001–009 pass with minimal hybrid module composing economy + military.

## What broke

- `TutorialEconomyBot` initially failed: wrong terrain types for woodcutter/quarry, road chains from HQ only, bot observation snapshot missing `objective`, and SettlerObjective ignored `paused_*` building states.
- `ECON_BOT_002` remains red — quarantined from default `run_tests.gd` to `run_bot_long.gd`.
- Full `run_tests.gd` exceeds 900s timeout on this machine; use `TEST_FILTER` slices for CI-style gates.
- Hybrid fog snapshot shape differs from military; `MapViewport._draw_fog` needed type guards.
- Typo `viewner` in `human_proof_harness_controller.gd` blocked editor load.

## Verification lessons

- Wrap long bot smokes with adequate timeout (tutorial economy bot ~25s after fixes).
- `run_bot_fast.gd` must not nest broken `timeout` via `OS.execute` from inside Godot; call subprocess Godot directly.
- Always check exit code **and** `SCRIPT ERROR` lines; hybrid MVP verify passed exit 0 with fog errors before fix.

## Suggested next run

- Run full `capture_*` screenshot suites and commit audit PNGs.
- Fix or retire `ECON_BOT_002` dual-bot expansion bot.
- Pin `kernel_smoke_hybrid.json` hash after hybrid variant stabilizes.
- Mobile touch controls for 3D spectator (deferred).
