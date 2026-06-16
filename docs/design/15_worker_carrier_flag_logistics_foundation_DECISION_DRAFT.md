# Worker, Carrier, Flag, and Logistics Foundation — Decision Draft

**Status:** Needs Human Confirmation (per `docs/design/11_design_decision_register.md`)

**Purpose:** Capture design decisions and a staged implementation backlog for RUN_02.7 (economy logistics foundation hardening). Produced by RUN_02.6 audit; do not treat as ratified until user sign-off.

**Inputs:** [`RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md`](../../audit/RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md), [`RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md`](../../audit/RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md).

---

## Decision DDR-LOG-001 — Flags vs road nodes

| | |
|---|---|
| **Question** | Should flags be explicit simulation entities separate from road nodes? |
| **Why it matters** | Viewer labels, docs/glossary alignment, hybrid military site routing mental model. |
| **Options** | (a) Road nodes only — glossary “flag” = alias (b) Separate flag entities (c) Named road node subtypes |
| **Recommended default** | **(a)** Road nodes only — already implemented |
| **Consequence** | Document “flag = road node” in glossary; no duplicate graph. Viewer shows nodes at junctions/buildings. |
| **Needs Human Confirmation** | Yes |

---

## Decision DDR-LOG-002 — Building entrances / ports

| | |
|---|---|
| **Question** | How should building entrances connect to the road network? |
| **Why it matters** | HQ 3×3 and multi-hex buildings need correct adjacency (GAP-02, ECON-NODE-001). |
| **Options** | (a) Anchor node only (current) (b) Configurable `ports[]` hex offsets per building (c) Auto-place on footprint edge |
| **Recommended default** | **(b)** `ports` in `buildings.json` as `[[dq, dr], ...]` offsets from anchor |
| **Consequence** | RUN_02.7 implements port placement + routing to nearest connected port node; tests for large footprints. |
| **Needs Human Confirmation** | Yes — **blocks RUN_02.7 entrance work** |
| **Blocks RUN_02.7?** | Yes, if large-building routing is in scope |

---

## Decision DDR-LOG-003 — Carrier assignment unit

| | |
|---|---|
| **Question** | What entity should carriers be assigned to? |
| **Why it matters** | Congestion, diagnostics, future segment capacity. |
| **Options** | (a) Per delivery job (current) (b) Per road segment reservation (c) Per full route reservation |
| **Recommended default** | **(a)** Per job; congestion as path cost (+5 per congested segment in pathfinder) |
| **Consequence** | Minimal change; segment occupancy limits deferred. Add contention tests if needed. |
| **Needs Human Confirmation** | Yes |

---

## Decision DDR-LOG-004 — Specialist worker movement

| | |
|---|---|
| **Question** | Should specialist workers move physically on the road network? |
| **Why it matters** | Scope vs Widelands fidelity; viewer animation. |
| **Options** | (a) Abstract slot fill at HQ (current) (b) Worker entities path on network |
| **Recommended default** | **(a)** Defer explicit worker movement |
| **Consequence** | Workers remain logical assignments; reachability check only. Focus RUN_02.7 on **ware** logistics. |
| **Needs Human Confirmation** | Yes |

---

## Decision DDR-LOG-005 — Builder model

| | |
|---|---|
| **Question** | How should construction labour be represented? |
| **Why it matters** | GAP-03; catalogue vs `builder_work` ware; hybrid construction sites. |
| **Options** | (a) Abstract `builder_work` ticks only (b) Explicit builder workers assigned to sites (c) Abstract work + optional builder entities later |
| **Recommended default** | **(c)** Keep abstract `builder_work` after materials; do not assign builder workers to sites in v1 |
| **Consequence** | Matches current `economy_construction_system.gd`; clarify docs distinguishing builder **worker** from builder **work** ware. |
| **Needs Human Confirmation** | Yes |

---

## Decision DDR-LOG-006 — Ware movement fidelity (node buffers)

| | |
|---|---|
| **Question** | Should wares stage in road-node buffers during transit? |
| **Why it matters** | GAP-01; ECON-ROAD-004 doc vs implementation. |
| **Options** | (a) Carrier holds ware in transit only (current) (b) Node staging at junctions (c) Both carrier and node buffers |
| **Recommended default** | **(a)** for v1; **tighten ECON-ROAD-004 docs** to “buffer schema reserved” OR rename rule to “node inventory capacity for future staging” |
| **Consequence** | Avoid large logistics rewrite; ROAD-004 test remains API-level unless (b) chosen. |
| **Needs Human Confirmation** | Yes — **blocks RUN_02.7 if (b) chosen** |
| **Blocks RUN_02.7?** | Only if staging implementation required |

---

## Decision DDR-LOG-007 — Data-driven depth

