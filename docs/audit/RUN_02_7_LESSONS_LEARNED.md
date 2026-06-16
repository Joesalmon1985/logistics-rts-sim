# RUN_02.7 — Lessons Learned

Date: 2026-06-16

## What worked

- Phased implementation with hard gate after `ports[]` caught ECON-SMOKE-003 regression early (HQ port-only config broke anchor road connections).
- Test-first additions (job block reasons, tool diagnostics, construction blocked-until-delivery) found real production-system interaction bugs (tool `paused_reason` overwritten by production tick).
- Viewer observability via `LayerRenderer` render-data layers kept tests deterministic without pixel assertions.
- Config validation strict/non-strict split avoided breaking runtime loads while giving tests a fail-fast path.

## What broke

- Initial HQ `ports: [[1, 2]]` without anchor `[0, 0]` broke existing smokes that connect roads to HQ anchor hex.
- JSON `ports` values parsed as floats caused `RUN01_DETERMINISM_001` snapshot float violation until `get_building_ports()` normalized to integers.
- `request_delivery()` does not create jobs when no route exists — no-route tests must seed jobs directly.

## Verification lessons

- Baseline: 167/167 tests, ECON-SMOKE-001–005, INTEGRITY PASS (exit 0).
- Final: 174/174 tests, ECON-SMOKE-001–006, GOD-SMOKE-001–003, INTEGRITY PASS (exit 0).
- ECON-SMOKE-004 replay hash changed intentionally (`77d9b33e…` → `40fc3d9815…`) due to HQ multi-port + logistics routing; ECON-SMOKE-006 hash pinned separately.

## Risks for next runs

- Multi-port buildings need smokes/tests to connect roads to the intended port hex, not only anchor.
- Training snapshot still omits `road_nodes` by design — document before RUN_06 if neural observers need node-level features.

## Suggested next run

- RUN_07 hybrid wiring (blocked until RUN_02.7 acceptance met — now met on branch).
