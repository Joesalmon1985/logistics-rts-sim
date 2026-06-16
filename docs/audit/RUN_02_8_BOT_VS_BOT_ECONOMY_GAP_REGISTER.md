# RUN_02.8 — Bot-vs-Bot Economy Gap Register

Date: 2026-06-16

Companion: [`RUN_02_8_BOT_VS_BOT_ECONOMY_CURRENT_STATE.md`](RUN_02_8_BOT_VS_BOT_ECONOMY_CURRENT_STATE.md)

Severity: **Critical** (blocks acceptance) | **High** (blocks smoke) | **Medium** (quality/risk) | **Low** (deferrable)

---

## Ratified design decisions (resolved gaps)

These were open design questions; user ratified on 2026-06-16. No longer block implementation.

| Former gap | Ratified decision |
|------------|-------------------|
| Road network sharing | **Isolated per-player graphs** — no shared roads/nodes in v1 |
| Resource layout | **Mirrored/partitioned pockets** — not shared centre resources |
| Turn order | **Strict alternation:** p1 command → p2 command → tick |
| Bot visibility | **Full-map omniscient** via snapshot/legal commands |
| Match end | **Fixed `max_ticks` + stall detection** |
| Scoring | Simple v1 formula; roads not scored; blocked-jobs penalty |
| Bot type | **Rule-based expansion bots** — not neural |
| Viewer | Ownership overlay + scoreboard **required** |

---

## Minimum successful match criteria (acceptance reference)

A RUN_02.8 bot-vs-bot match is successful when, under fixed seed:

- Both players start with valid independent economy state
- Both bots issue only valid command-API commands
- Both bots complete at least **5 new buildings** (each)
- Both bots create at least **10 road segments or road nodes** (each) — behavioural gate, not scored
- Both bots complete at least **10 delivery jobs** (each)
- Both `economy_scores` increase over time
- Neither bot stalled more than **N ticks** without recorded reason (N tunable, default 200)
- Final replay/hash deterministic (pinned smoke hash)
- Viewer/contact sheet shows both economies expanding; ownership distinguishable at a glance

---

## Gap register

