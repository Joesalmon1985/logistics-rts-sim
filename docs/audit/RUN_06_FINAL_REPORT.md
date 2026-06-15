# RUN_06 Final Report — Independent Linux Assessment

```
Branch:
Summary:
Files changed:
Tests run:
Results:
Known issues:
Unimplemented requirements:
Uncommitted changes:
Suggested next run:
```

Branch: `feature/run-06-neural-training-trial-games` @ `7b6670a`

Summary: Independent Linux assessment (Godot 4.5.2) confirms RUN_06 **milestone pass** on all required gates: prior-kernel regression (158/158 + smokes + integrity), RUN_06 Godot training smokes, and Python pytest (11/11, no skips). **RUN_06 proves neural imitation via held-out BC accuracy** (economy 77.7%, military 66.1% trained on holdout seeds 13/23). **RUN_06 does not prove rollout gameplay improvement**: advisory rollout evaluation could not be completed reliably (IPC timeouts / JSON truncation on long economy rollouts; partial military runs also returned `military_score=0`). This matches lessons learned that smoke scenarios yield zero rollout scores for all policies.

Files changed: (assessment only — no code edits) `docs/audit/RUN_06_FINAL_REPORT.md` (this file). Generated gitignored artifacts under `checkpoints/`, `training_runs/` during verification.

Tests run:

**Environment**
```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
"$GODOT" --version   # 4.5.2.stable.official.6ce3de25a
git check-ignore .venv   # .venv (ignored)
python3 -m venv .venv && pip install -r requirements.txt
timeout 120s "$GODOT" --headless --path godot_project --import   # exit=0
```

**Phase 2a — tool paths (verified)**
```bash
ls godot_project/sim/tools/*training*env*.gd
# run_training_env.gd, training_env_server.gd
ls godot_project/sim/tools/*training*smoke*.gd
# run_training_smoke.gd
```
No documentation drift on script names.

**Phase 1 — prior-kernel gate (feature branch)**
| Command | Exit | Status |
|---------|------|--------|
| `timeout 400s … run_tests.gd` | 0 | **passed** — 158/158, no SCRIPT ERROR |
| `timeout 60s … run_smoke.gd --variant=economy_trial …` | 0 | **passed** |
| `timeout 60s … run_smoke.gd --variant=military_trial …` | 0 | **passed** |
| `timeout 60s … run_smoke.gd --variant=hybrid …` | 0 | **passed** |
| `timeout 120s … run_economy_smoke.gd` | 0 | **passed** |
| `timeout 120s … run_military_smoke.gd --smoke=all --seed=1` | 0 | **passed** |
| `timeout 600s … run_bot_smoke.gd --smoke=all --seed=42` | 0 | **passed** |
| `timeout 120s … run_viewer_smoke.gd --smoke=all` | 0 | **passed** |
| `timeout 200s … verify_failure_modes.gd` | 0 | **passed** — INTEGRITY PASS |

Note: first `run_tests.gd` attempt in sandbox segfaulted (exit 139); rerun outside sandbox passed. `main` baseline not run (feature gate green).

**Phase 2 — RUN_06 Godot smokes**
| Command | Exit | Status |
|---------|------|--------|
| `timeout 300s … run_training_smoke.gd` | 124 | **timed out** after TRN-SMOKE-001 (TRN-SMOKE-002 incomplete) |
| `timeout 600s … run_training_smoke.gd` (rerun) | 0 | **passed** — TRN-SMOKE-001/002, TRAINING SMOKE PASS |
| `timeout 120s … run_training_env.gd -- --self-test` | 0 | **passed** |

**Phase 2c — artifact cleanup before pytest**
```bash
rm -rf training_runs/run06_smoke_test checkpoints/run06_smoke_test \
       training_runs/run06_smoke checkpoints/run06_smoke
```

**Phase 3 — Python pytest**
```bash
timeout 1800s pytest python_tools/tests/ -v   # exit=0, 702s
```
11 passed, 0 skipped, 0 failed.

**Phase 3b — rollout advisory (recorded, not a pass gate)**
- BC holdout accuracy recomputed from `training_runs/run06_smoke/bc_holdout/` datasets:

| Variant | Holdout seed | Trained | Random | Untrained |
|---------|--------------|---------|--------|-----------|
| economy_trial | 13 | **0.777** | 0.018 | 0.000 |
| military_trial | 23 | **0.661** | 0.067 | 0.054 |

