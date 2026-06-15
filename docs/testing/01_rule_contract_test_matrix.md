# Rule Contract Test Matrix

Maps mechanic families and rule IDs to required test types. RUN_02+ implements tests listed here.

## Economy (ECON-*)

### Map, terrain, buildability

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-MAP-001 | unit | test_ECON_MAP_001_axial_neighbors_correct |
| ECON-MAP-002 | unit | test_ECON_MAP_002_out_of_bounds_rejected |
| ECON-TERR-001 | unit | test_ECON_TERR_001_water_not_buildable |
| ECON-SLOPE-001 | unit | test_ECON_SLOPE_001_slope_blocks_steep_road |
| ECON-BUILD-001 | unit | test_ECON_BUILD_001_footprint_overlap_rejected |

### Roads, nodes, pathfinding

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-ROAD-001 | unit | test_ECON_ROAD_001_roads_connect_adjacent_hexes |
| ECON-ROAD-002 | unit | test_ECON_ROAD_002_terrain_blocks_road_on_water |
| ECON-ROAD-003 | unit | test_ECON_ROAD_003_slope_affects_road_legality |
| ECON-ROAD-004 | integration | test_ECON_ROAD_004_node_buffers_wares |
| ECON-ROAD-005 | integration | test_ECON_ROAD_005_carrier_moves_along_road |
| ECON-ROAD-006 | integration | test_ECON_ROAD_006_congestion_in_diagnostics |
| ECON-ROAD-007 | integration | test_ECON_ROAD_007_blocked_route_in_diagnostics |
| ECON-NODE-001 | integration | test_ECON_NODE_001_node_adjacent_to_building_port |
| ECON-PATH-001 | unit | test_ECON_PATH_001_shortest_path_on_network |
| ECON-PATH-002 | unit | test_ECON_PATH_002_slope_affects_path_cost |
| ECON-PATH-003 | integration | test_ECON_PATH_003_blocked_path_returns_failure_reason |

### Workers, tools, provisioning

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-WORKER-001 | unit | test_ECON_WORKER_001_building_requests_worker_type |
| ECON-WORKER-002 | integration | test_ECON_WORKER_002_worker_supplied_from_storehouse |
| ECON-WORKER-003 | integration | test_ECON_WORKER_003_missing_worker_pauses_production |
| ECON-WORKER-004 | integration | test_ECON_WORKER_004_tool_required_before_assign |
| ECON-WORKER-005 | integration | test_ECON_WORKER_005_shortage_emits_event |
| ECON-TOOL-001 | unit | test_ECON_TOOL_001_specialist_requires_tools |
| ECON-TOOL-002 | integration | test_ECON_TOOL_002_missing_tool_blocks_assignment |
| ECON-TOOL-003 | unit | test_ECON_TOOL_003_requirements_configurable_by_type |
| ECON-TOOL-004 | integration | test_ECON_TOOL_004_shortage_in_diagnostics |

### Forest management

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-FOREST-001 | unit | test_ECON_FOREST_001_tree_density_on_hex |
| ECON-FOREST-002 | integration | test_ECON_FOREST_002_woodcutter_reduces_density |
| ECON-FOREST-003 | integration | test_ECON_FOREST_003_forester_increases_density |
| ECON-FOREST-004 | integration | test_ECON_FOREST_004_low_density_blocks_timber |
| ECON-FOREST-005 | scenario | test_ECON_FOREST_005_depletion_regrowth_events |

