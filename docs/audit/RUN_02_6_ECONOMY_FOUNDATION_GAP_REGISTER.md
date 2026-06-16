# RUN_02.6 — Economy Foundation Gap Register

Structured gaps for the Trial Economy Game logistics foundation. Each entry cross-checks documentation, implementation, tests, and export/viewer layers per the Evidence standard.

**Audit commit:** `8b6c399` on `feature/run-02-6-economy-foundation-audit-plan`.

---

## Summary by severity

| Severity | Count | IDs |
|----------|-------|-----|
| Blocker (for hybrid) | 1 | GAP-12 |
| High | 4 | GAP-01, GAP-02, GAP-07, GAP-13 |
| Medium | 6 | GAP-03, GAP-04, GAP-05, GAP-08, GAP-09, GAP-14 |
| Low | 4 | GAP-06, GAP-10, GAP-11, GAP-15 |

---

## Gap entries

### GAP-01 — Road node buffers not used in live logistics

| Field | Detail |
|-------|--------|
| **Category** | Partial implementation; documentation mismatch |
| **Severity** | High |
| **Design decision required?** | Yes (ware movement fidelity) |
| **Documented intent** | `02_trial_economy_game_requirements.md` ECON-ROAD-004: road nodes act as transfer/buffer points (ware staging). Display layer §14: node buffer ware counts. |
| **Implementation** | Nodes have `inventory` with `road_node_buffer_capacity` (`economy_road_system.gd` `_ensure_node_at_hex`). `economy_logistics_system.gd` never reads/writes node inventory; wares on carrier entity during transit; endpoint store-to-store transfer. |
| **Tests** | `hardening/test_econ_road_004_node_buffers_wares` — manual `inventory_add` on node entity only. |
| **Export** | `road_nodes[].inventory` exported; often empty in normal play. |
| **Recommendation** | Decide v1 model: (a) document buffers as reserved schema, or (b) implement junction staging in RUN_02.7. |
| **Evidence** | Impl: `economy_logistics_system.gd` (no node inventory); `economy_road_system.gd` (creates inventory). Test: `test_econ_road_004`. Doc: §25 ECON-ROAD-004. |

### GAP-02 — No configurable building ports / multi-entrance model

| Field | Detail |
|-------|--------|
| **Category** | Partial implementation; design decision required |
| **Severity** | High |
| **Design decision required?** | Yes |
| **Documented intent** | ECON-NODE-001; `06_config_and_data_schema.md` `ports: [road_node_south]`; §21 building ports. |
| **Implementation** | Single `road_node_id` at building anchor. `buildings.json` has no `ports`. HQ footprint 3×3 uses one node at anchor hex. |
| **Tests** | No `test_ECON_NODE_001` (searched `glob **/test*NODE_001*` — not found). |
| **Export** | No `ports[]` on buildings in snapshot. |
| **Recommendation** | RUN_02.7: add `ports` offsets in config + placement rules; test adjacency for large footprints. |
| **Searches for absence** | `grep ports buildings.json` — no `ports` key; `glob test*NODE_001*` — empty. |

### GAP-03 — Builder workers ≠ construction labour

| Field | Detail |
|-------|--------|
| **Category** | Partial; unclear vs catalogue |
| **Severity** | Medium–High |
| **Design decision required?** | Yes |
| **Documented intent** | `builder_work` ware for construction labour (§27). Catalogue lists builder worker type with tools. |
| **Implementation** | Construction uses abstract `builder_work_remaining` ticks after materials (`economy_construction_system.gd`). `builder` workers in starting pool; `brick_kiln` uses `worker_type: builder` for **production**, not site assignment. |
| **Tests** | `hardening/test_econ_cons_001` — builder_work ticks; no test for builder worker at construction site. |
| **Export** | `construction_sites.builder_work_remaining`; worker_markers show builders at HQ when idle. |
| **Recommendation** | Clarify in glossary/docs: builder **workers** vs builder **work** ware; keep abstract work for v1 unless user wants explicit site builders. |

