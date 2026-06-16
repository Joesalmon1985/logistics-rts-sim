# Worker, Carrier, Road Node, and Logistics Foundation — Ratified Decisions

**Status:** Approved / Ratified (2026-06-16)

**Supersedes:** [`15_worker_carrier_flag_logistics_foundation_DECISION_DRAFT.md`](15_worker_carrier_flag_logistics_foundation_DECISION_DRAFT.md) (audit draft)

**Authority:** Authoritative design basis for **RUN_02.7** economy logistics foundation implementation.

**Sources:** RUN_02.6 audit ([`RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md`](../../audit/RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md), [`RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md`](../../audit/RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md)) and human ratification of DDR-LOG-001–012.

---

## Decision DDR-LOG-001 — Flags vs road nodes

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | **Road node** is the authoritative simulation entity. **Flag** is a Widelands-style / player-facing alias or viewer label for a road node — not a separate graph or entity type. |
| **Consequence for RUN_02.7** | No `flag` entity bucket. Document and viewer use “road node” in technical docs; “flag” only in player-facing copy where helpful. |
| **Deferred** | Separate flag entities; flag-as-distinct-graph semantics. |

---

## Decision DDR-LOG-002 — Building entrances / ports

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | Building access uses configurable **`ports[]`** in building config: list of `[dq, dr]` offsets from building anchor. If `ports[]` is omitted, default is **`[[0, 0]]`** (anchor port). RUN_02.7 implements port placement and routing to valid building entrance nodes. |
| **Consequence for RUN_02.7** | Implement `ports[]` in config loader, road-node creation per port, pathfinding to entrance nodes; test multi-hex footprints (ECON-NODE-001). |
| **Deferred** | Auto-edge port placement without config. |

---

## Decision DDR-LOG-003 — Carrier assignment

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | For v1, carriers are assigned **per delivery job**. Segment reservation, segment-owned carriers, and carrier-per-road mechanics are **deferred**. |
| **Consequence for RUN_02.7** | Keep `_take_idle_carrier` job model; document first-idle policy; optional contention tests. Congestion remains path **cost** modifier. |
| **Deferred** | Per-segment reservation; carrier-per-road assignment. |

---

## Decision DDR-LOG-004 — Specialist worker movement

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | Specialist workers remain **abstract assigned slots** for v1. No physical specialist-worker pathing, commuting, housing, or labour simulation in RUN_02.7. |
| **Consequence for RUN_02.7** | Worker system stays slot-fill + reachability check; focus implementation on **ware** logistics. |
| **Deferred** | Worker entities moving on road network; wages, housing, commuting, morale, strikes. |

---

## Decision DDR-LOG-005 — Builder model

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | Construction uses **material delivery through logistics**, then **abstract `builder_work` progress**. Do **not** implement explicit site-assigned builder worker entities in v1. Documentation must distinguish **builder workers** (future/worker-type concept) from **`builder_work`** (abstract construction progress ware). |
| **Consequence for RUN_02.7** | Align `economy_construction_system.gd` behaviour with docs; glossary and requirements updated; no builder-to-site assignment. |
| **Deferred** | Explicit builder workers assigned to construction sites. |

---

## Decision DDR-LOG-006 — Ware movement fidelity / node buffers

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | **Carrier-held transit** for v1. Wares do **not** actively stage in road-node buffers during RUN_02.7. Road-node `inventory` / buffer schema is **reserved / future design** unless node staging is deliberately implemented in a later run. **ECON-ROAD-004** documentation updated to reflect this (not active staging). |
| **Consequence for RUN_02.7** | No logistics reads/writes to node buffers; update requirements and tests/docs for ROAD-004 semantics (capacity schema vs active staging). |
| **Deferred** | Junction ware staging; dual carrier + node buffer model. |

---