### Food and mines

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-FOOD-001 | unit | test_ECON_FOOD_001_food_consumed_as_support |
| ECON-FOOD-002 | integration | test_ECON_FOOD_002_mine_requires_food |
| ECON-FOOD-003 | integration | test_ECON_FOOD_003_deep_mine_requires_meals |
| ECON-FOOD-004 | integration | test_ECON_FOOD_004_missing_food_pauses_mine |
| ECON-FOOD-005 | integration | test_ECON_FOOD_005_shortage_in_diagnostics |
| ECON-MINE-001 | unit | test_ECON_MINE_001_requires_matching_deposit |
| ECON-MINE-002 | integration | test_ECON_MINE_002_consumes_food_while_producing |
| ECON-MINE-003 | integration | test_ECON_MINE_003_deposit_depletes_over_time |
| ECON-MINE-004 | integration | test_ECON_MINE_004_basic_and_deep_tier |
| ECON-MINE-005 | integration | test_ECON_MINE_005_deep_requires_meals |
| ECON-MINE-006 | integration | test_ECON_MINE_006_depletion_in_diagnostics |

### Upgrades

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-UPGRADE-001 | unit | test_ECON_UPGRADE_001_building_has_upgrade_target |
| ECON-UPGRADE-002 | integration | test_ECON_UPGRADE_002_upgrade_requires_materials |
| ECON-UPGRADE-003 | integration | test_ECON_UPGRADE_003_uses_logistics_construction |
| ECON-UPGRADE-004 | integration | test_ECON_UPGRADE_004_upgraded_changes_capabilities |
| ECON-UPGRADE-005 | integration | test_ECON_UPGRADE_005_upgrade_emits_events |

### Logistics, wares, chains

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-WARE-001 | unit | test_ECON_WARE_001_ware_types_enumerated |
| ECON-CARR-001 | integration | test_ECON_CARR_001_carrier_delivers_one_ware |
| ECON-CARR-002 | integration | test_ECON_CARR_002_carrier_moves_per_tick_speed |
| ECON-JOB-001 | integration | test_ECON_JOB_001_job_created_on_missing_input |
| ECON-JOB-003 | integration | test_ECON_JOB_003_job_lifecycle_transitions |
| ECON-STOR-001 | unit | test_ECON_STOR_001_storage_full_blocks_input |
| ECON-CONS-001 | integration | test_ECON_CONS_001_construction_consumes_wares |
| ECON-CONS-003 | integration | test_ECON_CONS_003_completion_spawns_building |
| ECON-CHAIN-001 | scenario | test_ECON_CHAIN_001_forest_to_timber |
| ECON-CHAIN-010 | scenario | test_ECON_CHAIN_010_workshop_produces_tools |
| ECON-CHAIN-011 | scenario | test_ECON_CHAIN_011_full_building_completion |
| ECON-PRI-001 | unit | test_ECON_PRI_001_construction_priority_order |
| ECON-PRI-002 | unit | test_ECON_PRI_002_production_priority_order |
| ECON-PRI-003 | integration | test_ECON_PRI_003_ware_routing_priority_when_scarce |

### Diagnostics, snapshot, determinism

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-DIAG-001 | integration | test_ECON_DIAG_001_per_building_status |
| ECON-DIAG-004 | integration | test_ECON_DIAG_004_csv_exports_generated |
| ECON-SNAP-002 | integration | test_ECON_SNAP_002_all_display_layers_present |
| ECON-DET-002 | scenario | test_ECON_DET_002_replay_hash_stable |
| ECON-STALL-001 | integration | test_ECON_STALL_001_stall_emits_diagnostic |

### Commands and events

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-CMD-ROAD-001 | unit | test_ECON_CMD_ROAD_001_place_road_command |
| ECON-CMD-BLD-003 | unit | test_ECON_CMD_BLD_003_upgrade_building_command |
| ECON-EVT-007 | integration | test_ECON_EVT_007_ware_delivered_event_emitted |
| ECON-EVT-018 | integration | test_ECON_EVT_018_tree_density_changed_event |

