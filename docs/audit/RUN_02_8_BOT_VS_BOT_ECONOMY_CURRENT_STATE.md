# RUN_02.8 — Bot-vs-Bot Economy Current State Audit

Date: 2026-06-16

Audit type: documentation / planning only — no code changes in this pass.

## Executive summary

**The repository cannot yet support a full deterministic two-player economy-only bot-vs-bot expansion match end-to-end.**

RUN_02.7 (merged to `main` at `3236685`) delivered the logistics foundation needed as a prerequisite: building `ports[]`, delivery jobs with `block_reason`, carrier-held transit, construction via delivered materials then `builder_work`, diagnostics, snapshot/export logistics fields, and viewer observability for road nodes, ports, carriers, jobs, and blocked jobs.

What remains for RUN_02.8:

- Dual-player economy bootstrap (scenarios, init, per-player HQ meta)
- Player-scoped economy safety (isolated road/logistics networks, worker pools, pathfinding filter)
- Per-player economy scoring
- Rule-based expansion bots (not tutorial-hardcoded coords)
- Dual-bot turn loop (strict alternation)
- Viewer ownership overlay + scoreboard
- Headless smokes proving both bots expand and replay/hash stays deterministic

**Recommendation:** Proceed with RUN_02.8 implementation using ratified design decisions (isolated per-player logistics networks, symmetric mirrored map, simple scoring, rule-based bots). Defer neural bots.

---

## RUN_02.7 dependency checklist

| RUN_02.7 deliverable | Status on `main` (`3236685`) | Evidence |
|----------------------|------------------------------|----------|
| HQ/storehouse → road network → construction (no teleport) | Met | ECON-SMOKE-006, CONS-004 tests |
| Road nodes + segments; `ports[]` entrance routing | Met | ECON-NODE-001, `buildings.json` HQ ports |
| Delivery jobs with `block_reason` | Met | ECON-JOB-001/002 |
| Carrier-held transit; per-job carriers | Met | ECON-CARR-001, logistics system |
| `builder_work` after material delivery | Met | ECON-CONS-001, CONS-004 |
| Config-only brick chain proof | Met | `test_econ_chain_brick_config_only.gd` |
| Full/debug snapshot logistics fields | Met | ECON-SNAP-003 |
| ECON-SMOKE-001–006 + replay hash | Met | `run_economy_smoke.gd` |
| Viewer logistics observability | Met | GOD-SMOKE-003, `layer_renderer.gd` |
| Viewer anti-cheat (snapshot-driven only) | Met | RUN_02.7 viewer rule |

RUN_02.8 may begin from `main` with RUN_02.7 acceptance satisfied.

---

## Architecture today vs RUN_02.8 target

### Current: single-bot economy flow

```
HeuristicEconomyBot (p1 only)
    → observe(snapshot)
    → decide() → SimCommand
    → CommandValidator.validate_and_apply()
    → SimulationTickRunner.tick()
    → repeat until is_complete() or max_ticks
```

Files: [`godot_project/sim/bot/bot_runner.gd`](godot_project/sim/bot/bot_runner.gd), [`godot_project/sim/bot/heuristic_economy_bot.gd`](godot_project/sim/bot/heuristic_economy_bot.gd)

### Target: dual-bot economy flow (RUN_02.8)

```
DualBotRunner
  each tick:
    ExpansionBot_p1 → observe(snapshot) + legal_commands → decide → validate
    ExpansionBot_p2 → observe(snapshot) + legal_commands → decide → validate
    SimulationTickRunner.tick()
  until max_ticks or stall detection
```

Bots use snapshot + legal-command observation only (full-map omniscient for v1). All commands through `CommandValidator`. Isolated per-player logistics networks.

---

## 1. Multi-player support

### Can the economy simulation represent two economy players?

**Partial.**

- [`variant_stub_base.gd`](godot_project/sim/variants/variant_stub_base.gd) adds every `scenario.players` entry to `state.players`.
- [`economy_init.gd`](godot_project/sim/economy/economy_init.gd) only bootstraps `players[0]`: one HQ, one ownership radius, global `hq_position_q` / `hq_position_r` meta, one worker/carrier pool.

```11:14:godot_project/sim/economy/economy_init.gd
	var player_id: String = "p1"
	if not scenario.get("players", []).is_empty():
		player_id = str(scenario["players"][0])
```

Military init ([`military_init.gd`](godot_project/sim/military/military_init.gd)) already loops all players and uses `spawns[]` — pattern to mirror for economy.

