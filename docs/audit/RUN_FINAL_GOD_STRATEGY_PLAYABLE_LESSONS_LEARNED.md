# Lessons — Final God-Strategy Playable (Phases 2–8 attempt)

## What worked

- Worker needs / policy / selection / pollution systems with GOD_C–E / GOD_G green
- Micro scenario path must include `.json` (empty scenario → vacuous failures)
- Fair AI eval must **not** reuse combat-speed domination regression (1-HP hubs + pre-placed squads → tick-1 “wins”)
- Main menu God-Strategy FFA entry + mobile shell load tests

## What broke

- `PollutionSystem` parse error (`NEED_SCALE` undeclared) → class_name methods vanish mid-match (`is_shared_collapse` missing)
- Domination regression as AI eval → vacuous tick-1 domination
- Headless SubViewport screenshot capture often yields empty textures; synthesize layout diagrams as fallback
- Long fair evals (800 ticks × multiple bots) timeout; keep AI eval ≤120 ticks for CI

## Verification lessons

- Always grep for `SCRIPT ERROR` / `Parse Error` even when tests “pass”
- Separate **proof** scenarios (combat-speed) from **bot comparison** scenarios
- Phase 6 kill switch is real: teacher imitation ≠ neural product gate

## Risks for next runs

- Real neural training is the critical path; product claims blocked until then
- Class registration fragility when any god-strategy script fails to compile
