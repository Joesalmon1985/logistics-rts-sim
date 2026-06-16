# RUN_02.7 — Economy Logistics Foundation Refactor and Viewer Observability

## Purpose

Implement the ratified economy logistics foundation (DDR-LOG-001–012) and make it **functionally visible** in the economy viewer.

This run brings the Trial Economy Game closer to the intended Widelands-style base:

- Road nodes / flags as visible logistics points
- Building entrances/ports
- Delivery jobs
- Carrier-held ware movement along node routes
- Construction supplied by delivered materials
- Abstract `builder_work` progress after delivery
- Worker/tool diagnostics
- Config-driven extension proof
- Viewer-visible logistics state

**Design authority:** [`docs/design/15_worker_carrier_road_node_logistics_foundation_RATIFIED.md`](../design/15_worker_carrier_road_node_logistics_foundation_RATIFIED.md)

**Scope distinction:** Viewer **polish** is out of scope. Viewer **observability** is in scope.

## Branch Name

`feature/run-02-7-economy-logistics-foundation`

Branch from `main` after confirming RUN_02.6 ratification commits (`03f3cb1`, `9c5cbaf`) are present.

## Prerequisite Documents

- [`docs/design/15_worker_carrier_road_node_logistics_foundation_RATIFIED.md`](../design/15_worker_carrier_road_node_logistics_foundation_RATIFIED.md)
- [`docs/audit/RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md`](../audit/RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md)
- [`docs/audit/RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md`](../audit/RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md)
- [`docs/audit/RUN_02_6_LESSONS_LEARNED.md`](../audit/RUN_02_6_LESSONS_LEARNED.md)
- [`docs/architecture/06_config_and_data_schema.md`](../architecture/06_config_and_data_schema.md)
- [`docs/architecture/02_command_event_snapshot_contract.md`](../architecture/02_command_event_snapshot_contract.md)
- [`docs/testing/01_rule_contract_test_matrix.md`](../testing/01_rule_contract_test_matrix.md) §RUN_02.7
- [`docs/testing/02_smoke_test_matrix.md`](../testing/02_smoke_test_matrix.md) — ECON-SMOKE-006
- [`docs/verification/local_verification.md`](../verification/local_verification.md)

Depends on RUN_02 economy baseline and RUN_01 kernel (DDR-026, DDR-029).

## Pre-flight (before any RUN_02.7 implementation)

Before implementing (see `.cursor/rules/10-run-verification-and-logging.mdc`):

1. Confirm branch, commit, and clean working tree.
2. Read the ratified DDR-LOG doc and RUN_02.6 audit artifacts.
3. Baseline verification (if practical):
   - `sim/tools/run_tests.gd` — full kernel/economy suite
   - `sim/tools/run_economy_smoke.gd` — ECON-SMOKE-001–005
   - `sim/tools/verify_failure_modes.gd`
4. Record exact commands and exit codes.
5. Check for stale Godot processes before and after (`pgrep -af Godot`; kill only confirmed stale project processes).
6. Export `GODOT_USER_DATA_DIR`; use `timeout` wrappers; `--import` after fresh checkout or new `class_name` types.
7. Do not count aborted, timed-out, errored, or not-run commands as passes. If verification fails, **stop and report** before editing gameplay systems.

## In-Scope Work

- Economy logistics simulation refactor/implementation (phases 1–6 below)
- Tests and smoke coverage (test-first per phase)
- Config validation
- Config-only brick-chain proof (smallest useful config-driven proof — not a full content pack)
- Snapshot/export changes needed to expose implemented mechanics
- Viewer changes required to make new mechanics **inspectable** (functional visibility, not polish)
- Deterministic visual scenario / contact sheet / screenshot evidence showing mechanics are visible
- ECON-SMOKE-006 deterministic logistics scenario
- Regression protection for existing economy/kernel smokes and replay checks

## Out-of-Scope Work

- UI redesign, aesthetic polish, full viewer re-skin
- Hybrid military wiring
- Civic systems
- Wages/labour-market simulation
- Housing/commuting/morale/strikes
- Disease/pandemic systems
- Power systems
- Ports/ships
- Node-buffer staging
- Specialist-worker pathing
- Neural/training changes beyond documenting or protecting snapshot compatibility

## Implementation Phases

### Phase 0 — Baseline and safety

**Goal:** Establish a clean baseline and protect existing behaviour.

- Branch/commit/status recording
- Baseline test/smoke commands
- Generated artefact handling
- Stale Godot process check
- Regression command list

**Acceptance:** Existing tests/smokes recorded before implementation.

### Phase 1 — Config validation and config-only economy extension proof (DDR-LOG-007)

**Goal:** Prove the economy can add new wares/buildings/recipes without GDScript special cases.

- Lightweight config validation in EconomyConfig/config-loading layer
- Validation for ware/building/recipe cross-refs where practical
- Config-only chain proof (preferably bricks: ware + input + recipe/building such as brick_kiln/brickworks)