## Decision DDR-LOG-007 — Data-driven depth

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | RUN_02.7 adds **lightweight config validation** and proves at least one **config-only** new ware/recipe/building chain — preferably a **brick chain** — to show the economy is not hard-coded to wood/planks only. |
| **Consequence for RUN_02.7** | `EconomyConfig` validation hooks; integration test for config-only chain without GDScript ware enums. |
| **Deferred** | Full schema DSL; large content packs. |

---

## Decision DDR-LOG-008 — Minimum v1 logistics model

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | Before **RUN_07** hybrid work, the economy foundation must include: |

1. Road nodes and road segments as real simulation entities
2. Building entrances via `ports[]`
3. Explicit delivery jobs (source, destination, ware, quantity, status, `block_reason`)
4. Explicit carrier entities assigned per job
5. Node-based carrier routes
6. Materials delivered before construction progress
7. Abstract `builder_work` progress after materials
8. Abstract specialist worker slots with tool gating
9. Diagnostics: no-route, no-carrier, missing-worker, missing-tool
10. At least one config-only new ware/recipe/building chain test
11. Full/debug snapshots exposing roads, nodes, carriers, jobs, construction state
12. Deterministic logistics smoke coverage
13. All existing economy/kernel smokes and replay checks passing

| **Consequence for RUN_02.7** | Backlog stages 1–9 in this document define implementation order; RUN_07 blocked until acceptance criteria met. |
| **Deferred** | Hybrid military wiring (RUN_07). |

---

## Decision DDR-LOG-009 — Deferred systems

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | Defer: wages, housing, commuting, morale, strikes, disease, civic buildings, power, ports/ships, **hybrid military wiring** (RUN_07), neural training changes, viewer visual polish. **Snapshot/export readiness remains in scope for RUN_02.7** — economy foundation is prepared for later hybrid and viewer work (see DDR-LOG-010). |
| **Consequence for RUN_02.7** | Scope guard: no civic systems, hybrid implementation, viewer **polish**, or UI redesign in RUN_02.7; viewer **observability** and snapshot/export logistics fields/tests are in scope (see [`RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md`](../runs/RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md) Phase 9). |
| **Deferred** | Hybrid **implementation** (not hybrid-prep snapshot/export work); civic, wages, labour simulation, viewer polish, node-buffer staging, specialist-worker pathing. |

---

## Decision DDR-LOG-010 — Snapshot contract

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | **Full/debug snapshots** must expose logistics state for later visual review: `roads`, `road_nodes`, building ports/entrances (when implemented), `carrier_markers`, delivery jobs / movement markers, route/status/`block_reason`, construction material state, `diagnostics`. **Training snapshots** may omit visual/logistics fields unless neural/training work requires them; any omission must be **explicitly documented** in architecture docs. |
| **Consequence for RUN_02.7** | Snapshot tests assert logistics keys on full export; document training snapshot omissions in `02_command_event_snapshot_contract.md`. |
| **Deferred** | Pixel/render contracts; policy confidence in snapshots. |

---

## Decision DDR-LOG-011 — Backwards compatibility

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | Building configs without `ports[]` default to **`ports: [[0, 0]]`**. Existing tests and scenarios should continue to work unless explicitly updated during RUN_02.7 implementation. |
| **Consequence for RUN_02.7** | Loader default; regression suite must stay green after ports implementation. |
| **Deferred** | — |

---

## Decision DDR-LOG-012 — Terminology

| | |
|---|---|
| **Status** | **Approved** |
| **Final decision** | Use **road node** as the simulation term. Use **flag** only as a player-facing / Widelands analogy. Documentation must **not** imply a separate flag entity. |
| **Consequence for RUN_02.7** | Glossary, requirements, catalogue, and code comments aligned. |
| **Deferred** | — |

---

## Minimum v1 model (ratified summary)

See DDR-LOG-008. Explicitly **not** in v1: node-buffer staging (DDR-LOG-006), worker pathing (DDR-LOG-004), site-assigned builders (DDR-LOG-005), segment carrier assignment (DDR-LOG-003).

