# RUN_02.8c — Test Runtime Efficiency and Verification Hygiene

Infrastructure-only run. No gameplay feature changes.

## Goal

Make the Godot test workflow faster, clearer, and safer without weakening, deleting, skipping, or watering down existing coverage.

## Branch

`feature/run-02-8c-test-runtime-efficiency`

## Scope

- Timing and profiling in `run_tests.gd`
- Filter/category/list/manifest-check selection
- Manifest integrity check (manual manifest preserved)
- `SimTestLoop` early-exit helpers for econ bot B–E tests
- Smoke export opt-in hygiene (`run_bot_smoke.gd`)
- Tiered verification docs
- Experimental opt-in parallel wrapper (`python_tools/testing/parallel_godot_tests.py`)

## Hard rules

- Do not delete tests or weaken assertions
- Do not remove replay/hash or failure-mode integrity checks
- Serial full closure gate remains authoritative
- No broad Godot process kills
- No parallel runs inside one Godot process (wrapper only)

## Acceptance checklist

- [ ] Phase 0 clean tree (only allowed untracked debug file)
- [ ] `--manifest-check` exits 0
- [ ] `TEST_FILTER=definitely_no_such_test` exits non-zero
- [ ] Timing output: total, per-test, slowest 20, optional JSON
- [ ] Filter/category/list workflows documented and working
- [ ] B–E bot slice faster via early exit (same caps/assertions)
- [ ] Smoke exports opt-in only
- [ ] Parallel wrapper prototype (optional Phase H)
- [ ] `docs/verification/local_verification.md` updated (narrow count fixes)
- [ ] Runtime report and lessons learned audit docs created

## Verification

See [`docs/verification/local_verification.md`](../verification/local_verification.md) — Tier 1/2/2b/3 workflows.
