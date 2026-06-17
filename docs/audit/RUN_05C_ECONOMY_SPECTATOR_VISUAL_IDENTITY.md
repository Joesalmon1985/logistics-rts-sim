# RUN_05C.1 — Economy Spectator Visual Identity

Date: 2026-06-17  
Branch: `feature/run-05c-economy-spectator-visual-identity`  
WIP ancestor: `214bcd3`

## Scope

Visual groundwork for the economy spectator scene — placeholder sprites, render-data bridge, activity panels, and start→end delta verification. **Not** bot expansion proof or RUN_02.8b closure.

| Topic | Status |
|-------|--------|
| `ECON_BOT_002` | **Unresolved** — separate gameplay problem |
| `BOT-SMOKE-004` / `ECON-SMOKE-007` | **Outstanding** — unrelated to this run |
| Automated demo path | `EconomyDemo.build_production_chain()` seed 42 only |
| Dual-bot view | Manual UI toggle only |

## Scene and controller

| Component | Path |
|-----------|------|
| Scene | `res://client/scenes/economy_spectator_bot_demo.tscn` |
| Scene script | `res://client/scenes/economy_spectator_bot_demo.gd` |
| Controller | `res://client/core/economy_spectator_controller.gd` |
| Render bridge | `res://client/adapters/economy_spectator_render_bridge.gd` |
| Sprite selector | `res://client/adapters/economy_sprite_state_selector.gd` |
| Metrics | `res://client/adapters/economy_spectator_metrics.gd` |
| Sprite layer | `res://client/ui/economy_sprite_layer.gd` |
| Mock atlas | `res://client/adapters/mock_economy_atlas.gd` |

### Demo modes

| Mode | Constant | Automated? |
|------|----------|------------|
| Production chain (default) | `DEMO_PRODUCTION_CHAIN` | Yes — all tests/smokes |
| Dual-bot diagnostic | `DEMO_DUAL_BOT` | No — manual button only |

### Panels

- Title / help — production-chain wording (no BvB proof claims)
- Status, debug counts, summary, demo status, **activity hint**
- Event log, sprite legend
- Optional **Dual-Bot View** button (diagnostic)

## Smoke gate (`meaningful_count`)

Start→end deltas only between reset and 30 `step_tick()` calls. Requires `meaningful_count >= 2` and `non_sprite_delta_count >= 1`.

See [`RUN_05C_VISUAL_GROUNDWORK_CONTINUATION.md`](RUN_05C_VISUAL_GROUNDWORK_CONTINUATION.md) for signal table.

## Tests and smokes

| ID | Runner | Pass criteria |
|----|--------|---------------|
| VIS-SPECTATOR-001 | `run_tests.gd` | Production chain, 30 ticks, smoke gate |
| VIS-SPECTATOR-002 | `run_tests.gd` | Sprites grounded in real render data |
| VIS-SPECTATOR-003 | `run_tests.gd` | ≥15 mock atlas animations load |
| VIS-SPECTATOR-004 | `run_tests.gd` | `measure_activity_after_steps(30)` passes gate |
| GOD-SMOKE-ECON-SPECTATOR-001 | `run_economy_spectator_smoke.gd` | Same gate as 001 |
| GOD-CAPTURE-ECON-SPECTATOR | `run_economy_spectator_capture.gd -- --assert` | Tick 0/30 PNGs, pixel delta > 5 |

## Review readiness

**Visual groundwork only.** Ready when production-chain path passes strengthened delta gates, VIS-SPECTATOR-001..004 pass, capture passes or limitation documented, and `ECON_BOT_002` remains explicitly unresolved.