### GAP-04 — AssignWorkerCommand / worker_request_pending unused

| Field | Detail |
|-------|--------|
| **Category** | Unclear implementation |
| **Severity** | Medium |
| **Design decision required?** | No (cleanup or wire in RUN_02.7) |
| **Documented intent** | Worker assignment commands in command registry. |
| **Implementation** | `assign_worker_command.gd` sets `worker_request_pending = true`. `economy_worker_system.gd` `_fulfill_worker_requests` auto-fills all slots; never reads flag. |
| **Tests** | No direct test for command-driven assignment vs auto-fill. |
| **Recommendation** | Remove dead field or implement command-triggered assignment in RUN_02.7. |

### GAP-05 — Tool shortage diagnostics counter always zero

| Field | Detail |
|-------|--------|
| **Category** | Partial implementation; missing tests |
| **Severity** | Medium |
| **Design decision required?** | No |
| **Documented intent** | ECON-TOOL-004; stall reasons include tool shortage. |
| **Implementation** | `tool_missing` **events** emitted. `_shortage_diagnostics` sets `tools: 0` always — only counts `paused_reason` food_missing / missing_worker. |
| **Tests** | `test_econ_tool_004` checks production diagnostics workers=0 and tool_missing event, **not** `shortages.tools`. |
| **Export** | `diagnostics.shortages.tools` always 0 in normal tool-shortage scenarios. |
| **Recommendation** | RUN_02.7: count tool_missing buildings or track tool stall reason; extend TOOL-004. |

### GAP-06 — BLD_PAUSED_NO_ROUTE never assigned

| Field | Detail |
|-------|--------|
| **Category** | Partial (dead constant) |
| **Severity** | Low |
| **Design decision required?** | No |
| **Implementation** | Constant in `economy_constants.gd`; grep `BLD_PAUSED_NO_ROUTE` in `sim/economy/` — only definition, no assignment. Production uses input_missing / food_missing; routing blocks appear on **jobs**, not buildings. |
| **Recommendation** | Wire building pause on unreachable inputs OR remove constant and document job-level routing blocks. |

### GAP-07 — No test proving config-only new ware/recipe chain

| Field | Detail |
|-------|--------|
| **Category** | Missing tests; modularity gap |
| **Severity** | High |
| **Design decision required?** | No (test in RUN_02.7) |
| **Implementation** | `bricks`, `brick_kiln`, `bricks_from_stone` in config without code changes to ware enums. |
| **Tests** | `grep bricks sim/tests/economy` — **no matches**. |
| **Recommendation** | RUN_02.7 acceptance: add ware via config only + one integration test (e.g. stone → bricks). |

### GAP-08 — Snapshot tests do not assert logistics arrays

| Field | Detail |
|-------|--------|
| **Category** | Missing tests; viewer-readiness |
| **Severity** | Medium |
| **Implementation** | Full snapshot exports logistics fields (verified `snapshot_exporter.gd`). |
| **Tests** | `test_econ_snap_002` — display_layers only. No assertion on `roads`, `road_nodes`, `carrier_markers`. |
| **Recommendation** | RUN_02.7: ECON-SNAP logistics assertion test or extend snap_002. |

### GAP-09 — Training snapshot omits key logistics layers

| Field | Detail |
|-------|--------|
| **Category** | Snapshot/export gap |
| **Severity** | Medium |
| **Design decision required?** | Yes (if neural observer needs node-level logistics) |
| **Implementation** | `export_training_snapshot` omits `road_nodes`, `worker_markers`, `ware_movement_markers`. |
| **Export** | Training/BC path uses compact snapshot; rollout observer uses full/debug snapshot. |
| **Recommendation** | Defer unless RUN_06/RUN_02.5 training views require node markers; document split intentional for size. |

### GAP-10 — RUN_02 acceptance checklist unchecked in run doc

