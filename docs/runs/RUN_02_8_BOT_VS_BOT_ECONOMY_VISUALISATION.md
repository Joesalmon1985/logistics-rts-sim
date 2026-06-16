# RUN_02.8 — Bot-vs-Bot Economy Expansion Visualisation Trial

## Purpose

Prove that the Trial Economy Game can support a **deterministic two-player economy-only match** where both rule-based bots expand their economies, and the viewer lets a human follow the match.

This is **not** a neural training run. Use simple deterministic rule-based economy bots first.

**Design dependency:** RUN_02.7 economy logistics foundation (ports, delivery jobs, carrier transit, construction via delivery + `builder_work`, diagnostics, snapshot/export, viewer observability).

**Audit artifacts:**

- [`docs/audit/RUN_02_8_BOT_VS_BOT_ECONOMY_CURRENT_STATE.md`](../audit/RUN_02_8_BOT_VS_BOT_ECONOMY_CURRENT_STATE.md)
- [`docs/audit/RUN_02_8_BOT_VS_BOT_ECONOMY_GAP_REGISTER.md`](../audit/RUN_02_8_BOT_VS_BOT_ECONOMY_GAP_REGISTER.md)
- [`docs/audit/RUN_02_8_AUDIT_FINAL_REPORT.md`](../audit/RUN_02_8_AUDIT_FINAL_REPORT.md)

## Branch name

`feature/run-02-8-bot-vs-bot-economy`

Branch from `main` after confirming RUN_02.7 merge (`3236685` or later).

## Prerequisite documents

- [`docs/runs/RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md`](RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md)
- [`docs/design/15_worker_carrier_road_node_logistics_foundation_RATIFIED.md`](../design/15_worker_carrier_road_node_logistics_foundation_RATIFIED.md)
- [`docs/audit/RUN_02_7_FINAL_REPORT.md`](../audit/RUN_02_7_FINAL_REPORT.md)
- [`docs/audit/RUN_02_7_LESSONS_LEARNED.md`](../audit/RUN_02_7_LESSONS_LEARNED.md)
- [`docs/architecture/02_command_event_snapshot_contract.md`](../architecture/02_command_event_snapshot_contract.md)
- [`docs/architecture/06_config_and_data_schema.md`](../architecture/06_config_and_data_schema.md)
- [`docs/testing/01_rule_contract_test_matrix.md`](../testing/01_rule_contract_test_matrix.md) §RUN_02.8
- [`docs/testing/02_smoke_test_matrix.md`](../testing/02_smoke_test_matrix.md) — ECON-SMOKE-007, BOT-SMOKE-004
- [`docs/testing/03_godot_viewer_test_plan.md`](../testing/03_godot_viewer_test_plan.md) §RUN_02.8
- [`docs/verification/local_verification.md`](../verification/local_verification.md)

Depends on RUN_02 economy baseline, RUN_01 kernel, and RUN_02.7 logistics acceptance.

---

## Ratified design decisions

User-ratified (2026-06-16). **Do not reopen without explicit user approval.**

| Decision | Ratified answer |
|----------|-----------------|
| Bot type | Deterministic rule-based expansion bots; **not** neural |
| Match type | Two-player **economy-only** — no combat |
| Map layout | Symmetric mirrored spawns |
| Resources | Mirrored/partitioned resource pockets — **not** shared centre resources |
| Scoring | Simple v1 formula (below) |
| Stopping criteria | Fixed `max_ticks` **plus** stall detection |
| Road network rules | **Isolated per-player road/logistics graphs** — no shared roads/nodes in v1 |
| Turn model | Strict alternation: p1 command → p2 command → simulation tick |
| Bot visibility | Full-map omniscient via snapshot/legal-command observation |
| Bot commands | Normal command API only (`CommandValidator`) |
| Bot observation | Snapshot + legal commands only — no raw internal state reads |
| Viewer scoreboard | **Required** for acceptance |
| Viewer ownership | **Required** — minimal overlay; p1/p2 identifiable without JSON |
| Screenshots | Contact sheet: start / early / mid / late / final + logistics overlay |

### Road network isolation (primary design rule)

For RUN_02.8, **do not let players share roads/nodes**. Shared networks introduce ambiguity around ownership, carriers, delivery jobs, access rights, blocking, and future military/civic interactions.

**Player-scoping for v1:**

- Roads, road nodes, carriers, delivery jobs, construction sites, and worker pools are **player-scoped**
- Economy delivery pathfinding filters to the **acting player's logistics network**
- Cross-player road adjacency must not create routable paths between player networks