**Brick-chain scope guard:** The brick-chain proof should be the **smallest useful config-driven proof**. Do not create a large new content pack (no overbuilding clay extraction, kilns, fuel chains, storage networks, or balanced brick economy). The goal is to prove generic wares/buildings/recipes work without GDScript special cases — not to fully balance a brick economy.

**Acceptance:** A test proves the new chain is config-driven and does not require hard-coded simulation additions beyond generic systems.

### Phase 2 — Building `ports[]` and entrance routing (DDR-LOG-002, DDR-LOG-011)

**Goal:** Implement configurable building entrances with anchor fallback.

- `ports[]` offsets in building config/schema handling
- Default `ports` to `[[0, 0]]` where omitted
- Valid building entrance/port node creation
- Route connection rules from road network to building ports
- Support for large/multi-hex buildings (e.g. HQ)

**Acceptance:** Simple buildings retain anchor fallback. Large buildings route via valid configured ports. Tests prove port-based entrance routing.

### Phase 3 — Delivery job lifecycle and route diagnostics

**Goal:** Tighten explicit delivery jobs and their diagnostics.

- Stable delivery job fields: source, destination, ware type, quantity, status, `block_reason`
- Clearer lifecycle transitions
- No-route/no-carrier behaviour
- Wire or remove stale/unused constants if appropriate
- Blocked jobs visible in diagnostics/export

**Acceptance:** Tests prove job creation, blocking, recovery, and `block_reason` accuracy.

### Phase 4 — Carrier assignment and carrier-held transit (DDR-LOG-003, DDR-LOG-006)

**Goal:** Per-job carriers; node-based routes; no teleport.

- Carriers assigned per delivery job
- Route is node-based
- Carrier holds ware while in transit
- Road-node buffers are **not** active ware staging in v1
- No direct building-to-building teleport
- Route/progress/status data is observable

**Acceptance:** Tests prove carriers gate delivery and move wares along node routes. Docs/comments/tests make carrier-held transit semantics clear.

### Phase 5 — Construction and `builder_work` workflow (DDR-LOG-005)

**Goal:** Materials via logistics; abstract builder progress after delivery.

- Construction requires materials delivered through logistics
- `builder_work` progresses only after material requirements are satisfied
- No explicit site-assigned builder entities
- Builder worker terminology and `builder_work` abstraction remain clear

**Acceptance:** Tests prove construction does not progress before delivery and does progress after delivery.

### Phase 6 — Logistics diagnostics

**Goal:** Fix and expose logistics diagnostics.

- Missing-worker diagnostics
- Missing-tool/tool-shortage diagnostics
- No-route diagnostics
- No-carrier diagnostics
- Optional human-readable stall reason strings if low risk
- Diagnostics appear in appropriate snapshots/export

**Acceptance:** Tests prove diagnostic counters/block reasons in relevant shortage/failure scenarios.

### Phase 7 — Snapshot/export contract (DDR-LOG-010)

**Goal:** Full/debug snapshots expose logistics state for visual review.

Full/debug snapshots should expose:

- `roads`, `road_nodes`
- Building ports/entrances
- Carriers; carrier route/progress/status
- Delivery jobs; job status/`block_reason`
- Construction material state
- `builder_work`/construction progress
- Diagnostics
- Blocked job markers if applicable

Training snapshots: do not bloat training input unnecessarily; document intentional omissions; preserve neural/training compatibility unless a change is explicitly required.

**Acceptance:** Snapshot tests assert relevant logistics fields are present and populated after a deterministic logistics scenario.

### Phase 8 — ECON-SMOKE-006 deterministic logistics scenario

**Goal:** End-to-end logistics smoke: HQ/storehouse → road node network → construction site.

Prove:

- Route exists through road nodes
- Delivery job is created
- Carrier is assigned per job
- Material moves along route
- Material arrives at construction site
- `builder_work` starts only after delivery
- Construction progresses/completes if intended
- Diagnostics remain correct
- Full/debug snapshot contains roads, nodes, ports, carriers, jobs, and construction state
- Replay/hash remains deterministic

**Acceptance:** ECON-SMOKE-006 passes and is included in local verification.

### Phase 9 — Viewer observability (functional visibility, not polish)

**Goal:** Ensure implemented RUN_02.7 mechanics can be seen and inspected in the economy viewer.

**Viewer anti-cheat rule:** The viewer must display authoritative snapshot/export state only. It must not infer, fabricate, or hard-code logistics state that is not exposed by the simulation/export layer. If the viewer cannot show something, the fix should usually be in snapshot/export first, then viewer rendering.

Implement viewer support as needed to show:

- Road nodes / flags
- Building ports/entrances
- Roads
- Carriers; carrier route/progress/status
- Delivery jobs
- Ware movement markers if available
- Blocked jobs
- No-route/no-carrier/missing-tool/missing-worker states
- Construction material state
- `builder_work`/construction progress

