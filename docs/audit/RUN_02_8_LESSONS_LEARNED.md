# RUN_02.8 — Lessons Learned

## What worked

- Two-player `spawns[]` bootstrap mirroring military init; SP fallback unchanged.
- Post-map-prep starting buildings in `EconomyBvbSetup` (forests/deposits must exist before woodcutter placement).
- Road spine from HQ to each spawn building unlocks workers, production, and deliveries quickly.
- Lightweight `export_bot_observation_snapshot` keeps dual-bot matches runnable in CI (~90s / 30 ticks).
- Strict p1→p2→tick alternation with pinned replay hash.

## What broke

- Starting buildings in scenario JSON silently failed when placed before map prep or on wrong terrain/HQ footprint.
- Dual-bot per-tick full snapshot export was too slow for 4500-tick plan targets.
- `get_summary()` diagnostics tick changed replay hash — smokes must hash state immediately after `run_match`.
- Bot smoke seed override (default 42) caused hash drift vs scenario seed 28001.

## Test / verification lessons

- Wrap dual-bot smokes with **200s+** timeout; GOD-SMOKE-005 needs **130s+** (30-tick match + viewer load).
- ECON-SCORE-001 must compare tick-0 scores before mutations, not post-mutation state.
- Run Godot verification outside sandbox (`all` permissions) — headless segfault in sandbox on this machine.

## Risks for next runs

- Match length capped at **30 ticks** for CI; longer horizons need perf work or offline review only.
- Bot heuristics are minimal — tune before using as training baseline.
- Visual review bundle requires non-headless Godot with display.

## Suggested prompt improvements

- Explicitly require post-map spawn building placement when resources depend on map prep.
- Document replay hash must use `ReplayHash.compute_state_hash` post-match, not post-diagnostics tick.
