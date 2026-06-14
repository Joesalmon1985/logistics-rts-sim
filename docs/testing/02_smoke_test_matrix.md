# Smoke Test Matrix

End-to-end smoke scenarios per run. Pass = all criteria met headlessly unless noted.

## Smoke reporting rules (all runs)

- Wrap every smoke command in a `timeout` (see `docs/verification/local_verification.md`); exit code `0` is required for a pass.
- Each smoke resolves to exactly one status: **passed**, **failed**, **errored**, **timed out**, or **not run**.
- A report **must not** collapse `errored`, `timed out`, or `not run` into `passed`. Exit code `124` is a timeout (failure); an aborted/killed task is `not run`, never a pass.
- Where practical, include the negative/failure-mode checks (`sim/tools/verify_failure_modes.gd`) that must exit non-zero.
- Report the exact command run and its exit code. See `.cursor/rules/10-run-verification-and-logging.mdc`.

## RUN_01 — Core Simulation Kernel

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| KERN-SMOKE-001 | Create game from scenario fixture | game_created event |
| KERN-SMOKE-002 | Submit noop command, tick 10 | tick_advanced × 10 |
| KERN-SMOKE-003 | Export snapshot | Valid JSON schema_version |
| KERN-SMOKE-004 | Replay hash | Hash matches expected |

Verification must use timeout-wrapped Godot commands; exit code `0` required. See `docs/verification/local_verification.md` and `sim/tools/verify_failure_modes.gd` for failure-mode integrity checks.

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
| MIL-SMOKE-001 | Build base | Hub + generator + processor + barracks placed |
| MIL-SMOKE-002 | Harvest/process | Credits increase from collector → processor loop |
| MIL-SMOKE-003 | Maintain power | Unpowered production blocked; power restored |
| MIL-SMOKE-004 | Unlock tech | Prerequisite research; advanced unit queued |
| MIL-SMOKE-005 | Scout enemy | Fog/shroud updates; enemy revealed in vision |
| MIL-SMOKE-006 | Attack enemy | Attack-move or patrol destroys hostile unit/building |
| MIL-SMOKE-007 | Repair | Damaged unit or building healed at repair depot |
| MIL-SMOKE-008 | Win/loss | Command hub destroyed → defeat; opponent hub destroyed → victory |
| MIL-SMOKE-009 | Replay/diagnostics | Replay hash match; diagnostics export readable |

Bundled skirmish fixtures: fixed `skirmish_1v1` map + seeded `generated_skirmish` map (see MIL-MAPGEN).

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
| HYB-SMOKE-001 | Logistics-fed construction | military_construction_completed via wares |
| HYB-SMOKE-002 | Logistics-fed production | unit_spawned after ware delivery |
| HYB-SMOKE-003 | Power/radar/tech | Integrated systems functional |
| HYB-SMOKE-004 | Delegated attack | Hostile hub destroyed via DelegatedAttackCommand |
| HYB-SMOKE-005 | Defence priorities | Patrol zone or guard policy engages hostiles |
| HYB-SMOKE-006 | Repair priority | Repair depot heals with ware-fed jobs |
| HYB-SMOKE-007 | Reject direct move | MoveUnitCommand rejected for human |
| HYB-SMOKE-008 | Win/loss/stall | Victory, defeat, or stall diagnostic detected |
| HYB-SMOKE-009 | Replay hash | Hash match |

## RUN_08 — Hybrid Bots / Balance

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| HYB-BOT-001 | Hybrid bot 200 ticks | No deadlock stall; delegated attack/defence/repair used |
| HYB-BOT-002 | Hybrid bot skirmish | Two bots complete with win/loss; fog-respecting |
| BAL-SMOKE-001 | Balance scenario | Both players viable path |

## RUN_09 — Godot Hybrid Observer

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| GOD-HYB-001 | Hybrid snapshot render | Economy + military + fog/shroud layers |
| GOD-HYB-002 | Delegated order UI | Attack, patrol, guard, repair priority commands accepted |
| GOD-HYB-003 | Replay/spectator | Snapshot sequence displays without local rule inference |

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
| NET-SMOKE-001 | Two clients same LAN | Same hash at tick 100; command sync identical |
| NET-SMOKE-002 | Desync injection detected | Pause triggered |
| NET-SMOKE-003 | Fog/intel sync | Per-client intel consistent with authoritative snapshot |
| PKG-SMOKE-001 | Export APK/IPA | Builds without error |