### Can each player own HQ / storehouse / buildings / roads / carriers / workers?

| Entity | `player_id` on entity | Bootstrap for p2 | Command NOT_OWNER |
|--------|----------------------|------------------|-------------------|
| HQ / buildings | Yes | No | Yes |
| Roads / road nodes | Yes | No | Partial |
| Carriers | Yes | No | Via job owner |
| Delivery jobs | Yes | No | — |
| Workers | Yes | No | Partial |
| Construction sites | Yes | No | Yes |

### Are inventories, delivery jobs, and construction sites player-scoped?

- **Entity level:** inventories live in per-building stores tagged via building `player_id`; jobs and sites carry `player_id`.
- **Diagnostics / scoring:** global — `progress_score`, `ware_totals` aggregate all stores ([`economy_diagnostics_system.gd`](godot_project/sim/economy/economy_diagnostics_system.gd)).

### Road networks: per-player, shared, or ambiguous?

**Ambiguous today — must become isolated in RUN_02.8.**

- Road segments and nodes carry `player_id` ([`economy_road_system.gd`](godot_project/sim/economy/economy_road_system.gd)).
- Pathfinding ([`economy_pathfinding.gd`](godot_project/sim/economy/economy_pathfinding.gd)) routes across **all** connected nodes with no owner filter.
- `can_place_road` ([`economy_map_rules.gd`](godot_project/sim/economy/economy_map_rules.gd)) does not check hex/building ownership.
- Adjacent player networks could interconnect and route across owners.

**Ratified RUN_02.8 target:** isolated per-player road/logistics graphs; pathfinding filtered to acting player's network.

### Can two players build simultaneously without state collisions?

**No guarantee today.**

- Single global HQ drives `within_build_radius` ([`economy_map_rules.gd`](godot_project/sim/economy/economy_map_rules.gd) lines 72–76).
- Worker assignment ([`economy_worker_system.gd`](godot_project/sim/economy/economy_worker_system.gd) `_take_idle_worker`) does not filter by `player_id`.
- Footprint overlap is global; deposits/forests are map-global resources.
- No economy scenarios with `"players": ["p1", "p2"]` — all 12 files under `sim/data/scenarios/economy/` are p1-only.

### Design authority

- ECON-OWN-001/002 ([`docs/design/02_trial_economy_game_requirements.md`](docs/design/02_trial_economy_game_requirements.md)): ownership hooks required; multiplayer gameplay explicitly out of RUN_02 scope but hooks must remain.

---

## 2. Bot command support

### Is there a bot / AI command runner?

**Yes.**

| Component | Path | Role |
|-----------|------|------|
| `Bot` interface | `sim/bot/bot.gd` | `observe`, `decide`, `is_complete` |
| `BotRunner` | `sim/bot/bot_runner.gd` | Single-bot loop through validator |
| `HeuristicEconomyBot` | `sim/bot/heuristic_economy_bot.gd` | Scripted plan for `tiny_success_map` |
| `HeuristicEconomyBuilderBot` | `sim/economy/bot/heuristic_economy_builder_bot.gd` | ECON-SMOKE-005 builder |
| `HeuristicMilitaryBot` | `sim/bot/heuristic_military_bot.gd` | BOT-SMOKE-002 |
| `run_bot_smoke.gd` | `sim/tools/run_bot_smoke.gd` | BOT-SMOKE-001/002, REP-SMOKE-001 |
| Viewer bot step | `client/ui/viewer_root.gd` | One command per click |

### Can a bot submit the same commands as a human?

**Yes.** All paths use `CommandValidator.validate_and_apply` — no bypass. Verified by [`test_bot_cmd_001_bot_uses_command_api.gd`](godot_project/sim/tests/bot/test_bot_cmd_001_bot_uses_command_api.gd).

Command pipeline:

```
Client → CommandRegistry.create → CommandValidator.validate_and_apply → Events + Snapshot
```

Adapters: `client/adapters/command_submitter.gd`, `sim/tools/training_env_server.gd`.

### Legal-action enumeration API?

**Yes, partial for economy.**

- [`CommandAvailabilityProvider.enumerate_legal_commands`](godot_project/sim/core/command_availability_provider.gd) — main API; adds `NoopCommand`.
- [`EconomyCommandAvailability.list_candidates`](godot_project/sim/economy/economy_command_availability.gd) — worker policy, bounded roads, three building types (`storehouse`, `woodcutter`, `sawmill`).
- Does **not** enumerate: assign worker, upgrade, cancel, enable/disable, remove road, priority commands.
- Uses global `hq_position_q/r` meta — not per-actor HQ.

