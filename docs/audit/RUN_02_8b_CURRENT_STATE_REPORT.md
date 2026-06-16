# RUN 02.8b — Current State Report (Recovery Baseline)

Date: 2026-06-16  
Branch: `feature/run-02-8-bot-vs-bot-economy` @ `dcd3a66`  
Recovery start: modular phase ladder (no wholesale stash pop).

---

## 1. Branch / commit / patch path

| Item | Value |
|------|-------|
| Branch | `feature/run-02-8-bot-vs-bot-economy` |
| Baseline commit | `dcd3a66` — feat: RUN_02.8 two-player bot-vs-bot economy and viewer closure |
| Working tree at Phase A | Clean (no uncommitted prod changes) |
| Stash | `stash@{0}` — "RUN_02.8b stuck WIP before modular recovery" |
| Preserve patch (archived) | [`RUN_02_8b_current_uncommitted_patch.diff`](RUN_02_8b_current_uncommitted_patch.diff) |
| Live preserve (read-only) | `/tmp/run_02_8b_preserve/current_uncommitted_patch.diff` |

---

## 2. Changed files by area (from preserved stash)

From [`RUN_02_8b_preserved_diff_stat.txt`](RUN_02_8b_preserved_diff_stat.txt):

| Area | Files |
|------|-------|
| Bot runtime | `dual_bot_runner.gd`, `economy_bot_observation.gd`, `economy_expansion_bot.gd` (+311/-136 in bot) |
| Scenario / setup | `expansion_duel_2p.json`, `economy_bvb_setup.gd` |
| Tests | `test_econ_bot_002_dual_bot_expansion_metrics.gd` |
| Tooling | `run_economy_bvb_quick_metrics.gd` |
| Untracked in stash | `run_economy_bvb_site_debug.gd` (diagnostic only) |

Stash is **not applied** — reference only for targeted phase ports.

---

## 3. Test/smoke status on clean `dcd3a66`

Environment:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
```

| Command | Status | Exit |
|---------|--------|------|
| `timeout 580s ... run_tests.gd` | **181/182 passed** — `test_ECON_BOT_002` failed (expected baseline gap) | 1 |
| `timeout 120s ... verify_failure_modes.gd` | INTEGRITY PASS | 0 |
| `timeout 130s ... run_viewer_smoke.gd -- --smoke=005` | GOD-SMOKE-005 PASS | 0 |
| `timeout 200s ... run_bot_smoke.gd -- --smoke=004` | BOT-SMOKE-004 FAIL — p1 bot-built buildings below minimum | 1 |
| `timeout 200s ... run_economy_smoke.gd -- --smoke=007` | ECON-SMOKE-007 FAIL — p1 bot-built buildings below minimum | 1 |
| `TEST_FILTER=econ_bot_002 ... run_tests.gd` | FAIL — buildings/deliveries/scores/stalls for both players | 1 |

**ECON-BOT-002 failure detail:** per-player delta-from-baseline gates not met at proof profile `max_ticks=800`: buildings ≥5, roads ≥10, deliveries ≥10.

---

## 4. Metrics at 100 / 250 / 500 ticks

Tool: `run_economy_bvb_quick_metrics.gd -- --max-ticks=N` (proof profile, dual-bot).

| Ticks | wall_ms | ms/tick | p1 b/r/d | p2 b/r/d | p1 sites | p1 active bldg | command_hist |
|-------|---------|---------|----------|----------|----------|----------------|--------------|
| 100 | 13966 | 139.64 | 0/39/0 | 0/39/0 | 1 | 0 | PlaceRoad×44, PlaceBuildingPlan×2 |
| 250 | 31308 | 125.22 | 0/39/0 | 0/39/0 | 1 | 0 | PlaceRoad×44, PlaceBuildingPlan×2 |
| 500 | 60190 | 120.37 | 0/39/0 | 0/39/0 | 1 | 0 | PlaceRoad×44, PlaceBuildingPlan×2 |

Notes:

- Delta metrics (`b/r/d`) via `DualBotRunner.count_player_*_built` — all zero for buildings and deliveries.
- Road count plateaus at 39 per player; no further road commands after early ticks.
- One construction site per side but no completions or deliveries in 500 ticks.

---

## 5. Behaviour answers

| Question | Answer |
|----------|--------|
| **Buildings complete?** | **No** — 0 bot-built active non-HQ buildings at 500 ticks; 1 construction site stuck |
| **Deliveries complete?** | **No** — 0 delta deliveries for both players at all horizons |
| **Road spam?** | **Partial** — 39 roads/player then plateau (network_size brake at 40); not runaway but roads do not connect/deliver |
| **p1/p2 symmetric?** | **Yes at baseline** — identical metrics at 100/250/500 (same b/r/d and command counts) |
| **Regressions this session** | None introduced — baseline only. Known gaps: bot places sites but does not complete chain; proof gates fail; `SMOKE_007_EXPECTED_HASH` empty; stash chain rewrite untested |

---

## Next phases

Modular recovery ladder B→K: one test + minimal fix per phase; checkpoint commit each. Do not `git stash pop`.
