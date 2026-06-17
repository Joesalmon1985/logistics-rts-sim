# RUN_05C — Economy Spectator Visual Identity

Date: 2026-06-17 (updated RUN_05C.3)  
Branch: `feature/run-05c-economy-spectator-visual-identity`  
WIP ancestor: `214bcd3`

## Scope

Visual groundwork (RUN_05C.1), polish (RUN_05C.2), and review-led UX polish (RUN_05C.3) for the economy spectator — placeholder sprites, render-data bridge, activity panels, grounding reasons, chain trace, and start→end delta verification. **Not** bot expansion proof or RUN_02.8b closure.

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

### Panels (RUN_05C.3)

- Title + **top status banner** (visual demo, not bot proof)
- **Transport bar** — tick | play state | speed | activity score
- Economy summary, demo status, map activity hint
- **Active chain trace** panel (render-data derived)
- **What changed recently?** panel (grounded deltas)
- Event log, grouped family legend
- Capture review path label
- Toggles: sprites, routes/flow, logistics, roads, ownership
- Optional **Dual-Bot View** button (diagnostic)

## Smoke gates

**Authoritative:** `meaningful_count >= 2`, `non_sprite_delta_count >= 1`, flexible visual-family representation (all exposable families represented; no faking).

**Sanity only:** pixel delta tick0→tick30 > 5.

Construction stage sprites required in atlas/resolver tests only — not in production-chain smoke unless `construction_sites` present.

See [`RUN_05C_2_VISUAL_POLISH_REPORT.md`](RUN_05C_2_VISUAL_POLISH_REPORT.md).

## Tests and smokes

| ID | Runner | Pass criteria |
|----|--------|---------------|
| VIS-SPECTATOR-001 | `run_tests.gd` | Production chain, 30 ticks, smoke gate |
| VIS-SPECTATOR-002 | `run_tests.gd` | Sprites grounded; grounding reasons present |
| VIS-SPECTATOR-003 | `run_tests.gd` | All 21 mock atlas animations load |
| VIS-SPECTATOR-004 | `run_tests.gd` | `measure_activity_after_steps(30)` passes gate |
| VIS-SPECTATOR-005 | `run_tests.gd` | Synthetic fixtures prove resolver contract |
| VIS-SPECTATOR-006 | `run_tests.gd` | Overlay toggles viewer-only; sprite budget ≤ 48 |
| GOD-SMOKE-ECON-SPECTATOR-001 | `run_economy_spectator_smoke.gd` | Delta + visual-family gate + sprite count |
| GOD-CAPTURE-ECON-SPECTATOR | `run_economy_spectator_capture.gd -- --assert` | tick 0/10/30/60 + contact sheet + REVIEW_NOTES.md |

## Launch

```bash
$GODOT --path godot_project res://client/scenes/economy_spectator_bot_demo.tscn
```

## Review readiness

**Visual groundwork only.** See [`RUN_05C_3_VISUAL_POLISH_REPORT.md`](RUN_05C_3_VISUAL_POLISH_REPORT.md).