**Pathfinding compatibility:** Add player-scoped pathfinding without breaking existing single-player callers. Prefer an optional `player_id` parameter or wrapper that preserves existing single-player behaviour when omitted (infer from source/destination owner). New dual-player economy code passes `player_id` explicitly. Avoid a hard signature change to `find_route(state, from_node_id, to_node_id)` that breaks many existing tests.

### Scoring formula (v1)

Per-player `economy_score` at tick T:

```
economy_score =
  10 * completed_buildings
+  5  * active_production_buildings
+  1  * floor(total_stock_value / 10)
+  1  * completed_deliveries_count
-  2  * blocked_jobs_count
```

**Definitions:**

- `completed_buildings` = completed non-HQ, non-decorative economy buildings, including production/logistics/support buildings where relevant
- `active_production_buildings` = completed production buildings that have produced or attempted production within a recent tick window
- `blocked_jobs_count` penalises dysfunctional logistics sprawl
- Roads/nodes are required for **minimum-match acceptance** but are **not directly scored**, to avoid incentivising road spam

Export: `diagnostics.economy_scores: { "p1": N, "p2": M }` in snapshot/export.

### Minimum successful match criteria

Under fixed seed, a successful bot-vs-bot match requires:

- Both players start with valid independent economy state
- Both bots issue only valid command-API commands
- Both bots complete at least **5 new buildings** (each)
- Both bots create at least **10 road segments or road nodes** (each) — behavioural gate, not scored
- Both bots complete at least **10 delivery jobs** (each)
- Both `economy_scores` increase over time
- Neither bot stalled more than **N ticks** without recorded reason (default N=200, tunable)
- Final replay/hash deterministic (pinned smoke hash)
- Viewer/contact sheet shows both economies expanding; ownership distinguishable at a glance

### Bot anti-cheat rules

1. **Command API only** — `CommandValidator.validate_and_apply`; no bypass
2. **Observation boundary** — bots inspect snapshots and `enumerate_legal_commands` through a consistent observation object; no raw `GameState` bucket reads
3. Full-map omniscient observation is acceptable for v1 if delivered via export, not ad-hoc state access

---

## Pre-flight (before implementation)

Per `.cursor/rules/10-run-verification-and-logging.mdc`:

1. Checkout `feature/run-02-8-bot-vs-bot-economy`; confirm clean working tree
2. Baseline verification on `main` / branch point:
   - `sim/tools/run_tests.gd` — full suite
   - `sim/tools/run_economy_smoke.gd` — ECON-SMOKE-001–006
   - `sim/tools/run_viewer_smoke.gd` — GOD-SMOKE-001–003
   - `sim/tools/verify_failure_modes.gd`
3. Record exact commands and exit codes
4. Export `GODOT_USER_DATA_DIR`; use `timeout` wrappers
5. If baseline fails, **stop and report** before gameplay edits

---

## In-scope work

- Two-player economy bootstrap (`spawns[]`, dual HQ, starters, ownership)
- Player-scoped economy safety (isolated networks, workers, pathfinding, build radius)
- Per-player economy scoring in diagnostics/snapshot
- `EconomyExpansionBot` (rule-based, snapshot/legal observation)
- `DualBotRunner` (strict alternation)
- Symmetric two-player scenario + map (partitioned resources)
- BOT-SMOKE-004, ECON-SMOKE-007, dual-bot replay hash
- Viewer ownership overlay + scoreboard
- Bot-vs-bot visual review capture (contact sheet)
- Tests: ECON-MP-001/002, ECON-BOT-002/003/004, viewer ownership/scoreboard tests
- Regression: ECON-SMOKE-001–006, kernel suite, failure modes

## Out-of-scope work

- Neural bots / training loop changes (beyond snapshot compatibility)
- Shared road networks / inter-player trade
- Hybrid military wiring (RUN_07)
- Full legal enumeration of every economy command
- Viewer aesthetic polish / reskin
- Multiplayer networking (RUN_12)
- Hybrid two-bot balance (RUN_08)

---

## Implementation phases

Explicit split: **bootstrap first**, then **prove economies cannot leak**.

### Phase 0 — Audit and ratification (complete)

**Goal:** Document current state; ratify design; establish baseline at implementation start.

- [x] Audit current-state doc
- [x] Gap register
- [x] Authoritative run doc (this file)
- [x] Test/smoke matrix planning rows
- [ ] Baseline verification recorded at implementation branch start