Economy commands registered ([`economy_command_registry.gd`](godot_project/sim/economy/economy_command_registry.gd)): `PlaceRoadCommand`, `RemoveRoadCommand`, `PlaceBuildingPlanCommand`, `CancelConstructionCommand`, `UpgradeBuildingCommand`, `EnableBuildingCommand`, `DisableBuildingCommand`, `SetConstructionPriorityCommand`, `SetProductionPriorityCommand`, `SetWarePriorityCommand`, `AssignWorkerCommand`, `SetWorkerPolicyCommand`.

Snapshots attach `available_commands` per `actor_id` in debug/training export.

### Can a bot place buildings, roads, nodes, priorities?

- **Scripted bot:** hard-coded coordinates for `tiny_success_map` only ([`heuristic_economy_bot.gd`](godot_project/sim/bot/heuristic_economy_bot.gd) `_build_plan()`).
- **Legal enumeration:** roads and limited buildings only; priorities not enumerated.
- **Expansion bot (RUN_02.8):** needs heuristic over legal commands + deposit-aware placement.

### Can a bot inspect enough snapshot/state?

**Yes for single-player tutorial.**

Debug snapshot ([`snapshot_exporter.gd`](godot_project/sim/core/snapshot_exporter.gd)) includes: `buildings`, `construction_sites`, `roads`, `road_nodes`, `delivery_jobs`, `carrier_markers`, `ware_movement_markers`, `blocked_job_markers`, `diagnostics`, `display_layers`, `entities`.

Missing for competitive play: per-player `economy_scores`, per-player ware totals.

### Invalid commands rejected cleanly?

**Yes.** Validator returns error codes; failure-mode integrity checks preserved.

### Bot observation boundary (RUN_02.8 requirement)

Bots must use snapshot + `enumerate_legal_commands` through a consistent observation object — not raw `GameState` bucket reads. Full-map omniscient is acceptable for v1 if delivered via export.

**Gap:** `HeuristicEconomyBuilderBot.tick_act` reads `GameState` directly — expansion bot should not follow that pattern.

---

## 3. Economy expansion model

### What does “biggest economy” mean today?

**Undefined for competitive play.**

Existing single-player metrics:

| Metric | Scope | Location |
|--------|-------|----------|
| `progress_score` | Global | `economy_production_system.gd`, diagnostics meta |
| `ware_totals` | Global (all stores) | `economy_diagnostics_system.gd` |
| Scenario victory | Single-player | `complete_building`, `ware_threshold`, `worker_policy` |
| Policy fitness | Single actor | `policy_episode_result.gd` |
| CSV export | Global | `economy_export.gd` |

### Can we measure (per player)?

| Metric | Today | Per-player in RUN_02.8 |
|--------|-------|------------------------|
| Total buildings | Yes (count from state) | Filter by `player_id` |
| Completed buildings | Yes | Filter by `player_id` |
| Production buildings | Yes | Filter + active window |
| Construction sites | Yes | Filter by `player_id` |
| Roads / road nodes | Yes | Filter by `player_id` |
| Active carriers | Yes | Filter by `player_id` |
| Ware throughput | Events / CSV | Per-player counters |
| Stock value | Yes (global totals) | Sum player stores |
| Completed deliveries | Events | Per-player counters |
| Economy score over time | No | `diagnostics.economy_scores` |

### Building / resource variety

**Sufficient for meaningful expansion display.**

20+ building types in [`buildings.json`](godot_project/sim/data/config/buildings.json): woodcutter → sawmill → workshop chain, mines, food chain, brick_kiln, upgrades, storage.

Forest regrowth via forester (`increases_tree_density` in production system). Mine deposits deplete finitely. Mirrored resource pockets on a larger map required for long dual expansion.

### Ratified RUN_02.8 scoring (v1)

```
economy_score =
  10 * completed_buildings
+  5  * active_production_buildings
+  1  * floor(total_stock_value / 10)
+  1  * completed_deliveries_count
-  2  * blocked_jobs_count
```

Definitions:

- `completed_buildings` = completed non-HQ, non-decorative economy buildings (production/logistics/support)
- `active_production_buildings` = completed production buildings that produced or attempted production within a recent tick window
- Roads/nodes required for minimum-match acceptance but **not directly scored**

