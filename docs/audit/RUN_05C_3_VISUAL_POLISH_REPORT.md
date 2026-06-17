# RUN_05C.3 — Economy Spectator Visual Polish Report

Date: 2026-06-17  
Branch: `feature/run-05c-economy-spectator-visual-identity`  
Starting commit: `2a7b3fc` (RUN_05C.2 end)

## Purpose

Review-led visual polish pass on the production-chain economy spectator. **Not** bot expansion proof, **not** authoritative gameplay changes.

| Topic | Status |
|-------|--------|
| `ECON_BOT_002` | **Unresolved and untouched** |
| `BOT-SMOKE-004` / `ECON-SMOKE-007` | **Outstanding — not in scope** |
| Automated demo | `EconomyDemo.build_production_chain()` seed 42 only |
| Dual-bot | Manual diagnostic toggle only |
| Generated review artefacts | **Not committed** (under `training_runs/`) |

## Review artefacts inspected

Path: `training_runs/visual_review/run_05c_economy_spectator/`

- `spectator_tick0.png`, `spectator_tick10.png`, `spectator_tick30.png`, `spectator_tick60.png`
- `contact_sheet.html`, `capture_manifest.json`, `REVIEW_NOTES.md` (post-RUN_05C.3)

See [`RUN_05C_3_VISUAL_REVIEW_REGISTER.md`](RUN_05C_3_VISUAL_REVIEW_REGISTER.md) for 31 pre-polish observations.

## Visual improvements delivered

### Screen hierarchy (Phase 1)

- Top banner: **Production-chain spectator — visual demo, not bot proof**
- Unified transport bar: tick | play state | speed | activity score
- Grouped legend by family (logistics, wares, buildings, construction, storage, warnings)
- Dedicated **What changed recently?** panel (prominent over debug text)
- Capture review path displayed in UI
- Debug line reduced to events + exposable family count

### Cause/effect trace (Phase 2)

- **Active chain** panel derived from render-data fingerprint (shows “not currently active” when absent)
- Forest source hex highlighted (SRC D: label + ring)
- Stronger ware-route arrows and src/dst hints
- Delivery job directional trails
- Moving carriers larger with motion ring vs idle dots
- Destination building pulse on real `ware_delivered` / delivery events

### Animation rhythm (Phase 3)

- Per-animation FPS overrides (road 6, carriers 10, wares 9, pulses 12)
- Pulse durations tuned per animation family; no duplicate pulse timers per key
- Sprite budget cap `SPRITE_BUDGET_CAP = 48` enforced in sprite layer

### Capture package (Phase 4)

- Contact sheet: tick, sprites, activity score, meaningful_count, pixel Δ@0, families, manifest link
- Manifest extended with activity metrics per milestone
- `REVIEW_NOTES.md` auto-generated

### Quality gates (Phase 5)

- **VIS-SPECTATOR-006** — overlay toggles do not change replay hash/snapshot; sprite budget bounded; duplicate refresh stable

## Verification (Phase 7)

| Command | Exit | Status |
|---------|------|--------|
| `TEST_FILTER=vis_spectator run_tests.gd` | 0 | 6/6 passed |
| `run_economy_spectator_smoke.gd` | 0 | meaningful_count=5, families=3/3, sprites=10 |
| `run_economy_spectator_capture.gd -- --assert` | 0 | pixel_delta_0_30=1261 |
| `run_viewer_smoke.gd` | 0 | pass |
| `verify_failure_modes.gd` | 0 | pass |
| `git diff --check` | 0 | clean |

**SCRIPT ERROR count:** 0 (capture emits non-fatal Control size warning only).

## Review readiness

**Ready for review as visual groundwork only** — all VIS-SPECTATOR tests, spectator smoke, capture, viewer smoke, and failure-mode integrity pass; effects remain grounded; `ECON_BOT_002` untouched.
