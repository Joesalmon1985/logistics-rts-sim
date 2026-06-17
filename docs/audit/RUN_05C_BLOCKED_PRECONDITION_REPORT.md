# RUN_05C — Blocked Precondition Report

Date: 2026-06-17  
Branch: `feature/run-05c-economy-spectator-visual-identity`  
Starting commit: `c9b5111941ba24eee0fb85ce535aaebf47e8116e` (main)

## Stop decision

**RUN_05C was stopped** because its **Phase 0 dependency gate was not satisfied**. Per the run plan abort rules, continuing spectator implementation after a failed precondition gate would violate the run contract.

Implementation was halted **before** Phase 7–8 (full docs, full verification, merge). Work through early Phase 6 was already present in the working tree when the stop was ordered.

---

## Phase 0 gate results

| Check | Required | Result | Evidence |
|-------|----------|--------|----------|
| RUN_02.8b final report on `main` | `docs/audit/RUN_02_8b_FINAL_REPORT.md` (or equivalent closure doc) | **FAIL** | Only [`RUN_02_8b_CURRENT_STATE_REPORT.md`](RUN_02_8b_CURRENT_STATE_REPORT.md) exists (recovery baseline, 2026-06-16). No final closure report. |
| `ECON_BOT_002` green twice | exit 0 × 2 | **FAIL** | `TEST_FILTER=econ_bot_002` timed out at 120s (run 1). Run 2 not completed. Prior audit [`RUN_02_8E_HUMAN_PROOF_REPORT.md`](RUN_02_8E_HUMAN_PROOF_REPORT.md) records `ECON_BOT_002` still red (buildings gate). |
| `BOT-SMOKE-004` green | exit 0 | **FAIL / not run** | `timeout 200s run_bot_smoke.gd -- --smoke=004` timed out (exit 124) with no PASS line. |
| `ECON-SMOKE-007` green | exit 0 | **NOT RUN** | Blocked when `BOT-SMOKE-004` command timed out in same shell batch. |
| Checkpoint export artifacts | `training_runs/visual_review/run_02_8_economy_bvb/checkpoints/tick_*.json` + manifest | **FAIL** | `checkpoints:no` — directory/artifacts absent in repo workspace. Export tool exists: `run_economy_bvb_checkpoint_export.gd`. |
| Visual review path | `run_economy_bvb_visual_review.gd` exit 0 + contact sheet output | **PARTIAL** | Review runner exists in tree; output artifacts not present without prior checkpoint export. |

**Gate summary:** 0/6 fully satisfied. **Abort was correct.**

---

## Useful work created before stop

Despite the precondition failure, coherent groundwork was implemented (Phases 1–2 plus partial 3–6):

### Placeholder spritesheet pipeline (Phase 2)

- [`godot_project/client/tools/generate_mock_economy_atlas.gd`](../godot_project/client/tools/generate_mock_economy_atlas.gd) — procedural PNG+JSON generator
- [`godot_project/client/adapters/mock_economy_atlas.gd`](../godot_project/client/adapters/mock_economy_atlas.gd) — `SpriteFrames` loader (uses `Image.load_from_file` for headless)
- [`godot_project/client/art/mock_economy_sprites.png`](../godot_project/client/art/mock_economy_sprites.png) + [`.json`](../godot_project/client/art/mock_economy_sprites.json) — 15 named placeholder animations

### Spectator runtime (Phases 3–5, partial)

- [`godot_project/client/core/economy_spectator_controller.gd`](../godot_project/client/core/economy_spectator_controller.gd) — live `DualBotRunner` stepping, read-only snapshot bridge
- [`godot_project/client/adapters/economy_sprite_state_selector.gd`](../godot_project/client/adapters/economy_sprite_state_selector.gd) — real-state → animation mapping
- [`godot_project/client/adapters/economy_spectator_render_bridge.gd`](../godot_project/client/adapters/economy_spectator_render_bridge.gd) — tick deltas + effect queue
- [`godot_project/client/ui/economy_sprite_layer.gd`](../godot_project/client/ui/economy_sprite_layer.gd) — `AnimatedSprite2D` pool
- [`godot_project/client/scenes/economy_spectator_bot_demo.tscn`](../godot_project/client/scenes/economy_spectator_bot_demo.tscn) + [`.gd`](../godot_project/client/scenes/economy_spectator_bot_demo.gd) — spectator scene with dual-bot controls
- [`godot_project/client/ui/map_viewport.gd`](../godot_project/client/ui/map_viewport.gd) — public `hex_center_pixel_float()` for sprite positioning
- [`godot_project/client/tools/viewer_test_harness.gd`](../godot_project/client/tools/viewer_test_harness.gd) — `spectator` variant mount

**Demo source used in WIP:** live `DualBotRunner` + `EconomyBvbSetup.PROFILE_VISUAL` (not checkpoint replay — checkpoints missing).

### Tests and smokes (Phase 6, partial)

