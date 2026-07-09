# RUN — Final God-Strategy Playable

**Status:** Hard-stopped at Phase 6 (2026-07-09) — Phases 0–4 + 7 sim green; Phase 5/8 scaffolding only  
**PRD:** [`docs/prd/FINAL_GOD_STRATEGY_PLAYABLE_PRD.md`](../prd/FINAL_GOD_STRATEGY_PLAYABLE_PRD.md)  
**Rationale:** [`docs/audit/FINAL_PRD_RATIONALE.md`](../audit/FINAL_PRD_RATIONALE.md)  
**Lessons:** [`docs/audit/RUN_FINAL_GOD_STRATEGY_PLAYABLE_LESSONS_LEARNED.md`](../audit/RUN_FINAL_GOD_STRATEGY_PLAYABLE_LESSONS_LEARNED.md)  
**Phase 6 gap:** [`docs/audit/god_strategy/final/phase6/GAP_REPORT.md`](../audit/god_strategy/final/phase6/GAP_REPORT.md)  
**Next run:** [`RUN_FINAL_GOD_STRATEGY_PHASE6_NEURAL.md`](RUN_FINAL_GOD_STRATEGY_PHASE6_NEURAL.md)  
**Branch prefix:** `feature/final-god-strategy-<phase-slug>`  
**Base:** `main` (phases 2–8 WIP merged from `feature/final-god-strategy-phases2-8`)

---

## Purpose

Execute the Final God-Strategy Playable PRD through Phases **0–8** with hard kill switches, so the product sentence is either **literally true** or the run **stops honestly** at the first failed gate.

---

## Prerequisite documents

Read before any code:

- `.cursor/rules/00-project-identity.mdc`
- `.cursor/rules/01-autonomous-run-policy.mdc`
- `.cursor/rules/02-test-first-development.mdc`
- `.cursor/rules/03-godot-sim-authority.mdc`
- `.cursor/rules/05-ip-originality.mdc`
- `.cursor/rules/06-git-branch-and-review-policy.mdc`
- `.cursor/rules/07-final-report-format.mdc`
- `.cursor/rules/08-neural-training-gates.mdc`
- `.cursor/rules/10-run-verification-and-logging.mdc`
- `docs/prd/FINAL_GOD_STRATEGY_PLAYABLE_PRD.md`
- `docs/design/HYBRID_GOD_STRATEGY_DESIGN_GOALS_FINAL.md`
- `docs/prd/HYBRID_GOD_STRATEGY_MVP_PRD.md` (systems reference only)
- `docs/audit/CURRENT_STATE_OF_PLAY_HYBRID_GAME.md`
- `docs/audit/RUN_GOD_STRATEGY_MILESTONE_A_LESSONS_LEARNED.md`
- `docs/audit/RUN_FINAL_GOD_STRATEGY_PLAYABLE_LESSONS_LEARNED.md`
- `docs/verification/local_verification.md`

---

## In-scope / out-of-scope

**In scope:** PRD Phases 0–8 (domination → ages/culture → workers → heuristic god AI → neural training → pollution → mobile human-vs-neural + DISPLAY screenshots).

**Out of scope:** WiFi multiplayer, store packaging, catalogue expansion beyond PRD §8 freeze, claiming success on scaffolds, silent scripted opponents in Phase 8.

---

## Phase checklist

Do not mark a phase complete without committed artefacts and exit-0 verification for that phase’s hard exit.

| Phase | Slug | Hard exit summary | Status |
|-------|------|-------------------|--------|
| 0 | `phase0-domination` | Scripted domination vs 3 passive; logistics warfare; HYB-SMOKE green | [x] passed 2026-07-08 |
| 1 | `phase1-ages-culture` | Ages + draft + Age 3 VP; reach Age 2+ smoke | [x] passed 2026-07-08 (GOD_B 5/5) |
| 2 | `phase2-worker-needs` | Cohort needs + throughput delta + replay hash | [x] GOD_C 3/3 (2026-07-09) |
| 3 | `phase3-worker-policy` | WorkerPolicy API + masks + perf note | [x] GOD_D 3/3 (2026-07-09) |
| 4 | `phase4-societal-selection` | Divergent culture distributions + telemetry | [x] GOD_E 2/2 (2026-07-09) |
| 5 | `phase5-heuristic-god-ai` | Modular heuristic beats scripted; inspectable proposals | [~] partial — proposals + fair eval scaffold; multi-seed domination bar thin |
| 6 | `phase6-neural-god-ai` | Neural beats **passive and heuristic**; ≥1 neural 4P ending | [ ] **hard stop** — teacher imitation only; see gap report |
| 7 | `phase7-pollution-collapse` | Pollution harm + restoration + shared collapse tests | [x] GOD_G 3/3 (2026-07-09) — sim only; product still blocked by Phase 6 |
| 8 | `phase8-mobile-human-vs-neural` | Touch 4P FFA vs neural; DISPLAY screenshot pack committed | [ ] **blocked on Phase 6** — menu + shell + headless screenshots only |