- Full-grid `evaluate_variant()` rollout advisory (seeds 11–13 / 21–23, tick budgets 400/600): **not completed**
  - Economy 400-tick trained rollout: `JSONDecodeError` on IPC response (~24 KB payload)
  - Economy 100-tick / random-policy rollouts: `TimeoutError` on `op=step` (>120s)
  - Military 600-tick: one successful sanity run (`military_score=0`, 53s); full grid failed with timeout or JSON decode on retry
- Partial debug metrics (single episodes): `economy_score=0`, `military_score=0` for all policy kinds tested

**Phase 4 — static checks**
| Check | Result |
|-------|--------|
| Weights in git | 0 tracked files |
| `.gitignore` | `checkpoints/`, `training_runs/`, `models/` present |
| Python gameplay authority | **passed** — orchestration via `godot_env` RPC; `metrics.py` only aggregates exported events |
| `git diff --check` | clean |

Results:

| Acceptance item | Required pass? | Status |
|-----------------|----------------|--------|
| Prerequisites (RUN_01–04 + gates) | yes | **passed** |
| Python drives Godot headless (DDR-033) | yes | **passed** |
| TRN-SMOKE-001 | yes | **passed** (Godot + pytest) |
| TRN-SMOKE-002 | yes | **passed** (Godot + pytest) |
| TRN-SMOKE-003 | yes | **passed** (fresh artifacts under `checkpoints/run06_smoke_test/`, `training_runs/run06_smoke_test/`) |
| TRN-IMPROVE-ECON-001 (BC accuracy) | yes | **passed** (`test_trn_improve_econ_001`, holdout 77.7%) |
| TRN-IMPROVE-MIL-001 (BC accuracy) | yes | **passed** (`test_trn_improve_mil_001`, holdout 66.1%) |
| Rollout gameplay improvement | advisory | **not proven** — full advisory grid incomplete; scores 0 where measured |
| BC label matching stable | yes | **passed** (4/4 unit tests) |
| Training artefacts fresh | yes | **passed** (cleanup before pytest; timestamps 2026-06-15 18:11–18:16 UTC) |
| No Python gameplay authority | yes | **passed** |
| Weights not committed | yes | **passed** |
| Existing Godot tests/smokes pass | yes | **passed** |
| Python tests pass (no skips) | yes | **passed** |

**Scope statements (required):**
- **RUN_06 proves neural imitation / policy selection improvement by BC accuracy** on holdout seeds 13 (economy) and 23 (military).
- **RUN_06 does not prove actual rollout gameplay improvement.** Advisory rollout metrics were zero on completed partial runs; full paired rollout grid could not be finished due to training-transport reliability limits on long economy episodes.

**Verdict: RUN_06 milestone PASS** (all required checklist rows green on fresh Linux verification).

Known issues:
- `run_training_smoke.gd` needs **>300s** on Linux for TRN-SMOKE-002 (documented 600s timeout for Linux in `local_verification.md` follow-up).
- Economy rollout IPC: JSON truncation / step timeouts on long episodes as state grows (`export_training_snapshot` helps per-tick but full 400-tick rollouts still fail intermittently).
- Smoke scenarios yield **zero** `economy_score` / `military_score` for trained/random/untrained in partial rollout probes (consistent with RUN_06 lessons learned).
- BC accuracy proves imitation, not in-game dominance — richer rollout eval deferred to RUN_07+.
- Godot must run **outside sandbox** on this Linux host (sandbox segfault exit 139).
- Stray session dir from failed IPC: `godot_project/run06_session_*` (untracked).

Unimplemented requirements: None for RUN_06 scoped acceptance. Out-of-scope items remain out-of-scope (production model quality, hybrid training, Godot UI integration).

Uncommitted changes:
```
?? godot_project/run06_session_d07403da78124fd4a61000aecd847a49/
?? godot_project/sim/tests/training/test_trn_env_004_response_sentinel_prefix.gd.uid
?? godot_project/sim/tools/run_training_smoke.gd.uid
?? godot_project/sim/tools/run_verify_debug.gd.uid
?? godot_project/sim/tools/training_command_match.gd.uid
?? docs/audit/RUN_06_FINAL_REPORT.md
```
(gitignored: `.venv/`, `checkpoints/`, `training_runs/`, `.pytest_cache/`)

Suggested next run: **RUN_07** — Headless Hybrid (`feature/run-07-headless-hybrid` per `docs/runs/`). Add richer rollout eval metrics once hybrid scenarios produce non-zero scores.
