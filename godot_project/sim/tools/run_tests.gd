extends SceneTree

## RUN_01+02 headless test harness for the authoritative simulation core (DDR-032).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")

const TEST_SCRIPTS: Array[String] = [
	"res://sim/tests/kernel/test_run01_kernel_001_create_game_state.gd",
	"res://sim/tests/kernel/test_run01_kernel_002_seeded_rng_reproducible.gd",
	"res://sim/tests/kernel/test_run01_kernel_003_command_result_shape.gd",
	"res://sim/tests/kernel/test_run01_kernel_004_invalid_command_does_not_mutate_state.gd",
	"res://sim/tests/kernel/test_run01_kernel_005_event_serialization.gd",
	"res://sim/tests/kernel/test_run01_kernel_006_snapshot_json_shape.gd",
	"res://sim/tests/kernel/test_run01_kernel_007_replay_hash_reproducible.gd",
	"res://sim/tests/hex/test_run01_hex_001_hex_has_six_neighbours.gd",
	"res://sim/tests/hex/test_run01_hex_002_hex_distance.gd",
	"res://sim/tests/hex/test_run01_hex_003_seeded_map_reproducible.gd",
	"res://sim/tests/variants/test_run01_variant_001_all_variants_register.gd",
	"res://sim/tests/variants/test_run01_variant_002_all_variants_smoke_headless.gd",
	"res://sim/tests/determinism/test_run01_determinism_001_no_float_in_hashed_state_fixture.gd",
	"res://sim/tests/economy/test_econ_map_001_axial_neighbors_reuse.gd",
	"res://sim/tests/economy/test_econ_terr_001_water_not_buildable.gd",
	"res://sim/tests/economy/test_econ_slope_001_slope_blocks_steep_road.gd",
	"res://sim/tests/economy/test_econ_build_001_footprint_overlap_rejected.gd",
	"res://sim/tests/economy/test_econ_road_001_roads_connect_adjacent_hexes.gd",
	"res://sim/tests/economy/test_econ_road_002_terrain_blocks_road_on_water.gd",
	"res://sim/tests/economy/test_econ_road_007_blocked_route_in_diagnostics.gd",
	"res://sim/tests/economy/test_econ_path_001_shortest_path_on_network.gd",
	"res://sim/tests/economy/test_econ_ware_001_ware_types_enumerated.gd",
	"res://sim/tests/economy/test_econ_job_003_job_lifecycle_transitions.gd",
	"res://sim/tests/economy/test_econ_stor_001_storage_full_blocks_input.gd",
	"res://sim/tests/economy/test_econ_worker_003_missing_worker_pauses_production.gd",
	"res://sim/tests/economy/test_econ_tool_002_missing_tool_blocks_assignment.gd",
	"res://sim/tests/economy/test_econ_forest_002_woodcutter_reduces_density.gd",
	"res://sim/tests/economy/test_econ_food_002_mine_requires_food.gd",
	"res://sim/tests/economy/test_econ_mine_001_requires_matching_deposit.gd",
	"res://sim/tests/economy/test_econ_upgrade_002_upgrade_requires_materials.gd",
	"res://sim/tests/economy/test_econ_cons_003_completion_spawns_building.gd",
	"res://sim/tests/economy/test_econ_pri_001_construction_priority_order.gd",
	"res://sim/tests/economy/test_econ_chain_001_forest_to_timber.gd",
	"res://sim/tests/economy/test_econ_chain_010_workshop_produces_tools.gd",
	"res://sim/tests/economy/test_econ_diag_001_per_building_status.gd",
	"res://sim/tests/economy/test_econ_snap_002_all_display_layers_present.gd",
	"res://sim/tests/economy/test_econ_det_002_replay_hash_stable.gd",
	"res://sim/tests/economy/test_econ_cmd_road_001_place_road_command.gd",
	"res://sim/tests/economy/test_scenario_tiny_success_map.gd",
	"res://sim/tests/economy/test_scenario_blocked_route_map.gd",
	"res://sim/tests/economy/test_scenario_missing_resource_map.gd",
	"res://sim/tests/economy/test_scenario_priority_test_map.gd",
	"res://sim/tests/economy/test_scenario_storage_full_map.gd",
	"res://sim/tests/economy/test_scenario_pathfinding_test_map.gd",
	"res://sim/tests/economy/test_scenario_forest_depletion_regrowth_map.gd",
	"res://sim/tests/economy/test_scenario_mine_food_support_map.gd",
	"res://sim/tests/economy/test_scenario_tool_shortage_worker_block_map.gd",
	"res://sim/tests/economy/test_scenario_building_upgrade_map.gd",
	"res://sim/tests/military/test_mil_scaffold_001_buckets_initialised.gd",
	"res://sim/tests/military/test_mil_scaffold_002_config_loads.gd",
	"res://sim/tests/military/test_mil_map_001_shared_hex_map_no_parallel_grid.gd",
	"res://sim/tests/military/test_mil_cmd_001_invalid_command_no_mutation.gd",
	"res://sim/tests/military/test_mil_res_001_harvest_return_credits.gd",
	"res://sim/tests/military/test_mil_build_001_placement_radius_footprint.gd",
	"res://sim/tests/military/test_mil_cons_001_construction_completes.gd",
	"res://sim/tests/military/test_mil_power_001_underpowered_blocks_production.gd",
	"res://sim/tests/military/test_mil_prod_001_queue_spawns_at_rally.gd",
	"res://sim/tests/military/test_mil_tech_001_prerequisite_unlocks_unit.gd",
	"res://sim/tests/military/test_mil_move_001_pathfinding_and_blocked.gd",
	"res://sim/tests/military/test_mil_combat_001_range_cooldown_damage.gd",
	"res://sim/tests/military/test_mil_combat_002_armour_and_projectile.gd",
	"res://sim/tests/military/test_mil_orders_001_attack_move_patrol_guard.gd",
	"res://sim/tests/military/test_mil_victory_001_hub_destroyed.gd",
	"res://sim/tests/military/test_mil_vision_001_fog_reveal_demote.gd",
	"res://sim/tests/military/test_mil_defence_001_turret_auto_targets.gd",
	"res://sim/tests/military/test_mil_repair_001_depot_heals_for_credits.gd",
]


