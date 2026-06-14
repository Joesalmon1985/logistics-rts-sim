# Rule Contract Test Matrix

Maps mechanic families and rule IDs to required test types. RUN_02+ implements tests listed here.

## Economy (ECON-*)

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| ECON-MAP-001 | unit | test_ECON_MAP_001_axial_neighbors_correct |
| ECON-MAP-002 | unit | test_ECON_MAP_002_out_of_bounds_rejected |
| ECON-TERR-001 | unit | test_ECON_TERR_001_water_not_buildable |
| ECON-SLOPE-001 | unit | test_ECON_SLOPE_001_slope_blocks_steep_road |
| ECON-BUILD-001 | unit | test_ECON_BUILD_001_footprint_overlap_rejected |
| ECON-ROAD-001 | unit | test_ECON_ROAD_001_roads_connect_adjacent_hexes |
| ECON-NODE-001 | integration | test_ECON_NODE_001_building_requires_adjacent_node |
| ECON-PATH-001 | unit | test_ECON_PATH_001_shortest_path_on_network |
| ECON-PATH-003 | integration | test_ECON_PATH_003_blocked_path_returns_failure_reason |
| ECON-WARE-001 | unit | test_ECON_WARE_001_ware_types_enumerated |
| ECON-CARR-001 | integration | test_ECON_CARR_001_carrier_delivers_one_ware |
| ECON-JOB-001 | integration | test_ECON_JOB_001_job_created_on_missing_input |
| ECON-STOR-001 | unit | test_ECON_STOR_001_storage_full_blocks_input |
| ECON-CONS-001 | integration | test_ECON_CONS_001_construction_consumes_wares |
| ECON-CHAIN-001 | scenario | test_ECON_CHAIN_001_forest_to_timber |
| ECON-CHAIN-002 | scenario | test_ECON_CHAIN_002_timber_to_planks |
| ECON-CHAIN-011 | scenario | test_ECON_CHAIN_011_full_building_completion |
| ECON-PRI-001 | unit | test_ECON_PRI_001_construction_priority_order |
| ECON-DET-002 | scenario | test_ECON_DET_002_replay_hash_stable |
| ECON-STALL-001 | integration | test_ECON_STALL_001_stall_emits_diagnostic |
| ECON-CMD-ROAD-001 | unit | test_ECON_CMD_ROAD_001_place_road_command |
| ECON-EVT-007 | integration | test_ECON_EVT_007_ware_delivered_event_emitted |

## Military (MIL-*)

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| MIL-HUB-001 | unit | test_MIL_HUB_001_hub_destruction_defeats_player |
| MIL-POWER-003 | unit | test_MIL_POWER_003_unpowered_barracks_cannot_produce |
| MIL-QUEUE-001 | integration | test_MIL_QUEUE_001_queue_processes_in_order |
| MIL-COMBAT-002 | unit | test_MIL_COMBAT_002_armour_modifier_applied |
| MIL-INTEL-001 | integration | test_MIL_INTEL_001_fog_hides_unexplored |
| MIL-MOVE-002 | unit | test_MIL_MOVE_002_move_command_updates_position |
| MIL-DET-002 | scenario | test_MIL_DET_002_replay_hash_stable |
| MIL-SMOKE-003 | smoke | test_MIL_SMOKE_003_unit_destroys_building |

## Hybrid (HYB-*)

| Rule ID | Test type | Example test name |
|---------|-----------|-------------------|
| HYB-LOGCON-001 | integration | test_HYB_LOGCON_001_military_site_uses_construction |
| HYB-LOGPROD-001 | integration | test_HYB_LOGPROD_001_barracks_requires_tool_delivery |
| HYB-NOSIDE-001 | unit | test_HYB_NOSIDE_001_no_instant_spawn |
| HYB-CTRL-001 | unit | test_HYB_CTRL_001_rejects_direct_move_unit |
| HYB-CTRL-006 | integration | test_HYB_CTRL_006_delegated_attack_visible_target |
| HYB-DEF-001 | integration | test_HYB_DEF_001_turret_target_priority |
| HYB-SMOKE-005 | smoke | test_HYB_SMOKE_005_full_scenario_replay_hash |

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