| | |
|---|---|
| **Question** | How fully data-driven should wares/buildings/recipes be in v1? |
| **Why it matters** | GAP-07; modularity claim for hybrid content expansion. |
| **Options** | (a) Incremental JSON edits without validation (b) Schema validation + config-only add-ware acceptance test |
| **Recommended default** | **(b)** One proven config-only chain (e.g. bricks or new harvestable) + validation hooks |
| **Consequence** | RUN_02.7 adds test; may add lightweight schema checks in `EconomyConfig`. |
| **Needs Human Confirmation** | Yes |

---

## Decision DDR-LOG-008 — Minimum v1 logistics model

| | |
|---|---|
| **Question** | What is the minimum acceptable logistics foundation before hybrid/viewer work? |
| **Recommended default** | See §Minimum v1 model below |
| **Needs Human Confirmation** | Yes |
| **Blocks RUN_02.7?** | Defines RUN_02.7 scope |

---

## Decision DDR-LOG-009 — Deferred systems

| | |
|---|---|
| **Question** | What must wait until foundation is stable? |
| **Recommended default** | Civic, wages, housing, commuting, morale, disease, power, ports/ships, viewer polish, hybrid military (RUN_07), neural training changes |
| **Needs Human Confirmation** | Yes (confirm defer list) |

---

## Minimum v1 model (recommended)

**Must exist before RUN_07 hybrid logistics integration:**

1. Road nodes + segments as real entities; building connected via entrance model (DDR-LOG-002).
2. Explicit carrier entities; jobs assigned per DDR-LOG-003; node-by-node movement.
3. Explicit delivery jobs with source, destination, ware, quantity, status, `block_reason`.
4. Construction: materials via logistics, then abstract `builder_work` (DDR-LOG-005).
5. Abstract specialist worker slots with tool gating; no worker pathing (DDR-LOG-004).
6. Diagnostics: no-route, no-carrier, missing-worker; tool shortage counted (fix GAP-05).
7. Config-driven ware/building/recipe with **one config-only addition tested** (DDR-LOG-007).
8. Full snapshot logistics for viewer; deterministic logistics smoke scenario.
9. Regression: ECON-SMOKE-001–005 + replay hash + economy hardening tests green.

**Explicitly deferred:** node buffer staging (unless DDR-LOG-006 (b)), worker commuting, civic systems, viewer visual pass, hybrid military wiring.

---

## Implementation backlog (RUN_02.7)

### Stage 1 — Formalise data/config model

| | |
|---|---|
| **Goal** | Prove modularity; validate config loads |
| **Files** | `sim/data/config/*`, `economy_config.gd` |
| **Behaviour** | Schema version checks; optional validation errors on load |
| **Tests** | Config-only new ware chain test (e.g. bricks) |
| **Export** | None required |
| **Risks** | Breaking existing scenarios if validation too strict |
| **Acceptance** | New ware/building/recipe works without GDScript enum changes; test passes |

### Stage 2 — Formalise road node / entrance model

| | |
|---|---|
| **Goal** | ECON-NODE-001 satisfied for multi-hex buildings |
| **Files** | `buildings.json`, `economy_road_system.gd`, `economy_map_rules.gd`, `economy_init.gd` |
| **Behaviour** | `ports[]` offsets; node per port or shared anchor policy |
| **Tests** | `test_ECON_NODE_001_node_adjacent_to_building_port` |
| **Export** | Optional `ports` in building snapshot |
| **Risks** | Footprint overlap with roads; migration of existing maps |
| **Acceptance** | HQ/large building routes from network to correct port hex |

### Stage 3 — Delivery jobs + route diagnostics

| | |
|---|---|
| **Goal** | Clear job lifecycle; building/route diagnostics aligned |
| **Files** | `economy_logistics_system.gd`, `economy_diagnostics_system.gd`, `economy_constants.gd` |
| **Behaviour** | Wire or remove `BLD_PAUSED_NO_ROUTE`; job blocked reasons complete |
| **Tests** | `test_ECON_JOB_001`, isolated JOB-002 or extend pri tests |
| **Export** | `diagnostics.delivery` includes waiting_carrier |
| **Risks** | Behaviour change for stalled buildings |
| **Acceptance** | Jobs created on missing input; priority ordering verified |

### Stage 4 — Carrier assignment + ware movement

| | |
|---|---|
| **Goal** | Document and test carrier contention; congestion behaviour |
| **Files** | `economy_logistics_system.gd`, `economy_pathfinding.gd` |
| **Behaviour** | Clarify first-idle policy; optional multi-carrier test scenario |
| **Tests** | Multi-carrier + congestion integration |
| **Export** | Carrier route fields stable |
| **Risks** | Replay hash re-pin if movement timing changes |
| **Acceptance** | Carriers gate delivery; ware moves node-by-node with events |

### Stage 5 — Builder / construction workflow

