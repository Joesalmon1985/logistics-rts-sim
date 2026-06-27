# Manual Playability QA — Current State (pre-fix baseline)

Captured at start of `feature/manual-playability-qa-polish` from `feature/mrp-full-stack-autonomous`.

## Known failures before this run (code + prior manual observation)

| Mode | Can launch | Readable title | Readable instructions | Tutorial next/back | Play/pause | First action | Blocker |
|------|------------|----------------|----------------------|-------------------|------------|--------------|---------|
| Settler Tutorial | yes | yes | yes | **no** — Next hidden after step 1 | yes | partial | Only Skip + Back to Menu after welcome |
| Settler Free Play | yes | yes | n/a | n/a | yes | yes | — |
| Skirmish Tutorial | yes | partial | partial | **no** | yes | partial | Same stranded navigation |
| Skirmish MVP | yes | partial | partial | n/a | yes | partial | Paper-thin UX |
| Hybrid Tutorial | yes | partial | partial | **no** | partial | partial | Exit did not return to menu; no keyboard shortcuts |
| Hybrid MVP | yes | partial | partial | n/a | partial | partial | Time controls inconsistent |
| 3D Bot Spectator | yes | partial | partial | n/a | yes | partial | Different autoplay model |
| Debug Viewers | yes | debug | debug | n/a | step only | n/a | Not a playable mode |

## Screenshot paths (post-fix captures)

Primary captures: `docs/audit/screenshots/manual_playability/`

Real GUI (if DISPLAY): `docs/audit/screenshots/manual_playability/real_gui/`

## Capture method

See `RUN_MANUAL_PLAYABILITY_QA_RESULTS.md` for final capture method (`in_engine_menu_route_fallback` or `real_desktop_window`).
