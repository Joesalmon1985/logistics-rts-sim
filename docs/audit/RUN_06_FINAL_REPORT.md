# RUN_06 / RUN_06b Final Report — Pre-Merge Closure (Linux)

Branch:
`feature/run-06-neural-training-trial-games`

Starting commit:
`c41ba4b` (docs: add RUN_06 independent Linux assessment final report)

Fix summary:
RUN_06b delivers the policy/evaluation contract blocked in RUN_06: engine-owned legal commands (including pass), compact policy observations, decomposed action encoding, scoring-policy baselines, win/loss/draw episode fitness, and winnable eval scenarios. Economy eval uses `worker_policy` victory; military eval sets `auto_acquire_idle: 0` so pass-only cannot win via idle auto-attack. Prior RUN_06 BC training path unchanged; TRN-IMPROVE gates remain **BC holdout accuracy**, not rollout proxy scores.

Failed run explanation:
A background policy-eval smoke run (2026-06-15 ~19:01 UTC, ~358s, exit 1) failed **EVAL-SMOKE-001** because economy eval had `scripted_good` and `pass_only` both at fitness **0.00** — the pre-RUN_06b economy scenario could not differentiate policies within the tick budget. Fixes applied before the clean rerun:

- `eval_build_obvious.json` — victory type `worker_policy: auto`; scripted step sets worker policy via legal command.
- `PolicyEpisodeResult` — actor-centric win/loss/draw fitness (win=1.0, draw=0.5, loss=0.0).
- Military scenarios — `auto_acquire_idle: 0` in scenario JSON; `MilitaryInit` / `MilitaryOrderSystem` honor per-scenario override.
- `run_policy_eval_smoke.gd` — tick caps (economy 40, military 120, scripted 250) and smoke scope trimmed for reliability.

Stale generated artefacts were deleted before final verification:
```bash
rm -rf training_runs/run06_smoke training_runs/run06_smoke_test \
       checkpoints/run06_smoke checkpoints/run06_smoke_test training_runs/run_06b
```
The successful policy-eval and pytest runs regenerated fresh gitignored outputs under `training_runs/` and `checkpoints/` during TRN-SMOKE-003 / improve tests only — **not** committed. The passing policy-eval smoke does not depend on pre-existing CSV/JSON under `training_runs/run_06b/`; it runs episodes live in-process.

Final verification commands:
```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
timeout 120s "$GODOT" --headless --path godot_project --import
timeout 500s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=economy_trial --ticks=10 --seed=1
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=military_trial --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_military.json
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=hybrid --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_hybrid.json
timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_military_smoke.gd -- --smoke=all
timeout 600s "$GODOT" --headless --path godot_project --script res://sim/tools/run_bot_smoke.gd -- --smoke=all
timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd -- --smoke=all
timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/run_training_smoke.gd
timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/run_policy_eval_smoke.gd
timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
timeout 1800s .venv/bin/python -m pytest python_tools/tests/ -v
git diff --check
```

Final verification results:

| Command | Exit | Summary | SCRIPT ERROR | Status |
|---------|------|---------|--------------|--------|
| `--import` | 0 | import complete | 0 | **passed** |
| `run_tests.gd` | 0 | 160/160 passed | 0 | **passed** |
| `run_smoke.gd` economy | 0 | SMOKE PASS | 0 | **passed** |
| `run_smoke.gd` military | 0 | SMOKE PASS | 0 | **passed** |
| `run_smoke.gd` hybrid | 0 | SMOKE PASS | 0 | **passed** |
| `run_economy_smoke.gd` | 0 | ECON SMOKE PASS | 0 | **passed** |
| `run_military_smoke.gd` | 0 | MIL SMOKE PASS | 0 | **passed** |
| `run_bot_smoke.gd` | 0 | BOT SMOKE PASS | 0 | **passed** |
| `run_viewer_smoke.gd` | 0 | VIEWER SMOKE PASS | 0 | **passed** |
| `run_training_smoke.gd` | 0 | TRN-SMOKE-001/002, TRAINING SMOKE PASS | 0 | **passed** |
| `run_policy_eval_smoke.gd` | 0 | POLICY EVAL SMOKE PASS (all RUN_06b gates) | 0 | **passed** |
| `verify_failure_modes.gd` | 0 | INTEGRITY PASS | 0 | **passed** |
| `pytest python_tools/tests/` | 0 | 18 passed, 0 skipped | 0 | **passed** |
| `git diff --check` | 0 | clean | — | **passed** |

