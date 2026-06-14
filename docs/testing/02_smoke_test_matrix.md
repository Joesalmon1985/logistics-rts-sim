# Smoke Test Matrix

End-to-end smoke scenarios per run. Pass = all criteria met headlessly unless noted.

## RUN_01 — Core Simulation Kernel

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| KERN-SMOKE-001 | Create game from scenario fixture | game_created event |
| KERN-SMOKE-002 | Submit noop command, tick 10 | tick_advanced × 10 |
| KERN-SMOKE-003 | Export snapshot | Valid JSON schema_version |
| KERN-SMOKE-004 | Replay hash | Hash matches expected |

## RUN_02 — Trial Economy Game

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| ECON-SMOKE-001 | Road network (terrain/slope) | Carriers path; road diagnostics OK |
| ECON-SMOKE-002 | Timber → planks | Planks in storage |
| ECON-SMOKE-003 | Full building | construction_completed event |
| ECON-SMOKE-004 | 500-tick replay | Hash match |
| ECON-SMOKE-005 | Heuristic bot → Workshop | Full logistics chain via bot; see `02_` §38 |

Bundled scenario maps (each must pass as scenario tests): `tiny_success_map`, `blocked_route_map`, `missing_resource_map`, `priority_test_map`, `storage_full_map`, `pathfinding_test_map`, `forest_depletion_regrowth_map`, `mine_food_support_map`, `tool_shortage_worker_block_map`, `building_upgrade_map`.

## RUN_03 — Trial Military Game

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| MIL-SMOKE-001 | Base setup | Hub + generator + barracks placed |
| MIL-SMOKE-002 | Produce infantry | unit_spawned |
| MIL-SMOKE-003 | Combat | Target building destroyed |
| MIL-SMOKE-004 | Defeat | player_defeated on hub loss |
| MIL-SMOKE-005 | Replay hash | Hash match |

## RUN_04 — Trial Bots / Replay / Telemetry

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| BOT-SMOKE-001 | Economy bot completes tutorial | economy_success |
| BOT-SMOKE-002 | Military bot wins skirmish | opponent defeated |
| REP-SMOKE-001 | Export/import replay | Identical hash |

## RUN_05 — Godot Trial Viewers

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| GOD-SMOKE-001 | Load economy snapshot | Layers render |
| GOD-SMOKE-002 | Submit place road via Godot | Snapshot updates |

## RUN_06 — Neural Training

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| TRN-SMOKE-001 | 10 deterministic rollouts | Identical command logs |
| TRN-SMOKE-002 | Replay after rollout | Hash match |

## RUN_07 — Headless Hybrid

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| HYB-SMOKE-001 | Military building via wares | military_construction_completed |
| HYB-SMOKE-002 | Ware-fed unit production | unit_spawned |
| HYB-SMOKE-003 | Delegated attack | Hostile hub destroyed |
| HYB-SMOKE-004 | Reject direct move | Command rejected |
| HYB-SMOKE-005 | Replay hash | Hash match |

## RUN_08 — Hybrid Bots / Balance

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| HYB-BOT-001 | Hybrid bot 200 ticks | No deadlock stall |
| BAL-SMOKE-001 | Balance scenario | Both players viable path |

## RUN_09 — Godot Hybrid Observer

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| GOD-HYB-001 | Hybrid snapshot render | Economy + military layers |
| GOD-HYB-002 | Delegated order UI | Command accepted |

## RUN_10 — Human Playable Hybrid

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| HUM-SMOKE-001 | Tutorial completable | Manual test checklist |
| HUM-SMOKE-002 | Touch targets | Minimum size met |

## RUN_11 — Expanded Content

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| EXP-SMOKE-001 | Load campaign mission 2 | Scenario starts |
| EXP-SMOKE-002 | New map | Hash stable |

## RUN_12 — Wi-Fi Multiplayer

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| NET-SMOKE-001 | Two clients same LAN | Same hash at tick 100 |
| NET-SMOKE-002 | Desync injection detected | Pause triggered |
| PKG-SMOKE-001 | Export APK/IPA | Builds without error |