| | |
|---|---|
| **Goal** | Align docs and behaviour for builder_work vs builder workers |
| **Files** | `economy_construction_system.gd`, design docs |
| **Behaviour** | Materials complete before builder_work ticks; blocked site states |
| **Tests** | Extend cons_001; ECON-CONS-002 progress events if desired |
| **Export** | `construction_sites` missing_wares in diagnostics |
| **Acceptance** | ECON-SMOKE-003 passes; site blocked until materials delivered |

### Stage 6 — Logistics diagnostics

| | |
|---|---|
| **Goal** | Tool shortage counts; optional human-readable stall strings |
| **Files** | `economy_diagnostics_system.gd`, `snapshot_exporter.gd` |
| **Behaviour** | Fix `shortages.tools`; export stall text (GAP-14) |
| **Tests** | Extend tool_004; diag tests |
| **Export** | `stall_reason_text` optional field |
| **Acceptance** | Tool-shortage scenario shows non-zero tools shortage or explicit reason |

### Stage 7 — Deterministic logistics smoke scenario

| | |
|---|---|
| **Goal** | End-to-end HQ → construction site under road network |
| **Files** | `sim/data/scenarios/`, `run_economy_smoke.gd` |
| **Behaviour** | New scenario or ECON-SMOKE-006: contention/blocked route recovery |
| **Tests** | Scenario + replay hash |
| **Export** | — |
| **Acceptance** | Smoke passes; hash pinned |

### Stage 8 — Snapshot/export for viewer readiness

| | |
|---|---|
| **Goal** | Viewer can show full logistics without guessing |
| **Files** | `snapshot_exporter.gd`, snapshot tests |
| **Behaviour** | Assert logistics arrays; decide training snapshot fields (DDR-LOG-009 / GAP-09) |
| **Tests** | Extend snap_002 or new ECON-SNAP-001 logistics test |
| **Export** | road_nodes, carriers, jobs, construction in full + debug snapshots |
| **Acceptance** | Snapshot test asserts logistics keys populated after road+carrier scenario |

### Stage 9 — Regression tests

| | |
|---|---|
| **Goal** | Close matrix gaps without weakening existing tests |
| **Files** | `sim/tests/economy/` |
| **Tests** | MAP-002, NODE-001, JOB-001/002, SNAP logistics, config-only ware |
| **Acceptance** | Full suite + ECON-SMOKE-001–005 green |

### Stage 10 — Visual display run (separate)

| | |
|---|---|
| **Goal** | Improve economy viewer after sim foundation stable |
| **Files** | `client/` viewers, RUN_05 / RUN_02.5c follow-up |
| **Note** | **Not RUN_02.7** — starts after stages 1–9 acceptance |

---

## Future implementation acceptance criteria (RUN_02.7)

- [ ] HQ/storehouse supplies construction site through road network (not direct teleport).
- [ ] Road nodes are real simulation entities; building entrances connect per DDR-LOG-002.
- [ ] Delivery jobs have source, destination, ware type, quantity, status, block_reason.
- [ ] no-route / no-carrier / missing-worker / missing-tool diagnostics where applicable.
- [ ] Carriers required for ware movement to proceed.
- [ ] Builder work progresses only after required materials delivered.
- [ ] At least one new ware/resource/recipe added through config/data with test.
- [ ] Snapshots expose roads, nodes, carriers, jobs, construction progress for viewer.
- [ ] ECON-SMOKE-001–005 + replay hash pass; economy hardening tests pass.
- [ ] Existing economy behaviour not regressed (167+ test suite green).

---

## Decision control table (for RUN_02.6 final report)

| Decision | Recommended default | User decision needed before implementation? | Blocks RUN_02.7? |
|----------|---------------------|-----------------------------------------------|------------------|
| DDR-LOG-001 Flags vs nodes | Road nodes only (flag = alias) | Yes | No |
| DDR-LOG-002 Building entrances | Configurable `ports[]` offsets | Yes | Yes (entrance stage) |
| DDR-LOG-003 Carrier assignment | Per delivery job | Yes | No |
| DDR-LOG-004 Worker movement | Abstract slot fill; defer pathing | Yes | No |
| DDR-LOG-005 Builder model | Abstract builder_work; no site builders v1 | Yes | No |
| DDR-LOG-006 Ware buffers | Carrier-only transit; doc fix | Yes | Yes if node staging chosen |
| DDR-LOG-007 Data-driven depth | Validation + config-only test | Yes | No |
| DDR-LOG-008 Minimum v1 model | As §Minimum v1 above | Yes | Yes (defines scope) |
| DDR-LOG-009 Deferred systems | Civic, viewer, hybrid, etc. | Yes | No |

---

## Related documents

- Run plan: [`RUN_02_6_ECONOMY_FOUNDATION_AUDIT_PLAN.md`](../runs/RUN_02_6_ECONOMY_FOUNDATION_AUDIT_PLAN.md)
- Gap register: [`RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md`](../../audit/RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md)
