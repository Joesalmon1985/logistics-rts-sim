# RUN_02.8d — Closure Recovery for ECON_BOT_002 and Smoke Hashes

Gameplay and hash closure on branch `feature/run-02-8c-test-runtime-efficiency`.

## Goal

Make the branch mergeable by resolving Tier 2 blockers without weakening tests or blind re-pinning:

- `test_ECON_BOT_002_dual_bot_expansion_metrics`
- `ECON-SMOKE-006` replay hash
- `REP-SMOKE-001` export/import hash identity

## Hard rules

- Do not weaken `ECON_BOT_002` thresholds
- Do not skip or delete tests
- Fix functional failure before smoke hash work
- Re-pin hashes only after deterministic, documented behaviour
- Do not touch `run_economy_bvb_chain_debug.gd`

## Branch

`feature/run-02-8c-test-runtime-efficiency` (continues 02.8c infrastructure work)

## Acceptance checklist

- [ ] Phase A: failures reproduced on `ae78996` and recovery branch; determinism recorded
- [ ] Phase B: failing gate identified (buildings / roads / deliveries / stalls)
- [ ] Phase C: `TEST_FILTER=econ_bot_002` exits 0 twice; B–E/F/G regression guards pass
- [ ] Phase D: smoke hashes deterministic; fix or justified re-pin complete
- [ ] Phase E: targeted verification green
- [ ] Phase F: full Tier 2 closure green (`188/188`, all smokes, integrity)
- [ ] Audit report with exact commands and exit codes
- [ ] PR opened to `main` (not merged)

## Verification

See [`docs/verification/local_verification.md`](../verification/local_verification.md) Tier 2.