| ID | Category | Severity | Gap | Evidence | Proposed fix | Phase |
|----|----------|----------|-----|----------|--------------|-------|
| GAP-02.8-001 | missing implementation | Critical | Economy init bootstraps only `players[0]` | `economy_init.gd` lines 11–14 | Loop `spawns[]`; per-player HQ, ownership, starters | 1 |
| GAP-02.8-002 | scenario gap | Critical | No `spawns[]` schema for economy scenarios | All 12 economy scenarios p1-only | Extend schema per `06_config_and_data_schema.md`; document in run doc | 1 |
| GAP-02.8-003 | scenario gap | Critical | No two-player economy scenario or map | Grep `players` in `scenarios/economy/` | `expansion_duel_2p.json` + 24×24 or 32×32 map | 1/5 |
| GAP-02.8-004 | missing implementation | Critical | Global `hq_position_q/r` meta — single HQ | `economy_init.gd`, `economy_map_rules.gd` | Per-player HQ map: `hq_positions` or lookup from buildings | 2 |
| GAP-02.8-005 | missing implementation | Critical | `within_build_radius` ignores per-player HQ | `economy_map_rules.gd` 72–76 | Resolve HQ by `player_id` | 2 |
| GAP-02.8-006 | missing implementation | Critical | Worker pool not player-scoped | `economy_worker_system.gd` `_take_idle_worker` | Filter workers by building owner / home store owner | 2 |
| GAP-02.8-007 | missing implementation | Critical | Pathfinding not filtered by player | `economy_pathfinding.gd` `find_route` | Optional `player_id` param; infer when omitted for SP compat | 2 |
| GAP-02.8-008 | missing implementation | High | Road placement does not enforce ownership / isolation | `economy_map_rules.can_place_road` | Reject bridging to opponent nodes; ownership checks | 2 |
| GAP-02.8-009 | partial implementation | High | Adjacent player road networks can interconnect | Pathfinder uses all nodes | Isolated graphs + no cross-player routing | 2 |
| GAP-02.8-010 | missing implementation | Critical | No dual-bot turn loop | `bot_runner.gd` single bot | `DualBotRunner`: p1 → p2 → tick | 5 |
| GAP-02.8-011 | missing implementation | Critical | No expansion bot (non-hardcoded coords) | `heuristic_economy_bot.gd` fixed plan | `EconomyExpansionBot` via legal commands + heuristics | 4 |
| GAP-02.8-012 | missing implementation | High | No per-player economy score | Global `progress_score` | `diagnostics.economy_scores` per ratified formula | 3 |
| GAP-02.8-013 | missing implementation | High | No per-player delivery/building counters for score | Global diagnostics | Per-player meta or diagnostics slice | 3 |
| GAP-02.8-014 | partial implementation | Medium | Legal enumeration uses global HQ | `economy_command_availability.gd` | Per-actor HQ from snapshot/state | 4 |
| GAP-02.8-015 | partial implementation | Medium | Legal enumeration incomplete vs registered commands | Only 3 building types enumerated | Expand for expansion bot needs (roads, key buildings) | 4 |
| GAP-02.8-016 | command legality gap | Medium | Priority/cancel not in enumeration | `economy_command_registry.gd` vs availability | Add if expansion bot needs them | 4 |
| GAP-02.8-017 | missing implementation | High | `HeuristicEconomyBuilderBot` reads raw `GameState` | `heuristic_economy_builder_bot.gd` | ECON-BOT-004: expansion bot snapshot-only | 4 |
| GAP-02.8-018 | missing tests | Critical | ECON-MP-001 two-player init | No test file | `test_econ_mp_001_two_player_init.gd` | 1 |
| GAP-02.8-019 | missing tests | Critical | ECON-MP-002 cross-player isolation | No test file | NOT_OWNER + no cross-route between players | 2 |
| GAP-02.8-020 | missing tests | Critical | ECON-BOT-002 dual-bot expansion metrics | None | Assert minimum match criteria | 6 |
| GAP-02.8-021 | missing tests | Critical | ECON-BOT-003 dual-bot replay hash | REP-SMOKE-001 is single-bot only | Pinned hash dual-bot scenario | 6 |
| GAP-02.8-022 | missing tests | High | ECON-BOT-004 bot observation boundary | None | Bot does not read raw state buckets | 4 |
| GAP-02.8-023 | missing tests | High | BOT-SMOKE-004 bot-vs-bot economy | `run_bot_smoke.gd` has 001–003 only | Add smoke runner case | 6 |
| GAP-02.8-024 | missing tests | High | ECON-SMOKE-007 economy bot-vs-bot E2E | `run_economy_smoke.gd` 001–006 | New smoke with pinned hash | 6 |
| GAP-02.8-025 | viewer observability gap | Critical | No ownership overlay (roads/nodes/carriers) | `map_viewport.gd` building borders only | Minimal p1/p2 tint overlay layer | 7 |
| GAP-02.8-026 | viewer observability gap | Critical | No economy scoreboard | `viewer_root.gd` / controller | Panel from `diagnostics.economy_scores` | 7 |
| GAP-02.8-027 | viewer observability gap | High | No dual-bot visual review bundle | `run_economy_logistics_visual_review.gd` SP only | `run_economy_bvb_visual_review.gd` | 7 |
| GAP-02.8-028 | viewer observability gap | Medium | No dual-bot last-action display | ViewerRoot single bot step | Optional event/decision panel | 7 |
| GAP-02.8-029 | replay/determinism gap | High | No dual-command replay ordering spec | Single-bot recorder | Document p1-before-p2 per tick in replay | 5/6 |
| GAP-02.8-030 | documentation gap | Medium | No RUN_02.8 run doc on main | — | `RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION_PLAN_DRAFT.md` → final at run end | 8 |
| GAP-02.8-031 | documentation gap | Medium | Test/smoke matrices lack RUN_02.8 rows | `01_rule_contract_test_matrix.md`, `02_smoke_test_matrix.md` | Add ECON-MP-*, ECON-BOT-*, BOT-SMOKE-004, ECON-SMOKE-007 | 8 |
| GAP-02.8-032 | documentation gap | Low | `local_verification.md` lacks RUN_02.8 commands | — | Add Track entries after smokes exist | 8 |
| GAP-02.8-033 | partial implementation | Low | Tile `owner_player_id` not shown in economy viewer | Snapshot has field; no overlay | Optional hex ownership tint in overlay | 7 |

