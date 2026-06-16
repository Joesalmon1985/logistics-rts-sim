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

## Post-audit design ratification (2026-06-16)

Human review completed. **DDR-LOG-001–012 approved** and recorded in [`15_worker_carrier_road_node_logistics_foundation_RATIFIED.md`](../design/15_worker_carrier_road_node_logistics_foundation_RATIFIED.md).

Key ratified points:

- Road nodes authoritative; flags are player-facing aliases only
- `ports[]` building entrances with default `[[0, 0]]`
- Carriers per delivery job; carrier-held ware transit (no active node-buffer staging in v1)
- Abstract specialist workers and abstract `builder_work` after material delivery
- Config validation + config-only ware chain proof in RUN_02.7
- Full/debug snapshot logistics contract; training snapshot omissions documented
- RUN_07 hybrid blocked until RUN_02.7 minimum v1 model passes

Implementation remains deferred to **RUN_02.7**.

## Prompt improvement

- RUN_02.7 prompt should reference ratified doc directly; decisions are no longer open.