### Scenarios (bundled maps)

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-SCEN-tiny_success | scenario | test_scenario_tiny_success_map |
| ECON-SCEN-blocked_route | scenario | test_scenario_blocked_route_map |
| ECON-SCEN-missing_resource | scenario | test_scenario_missing_resource_map |
| ECON-SCEN-priority_test | scenario | test_scenario_priority_test_map |
| ECON-SCEN-storage_full | scenario | test_scenario_storage_full_map |
| ECON-SCEN-pathfinding | scenario | test_scenario_pathfinding_test_map |
| ECON-SCEN-forest | scenario | test_scenario_forest_depletion_regrowth_map |
| ECON-SCEN-mine_food | scenario | test_scenario_mine_food_support_map |
| ECON-SCEN-tool_shortage | scenario | test_scenario_tool_shortage_worker_block_map |
| ECON-SCEN-upgrade | scenario | test_scenario_building_upgrade_map |

### Smoke and bot

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-SMOKE-005 | smoke | test_ECON_SMOKE_005_bot_builds_workshop_via_logistics |
| ECON-BOT-001 | smoke | test_ECON_BOT_001_heuristic_uses_command_api |

### RUN_02 implemented coverage (actual)

This subsection records the **actual** automated coverage delivered by RUN_02, distinct from
the aspirational representative rows above. The rows above are not removed (traceability).

Headless suite: `sim/tools/run_tests.gd` — **48 tests** (13 RUN_01 + 35 RUN_02), exit `0`.
Economy smokes: `sim/tools/run_economy_smoke.gd` — ECON-SMOKE-001..005, exit `0`.

Dedicated economy test files (one representative behaviour test per §19–§31 family, plus map/road/path/ware/chain/diag/snap/det/cmd):

| Test file | Primary rule ID(s) | Verifies |
|-----------|--------------------|----------|
| test_econ_map_001_axial_neighbors_reuse | ECON-MAP-001 | shared hex neighbours reused |
| test_econ_terr_001_water_not_buildable | ECON-TERR-001 | water not buildable |
| test_econ_slope_001_slope_blocks_steep_road | ECON-SLOPE-001 | slope blocks steep road |
| test_econ_build_001_footprint_overlap_rejected | ECON-BUILD-001 | footprint overlap rejected |
| test_econ_road_001_roads_connect_adjacent_hexes | ECON-ROAD-001 | road connects adjacent hexes |
| test_econ_road_002_terrain_blocks_road_on_water | ECON-ROAD-002 | terrain blocks road |
| test_econ_road_007_blocked_route_in_diagnostics | ECON-ROAD-007 | blocked route in diagnostics |
| test_econ_path_001_shortest_path_on_network | ECON-PATH-001 | shortest path on road graph |
| test_econ_ware_001_ware_types_enumerated | ECON-WARE-001 | ware types enumerated |
| test_econ_job_003_job_lifecycle_transitions | ECON-JOB-003 | job created→delivered lifecycle |
| test_econ_stor_001_storage_full_blocks_input | ECON-STOR-001 | storage-full blocks input |
| test_econ_worker_003_missing_worker_pauses_production | ECON-WORKER-003 | missing worker pauses production |
| test_econ_tool_002_missing_tool_blocks_assignment | ECON-TOOL-002 | missing tool blocks assignment |
| test_econ_forest_002_woodcutter_reduces_density | ECON-FOREST-002 | woodcutter reduces tree density + event |
| test_econ_food_002_mine_requires_food | ECON-FOOD-002 | mine requires food support |
| test_econ_mine_001_requires_matching_deposit | ECON-MINE-001 | mine requires matching deposit |
| test_econ_upgrade_002_upgrade_requires_materials | ECON-UPGRADE-002 | upgrade requires materials |
| test_econ_cons_003_completion_spawns_building | ECON-CONS-003 | construction completion spawns building |
| test_econ_pri_001_construction_priority_order | ECON-PRI-001 | construction priority order |
| test_econ_chain_001_forest_to_timber | ECON-CHAIN-001 | forest → timber chain |
| test_econ_chain_010_workshop_produces_tools | ECON-CHAIN-010 | workshop produces tools |
| test_econ_diag_001_per_building_status | ECON-DIAG-001 | per-building diagnostics |
| test_econ_snap_002_all_display_layers_present | ECON-SNAP-002 | all snapshot display layers present |
| test_econ_det_002_replay_hash_stable | ECON-DET-002 | replay hash stable |
| test_econ_cmd_road_001_place_road_command | ECON-CMD-ROAD-001 | place-road command pipeline |