---

## Implementation backlog (RUN_02.7)

Stages unchanged from audit draft; all stages now governed by ratified DDR-LOG decisions above.

| Stage | Goal |
|-------|------|
| 1 | Config validation + config-only ware chain proof (DDR-LOG-007) |
| 2 | `ports[]` + entrance routing (DDR-LOG-002, DDR-LOG-011) |
| 3 | Delivery jobs + route diagnostics |
| 4 | Carrier per-job assignment + node-by-node transit (DDR-LOG-003, DDR-LOG-006) |
| 5 | Materials then `builder_work` (DDR-LOG-005) |
| 6 | Diagnostics fixes (tool shortage, stall strings) |
| 7 | Deterministic logistics smoke (HQ → site via network) |
| 8 | Full/debug snapshot logistics assertions (DDR-LOG-010) |
| 9 | Viewer observability — functional visibility in economy viewer (Phase 9 of [`RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md`](../runs/RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md)); **not** aesthetic polish |
| 10 | Regression and final verification (Phase 10 of run doc) |

Detail per stage: see [`RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md`](../runs/RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md) — implement per ratified decisions only.

---

## RUN_02.7 acceptance criteria (ratified)

- [ ] HQ/storehouse supplies construction site through road network (no teleport).
- [ ] Road nodes + segments real; building entrances via `ports[]` (DDR-LOG-002).
- [ ] Delivery jobs: source, destination, ware type, quantity, status, `block_reason`.
- [ ] no-route / no-carrier / missing-worker / missing-tool diagnostics.
- [ ] Carriers required for ware movement; carrier-held transit only (DDR-LOG-006).
- [ ] `builder_work` only after materials delivered (DDR-LOG-005).
- [ ] Config-only new ware/recipe/building chain test (DDR-LOG-007).
- [ ] Full/debug snapshots expose logistics per DDR-LOG-010.
- [ ] ECON-SMOKE-001–006 + replay hash pass.
- [ ] Full test suite green; existing scenarios compatible (DDR-LOG-011).
- [ ] Viewer observability acceptance met (road nodes, ports, carriers, jobs, diagnostics visible from snapshot/export; no viewer-fabricated state).

---

## Decision control table (ratified)

| Decision | Final decision | User decision needed? | Blocks RUN_02.7? |
|----------|----------------|----------------------|------------------|
| DDR-LOG-001 | Road nodes only; flag = alias | No — ratified | No |
| DDR-LOG-002 | `ports[]` offsets; default `[[0,0]]` | No — ratified | Yes (entrance stage) |
| DDR-LOG-003 | Per delivery job | No — ratified | No |
| DDR-LOG-004 | Abstract worker slots | No — ratified | No |
| DDR-LOG-005 | Abstract `builder_work` | No — ratified | No |
| DDR-LOG-006 | Carrier-held transit; buffers reserved | No — ratified | No |
| DDR-LOG-007 | Validation + config-only chain test | No — ratified | No |
| DDR-LOG-008 | Minimum v1 model as listed | No — ratified | Defines scope |
| DDR-LOG-009 | Deferred systems list | No — ratified | No |
| DDR-LOG-010 | Full/debug snapshot logistics | No — ratified | No |
| DDR-LOG-011 | Default `ports: [[0,0]]` | No — ratified | No |
| DDR-LOG-012 | Road node terminology | No — ratified | No |

---

## Related documents

- **Implementation run:** [`RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md`](../runs/RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md)
- Audit run plan: [`RUN_02_6_ECONOMY_FOUNDATION_AUDIT_PLAN.md`](../runs/RUN_02_6_ECONOMY_FOUNDATION_AUDIT_PLAN.md)
- Gap register: [`RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md`](../../audit/RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md)
- Glossary: [`10_glossary.md`](10_glossary.md)
- Requirements: [`02_trial_economy_game_requirements.md`](02_trial_economy_game_requirements.md)