**Acceptance:** Audit docs and ratification on `main`; baseline recorded before Phase 1 code.

### Phase 1 — Two-player scenario bootstrap

**Goal:** Two independent economies exist at tick 0.

- Extend economy scenario schema with `spawns[]` (mirror military pattern)
- Per spawn: `player_id`, `q`, `r`, optional `starting_wares`, workers, tools overrides
- `EconomyInit`: loop spawns — HQ, ownership radius, workers, carriers, starters per player
- Per-player HQ position meta (map or meta keyed by player)
- Symmetric map skeleton (minimal before full resource layout in Phase 5)

**Tests:** ECON-MP-001 — both players have HQ, stores, workers, carriers, ownership hexes

**Acceptance:** Scenario with `players: ["p1","p2"]` initialises; both HQs present; `state.players.size() == 2`.

### Phase 2 — Player-scoped economy safety

**Goal:** Economies cannot leak into each other.

- `within_build_radius` uses per-player HQ
- Worker assignment filtered by building owner / home store owner
- Pathfinding: optional `player_id` filter; SP compat when omitted
- Road placement: ownership rules; no bridging to opponent network nodes
- Isolated logistics graphs enforced

**Pathfinding compatibility:** Optional `player_id` on `find_route` or wrapper; infer owner from nodes when omitted; dual-player callers pass explicitly.

**Tests:** ECON-MP-002 — cross-player NOT_OWNER; no route p1→p2; no cross-player worker assignment

**Acceptance:** ECON-MP-002 green; existing single-player economy tests still green.

### Phase 3 — Per-player economy score

**Goal:** Competitive metric in diagnostics and snapshot.

- Implement ratified scoring formula per player
- Track: completed buildings, active production window, stock value, deliveries, blocked jobs
- Export `diagnostics.economy_scores`

**Tests:** ECON-SCORE-001 unit tests; snapshot asserts field present

**Acceptance:** Debug snapshot contains `economy_scores` with integer values for p1 and p2.

### Phase 4 — Economy expansion bot

**Goal:** Rule-based bot expands via legal commands, not hardcoded coords.

- `EconomyExpansionBot` extends `Bot`
- Observes snapshot + `enumerate_legal_commands` only (ECON-BOT-004)
- Heuristic: roads to deposits → extractors → processors → storage; deterministic tie-break
- Expand `EconomyCommandAvailability` as needed (per-actor HQ, more building types)

**Tests:** ECON-BOT-004 observation boundary; bot issues only registered command types

**Acceptance:** Bot places roads/buildings via validator without raw state reads.

### Phase 5 — Dual-bot runner + full scenario

**Goal:** Headless dual-bot match on symmetric map.

- `DualBotRunner`: each tick — p1 observe/decide/validate → p2 observe/decide/validate → tick
- Replay records both commands per tick (p1 before p2)
- Full scenario: `expansion_duel_2p.json` + map 24×24 or 32×32, mirrored spawns, partitioned forests/mines
- `max_ticks`, `stall_ticks_threshold` in scenario

**Tests:** Integration test for alternation order and command `player_id`s

**Acceptance:** Dual-bot run completes `max_ticks`; both players have expanded state.

### Phase 6 — Smokes, replay hash, minimum match

**Goal:** Automated proof of expansion and determinism.

- BOT-SMOKE-004 in `run_bot_smoke.gd`
- ECON-SMOKE-007 in `run_economy_smoke.gd`
- ECON-BOT-002: minimum successful match numeric gates
- ECON-BOT-003: dual-bot replay export → import → identical hash (pinned)
- Document replay command order in architecture doc

**Acceptance:** New smokes exit 0; minimum match criteria met; hash pinned.

### Phase 7 — Viewer observability

**Goal:** Human can follow the match.

**Minimum viewer requirements:**

1. **Ownership overlay** — buildings, roads, road nodes, carriers, construction sites distinguishable as p1 vs p2 without JSON
2. **Dual scoreboard** from `diagnostics.economy_scores`
3. Logistics layer toggles (RUN_02.7 baseline)
4. `run_economy_bvb_visual_review.gd` — contact sheet: start / early / mid / late / final + logistics overlay

**Viewer anti-cheat:** Scoreboard and overlay from snapshot/export only.

**Tests:** GOD-SMOKE-005 or VIS-BVB-* ownership/scoreboard render-data tests

**Acceptance:** Viewer observability checklist met; contact sheet generated deterministically.

### Phase 8 — Documentation and regression

