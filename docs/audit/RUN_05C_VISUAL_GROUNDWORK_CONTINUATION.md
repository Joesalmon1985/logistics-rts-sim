# RUN_05C.1 — Economy Spectator Visual Groundwork Continuation

Date: 2026-06-17  
Branch: `feature/run-05c-economy-spectator-visual-identity`  
WIP ancestor commit: `214bcd3` — `wip: preserve RUN_05C spectator groundwork blocked by precondition`

## Re-scope statement

**RUN_05C.1 is visual groundwork only.** It continues from preserved WIP on the feature branch without claiming RUN_02.8b closure or full bot-vs-bot economy expansion proof.

| Topic | Status |
|-------|--------|
| RUN_02.8b closure | **Not required** for this continuation |
| `ECON_BOT_002` | **Remains unresolved** — separate gameplay/scenario viability problem; must not be fixed, weakened, or re-pinned here |
| `BOT-SMOKE-004` / `ECON-SMOKE-007` | **Outstanding** — documented as unrelated red tests |
| Bot expansion proof | **Not claimed** by this run |
| Review target | Visual groundwork review candidate only — **not** project closure |

## Do not run or fix bot closure

**Do not run long bot closure commands** (`ECON_BOT_002`, `BOT-SMOKE-004`, `ECON-SMOKE-007`) except briefly to document known unrelated red status if not already recorded.

**Do not spend time fixing timeouts or failures** in those tests during this run.

## Branch ancestry check

HEAD may be later than `214bcd3` if re-scope docs were committed. Requirement is ancestry, not exact HEAD:

```bash
git merge-base --is-ancestor 214bcd3 HEAD
echo "ancestor=$?"   # expect 0
```

## Problem with prior WIP smoke

`GOD-SMOKE-ECON-SPECTATOR-001` previously passed with `render deltas=0, sprites=12, tick=80` using dual-bot stepping. Static sprites at end state satisfied the weak gate while **nothing changed over time**.

**RUN_05C.1 must require `meaningful_count >= 2` from start→end changes only** (see below).

## `meaningful_count` rule (critical)

Counts only **start→end changes** between reset (tick 0) and after N steps. **Not** mere presence at end state.

### Valid signals (delta required)

| Signal | Start→end check |
|--------|-----------------|
| `events_grew` | `event_log` length increased |
| `production_progress_changed` | building `production_progress` increased |
| `carrier_or_route_count_changed` | carrier moving count, ware routes, or carrier markers changed |
| `sprite_state_set_changed` | sprite state keys or animation names differ — **not** just `count > 0` |
| `construction_progress_changed` | site delivery % or materials changed |
| `chain_fingerprint_changed` | forest D, store totals, sawmill/woodcutter metrics changed |
| `entity_categories_changed` | `LayerRenderer.count_entities` categories differ |
| `screenshot_pixels_changed` | capture smoke: pixel delta between tick 0 and tick N frames |

### Not sufficient alone (must not count toward meaningful_count)

- sprites exist at end
- buildings/roads exist at end
- initial render data is non-empty
- tick advanced with no visual/state change
- static sprite count > 0 with unchanged state keys

**Smoke gate:** `meaningful_count >= 2`, with at least one non-sprite delta signal recommended.

## Selected stable demo source (Phase 1)

**Primary (default and only automated path):** `EconomyDemo.build_production_chain()` — seed **42**.

| Property | Value |
|----------|-------|
| Scene | `res://client/scenes/economy_spectator_bot_demo.tscn` |
| Controller mode | `production_chain` |
| Transport | `step_tick()` / autoplay sim ticks |
| Smoke/test ticks | **30** |
| Expected start→end activity | Forest D drop, carrier/route changes, production events, sprite key set changes |

**Secondary (manual only):** `dual_bot` via `DualBotRunner` — optional UI toggle for stall diagnostics.

### Dual-bot mode boundary

**Dual-bot mode is allowed as a secondary/manual diagnostic view only.**

**No RUN_05C.1 test or smoke may depend on dual-bot success.** All automated gates use production-chain mode only.

**Not used:** checkpoint replay (artifacts absent).

## Implementation checklist

