extends SceneTree

## RUN_01+02 headless test harness for the authoritative simulation core (DDR-032).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const TestRunnerSupportScript := preload("res://sim/tools/test_runner_support.gd")

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
	"res://sim/tests/bot/test_bot_cmd_001_bot_uses_command_api.gd",
	"res://sim/tests/bot/test_bot_det_001_bot_rollout_deterministic.gd",
	"res://sim/tests/bot/test_econ_bot_002_dual_bot_expansion_metrics.gd",
	"res://sim/tests/bot/test_econ_bot_003_dual_replay_hash.gd",
	"res://sim/tests/bot/test_econ_bot_004_observation_boundary.gd",
	"res://sim/tests/bot/test_econ_bot_b001_single_building_plan.gd",
	"res://sim/tests/bot/test_econ_bot_c001_road_connects_target.gd",
	"res://sim/tests/bot/test_econ_bot_d001_site_receives_delivery.gd",
	"res://sim/tests/bot/test_econ_bot_e001_completes_woodcutter.gd",
	"res://sim/tests/bot/test_econ_bot_f001_dual_symmetry_minimal.gd",
	"res://sim/tests/bot/test_econ_bot_g001_road_cap.gd",
	"res://sim/tests/human_proof/test_econ_human_proof_001_same_start_as_econ_bot_002.gd",
	"res://sim/tests/human_proof/test_econ_human_proof_002_metrics_match_dual_bot_runner.gd",
	"res://sim/tests/human_proof/test_econ_human_proof_003_command_record_replay_round_trip.gd",
	"res://sim/tests/human_proof/test_econ_human_proof_004_oracle_one_building.gd",
	"res://sim/tests/replay/test_rep_smoke_001_export_import_identical_hash.gd",
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
	"res://sim/tests/economy/test_econ_job_001_job_created_on_missing_input.gd",
	"res://sim/tests/economy/test_econ_job_002_block_reasons.gd",
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
	"res://sim/tests/economy/test_econ_chain_brick_config_only.gd",
	"res://sim/tests/economy/test_econ_config_validation.gd",
	"res://sim/tests/economy/test_econ_node_001_building_ports.gd",
	"res://sim/tests/economy/test_econ_chain_010_workshop_produces_tools.gd",
	"res://sim/tests/economy/test_econ_diag_001_per_building_status.gd",
	"res://sim/tests/economy/test_econ_snap_002_all_display_layers_present.gd",
	"res://sim/tests/economy/test_econ_snap_003_logistics_arrays_present.gd",
	"res://sim/tests/economy/test_econ_mp_001_two_player_init.gd",
	"res://sim/tests/economy/test_econ_mp_002_cross_player_isolation.gd",
	"res://sim/tests/economy/test_econ_score_001_per_player_scoring.gd",
	"res://sim/tests/economy/test_econ_det_002_replay_hash_stable.gd",
	"res://sim/tests/economy/test_econ_cmd_road_001_place_road_command.gd",
	"res://sim/tests/economy/test_scenario_tiny_success_map.gd",
	"res://sim/tests/economy/test_eval_build_obvious_worker_policy_victory.gd",
	"res://sim/tests/economy/test_scenario_blocked_route_map.gd",
	"res://sim/tests/economy/test_scenario_missing_resource_map.gd",
	"res://sim/tests/economy/test_scenario_priority_test_map.gd",
	"res://sim/tests/economy/test_scenario_storage_full_map.gd",
	"res://sim/tests/economy/test_scenario_pathfinding_test_map.gd",
	"res://sim/tests/economy/test_scenario_forest_depletion_regrowth_map.gd",
	"res://sim/tests/economy/test_scenario_mine_food_support_map.gd",
	"res://sim/tests/economy/test_scenario_tool_shortage_worker_block_map.gd",
	"res://sim/tests/economy/test_scenario_building_upgrade_map.gd",
	"res://sim/tests/economy/hardening/test_econ_worker_001_building_requests_worker_type.gd",
	"res://sim/tests/economy/hardening/test_econ_worker_002_worker_supplied_from_storehouse.gd",
	"res://sim/tests/economy/hardening/test_econ_worker_004_tool_required_before_assign.gd",
	"res://sim/tests/economy/hardening/test_econ_worker_005_shortage_emits_event.gd",
	"res://sim/tests/economy/hardening/test_econ_tool_001_specialist_requires_tools.gd",
	"res://sim/tests/economy/hardening/test_econ_tool_003_requirements_configurable_by_type.gd",
	"res://sim/tests/economy/hardening/test_econ_tool_004_shortage_in_diagnostics.gd",
	"res://sim/tests/economy/hardening/test_econ_forest_001_tree_density_on_hex.gd",
	"res://sim/tests/economy/hardening/test_econ_forest_003_forester_increases_density.gd",
	"res://sim/tests/economy/hardening/test_econ_forest_004_low_density_blocks_timber.gd",
	"res://sim/tests/economy/hardening/test_econ_forest_005_depletion_regrowth_events.gd",
	"res://sim/tests/economy/hardening/test_econ_food_001_food_consumed_as_support.gd",
	"res://sim/tests/economy/hardening/test_econ_food_003_deep_mine_requires_meals.gd",
	"res://sim/tests/economy/hardening/test_econ_food_004_missing_food_pauses_mine.gd",
	"res://sim/tests/economy/hardening/test_econ_food_005_shortage_in_diagnostics.gd",
	"res://sim/tests/economy/hardening/test_econ_mine_002_consumes_food_while_producing.gd",
	"res://sim/tests/economy/hardening/test_econ_mine_003_deposit_depletes_over_time.gd",
	"res://sim/tests/economy/hardening/test_econ_mine_004_basic_and_deep_tier.gd",
	"res://sim/tests/economy/hardening/test_econ_mine_005_deep_requires_meals.gd",
	"res://sim/tests/economy/hardening/test_econ_mine_006_depletion_in_diagnostics.gd",
	"res://sim/tests/economy/hardening/test_econ_upgrade_001_building_has_upgrade_target.gd",
	"res://sim/tests/economy/hardening/test_econ_upgrade_003_uses_logistics_construction.gd",
	"res://sim/tests/economy/hardening/test_econ_upgrade_004_upgraded_changes_capabilities.gd",
	"res://sim/tests/economy/hardening/test_econ_upgrade_005_upgrade_emits_events.gd",
	"res://sim/tests/economy/hardening/test_econ_road_003_slope_affects_road_legality.gd",
	"res://sim/tests/economy/hardening/test_econ_road_004_node_buffers_wares.gd",
	"res://sim/tests/economy/hardening/test_econ_road_005_carrier_moves_along_road.gd",
	"res://sim/tests/economy/hardening/test_econ_road_006_congestion_in_diagnostics.gd",
	"res://sim/tests/economy/hardening/test_econ_path_002_slope_affects_path_cost.gd",
	"res://sim/tests/economy/hardening/test_econ_path_003_blocked_path_returns_failure_reason.gd",
	"res://sim/tests/economy/hardening/test_econ_pri_002_production_priority_order.gd",
	"res://sim/tests/economy/hardening/test_econ_pri_003_ware_routing_priority_when_scarce.gd",
	"res://sim/tests/economy/hardening/test_econ_carr_001_carrier_delivers_one_ware.gd",
	"res://sim/tests/economy/hardening/test_econ_carr_002_carrier_moves_per_tick_speed.gd",
	"res://sim/tests/economy/hardening/test_econ_cons_001_construction_consumes_wares.gd",
	"res://sim/tests/economy/hardening/test_econ_cons_004_blocked_until_carrier_delivery.gd",
	"res://sim/tests/economy/hardening/test_econ_diag_004_csv_exports_generated.gd",
	"res://sim/tests/economy/hardening/test_econ_stall_001_stall_emits_diagnostic.gd",
	"res://sim/tests/economy/hardening/test_econ_evt_007_ware_delivered_event_emitted.gd",
	"res://sim/tests/economy/hardening/test_econ_evt_018_tree_density_changed_event.gd",
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
	"res://sim/tests/military/test_mil_skirmish_001_seeded_setup_deterministic.gd",
	"res://sim/tests/military/test_mil_diag_001_snapshot_and_determinism.gd",
	"res://sim/tests/military/hardening/test_mil_econ_001_resource_field_on_map.gd",
	"res://sim/tests/military/hardening/test_mil_econ_003_collector_harvests_node.gd",
	"res://sim/tests/military/hardening/test_mil_econ_005_processor_converts_to_credits.gd",
	"res://sim/tests/military/hardening/test_mil_cred_001_credits_consumed_on_production.gd",
	"res://sim/tests/military/hardening/test_mil_build_003_construction_queue_completes.gd",
	"res://sim/tests/military/hardening/test_mil_build_004_missing_prerequisite_rejected.gd",
	"res://sim/tests/military/hardening/test_mil_build_005_outside_build_radius_rejected.gd",
	"res://sim/tests/military/hardening/test_mil_hub_001_hub_destruction_defeats_player.gd",
	"res://sim/tests/military/hardening/test_mil_power_003_unpowered_barracks_cannot_produce.gd",
	"res://sim/tests/military/hardening/test_mil_power_005_low_power_in_diagnostics.gd",
	"res://sim/tests/military/hardening/test_mil_queue_001_unit_queue_processes_in_order.gd",
	"res://sim/tests/military/hardening/test_mil_queue_004_building_construction_queue_separate.gd",
	"res://sim/tests/military/hardening/test_mil_queue_005_unpowered_queue_blocked.gd",
	"res://sim/tests/military/hardening/test_mil_tech_002_research_unlocks_building.gd",
	"res://sim/tests/military/hardening/test_mil_tech_004_prerequisite_blocks_unit_queue.gd",
	"res://sim/tests/military/hardening/test_mil_fog_001_unexplored_hidden.gd",
	"res://sim/tests/military/hardening/test_mil_fog_002_shroud_hides_enemy_outside_vision.gd",
	"res://sim/tests/military/hardening/test_mil_vision_002_radar_extends_sight.gd",
	"res://sim/tests/military/hardening/test_mil_vision_003_scout_bonus_sight.gd",
	"res://sim/tests/military/hardening/test_mil_move_002_move_command_updates_position.gd",
	"res://sim/tests/military/hardening/test_mil_move_003_terrain_modifier_affects_cost.gd",
	"res://sim/tests/military/hardening/test_mil_move_005_collector_paths_to_processor.gd",
	"res://sim/tests/military/hardening/test_mil_order_003_attack_move_engages_en_route.gd",
	"res://sim/tests/military/hardening/test_mil_order_004_patrol_loops_waypoints.gd",
	"res://sim/tests/military/hardening/test_mil_order_005_guard_stays_near_target.gd",
	"res://sim/tests/military/hardening/test_mil_combat_002_projectile_or_instant_hit_config.gd",
	"res://sim/tests/military/hardening/test_mil_combat_003_cooldown_blocks_refire.gd",
	"res://sim/tests/military/hardening/test_mil_combat_007_turret_auto_targets.gd",
	"res://sim/tests/military/hardening/test_mil_damage_002_armour_modifier_applied.gd",
	"res://sim/tests/military/hardening/test_mil_repair_002_depot_heals_for_credits.gd",
	"res://sim/tests/military/hardening/test_mil_repair_004_unpowered_repair_stalled.gd",
	"res://sim/tests/military/hardening/test_mil_mapgen_002_seeded_map_deterministic.gd",
	"res://sim/tests/military/hardening/test_mil_mapgen_003_generated_skirmish_playable.gd",
	"res://sim/tests/military/hardening/test_mil_det_002_replay_hash_stable.gd",
	"res://sim/tests/military/hardening/test_mil_diag_001_stalled_harvest_flagged.gd",
	"res://sim/tests/military/hardening/test_mil_replay_002_skirmish_replay_hash.gd",
	"res://sim/tests/military/hardening/test_mil_snap_002_fog_layers_in_snapshot.gd",
	"res://sim/tests/military/hardening/test_mil_smoke_008_win_loss_triggers.gd",
	"res://sim/tests/viewer/test_vis_snap_001_render_data_from_snapshot.gd",
	"res://sim/tests/viewer/test_vis_render_001_construction_sites_and_carriers.gd",
	"res://sim/tests/viewer/test_vis_cmd_001_command_roundtrip.gd",
	"res://sim/tests/viewer/test_vis_avail_001_availability_bridge.gd",
	"res://sim/tests/viewer/test_vis_econ_chain_001_production_chain_delta.gd",
	"res://sim/tests/viewer/test_vis_econ_build_001_build_tutorial_flow.gd",
	"res://sim/tests/viewer/test_vis_roll_001_rollout_schema.gd",
	"res://sim/tests/viewer/test_vis_roll_002_loader.gd",
	"res://sim/tests/viewer/test_vis_roll_003_step_panels.gd",
	"res://sim/tests/viewer/test_vis_roll_004_optional_fields.gd",
	"res://sim/tests/viewer/test_vis_roll_006_mode_overlays.gd",
	"res://sim/tests/viewer/test_vis_roll_007_event_aggregation.gd",
	"res://sim/tests/viewer/test_vis_bvb_001_ownership_overlay.gd",
	"res://sim/tests/viewer/test_vis_bvb_002_scoreboard.gd",
	"res://sim/tests/viewer/test_vis_spectator_001_render_data_deltas.gd",
	"res://sim/tests/viewer/test_vis_spectator_002_effects_grounded.gd",
	"res://sim/tests/viewer/test_vis_spectator_003_mock_atlas_loads.gd",
	"res://sim/tests/viewer/test_vis_spectator_004_stable_demo_activity.gd",
	"res://sim/tests/viewer/test_vis_spectator_005_sprite_grounding_contract.gd",
	"res://sim/tests/viewer/test_vis_spectator_006_overlay_toggles_sprite_budget.gd",
	"res://sim/tests/training/test_trn_env_001_training_env_reset_snapshot_tick.gd",
	"res://sim/tests/training/test_trn_env_002_submit_records_replay_entry.gd",
	"res://sim/tests/training/test_trn_env_003_replay_round_trip.gd",
	"res://sim/tests/training/test_trn_env_004_response_sentinel_prefix.gd",
	"res://sim/tests/training/test_policy_contract_001_legal_pass.gd",
	"res://sim/tests/training/test_policy_contract_002_episode_result.gd",
]