Dedicated per-scenario behaviour tests (one per §37 bundled map, each asserts scenario-specific
state/diagnostics **and** per-scenario replay-hash determinism):

| Test file | Rule ID |
|-----------|---------|
| test_scenario_tiny_success_map | ECON-SCEN-tiny_success |
| test_scenario_blocked_route_map | ECON-SCEN-blocked_route |
| test_scenario_missing_resource_map | ECON-SCEN-missing_resource |
| test_scenario_priority_test_map | ECON-SCEN-priority_test |
| test_scenario_storage_full_map | ECON-SCEN-storage_full |
| test_scenario_pathfinding_test_map | ECON-SCEN-pathfinding |
| test_scenario_forest_depletion_regrowth_map | ECON-SCEN-forest |
| test_scenario_mine_food_support_map | ECON-SCEN-mine_food |
| test_scenario_tool_shortage_worker_block_map | ECON-SCEN-tool_shortage |
| test_scenario_building_upgrade_map | ECON-SCEN-upgrade |

**Known coverage gap (honest):** §35/§460 of `docs/design/02_trial_economy_game_requirements.md`
calls for every *individual* rule ID in §19–§31 to additionally gain a test. RUN_02 delivers at
least one dedicated **behaviour** test per family (state change + event/diagnostic + downstream
effect) plus the smokes and scenario suite, but does not yet have a separate test file for every
single rule ID enumerated above (e.g. ECON-WORKER-001/002/004/005, ECON-TOOL-001/003/004,
ECON-FOREST-001/003/004/005, ECON-FOOD-001/003/004/005, ECON-MINE-002..006, ECON-UPGRADE-001/003/004/005,
ECON-ROAD-003..006, ECON-PATH-002/003, ECON-PRI-002/003, ECON-CARR-*, ECON-CONS-001, ECON-DIAG-004,
ECON-STALL-001, ECON-EVT-*). Those mechanics are implemented and exercised by family/scenario/smoke
tests; **per-rule-ID test expansion is explicit RUN_04 scope** (see `docs/runs/RUN_04_TRIAL_BOTS_REPLAYS_TELEMETRY.md` In-Scope and Acceptance Checklist).

### RUN_04 per-rule-ID economy test hardening (scheduled)

RUN_04 must add dedicated headless tests for each rule ID listed in the Known coverage gap above.
Each test must assert state change + event/diagnostic + downstream effect for its rule ID.
Cross-reference: `docs/audit/RUN_02_LESSONS_LEARNED.md` ("Known remaining caveat") and
`docs/runs/RUN_04_TRIAL_BOTS_REPLAYS_TELEMETRY.md` acceptance checkbox for hardening completion.

## Military (MIL-*)

### Economy, harvesting, credits

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-ECON-001 | unit | test_MIL_ECON_001_resource_field_on_map |
| MIL-ECON-003 | integration | test_MIL_ECON_003_collector_harvests_node |
| MIL-ECON-005 | integration | test_MIL_ECON_005_processor_converts_to_credits |
| MIL-CRED-001 | unit | test_MIL_CRED_001_credits_consumed_on_production |

### Construction, buildability, prerequisites

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-BUILD-003 | integration | test_MIL_BUILD_003_construction_queue_completes |
| MIL-BUILD-004 | unit | test_MIL_BUILD_004_missing_prerequisite_rejected |
| MIL-BUILD-005 | unit | test_MIL_BUILD_005_outside_build_radius_rejected |
| MIL-HUB-001 | unit | test_MIL_HUB_001_hub_destruction_defeats_player |

### Power

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-POWER-003 | unit | test_MIL_POWER_003_unpowered_barracks_cannot_produce |
| MIL-POWER-005 | integration | test_MIL_POWER_005_low_power_in_diagnostics |