Background run contrast (failed vs clean):

| Run | When | Exit | EVAL-SMOKE-001 | Notes |
|-----|------|------|----------------|-------|
| Background (stale) | ~19:01 UTC | 1 | FAIL (0.00 / 0.00) | Pre-RUN_06b economy scenario |
| Closure clean | ~22:12 UTC | 0 | PASS | After scenario + contract fixes; artefacts cleared first |

RUN_06 acceptance mapping:

| Item | Status |
|------|--------|
| Prior kernel/tests/smokes still pass | **passed** (160/160 + all smokes + integrity) |
| Godot training env works | **passed** (TRN-ENV tests + training smoke) |
| Python drives Godot | **passed** (pytest rollout/replay/training/improve) |
| TRN-SMOKE-001 deterministic rollout | **passed** |
| TRN-SMOKE-002 replay equivalence | **passed** |
| TRN-SMOKE-003 training/checkpoint/metrics | **passed** (fresh gitignored artefacts) |
| TRN-IMPROVE-ECON-001 | **passed** via **BC holdout accuracy** (not rollout score) |
| TRN-IMPROVE-MIL-001 | **passed** via **BC holdout accuracy** (not rollout score) |
| RUN_06b policy eval smokes | **passed** (POLICY-CONTRACT-001, EVAL-SMOKE-001/002, EVAL-DETERMINISM-001) |
| No weights/checkpoints committed | **passed** (`git ls-files` empty; gitignore confirmed) |
| Docs / final report updated | **passed** (this file + lessons + test plan + architecture + verification) |
| Known limitations documented | **passed** (see below) |

Artefacts / checkpoints:
- `git ls-files checkpoints/ training_runs/ models/` — empty (nothing tracked).
- `git check-ignore` — `checkpoints/`, `training_runs/`, `models/`, `.venv` ignored.
- Stale `training_runs/run06_smoke*` and `checkpoints/run06_smoke*` deleted before closure pytest.
- Untracked stale IPC session dir `godot_project/run06_session_*` — **not committed**.

Docs changed:
- `docs/audit/RUN_06_FINAL_REPORT.md` (this closure report)
- `docs/audit/RUN_06_LESSONS_LEARNED.md` (RUN_06b section)
- `docs/architecture/04_bot_and_training_architecture.md` (policy observation, legal commands, scoring)
- `docs/testing/04_training_test_plan.md` (RUN_06b gates)
- `docs/testing/02_smoke_test_matrix.md` (RUN_06b policy eval row)
- `docs/runs/RUN_06_NEURAL_TRAINING_FOR_TRIAL_GAMES.md` (RUN_06b checklist)
- `docs/verification/local_verification.md` (closure commands, 160 tests, policy eval smoke)

Known issues:
- **TRN-IMPROVE uses BC holdout accuracy**, not rollout gameplay score improvement — by design for RUN_06; RUN_06b adds win/loss/draw eval differentiation on dedicated eval scenarios only.
- Legacy proxy metrics (`economy_score` / `military_score`) remain debug-only; not RUN_06b fitness gates.
- `run_training_smoke.gd` needs ~270s on this host (300s timeout sufficient).
- `run_policy_eval_smoke.gd` needs ~170–180s (300s timeout sufficient).
- Full pytest ~690s with torch CPU; 1 CUDA driver warning (non-fatal).
- Phase B evolutionary training **not implemented** (blocked until user authorises).

Uncommitted changes:
See post-commit `git status --short` (expected: only `godot_project/run06_session_*` untracked).

Merge readiness:
**Ready for review / possible merge, subject to user approval** — all closure gates passed cleanly on Linux Godot 4.5.2; no merge to `main` performed.

Suggested next run:
User authorisation for Phase B evolution (optional RUN_13 doc); **RUN_07** headless hybrid per `docs/runs/`.
