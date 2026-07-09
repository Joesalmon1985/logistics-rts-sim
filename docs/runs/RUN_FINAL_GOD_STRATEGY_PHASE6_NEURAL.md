# RUN — Final God-Strategy Phase 6 Neural (continuation)

**Status:** Ready — blocked predecessor stopped at Phase 6 kill switch (2026-07-09)  
**Predecessor:** [`RUN_FINAL_GOD_STRATEGY_PLAYABLE.md`](RUN_FINAL_GOD_STRATEGY_PLAYABLE.md)  
**Gap report:** [`docs/audit/god_strategy/final/phase6/GAP_REPORT.md`](../audit/god_strategy/final/phase6/GAP_REPORT.md)  
**Lessons:** [`docs/audit/RUN_FINAL_GOD_STRATEGY_PLAYABLE_LESSONS_LEARNED.md`](../audit/RUN_FINAL_GOD_STRATEGY_PLAYABLE_LESSONS_LEARNED.md)  
**PRD:** [`docs/prd/FINAL_GOD_STRATEGY_PLAYABLE_PRD.md`](../prd/FINAL_GOD_STRATEGY_PLAYABLE_PRD.md) § Phase 6  
**Branch:** `feature/final-god-strategy-phase6-neural`  
**Base:** `main` after merge of `feature/final-god-strategy-phases2-8`

---

## Purpose

Complete the **Phase 6 neural hard exit** so God-Strategy opponents are trained modular nets (not teacher imitation), then unlock Phase 8 productization.

This is the **single allowed continuation** after the Phase 6 stop — not a silent split of earlier phases.

---

## Prerequisite documents

- All docs listed in `RUN_FINAL_GOD_STRATEGY_PLAYABLE.md`
- `docs/audit/god_strategy/final/phase6/GAP_REPORT.md`
- `docs/audit/RUN_FINAL_GOD_STRATEGY_PLAYABLE_LESSONS_LEARNED.md`
- `docs/runs/RUN_ACTUAL_MODULAR_NN_TRAINING_AND_SPECTATOR_EVAL.md` (hybrid training pattern)
- `docs/architecture/04_bot_and_training_architecture.md`
- `.cursor/rules/08-neural-training-gates.mdc`
- `docs/verification/local_verification.md` (Final God-Strategy filters)

---

## In scope

- Python↔Godot headless training for god-strategy modular policies (BC from scripted/heuristic teachers; optional light RL).
- Checkpoints under gitignored `models/` or `checkpoints/`.
- `LearnedModularGodBot` loads real checkpoint (no silent scripted fallback in product mode).
- Win-rate tables: neural vs **passive** and vs **heuristic**, ≥N seeds (document N; start ≥3, raise over time).
- ≥1 committed 4P match ending with neural winner under domination or Age 3/4 rules (not tick-cap-only).
- Artefacts under `docs/audit/god_strategy/final/phase6/` (eval JSON, not weights).
- GOD_F_003 (or successor) green with real inference.

## Out of scope

- Claiming Phase 8 / PRD §1 complete before Phase 6 hard exit.
- Weakening win-rate bars or treating teacher imitation as neural.
- Catalogue expansion; WiFi multiplayer; store packaging.
- Replacing fair `god_strategy_ai_eval` with 1-HP combat-speed vacuous wins.

---

## Hard exit checklist

- [ ] Training produces god-strategy checkpoints + committed eval JSON
- [ ] Neural beats passive (win-rate table, documented seeds)
- [ ] Neural beats heuristic (win-rate table, documented seeds)
- [ ] ≥1 neural 4P domination or Age ending artefact
- [ ] Invalid action rate below documented threshold; fog-limited obs for eval bots
- [ ] Preflight still green: `verify_failure_modes`, `HYB_SMOKE`, `GOD_C`–`GOD_E`, `GOD_G`
- [ ] Update `RUN_FINAL_GOD_STRATEGY_PLAYABLE.md` Phase 6 → passed; unlock Phase 8 run

---

## Verification (illustrative)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
timeout 120s "$GODOT" --headless --path godot_project --import
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
TEST_FILTER=HYB_SMOKE timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=GOD_F timeout 2400s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
# Plus project-specific training CLI once added — document exact command here when implemented.
```

Use fair scenario `scenarios/god_strategy/god_strategy_ai_eval.json` (or successor). Do **not** use `god_strategy_domination_regression` for win-rate comparison.

---

## After Phase 6 passes

1. Commit + push phase6 artefacts.
2. Start Phase 8 product run: DISPLAY screenshot pack, real neural P2–P4, touch QA.
3. Only then mark PRD complete.

---

## Acceptance

- [ ] Phase 6 hard exit met **or** new gap report with honest stop
- [ ] Final report per `.cursor/rules/07-final-report-format.mdc`
- [ ] Lessons appended to `RUN_FINAL_GOD_STRATEGY_PLAYABLE_LESSONS_LEARNED.md`