### Phase 0

- Confirm `git merge-base --is-ancestor 214bcd3 HEAD` → `ancestor=0`
- This document finalized

### Phase 2 — Delta smoke

- Add `economy_spectator_metrics.gd` with start→end delta signals only
- Refactor `economy_spectator_controller.gd`: production_chain default, `measure_activity_after_steps()`
- Update `run_economy_spectator_smoke.gd`: production chain, 30 ticks, `meaningful_count >= 2`
- Update VIS-SPECTATOR-001

### Phase 3 — Scene

- Production-chain help/legend; activity hint panel
- Scene/panel verification gate (launch, step 30 ticks, panels update)
- Optional dual-bot toggle — manual only

### Phase 4 — Tests

- VIS-SPECTATOR-004: production chain, `meaningful_count >= 2`
- All VIS-SPECTATOR tests: **no** `DEMO_DUAL_BOT` path

### Phase 5 — Capture

- Production chain, 30 ticks, pixel start→end delta

### Phase 6 — Docs

- Identity, inventory, spritesheet spec, test matrix, local_verification updates

### Phase 7 — Focused verification

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

TEST_FILTER=vis_spectator timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_spectator_smoke.gd
timeout 180s "$GODOT" --path godot_project --script res://sim/tools/run_economy_spectator_capture.gd -- --assert
timeout 130s "$GODOT" --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

**Not required:** Tier 2, `ECON_BOT_002`, `BOT-SMOKE-004`, `ECON-SMOKE-007`. **Do not fix them.**

### Phase 8 — Commits and push

1. `docs: re-scope RUN_05C as visual groundwork`
2. `fix/test: strengthen spectator delta smoke`
3. `feat: improve economy spectator visual state and panels`
4. `docs: record RUN_05C visual groundwork verification`

Push branch. Do not merge.

## Sprite / state grounding (from WIP)

| Animation | Real state source |
|-----------|-------------------|
| `carrier_move_loop` / `carrier_idle` | `carrier_markers[].state` |
| `ware_log_move_loop` / `ware_generic_move_loop` | `ware_routes`, `delivery_jobs` |
| `road_flow_loop` | roads with active carrier routes |
| `construction_*` | `construction_sites` delivery % |
| `building_working_loop` / `building_idle` | `production_progress`, `production_activity` |
| `storage_activity_pulse` | ware totals delta |
| `warning_blocked_loop` | `blocked_job_markers`, stalls |

## Review readiness (visual groundwork only)

**Ready** when:

- Production-chain is default and sole automated test/smoke path
- Smoke proves `meaningful_count >= 2` from start→end changes
- VIS-SPECTATOR-001..004 pass
- Capture pass or documented limitation
- Docs state `ECON_BOT_002` unresolved
- No gameplay test weakening
- 0 SCRIPT ERROR in focused runs

**Not ready** if smoke passes with zero start→end deltas or static-sprite-only signals.

This is **review readiness for visual groundwork**, not project closure. Known-red gameplay tests may remain on `main`.

## Next step after review (separate runs)

- RUN_02.8b bot expansion closure (`ECON_BOT_002`, smokes, checkpoints)
- Resume from Phase 3 scene/panel verification, then Phase 4–8 on rebased branch
- Dual-bot spectator as manual proof viewer once stable

## Verification record (2026-06-17)

Focused Linux/Godot 4.5.2 runs on `feature/run-05c-economy-spectator-visual-identity`:

| Command | Exit | Status |
|---------|------|--------|
| `TEST_FILTER=vis_spectator run_tests.gd` | 0 | 4/4 passed |
| `run_economy_spectator_smoke.gd` | 0 | `meaningful_count=5`, `non_sprite=4`, `events_delta=127` |
| `run_economy_spectator_capture.gd -- --assert` | 0 | `pixel_delta=397`; PNGs under `run_05c_economy_spectator/` |
| `run_viewer_smoke.gd` | 0 | pass |
| `verify_failure_modes.gd` | 0 | pass |

0 `SCRIPT ERROR` lines in focused runs. `ECON_BOT_002` not run or fixed.

**Review readiness (visual groundwork): Ready**