---

## Gaps by category

### Missing implementation (16)

GAP-02.8-001, 004–013, 010, 011, 012, 013

### Partial implementation (5)

GAP-02.8-009, 014, 015, 033; entity `player_id` + NOT_OWNER (existing — bootstrap/scenarios incomplete)

### Unclear design (0 open)

All resolved — see ratified decisions.

### Missing tests (6)

GAP-02.8-018–022, 023, 024

### Viewer observability gap (4)

GAP-02.8-025–028

### Scenario gap (2)

GAP-02.8-002, 003

### Command legality gap (1)

GAP-02.8-016

### Replay/determinism gap (1)

GAP-02.8-029

### Documentation gap (3)

GAP-02.8-030–032

---

## Risk register

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Cross-player pathfinding leak | High (today) | Critical | Phase 2 isolation tests before bots |
| Cross-player worker assignment | High (today) | High | Phase 2 worker filter + ECON-MP-002 |
| `find_route` signature break | Medium | High | Optional `player_id`; infer for SP callers |
| Deposit exhaustion stalls match | Medium | Medium | Mirrored pockets; monitor in ECON-BOT-002 |
| Bot road spam without score incentive | Low | Low | Roads not scored; gate via min-match criteria |
| Dual-bot replay nondeterminism | Medium | Critical | Strict command order; pinned hash smoke |
| Map too small for dual expansion | High (16×16) | High | Dedicated 24×24+ symmetric map |
| Expansion bot too weak to meet min-match | Medium | High | Tune heuristics; lower thresholds only if needed |
| Viewer ownership overlay scope creep | Low | Low | Minimal functional overlay only — not polish |

---

## RUN_02.7 dependency status

| RUN_02.7 item | Status for RUN_02.8 |
|---------------|---------------------|
| Delivery jobs + block_reason | Closed — use in scoring/diagnostics |
| Carrier-held transit | Closed — per-player carriers in Phase 2 |
| Building ports | Closed |
| Snapshot logistics arrays | Closed — extend with `economy_scores` |
| Viewer logistics layers | Closed — extend with ownership overlay |
| ECON-SMOKE-006 | Closed — regression must stay green |
| Training snapshot `road_nodes` deferral | Open — document if RUN_06 needs nodes; not RUN_02.8 blocker |
| Node-buffer staging deferral | Open — not RUN_02.8 scope |

---

## Phase mapping summary

| Phase | Primary gaps closed |
|-------|---------------------|
| 1 | GAP-001, 002, 003, 018 |
| 2 | GAP-004–009, 019 |
| 3 | GAP-012, 013 |
| 4 | GAP-011, 014–017, 022 |
| 5 | GAP-010, 029 |
| 6 | GAP-020–024 |
| 7 | GAP-025–028, 033 |
| 8 | GAP-030–032 |

---

## Out of scope (not gaps for RUN_02.8)

- Neural bots / training loop changes
- Shared road networks / inter-player trade
- Hybrid military wiring (RUN_07)
- Full enumeration of every economy command
- Viewer aesthetic polish / reskin
- Multiplayer networking (RUN_12)
- Hybrid two-bot balance (RUN_08)