**PRD complete only when Phase 8 is checked and the product sentence is true.**

---

## DDR-028 single A/B split

Preferred: one long run completes 0–8.

| Track | Phases | Status |
|-------|--------|--------|
| **Final-A (attempt)** | 0–6 | Stopped at Phase 6 kill switch 2026-07-09; gap report committed |
| **Final-B** | 7–8 product | **Not started** — Phase 7 sim tests landed early as scaffolding; product Phase 8 waits on Phase 6 |

Continuation: [`RUN_FINAL_GOD_STRATEGY_PHASE6_NEURAL.md`](RUN_FINAL_GOD_STRATEGY_PHASE6_NEURAL.md) completes Phase 6, then Phase 8.

Forbidden: splitting before Phase 6; multiple silent splits; weakening DoD; claiming neural when using teacher imitation.

---

## Verification

Status vocabulary: **passed** | **failed** | **errored** | **timed out** | **not run**.

### Preflight (start of run and each phase)

```bash
export GODOT=...  # full path
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
# Kill only stale processes for this project if needed:
# pgrep -af 'Godot.*godot_project' ; pkill -f 'Godot.*godot_project'
timeout 120s "$GODOT" --headless --path godot_project --import
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
TEST_FILTER=HYB_SMOKE timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
```

### Phase filters (see also `local_verification.md`)

```bash
TEST_FILTER=GOD_C timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=GOD_D timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=GOD_E timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=GOD_G timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=GOD_F timeout 2400s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=god_strategy_mobile timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
```

Use `god_strategy_ai_eval.json` for bot win-rate comparison — **not** `god_strategy_domination_regression` (combat-speed proof only).

### Phase 8 screenshots

- Require real `DISPLAY` (Xvfb or desktop).
- Capture named sequence per PRD §10 into `docs/audit/screenshots/god_strategy_final/`.
- Commit PNGs + review JSON/MD; fail on 1×1 / map-only / clipped panels / &lt;44px targets.
- Headless synthesized layouts are **not** sufficient for Phase 8 hard exit.

### Full suite

Only at phase boundaries or run end; allow **900s+** (often 2400s+) for `run_tests.gd`. Prefer `TEST_FILTER` during iteration.

---

## Artefact layout

```text
docs/audit/god_strategy/final/
  phase0/ … phase8/
docs/audit/screenshots/god_strategy_final/
  gs_final_00_main_menu.png
  …
  review.json / review.md
docs/audit/RUN_FINAL_GOD_STRATEGY_PLAYABLE_LESSONS_LEARNED.md
docs/audit/god_strategy/final/phase6/GAP_REPORT.md
```

Model weights: `models/` or `checkpoints/` — **gitignored**. Commit eval JSON, not weights (unless explicitly requested).

---

## Operational checklist (Cursor)

1. Confirm PRD human-approved.
2. `git status` / branch / log; preserve hardening WIP.
3. Checkout `feature/final-god-strategy-<phase-slug>`.
4. Preflight; stop if red.
5. Implement **one** phase test-first; hard exit; commit; push.
6. On hard-exit failure: gap report; stop.
7. After Phase 8 only: claim PRD complete; lessons-learned; final report format.

---

## Acceptance (run-level)

- [x] Honest stop at Phase 6 with gap report (2026-07-09)
- [ ] All phase hard exits met
- [ ] If claiming complete: PRD §1 sentence true + Phase 8 screenshot pack in git
- [x] `verify_failure_modes.gd` still fails deliberate fixtures (integrity preserved)
- [ ] Final report per `.cursor/rules/07-final-report-format.mdc` when PRD complete

---

## Suggested next step

Execute [`RUN_FINAL_GOD_STRATEGY_PHASE6_NEURAL.md`](RUN_FINAL_GOD_STRATEGY_PHASE6_NEURAL.md): train real god-strategy modular checkpoints, prove neural vs passive and heuristic, then resume Phase 8 DISPLAY product gate.