| Field | Detail |
|-------|--------|
| **Category** | Documentation mismatch |
| **Severity** | Low |
| **Evidence** | `docs/runs/RUN_02_TRIAL_ECONOMY_GAME.md` all `[ ]`; `RUN_02_LESSONS_LEARNED.md` claims family-level completion. |
| **Recommendation** | Update RUN_02 doc checkboxes or add audit note cross-linking RUN_02.6 baseline — not blocking RUN_02.7. |

### GAP-11 — Catalogue IDs vs config IDs

| Field | Detail |
|-------|--------|
| **Category** | Documentation mismatch |
| **Severity** | Low |
| **Examples** | Catalogue `rations_kitchen` vs config `canteen` (meals_from_rations). |
| **Recommendation** | Align catalogue to config IDs or add alias table in glossary. |

### GAP-12 — Hybrid economy logistics dependency not fully satisfied

| Field | Detail |
|-------|--------|
| **Category** | Design decision required; blocker for RUN_07 |
| **Severity** | Blocker (for hybrid) |
| **Documented intent** | `04_hybrid_game_requirements.md` HYB-NOSIDE-002, HYB-LOGCON-003: military buildings use economy construction; carriers deliver to road nodes. |
| **Implementation** | Economy foundation exists but GAP-02 (entrances), GAP-01 (buffer semantics), GAP-03 (builder model clarity) weaken confidence for military site routing at scale. |
| **Recommendation** | Complete RUN_02.7 foundation before RUN_07 hybrid logistics integration. |

### GAP-13 — Carrier assignment is first-idle, no contention model

| Field | Detail |
|-------|--------|
| **Category** | Partial / simplified |
| **Severity** | High (for dense logistics) |
| **Implementation** | `_take_idle_carrier` returns first idle carrier; no load balancing, preemption, or segment capacity limits (congestion affects path **cost** only). |
| **Tests** | No multi-carrier contention test. |
| **Recommendation** | Accept for v1 or add contention tests + assignment policy in RUN_02.7 stage 4. |

### GAP-14 — Human-readable stall strings not exported

| Field | Detail |
|-------|--------|
| **Category** | Viewer-readiness; snapshot gap |
| **Severity** | Medium |
| **Evidence** | `RUN_02_5_VISUAL_LEGIBILITY_REVIEW.md`; `diagnostics.stall.primary_reason` is code string only in meta. |
| **Recommendation** | RUN_02.7 diagnostics stage: export `stall_reason_text` for viewer; optional. |

### GAP-15 — ECON-MAP-002 no dedicated test

| Field | Detail |
|-------|--------|
| **Category** | Missing tests |
| **Severity** | Low |
| **Documented intent** | Out-of-bounds queries fail gracefully. |
| **Implementation** | `economy_map_rules.gd`, `in_bounds` checks in placement/path code. |
| **Tests** | Matrix lists `test_ECON_MAP_002_out_of_bounds_rejected` — **file not found**. |
| **Searches** | `glob **/test*MAP_002*` — empty. |

---

## Appendix — Category index

| Category | Gap IDs |
|----------|---------|
| Missing implementation | — (none pure missing; mostly partial) |
| Partial implementation | GAP-01, GAP-02, GAP-03, GAP-05, GAP-06, GAP-13 |
| Unclear implementation | GAP-03, GAP-04 |
| Documentation mismatch | GAP-01, GAP-10, GAP-11 |
| Missing documentation | — |
| Missing tests | GAP-07, GAP-08, GAP-15; partial GAP-02, GAP-04 |
| Snapshot/export gap | GAP-09, GAP-14 |
| Viewer-readiness gap | GAP-08, GAP-14 |
| Modularity/config gap | GAP-07 |
| Design decision required | GAP-01, GAP-02, GAP-03, GAP-09, GAP-12 |

---

## Related documents

- Current state: [`RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md`](RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md)
- Decision draft: [`15_worker_carrier_road_node_logistics_foundation_RATIFIED.md`](../design/15_worker_carrier_road_node_logistics_foundation_RATIFIED.md)
