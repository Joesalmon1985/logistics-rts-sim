# Lessons — Final God-Strategy Playable (Phases 2–8 attempt)

**Date:** 2026-07-09  
**Branch merged:** `feature/final-god-strategy-phases2-8`  
**Outcome:** Honest hard stop at **Phase 6** (neural gate). Phases 2–4 and 7 sim tests green; Phase 5/8 scaffolding only.

---

## What worked

- Worker needs / policy / selection / pollution systems with **GOD_C–E** and **GOD_G** green.
- Micro scenario loader must append `.json` — missing extension loads empty scenario → vacuous pass/fail.
- Fair AI eval must **not** reuse combat-speed domination regression (1-HP hubs + pre-placed squads → tick-1 “wins”).
- Main menu God-Strategy FFA entry + mobile shell load tests (`god_strategy_mobile`).
- Preflight stayed green: `verify_failure_modes`, `HYB_SMOKE`, `GOD_A_017`.
- Stopping at the Phase 6 kill switch (gap report) preserved PRD integrity — no false product claim.

## What broke

- `PollutionSystem` parse error (`NEED_SCALE` undeclared) → `class_name` methods vanish mid-match (`is_shared_collapse` missing). Fix: qualify as `WorkerNeedsSystem.NEED_SCALE`.
- Domination regression as AI eval → vacuous tick-1 domination; introduced `god_strategy_ai_eval.json` for bot comparison.
- Headless SubViewport screenshot capture often yields empty textures; synthesize layout diagrams as fallback (not DISPLAY product evidence).
- Long fair evals (800 ticks × multiple bots) timeout; CI budget ≤120 ticks is too short for domination proof on fair hubs.
- Background GOD_F runs hit 1h timeouts without PASS/FAIL when still using heavy regression eval.

## Verification lessons

- Always grep for `SCRIPT ERROR` / `Parse Error` even when tests “pass”.
- Separate **proof** scenarios (combat-speed, `god_strategy_domination_regression`) from **bot comparison** scenarios (`god_strategy_ai_eval`).
- Phase 6 kill switch is real: teacher imitation ≠ neural product gate.
- Status vocabulary: timed-out / aborted runs are **not** passes (`docs/verification/local_verification.md`).
- After adding `class_name` types: run `--import` once; keep bootstrap load order (military before god-strategy).

## Command / doc changes

- Filters documented under “Final God-Strategy (Phases 2–8 filters)” in `local_verification.md`.
- Phase artefacts: `docs/audit/god_strategy/final/phase2/` … `phase8/`.
- Phase 6 gap: `docs/audit/god_strategy/final/phase6/GAP_REPORT.md`.
- Next run: `docs/runs/RUN_FINAL_GOD_STRATEGY_PHASE6_NEURAL.md`.

## Risks for next runs

- Real neural training is the critical path; product claims blocked until Phase 6 hard exit.
- Class registration fragility when any god-strategy script fails to compile.
- Fair multi-seed win-rate tables need longer tick budgets or combat-speed *without* 1-HP vacuous wins (document which).
- Phase 8 DISPLAY screenshots require real `DISPLAY` / Xvfb — headless synthesized PNGs are not enough.
- Do not weaken GOD_F assertions to greenwash teacher imitation as “neural”.

## Prompt improvements for next agent

1. Read Phase 6 gap report + this lessons file before coding.
2. Reuse hybrid modular training transport (`RUN_ACTUAL_MODULAR_NN_TRAINING_*` / hybrid checkpoints pattern) — do not invent a second pipeline.
3. Prove neural vs passive **and** heuristic with committed win-rate JSON under `final/phase6/`.
4. Only after Phase 6 green: resume Phase 8 product DISPLAY pack and claim PRD §1.