### Production queues

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-QUEUE-001 | integration | test_MIL_QUEUE_001_unit_queue_processes_in_order |
| MIL-QUEUE-004 | integration | test_MIL_QUEUE_004_building_construction_queue_separate |
| MIL-QUEUE-005 | unit | test_MIL_QUEUE_005_unpowered_queue_blocked |

### Tech / prerequisites

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-TECH-004 | unit | test_MIL_TECH_004_prerequisite_blocks_unit_queue |
| MIL-TECH-002 | integration | test_MIL_TECH_002_research_unlocks_building |

### Fog, shroud, vision

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-FOG-001 | integration | test_MIL_FOG_001_unexplored_hidden |
| MIL-FOG-002 | integration | test_MIL_FOG_002_shroud_hides_enemy_outside_vision |
| MIL-VISION-002 | integration | test_MIL_VISION_002_radar_extends_sight |
| MIL-VISION-003 | unit | test_MIL_VISION_003_scout_bonus_sight |

### Movement and pathfinding

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-MOVE-002 | unit | test_MIL_MOVE_002_move_command_updates_position |
| MIL-MOVE-003 | unit | test_MIL_MOVE_003_terrain_modifier_affects_cost |
| MIL-MOVE-005 | integration | test_MIL_MOVE_005_collector_paths_to_processor |

### Trial unit orders

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-ORDER-003 | integration | test_MIL_ORDER_003_attack_move_engages_en_route |
| MIL-ORDER-004 | integration | test_MIL_ORDER_004_patrol_loops_waypoints |
| MIL-ORDER-005 | integration | test_MIL_ORDER_005_guard_stays_near_target |

### Combat and damage

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-COMBAT-002 | unit | test_MIL_COMBAT_002_projectile_or_instant_hit_config |
| MIL-COMBAT-003 | unit | test_MIL_COMBAT_003_cooldown_blocks_refire |
| MIL-COMBAT-007 | integration | test_MIL_COMBAT_007_turret_auto_targets |
| MIL-DAMAGE-002 | unit | test_MIL_DAMAGE_002_armour_modifier_applied |

### Repair

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-REPAIR-002 | integration | test_MIL_REPAIR_002_depot_heals_for_credits |
| MIL-REPAIR-004 | unit | test_MIL_REPAIR_004_unpowered_repair_stalled |

### Optional capture / veterancy

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-CAPTURE-001 | integration | test_MIL_CAPTURE_001_engineer_captures_structure |
| MIL-VET-001 | integration | test_MIL_VET_001_veterancy_bonus_after_combat |

### Map generation / skirmish

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-MAPGEN-002 | scenario | test_MIL_MAPGEN_002_seeded_map_deterministic |
| MIL-MAPGEN-003 | smoke | test_MIL_MAPGEN_003_generated_skirmish_playable |

### Replay / diagnostics

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-DET-002 | scenario | test_MIL_DET_002_replay_hash_stable |
| MIL-DIAG-001 | integration | test_MIL_DIAG_001_stalled_harvest_flagged |
| MIL-REPLAY-002 | scenario | test_MIL_REPLAY_002_skirmish_replay_hash |
| MIL-SNAP-002 | integration | test_MIL_SNAP_002_fog_layers_in_snapshot |

### Smoke and bot

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-SMOKE-008 | smoke | test_MIL_SMOKE_008_win_loss_triggers |
| MIL-BOT-003 | smoke | test_MIL_BOT_003_two_bot_skirmish_completes |

### RUN_03 implemented coverage (DoD level)

The table above lists the aspirational per-rule-ID target set. RUN_03 implements **every
military mechanic family** to Definition-of-Done level with behaviour-proving headless tests
(state change + event/diagnostic + downstream effect), under `sim/tests/military/` and registered
in `sim/tools/run_tests.gd`. Family → covering test(s):

