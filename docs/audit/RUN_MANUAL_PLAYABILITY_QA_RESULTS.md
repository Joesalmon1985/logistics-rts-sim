# Manual Playability QA — Results

Branch: `feature/manual-playability-qa-polish`

## Capture method

`real_desktop_window` when `$DISPLAY` is set (detected `:0`); screenshots captured via in-engine main-menu-route SubViewport harness at **1280×720** (`in_engine_menu_route_fallback` semantics — same menu handlers, not isolated scene-only mounts for primary gates).

## Real GUI checklist

**NOT RUN in this automated session.** `$DISPLAY=:0` is available on the host, but the agent did not drive an interactive `./scripts/run_game.sh` session. Human follow-up: run the checklist in `local_verification.md` and save captures to `docs/audit/screenshots/manual_playability/real_gui/`.

## Tutorial navigation (four buttons always visible)

| Mode | Next/Back | Stranded (Skip+Menu only) | Result |
|------|-----------|---------------------------|--------|
| Settler Tutorial | PASS | Fixed | PASS |
| Skirmish Tutorial | PASS | Fixed | PASS |
| Hybrid Tutorial | PASS | Fixed | PASS |

## Play / pause

| Mode | Visible controls | Unpause / tick | Result |
|------|------------------|----------------|--------|
| Settler Tutorial / Free Play | PASS | PASS | PASS |
| Skirmish Tutorial / MVP | PASS | PASS | PASS |
| Hybrid Tutorial / MVP | PASS (keyboard added) | PASS | PASS |
| 3D Bot Spectator | PlayPauseBtn | PASS | PASS |

## Screenshot matrix

44 captures under `docs/audit/screenshots/manual_playability/` — review in `manual_playability_visual_review.json` (all entries pass after 3D HUD probe fix).

## Manual QA tests (`TEST_FILTER=manual_qa`)

16/18 complete within 400s batch (economy + military bot tests consume most of the window). Individual runs:

- `test_manual_qa_hybrid_bot_functional` — PASS (via `run_tutorial_hybrid_bot_smoke.gd`, ~98s)
- `test_manual_qa_hybrid_3d_bot_match_functional` — PASS

Recommend **500s** timeout for full `TEST_FILTER=manual_qa` suite (18 tests; hybrid bot subprocess ~93s).

## Bot functional summaries

Written to `docs/audit/bot_runs/manual_playability/`:

- `economy_bot_summary.json`
- `military_bot_summary.json`
- `hybrid_bot_summary.json`
- `hybrid_3d_bot_match_summary.json`

## Before / after

| Issue (before) | After |
|----------------|-------|
| Next hidden after step 1 | Back/Next/Skip/Menu always visible; Back disabled on step 1 |
| Hybrid Exit did not return to menu | Exit → main menu |
| Hybrid no keyboard time shortcuts | Space/Step/F1–F3 added |
| No Back button | Added to all three tutorial scenes |
| Headless-only node probes | Menu-route capture + button text in review JSON |

## Honest judgement

**Settler Tutorial and Settler Free Play are human-navigable** with consistent tutorial chrome and time controls. **Skirmish and Hybrid** tutorials now match the four-button rule; MVPs remain playable but still benefit from further UI polish and a real GUI checklist pass. **3D spectator** HUD/time controls pass automated probes; watch bot activity in a real window for confidence.

## Known gaps

- Real GUI checklist not executed in this run (DISPLAY available).
- Full `manual_qa` test suite needs ≥500s timeout when run as one batch.
- Skirmish/Hybrid MVPs are improved but not relabeled — first-action journeys pass automated menu-route tests.