Leverage existing tooling where possible:

- `sim/tools/run_viewer_smoke.gd` (GOD-SMOKE-003 logistics visibility)
- `sim/tools/run_viewer_capture.gd` (deterministic screenshot path)
- `docs/verification/local_verification.md` Track 2 checklist

Add or update a deterministic viewer scenario/review command if needed.

**Acceptance:** Repeatable path to open or capture the economy viewer and see the new logistics foundation working. Screenshot/contact-sheet path documented if tooling exists. Viewer observability must be driven by snapshot/export state — do not add viewer-only fake logistics state to make screenshots look correct.

### Phase 10 — Regression and final verification

**Goal:** Protect existing behaviour and close the run.

Run:

- Full test suite
- Economy tests
- ECON-SMOKE-001–006
- Failure-mode verification
- Replay/hash checks
- Viewer smoke/visual review command if available
- `git diff --check`
- Stale Godot process check

**Acceptance:** All required verification passes or any failure is documented with exact cause and follow-up. No out-of-scope systems implemented. Final report produced.

## Test-First Requirements

| Area | Test / rule ID |
|------|----------------|
| Config validation | new validation tests |
| Config-only brick chain | ECON-CHAIN-brick |
| `ports[]` anchor fallback | ECON-NODE-001 (default `[[0,0]]`) |
| `ports[]` large buildings | ECON-NODE-001 (multi-port) |
| Delivery job lifecycle | ECON-JOB-001, ECON-JOB-002 |
| No-route block reason | ECON-JOB-* / diagnostics |
| No-carrier block reason | ECON-JOB-* / ECON-CARR-001 |
| Carrier-held transit | ECON-CARR-001 |
| Construction blocked before delivery | ECON-CONS-001 |
| `builder_work` after delivery | ECON-CONS-001 |
| Missing-tool diagnostic | ECON-TOOL-004 |
| Missing-worker diagnostic | existing or new |
| Snapshot logistics fields | ECON-SNAP-003 |
| End-to-end smoke | ECON-SMOKE-006 |
| Viewer observability | viewer smoke/review path (extend GOD-SMOKE-003 or new) |
| Regression | ECON-SMOKE-001–005, ECON-DET-002, kernel suite |

Test-first per phase; never weaken existing tests.

## Viewer Observability Acceptance

**RUN_02.7 is not complete unless the new logistics mechanics are inspectable.**

Minimum viewer observability:

- [ ] Road nodes/flags visible
- [ ] Building ports/entrances visible or inspectable
- [ ] Carriers visible
- [ ] Delivery jobs visible or inspectable
- [ ] Blocked jobs/diagnostics visible or inspectable
- [ ] Construction material state and `builder_work` progress visible or inspectable
- [ ] Deterministic viewer/screenshot/review path documented
- [ ] **Viewer anti-cheat:** all displayed logistics state comes from snapshot/export; no viewer-fabricated state

## Documentation Updates During Implementation

Update when behaviour changes:

- Ratified logistics doc (implementation clarifications only — DDR-LOG decisions unchanged)
- `docs/architecture/06_config_and_data_schema.md`
- `docs/architecture/02_command_event_snapshot_contract.md`
- `docs/testing/01_rule_contract_test_matrix.md`
- `docs/testing/02_smoke_test_matrix.md`
- This run doc
- `docs/audit/RUN_02_7_LESSONS_LEARNED.md` + final report at run end

## Acceptance Checklist

Simulation and export (from ratified doc):

- [ ] HQ/storehouse supplies construction site through road network (no teleport)
- [ ] Road nodes + segments real; building entrances via `ports[]` (DDR-LOG-002)
- [ ] Delivery jobs: source, destination, ware type, quantity, status, `block_reason`
- [ ] no-route / no-carrier / missing-worker / missing-tool diagnostics
- [ ] Carriers required for ware movement; carrier-held transit only (DDR-LOG-006)
- [ ] `builder_work` only after materials delivered (DDR-LOG-005)
- [ ] Config-only new ware/recipe/building chain test (DDR-LOG-007; smallest useful proof)
- [ ] Full/debug snapshots expose logistics per DDR-LOG-010
- [ ] ECON-SMOKE-001–006 + replay hash pass
- [ ] Full test suite green; existing scenarios compatible (DDR-LOG-011)
- [ ] Viewer observability acceptance met (see above)
- [ ] Final report with phase summary, verification exit codes, and viewer evidence

## Final Report Requirements

At run end, per `.cursor/rules/07-final-report-format.mdc`, include:

- Branch and commits
- Files changed
- Implementation summary by phase
- Tests/smokes run with exit codes
- Viewer observability evidence
- Screenshot/contact-sheet paths if generated
- Known issues
- Unimplemented/deferred items
- Out-of-scope confirmation
- Stale Godot process status
- Working tree status