**Goal:** Close run with full regression and doc updates.

- Update `06_config_and_data_schema.md` (economy `spawns[]`)
- Confirm test/smoke matrices reflect implemented tests
- Update `local_verification.md` with RUN_02.8 commands
- `docs/audit/RUN_02_8_LESSONS_LEARNED.md` + implementation final report

**Acceptance:** Full regression green; docs updated; final report per `07-final-report-format.mdc`.

---

## Test-first requirements

| Area | Rule ID | Test / smoke |
|------|---------|--------------|
| Two-player init | ECON-MP-001 | `test_econ_mp_001_two_player_init.gd` |
| Player isolation | ECON-MP-002 | `test_econ_mp_002_cross_player_isolation.gd` |
| Economy score | ECON-SCORE-001 | Score unit tests |
| Bot observation | ECON-BOT-004 | `test_econ_bot_004_observation_boundary.gd` |
| Dual-bot metrics | ECON-BOT-002 | Minimum match criteria test |
| Dual-bot hash | ECON-BOT-003 | `test_econ_bot_003_dual_replay_hash.gd` |
| Bot-vs-bot smoke | BOT-SMOKE-004 | `run_bot_smoke.gd` |
| Economy BvB E2E | ECON-SMOKE-007 | `run_economy_smoke.gd` |
| Viewer ownership | GOD-SMOKE-005 / VIS-BVB-* | Ownership overlay + scoreboard |
| Regression | — | ECON-SMOKE-001–006, kernel suite, INTEGRITY |

Test-first per phase; never weaken existing tests.

---

## Viewer observability acceptance

RUN_02.8 is not complete unless:

- [ ] Ownership overlay: p1/p2 distinguishable for buildings, roads, nodes, carriers, sites at a glance
- [ ] Dual scoreboard shows `economy_scores` for p1 and p2
- [ ] Road nodes, ports, carriers, jobs visible (RUN_02.7 baseline)
- [ ] Blocked jobs / diagnostics inspectable
- [ ] Deterministic contact sheet: start / early / mid / late / final + logistics overlay
- [ ] **Viewer anti-cheat:** all displayed state from snapshot/export

---

## Acceptance checklist (run complete)

Simulation:

- [ ] Two-player economy init via `spawns[]`
- [ ] Isolated per-player road/logistics networks
- [ ] Per-player build radius, workers, pathfinding
- [ ] Per-player `economy_scores` in diagnostics/snapshot
- [ ] `EconomyExpansionBot` via command API + observation boundary
- [ ] `DualBotRunner` strict alternation
- [ ] Minimum successful match criteria met under fixed seed
- [ ] Dual-bot replay hash deterministic and pinned
- [ ] ECON-SMOKE-001–007 + BOT-SMOKE-004 pass
- [ ] Full test suite green; ECON-SMOKE-001–006 regression
- [ ] Failure-mode integrity pass

Viewer:

- [ ] Ownership overlay + scoreboard
- [ ] Contact sheet / visual review bundle
- [ ] Viewer observability acceptance met

Documentation:

- [ ] Implementation final report with verification exit codes

---

## Verification commands

```bash
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user_data"
export GODOT="${GODOT:-godot}"

timeout 450s $GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 450s $GODOT --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
timeout 120s $GODOT --headless --path godot_project --script res://sim/tools/run_bot_smoke.gd
timeout 60s $GODOT --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd
timeout 120s $GODOT --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

Non-headless (viewer review):

```bash
$GODOT --path godot_project --script res://sim/tools/run_economy_bvb_visual_review.gd
```

See [`docs/verification/local_verification.md`](../verification/local_verification.md) §RUN_02.8.

---

## Final report requirements

Per `.cursor/rules/07-final-report-format.mdc`:

- Branch and commits
- Implementation summary by phase
- Tests/smokes with exit codes
- Viewer evidence (contact sheet paths)
- Minimum match metrics achieved
- Pinned dual-bot replay hash
- Known issues / deferred items
- Stale Godot process check
- Working tree status

---

## Relationship to other runs

| Run | Relationship |
|-----|--------------|
| RUN_02.7 | Prerequisite — logistics + viewer logistics layers |
| RUN_04 | Bot interface, single-bot smokes — extend for dual-bot |
| RUN_07 | Hybrid wiring — after RUN_02.8 economy dual-player proof |
| RUN_08 | Hybrid two-bot balance — broader than economy-only |
| RUN_12 | Network multiplayer — not RUN_02.8 |