func _initialize() -> void:
	BootstrapScript.load_all()
	var expected: int = TEST_SCRIPTS.size()
	var passed: int = 0
	var failed: int = 0
	var errored: int = 0
	var ran: int = 0
	print("RUN_01+02 headless test harness — %d tests expected" % expected)

	for script_path in TEST_SCRIPTS:
		var script: Script = load(script_path)
		if script == null:
			errored += 1
			ran += 1
			print("ERROR %s (could not load script)" % script_path)
			continue

		if not script.can_instantiate():
			errored += 1
			ran += 1
			print("ERROR %s (script cannot instantiate)" % script_path)
			continue

		var test_instance: Object = script.new()
		if test_instance == null:
			errored += 1
			ran += 1
			print("ERROR %s (could not instantiate)" % script_path)
			continue

		var test_name: String = script_path.get_file()
		if test_instance.has_method("get_name"):
			test_name = str(test_instance.call("get_name"))

		test_instance.call("run")
		ran += 1

		var completed: bool = true
		if test_instance.has_method("run_completed"):
			completed = bool(test_instance.call("run_completed"))

		if not completed:
			errored += 1
			print("ERROR %s — run did not complete (possible script error)" % test_name)
			continue

		var ok: bool = bool(test_instance.call("passed"))
		if ok:
			passed += 1
			print("PASS %s" % test_name)
		else:
			failed += 1
			print("FAIL %s — %s" % [test_name, str(test_instance.call("failure_message"))])

	var exit_code: int = 0
	if failed > 0 or errored > 0 or ran != expected:
		exit_code = 1

	print("—")
	print(
		"Results: %d/%d ran, %d passed, %d failed, %d errored"
		% [ran, expected, passed, failed, errored]
	)
	if ran != expected:
		print("ERROR: expected %d tests but ran %d" % [expected, ran])
	if exit_code == 0:
		print("EXIT: 0 (pass)")
	else:
		print("EXIT: 1 (fail)")
	call_deferred("quit", exit_code)
