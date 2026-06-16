# RUN_02.6 Lessons Learned

Brief handoff after economy foundation audit/planning run.

## What worked

- Evidence standard (impl + test + doc + export) prevented doc-only false positives.
- Baseline verification (167 tests, all smokes) gives RUN_02.7 a trustworthy regression floor.
- Separating RUN_02.6 (audit) from RUN_02.7 (implementation) keeps scope clear.

## What broke / gaps found (audit — resolved at ratification)

- ECON-ROAD-004 vs carrier-only transit — **ratified** as carrier-held transit (DDR-LOG-006)
- Building `ports` — **ratified** for RUN_02.7 implementation (DDR-LOG-002)
- `diagnostics.shortages.tools` — fix planned RUN_02.7
- Matrix gaps (MAP-002, NODE-001, JOB-001, SNAP logistics) — planned RUN_02.7 tests

## Verification notes

### Audit phase (RUN_02.6 exploration)

- Godot 4.5.2; full suite ~2.5 min; economy smokes ~25s.
- Working tree stayed clean; only `godot_project/.godot_user/` touched (gitignored).
- No stale Godot processes after verification.

### Ratification phase (documentation-only)

- **Verification run: none** — documentation-only change; no code paths altered.

## Post-audit design ratification (2026-06-16)

Human review completed. **DDR-LOG-001–012 approved** and recorded in [`15_worker_carrier_road_node_logistics_foundation_RATIFIED.md`](../design/15_worker_carrier_road_node_logistics_foundation_RATIFIED.md).

Key ratified points:

- Road nodes authoritative; flags are player-facing aliases only
- `ports[]` building entrances with default `[[0, 0]]`
- Carriers per delivery job; carrier-held ware transit (no active node-buffer staging in v1)
- Abstract specialist workers and abstract `builder_work` after material delivery
- Config validation + config-only ware chain proof in RUN_02.7
- Full/debug snapshot logistics contract; training snapshot omissions documented
- Deferred: civic/hybrid **wiring**, viewer **polish**, and UI redesign; viewer **observability** and snapshot/export readiness **in scope for RUN_02.7**
- RUN_07 hybrid integration blocked until RUN_02.7 minimum v1 model passes

Implementation is **RUN_02.7** — see [`RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md`](../runs/RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md) (phases 0–10: simulation refactor, snapshot/export, ECON-SMOKE-006, viewer observability, regression). RUN_02.6 closure: [`RUN_02_6_FINAL_REPORT.md`](RUN_02_6_FINAL_REPORT.md).

## Prompt improvement

- RUN_02.7 prompt should reference [`15_worker_carrier_road_node_logistics_foundation_RATIFIED.md`](../design/15_worker_carrier_road_node_logistics_foundation_RATIFIED.md) and [`RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md`](../runs/RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md) directly; decisions are no longer open.