- `VIS-SPECTATOR-001/002/003` headless tests
- `GOD-SMOKE-ECON-SPECTATOR-001` smoke runner
- `run_economy_spectator_capture.gd` pixel capture runner (not fully verified this stop)
- `verify_failure_modes.gd` — invalid economy spectator smoke id check

### Not completed

- Phase 7 documentation (`RUN_05C_ECONOMY_SPECTATOR_VISUAL_IDENTITY.md`, render inventory, spritesheet spec, matrix/verification updates)
- Phase 8 full verification suite
- Checkpoint-based BvB replay spectator path
- Push / merge

---

## Focused verification (this stop only)

Environment: Linux Godot 4.5.2, `GODOT_USER_DATA_DIR=godot_project/.godot_user`

| Command | Exit | Status |
|---------|------|--------|
| `TEST_FILTER=vis_spectator run_tests.gd` | 0 | **passed** — 3/3 (001, 002, 003) |
| `run_economy_spectator_smoke.gd` | 0 | **passed** — `GOD-SMOKE-ECON-SPECTATOR-001` (deltas=0, sprites=12, tick=80) |
| `TEST_FILTER=econ_bot_002 run_tests.gd` | 124 | **timed out** at 120s |
| `run_bot_smoke.gd -- --smoke=004` | 124 | **timed out** at 200s |
| `run_economy_smoke.gd -- --smoke=007` | — | **not run** |

No `SCRIPT ERROR` lines in focused spectator test/smoke runs.

---

## Is this WIP safe to keep?

**Yes**, with caveats:

- Changes are **scoped** to client viewer/spectator, placeholder art, viewer tests, and smoke tooling. **No authoritative economy rule changes.**
- Coherent and reusable after RUN_02.8b closes: atlas pipeline, sprite-state selector, spectator controller, scene shell.
- **Caveats:**
  - Precondition gate was never satisfied; spectator demo source should switch to proven checkpoint replay once export exists.
  - `ECON_BOT_002` / dual-bot smokes still red or slow on `main`; WIP tests use live `DualBotRunner` and pass at 80 ticks but do not prove full RUN_02.8b acceptance.
  - Do not merge RUN_05C until Phase 0 checklist is green and Phase 7–8 complete.
  - Exclude unrelated untracked `.uid` files and `godot_project/.godot_user_parallel/` from any commit.

---

## What should happen next

1. **Complete RUN_02.8b closure on `main`** (or user-approved equivalent):
   - Land bot expansion proof (`ECON_BOT_002` exit 0 × 2)
   - `BOT-SMOKE-004` + `ECON-SMOKE-007` exit 0
   - Publish `docs/audit/RUN_02_8b_FINAL_REPORT.md`
   - Run `run_economy_bvb_checkpoint_export.gd` and commit or gitignore-document checkpoint artifacts
   - `run_economy_bvb_visual_review.gd` exit 0
2. **Re-open RUN_05C** from updated `main` on `feature/run-05c-economy-spectator-visual-identity` (rebase or fresh branch).
3. **Resume from preserved WIP** at Phase 3 gate verification (scene launch human review), then Phase 4–8 per original plan.
4. **Do not** weaken Phase 0 abort rules or fake spectator activity.

---

## Files changed (grouped)

### docs

- `docs/audit/RUN_05C_BLOCKED_PRECONDITION_REPORT.md` (this file)

### client/art

- `godot_project/client/art/mock_economy_sprites.png`
- `godot_project/client/art/mock_economy_sprites.json`

### client/adapters

- `godot_project/client/adapters/mock_economy_atlas.gd`
- `godot_project/client/adapters/economy_sprite_state_selector.gd`
- `godot_project/client/adapters/economy_spectator_render_bridge.gd`

### client/scenes

- `godot_project/client/scenes/economy_spectator_bot_demo.gd`
- `godot_project/client/scenes/economy_spectator_bot_demo.tscn`

### client/core + client/ui + client/tools

- `godot_project/client/core/economy_spectator_controller.gd`
- `godot_project/client/ui/economy_sprite_layer.gd`
- `godot_project/client/ui/map_viewport.gd` (modified)
- `godot_project/client/tools/generate_mock_economy_atlas.gd`
- `godot_project/client/tools/viewer_test_harness.gd` (modified)

### sim/tests

- `godot_project/sim/tests/viewer/test_vis_spectator_001_render_data_deltas.gd`
- `godot_project/sim/tests/viewer/test_vis_spectator_002_effects_grounded.gd`
- `godot_project/sim/tests/viewer/test_vis_spectator_003_mock_atlas_loads.gd`

### sim/tools

- `godot_project/sim/tools/run_economy_spectator_smoke.gd`
- `godot_project/sim/tools/run_economy_spectator_capture.gd`
- `godot_project/sim/tools/run_tests.gd` (modified — register 3 tests)
- `godot_project/sim/tools/verify_failure_modes.gd` (modified — invalid spectator smoke id)

### project/import/generated (excluded from commit)

- `godot_project/**/*.gd.uid` (Godot editor metadata)
- `godot_project/.godot_user_parallel/` (parallel test userdata)
- Unrelated untracked files from other runs (human proof, bvb debug tools, etc.)
