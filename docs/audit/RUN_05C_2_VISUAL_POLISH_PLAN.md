# RUN_05C.2 — Visual Polish Plan and Audit

Date: 2026-06-17  
Branch: `feature/run-05c-economy-spectator-visual-identity`  
Starting commit: `81c3cd7`

## Baseline (pre-polish)

| Command | Exit | Result |
|---------|------|--------|
| `TEST_FILTER=vis_spectator run_tests.gd` | 0 | 4/4 passed |
| `run_economy_spectator_smoke.gd` | 0 | `meaningful_count=5`, `non_sprite=4` |
| `run_economy_spectator_capture.gd -- --assert` | 0 | `pixel_delta=397` |

Capture paths: `~/.cache/wide_rts_export/run_05c_economy_spectator/spectator_tick{0,30}.png`

## What is already visible

- Hex map with terrain, roads, forest-density overlay, logistics lines (ware routes, delivery jobs)
- 16×16 placeholder sprites: carriers, ware routes, buildings, construction sites, road flow
- Right panel: status, debug counts, summary, activity hint, event log, sprite legend
- Production-chain demo (seed 42) steps deterministically with real start→end deltas

## Legibility problems

| Issue | Detail |
|-------|--------|
| Small sprites | 16×16 at 1.4× scale — hard to distinguish ware types |
| Generic ware color | Timber and other wares share similar brown/generic loops |
| No carrying state | Carriers moving vs carrying look the same |
| Construction stages | Only idle vs generic progress loop — no 25/50/75% distinction |
| Static feel | Some sprites present at tick 0 and unchanged — delta gate catches this but visuals still flat |
| Cause/effect | Activity hint shows current snapshot, not “what changed in last 30 ticks” |
| No overlay toggles | Cannot hide sprites or route lines independently |
| Legend | Color dots only — no animation name reference |

## Sprite families at tick 30 (production chain)

| Family | Exposed by demo? | Currently active? |
|--------|------------------|-------------------|
| Logistics (carriers, routes) | Yes | Yes |
| Buildings (working/idle) | Yes | Yes |
| Construction | Unlikely (no new sites in 30 ticks) | No — acceptable |
| Storage | Yes (HQ/store markers) | Partial |
| Warning/blocked | Unlikely in stable chain | No — acceptable |

Source → transport → processing traceable via forest D metric and carrier/route markers, but ware type not shown on sprites.

## RUN_05C.2 planned improvements

1. **Atlas:** 32×32 frames, 21 animation contract (carrying, plank/stone ware, construction stages)
2. **Resolver:** Grounding reasons on every sprite; synthetic tests for all mappings
3. **Scene:** Recent-changes panel, overlay toggles, expanded legend, route direction hints
4. **Smoke:** Flexible visual-family gate (no faking absent families)
5. **Capture:** tick 0/10/30/60 contact sheet under `training_runs/visual_review/run_05c_economy_spectator/`

## Authoritative gates (not pixel delta)

- `meaningful_count >= 2`, `non_sprite_delta_count >= 1`
- Sprite-state set changes
- Visual-family representation (flexible)
- Grounding reasons on selected states

## Out of scope

- `ECON_BOT_002`, bot smokes, authoritative economy rules, dual-bot automated gates, final art