---

## 4. Scenario support

| Question | Current state |
|----------|---------------|
| Symmetric two-player scenario? | **None.** All economy scenarios: `"players": ["p1"]`. |
| Per-player starter kit? | Config supports wares/workers/carriers/tools; init applies once. |
| Build space for both? | Maps 16×16 (`tiny_success_map`); one centre HQ — insufficient. Need 24×24 or 32×32 dedicated map. |
| Economy-only (no combat)? | **Yes** — `economy_trial` has no combat. |
| Long-run resources? | Forest regrowth yes; mines finite; mirrored pockets needed. |

Military reference: [`skirmish_1v1.json`](godot_project/sim/data/scenarios/military/skirmish_1v1.json) uses `spawns[]` with per-player hub positions.

Proposed economy scenario schema extension (RUN_02.8 Phase 1):

```json
{
  "players": ["p1", "p2"],
  "spawns": [
    {
      "player_id": "p1",
      "q": 6, "r": 6,
      "starting_wares": { ... },
      "starting_workers": { ... }
    },
    {
      "player_id": "p2",
      "q": 26, "r": 26,
      "starting_wares": { ... }
    }
  ],
  "build_radius": 12,
  "max_ticks": 3000,
  "stall_ticks_threshold": 200
}
```

---

## 5. Viewer support

### Can the viewer display both players?

**Technically yes; economy demos default to p1.**

- [`map_viewport.gd`](godot_project/client/ui/map_viewport.gd): `_player_color` — p1 blue, p2 red on building borders, sites, units.
- [`layer_renderer.gd`](godot_project/client/adapters/layer_renderer.gd): exports `player_id` on buildings, sites, carriers.
- [`viewer_controller.gd`](godot_project/client/core/viewer_controller.gd): `actor_id` defaults to `"p1"`.

### Ownership visible at a glance?

**Partial — gap for RUN_02.8.**

- Building/site borders coloured by `player_id`.
- Tile `owner_player_id` in snapshot — **no ownership overlay** in economy viewer.
- Roads, road nodes, carriers: colour partially from entity data; not sufficient without JSON for quick p1/p2 distinction across all entity types.

**RUN_02.8 requirement:** minimal ownership overlay so buildings, roads, nodes, carriers, and sites are identifiable without reading JSON.

### Logistics observability (RUN_02.7)

| Feature | Status |
|---------|--------|
| Road nodes / flags | Yes — cyan dots, logistics layer |
| Building ports | Yes — orange diamonds |
| Roads | Yes — yellow lines |
| Carriers | Yes — coloured dots, ware label, route interpolation |
| Delivery jobs | Yes — lines; red if blocked |
| Blocked jobs / diagnostics | Partial — blocked job styling; rollout summary counts; full JSON in visual review manifest |
| Construction / `builder_work` | Yes — site progress ring, delivered/required |

### Bot intent / recent command?

- ViewerRoot: single-step bot, one command per click.
- `BotRunner`: decisions array — not shown in economy viewer for dual bots.

### Economy scoreboard?

**No** — must add from `diagnostics.economy_scores` (RUN_02.8 Phase 7).

### Deterministic screenshots / contact sheets?

**Tooling exists; bot-vs-bot scenario missing.**

| Tool | Path | Notes |
|------|------|-------|
| Logistics visual review | `sim/tools/run_economy_logistics_visual_review.gd` | Contact sheet + manifest |
| Viewer capture | `sim/tools/run_viewer_capture.gd` | 1920×1080 PNG |
| Viewer pixel smoke | `sim/tools/run_viewer_pixel_smoke.gd` | Non-headless |
| Rollout capture | `sim/tools/run_rollout_review_capture.gd` | Multi-step modes |

RUN_02.8: `run_economy_bvb_visual_review.gd` — panels: start / early / mid / late / final + logistics overlay.

---

## 6. Testing / smoke / replay

| Capability | Status |
|------------|--------|
| Headless bot-vs-bot economy match | **No** |
| Replay/hash output | **Yes** single-bot — REP-SMOKE-001, ECON-SMOKE-004/006 |
| Snapshots at intervals | **Yes** — export tools, rollout observer |
| Viewer smoke loads bot-vs-bot | **No scenario** |
| Tests proving dual expansion + viewer | **None** |

### Existing smoke coverage