func _initialize() -> void:
	BootstrapScript.load_all()
	var cfg: Dictionary = TestRunnerSupportScript.parse_config(OS.get_cmdline_user_args())
	var resolved: Dictionary = TestRunnerSupportScript.resolve_scripts(TEST_SCRIPTS, cfg)

	if not resolved.get("ok", false):
		var mode: String = str(resolved.get("mode", ""))
		if mode == "manifest_fail":
			TestRunnerSupportScript.print_manifest_result(resolved.get("manifest_result", {}))
		elif mode == "exact_paths_invalid":
			print("ERROR: exact path list contains entries not in manifest:")
			for path in resolved.get("invalid_paths", []):
				print("  %s" % path)
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
		return

	if bool(cfg.get("manifest_check_only", false)):
		TestRunnerSupportScript.print_manifest_result(resolved.get("manifest_result", {}))
		print("EXIT: 0 (pass)")
		call_deferred("quit", 0)
		return

	if bool(cfg.get("list_only", false)):
		TestRunnerSupportScript.print_test_list(resolved.get("scripts", []))
		print("EXIT: 0 (pass)")
		call_deferred("quit", 0)
		return

	var scripts_to_run: Array = resolved.get("scripts", [])
	var selection_mode: String = str(resolved.get("mode", "full"))
	var filter_was_supplied: bool = bool(resolved.get("filter_was_supplied", false))

	if filter_was_supplied and scripts_to_run.is_empty():
		print("ERROR: zero tests selected (mode=%s)" % selection_mode)
		if str(cfg.get("category", "")) != "" and not TestRunnerSupportScript.CATEGORY_DIRS.has(str(cfg["category"])):
			print("ERROR: unknown TEST_CATEGORY '%s'" % str(cfg["category"]))
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
		return

	if selection_mode == "filter":
		print("TEST_FILTER=%s — running %d of %d tests" % [str(cfg.get("filter", "")), scripts_to_run.size(), TEST_SCRIPTS.size()])
	elif selection_mode == "filters":
		print("TEST_FILTERS=%s — running %d of %d tests" % [str(cfg.get("filters_csv", "")), scripts_to_run.size(), TEST_SCRIPTS.size()])
	elif selection_mode == "category":
		print("TEST_CATEGORY=%s — running %d of %d tests" % [str(cfg.get("category", "")), scripts_to_run.size(), TEST_SCRIPTS.size()])
	elif selection_mode == "exact_paths":
		print("EXACT PATHS — running %d of %d tests" % [scripts_to_run.size(), TEST_SCRIPTS.size()])

	var expected: int = scripts_to_run.size()
	var passed: int = 0
	var failed: int = 0
	var errored: int = 0
	var ran: int = 0
	var timing_records: Array = []
	var total_us: int = 0
	var run_start_us: int = Time.get_ticks_usec()

	print("RUN_02.8c headless test harness — %d tests expected" % expected)

	for script_path in scripts_to_run:
		var path_str: String = str(script_path)
		var test_start_us: int = Time.get_ticks_usec()
		var status: String = "passed"
		var test_name: String = path_str.get_file()

		var script: Script = load(path_str)
		if script == null:
			errored += 1
			ran += 1
			status = "errored"
			print("ERROR %s (could not load script)" % path_str)
			_record_timing(timing_records, path_str, test_name, test_start_us, status)
			total_us += Time.get_ticks_usec() - test_start_us
			continue

		if not script.can_instantiate():
			errored += 1
			ran += 1
			status = "errored"
			print("ERROR %s (script cannot instantiate)" % path_str)
			_record_timing(timing_records, path_str, test_name, test_start_us, status)
			total_us += Time.get_ticks_usec() - test_start_us
			continue

		var test_instance: Object = script.new()
		if test_instance == null:
			errored += 1
			ran += 1
			status = "errored"
			print("ERROR %s (could not instantiate)" % path_str)
			_record_timing(timing_records, path_str, test_name, test_start_us, status)
			total_us += Time.get_ticks_usec() - test_start_us
			continue

		if test_instance.has_method("get_name"):
			test_name = str(test_instance.call("get_name"))

		test_instance.call("run")
		ran += 1

		var completed: bool = true
		if test_instance.has_method("run_completed"):
			completed = bool(test_instance.call("run_completed"))

		if not completed:
			errored += 1
			status = "errored"
			print("ERROR %s — run did not complete (possible script error)" % test_name)
			_record_timing(timing_records, path_str, test_name, test_start_us, status)
			total_us += Time.get_ticks_usec() - test_start_us
			continue

		var ok: bool = bool(test_instance.call("passed"))
		if ok:
			passed += 1
			print("PASS %s" % test_name)
		else:
			failed += 1
			status = "failed"
			print("FAIL %s — %s" % [test_name, str(test_instance.call("failure_message"))])

		_record_timing(timing_records, path_str, test_name, test_start_us, status)
		total_us += Time.get_ticks_usec() - test_start_us

	var wall_us: int = Time.get_ticks_usec() - run_start_us
	TestRunnerSupportScript.print_timing_summary(timing_records, wall_us)

	var timing_json_path: String = str(cfg.get("timing_json_path", ""))
	if timing_json_path != "":
		var summary := {
			"total_s": float(wall_us) / 1_000_000.0,
			"ran": ran,
			"passed": passed,
			"failed": failed,
			"errored": errored,
			"expected": expected,
			"selection_mode": selection_mode,
			"manifest_count": TEST_SCRIPTS.size(),
		}
		if TestRunnerSupportScript.write_timing_json(timing_json_path, timing_records, summary):
			print("TIMING: wrote %s" % timing_json_path)
		else:
			print("TIMING: failed to write %s" % timing_json_path)

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


func _record_timing(
	records: Array,
	path: String,
	name: String,
	start_us: int,
	status: String,
) -> void:
	records.append({
		"path": path,
		"name": name,
		"duration_us": Time.get_ticks_usec() - start_us,
		"status": status,
		"category": TestRunnerSupportScript.category_for_path(path),
	})
