# RUN_02.6 Lessons Learned

Brief handoff after economy foundation audit/planning run.

## What worked

- Evidence standard (impl + test + doc + export) prevented doc-only false positives.
- Baseline verification (167 tests, all smokes) gives RUN_02.7 a trustworthy regression floor.
- Separating RUN_02.6 (audit) from RUN_02.7 (implementation) keeps scope clear.

## What broke / gaps found

- ECON-ROAD-004 (node buffers) vs carrier-only transit — needs DDR-LOG-006 decision.
- Building `ports` documented but not in config — blocks confident large-footprint routing.
- `diagnostics.shortages.tools` never populated despite tool_missing events.
- Several matrix rule IDs still lack dedicated test files (MAP-002, NODE-001, JOB-001, SNAP-001 logistics).

## Verification notes

- Godot 4.5.2; full suite ~2.5 min; economy smokes ~25s.
- Working tree stayed clean; only `godot_project/.godot_user/` touched (gitignored).
- No stale Godot processes after verification.

## Risks for RUN_02.7

- Replay hash re-pin likely if logistics timing or entrance placement changes.
- Hybrid (RUN_07) should wait until DDR-LOG-002 and minimum v1 model accepted.

## Prompt improvement

- Start RUN_02.7 prompt with decision control table answers from user.
- Explicitly list which DDR-LOG options were chosen before coding.