| Family | Covering RUN_03 test(s) |
|--------|--------------------------|
| Scaffold / shared map / no parallel grid | `test_mil_scaffold_001`, `test_mil_scaffold_002`, `test_mil_map_001` |
| Invalid command no mutation | `test_mil_cmd_001` |
| Resource harvest → processor → credits | `test_mil_res_001` |
| Placement / build radius / footprint / buildability | `test_mil_build_001` |
| Construction queue completion | `test_mil_cons_001` |
| Power supply/demand gating of production | `test_mil_power_001` |
| Unit production queue + rally | `test_mil_prod_001` |
| Tech prerequisite unlock | `test_mil_tech_001` |
| Pathfinding + movement + blocked path | `test_mil_move_001` |
| Combat range/cooldown/damage/destruction | `test_mil_combat_001` |
| Armour/damage-type modifier + projectile timing | `test_mil_combat_002` |
| Orders: attack-move / patrol / guard | `test_mil_orders_001` |
| Victory / defeat (hub destruction) | `test_mil_victory_001` |
| Fog/shroud/vision reveal + demote | `test_mil_vision_001` |
| Defence turret auto-target | `test_mil_defence_001` |
| Repair depot heal-for-credits + stall | `test_mil_repair_001` |
| Seeded skirmish setup determinism | `test_mil_skirmish_001` |
| Snapshot completeness + diagnostics + deterministic replay | `test_mil_diag_001` |

Smokes MIL-SMOKE-001–009 are implemented in `sim/tools/run_military_smoke.gd`.

**Deferred to RUN_04 (per-rule-ID granularity only):** following the RUN_02 precedent, the
expansion of every individual MIL-* rule ID above into its own dedicated 1:1 test is deferred to
RUN_04 hardening. **No whole mechanic family is deferred** — each family already has DoD-level
behaviour coverage in RUN_03. Speculative families not yet in scope (engineer capture
`MIL-CAPTURE-*`, veterancy `MIL-VET-*`, two-bot skirmish `MIL-BOT-*`) remain for their owning runs.

## Hybrid (HYB-*)

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| HYB-LOGCON-001 | integration | test_HYB_LOGCON_001_military_site_uses_construction |
| HYB-LOGPROD-001 | integration | test_HYB_LOGPROD_001_barracks_requires_tool_delivery |
| HYB-LOGREP-001 | integration | test_HYB_LOGREP_001_repair_requires_ware_delivery |
| HYB-NOSIDE-001 | unit | test_HYB_NOSIDE_001_no_instant_spawn |
| HYB-CTRL-001 | unit | test_HYB_CTRL_001_rejects_direct_move_unit |
| HYB-CTRL-006 | integration | test_HYB_CTRL_006_delegated_attack_visible_target |
| HYB-CTRL-009 | integration | test_HYB_CTRL_009_patrol_zone_engages_hostiles |
| HYB-CTRL-010 | integration | test_HYB_CTRL_010_guard_policy_defends_building |
| HYB-CTRL-011 | integration | test_HYB_CTRL_011_repair_priority_heals_target |
| HYB-DEF-001 | integration | test_HYB_DEF_001_turret_target_priority |
| HYB-INTEL-001 | integration | test_HYB_INTEL_001_merged_fog_with_economy |
| HYB-SMOKE-009 | smoke | test_HYB_SMOKE_009_full_scenario_replay_hash |

## Visualisation (VIS-*)

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| VIS-SNAP-001 | integration | test_VIS_SNAP_001_all_layers_present_in_fixture |
| VIS-CMD-001 | integration | test_VIS_CMD_001_command_round_trip |

## Bots (BOT-*)

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| BOT-CMD-001 | integration | test_BOT_CMD_001_bot_uses_command_api |
| BOT-DET-001 | scenario | test_BOT_DET_001_bot_rollout_deterministic |

## Traceability

Every family in `01_mechanic_coverage_contract.md` maps to at least one row above. Additional tests added during implementation runs without removing rows.