| Smoke | What it proves |
|-------|----------------|
| ECON-SMOKE-001–006 | Single-player economy + logistics + hash |
| BOT-SMOKE-001 | `HeuristicEconomyBot` completes tutorial |
| BOT-SMOKE-002 | Military p1 bot wins vs idle p2 |
| REP-SMOKE-001 | Economy single-bot replay round-trip |
| ECON-SMOKE-005 | Builder bot logistics chain |
| GOD-SMOKE-001–003 | Viewer render data, command round-trip, logistics visibility |

### Minimum successful match criteria (RUN_02.8 acceptance reference)

Under fixed seed, both players:

- Start with independent economy state
- Bots use command API only
- Each completes ≥5 new buildings
- Each creates ≥10 road segments or nodes (behavioural gate, not scored)
- Each completes ≥10 delivery jobs
- `economy_scores` increase over time
- Neither stalled >N ticks without recorded reason (N tunable, e.g. 200)
- Deterministic replay/hash
- Viewer/contact sheet shows both economies expanding with ownership distinguishable

### Tests that would prove RUN_02.8

| Rule ID | Purpose |
|---------|---------|
| ECON-MP-001 | Two-player init |
| ECON-MP-002 | NOT_OWNER + network isolation |
| ECON-BOT-002 | Dual-bot expansion metrics |
| ECON-BOT-003 | Dual-bot replay hash |
| ECON-BOT-004 | Bot observation boundary |
| BOT-SMOKE-004 | Headless bot-vs-bot smoke |
| ECON-SMOKE-007 | Economy bot-vs-bot end-to-end |
| GOD-SMOKE-005 (proposed) | Ownership overlay + scoreboard |

---

## Inventory of implementation areas

### Bot / AI

- `sim/bot/bot.gd`, `bot_runner.gd`, `heuristic_economy_bot.gd`, `heuristic_military_bot.gd`
- `sim/economy/bot/heuristic_economy_builder_bot.gd`
- `sim/tools/run_bot_smoke.gd`, `policy_eval_runner.gd`
- `sim/tools/training_env_server.gd` (single `actor_id` per episode)

### Command system

- `sim/core/command_validator.gd`, `command_registry.gd`, `command_availability_provider.gd`
- `sim/economy/economy_command_registry.gd`, `economy_command_availability.gd`
- Economy commands under `sim/economy/commands/`

### Economy simulation

- Init: `economy_init.gd`
- Logistics: `economy_logistics_system.gd`, `economy_road_system.gd`, `economy_pathfinding.gd`
- Workers: `economy_worker_system.gd`
- Production/diagnostics: `economy_production_system.gd`, `economy_diagnostics_system.gd`
- Map rules: `economy_map_rules.gd`

### Scenarios / maps

- `sim/data/scenarios/economy/*.json` (12 scenarios, all p1)
- `sim/data/maps/economy/*.json` (10 maps, 16×16 typical)

### Replay / hash

- `sim/core/replay_hash.gd`, `sim/tools/run_replay.gd`
- `test_econ_det_002_replay_hash_stable.gd`

### Snapshot / export

- `sim/core/snapshot_exporter.gd`
- `sim/tools/economy_export.gd`

### Viewer

- `client/adapters/layer_renderer.gd`, `client/ui/map_viewport.gd`
- `client/core/viewer_controller.gd`, `client/ui/viewer_root.gd`
- `client/demo/economy_logistics_review.gd`
- `client/tools/viewer_test_harness.gd`

### Smoke / capture runners

- `sim/tools/run_economy_smoke.gd`, `run_viewer_smoke.gd`, `run_viewer_capture.gd`
- `sim/tools/run_economy_logistics_visual_review.gd`

---

## Relationship to other runs

| Run | Relationship |
|-----|--------------|
| RUN_02.7 | Prerequisite — logistics foundation met |
| RUN_04 | Established bot interface, BOT-SMOKE-001, replay smokes |
| RUN_06 | Training env — single actor; not RUN_02.8 scope |
| RUN_07 | Hybrid wiring — blocked until economy dual-player proof |
| RUN_08 | Hybrid two-bot balance — broader than economy-only RUN_02.8 |
| RUN_12 | Multiplayer networking — not RUN_02.8 |

---

## Conclusion

RUN_02.7 provides the logistics and viewer observability foundation. RUN_02.8 must add dual-player bootstrap, isolated logistics networks, per-player scoring, expansion bots, dual-bot orchestration, and viewer ownership/scoreboard — documented in the gap register and implementation plan draft.
