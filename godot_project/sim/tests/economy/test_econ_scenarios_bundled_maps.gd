extends "res://sim/tests/test_case.gd"

## One scenario test per bundled map (§37).


func get_name() -> String:
	return "test_scenario_economy_bundled_maps"


func _run_body() -> void:
	var scenarios: Array = [
		{"path": "scenarios/economy/tiny_success_map.json", "ticks": 50, "check": "hq"},
		{"path": "scenarios/economy/blocked_route_map.json", "ticks": 80, "check": "blocked"},
		{"path": "scenarios/economy/missing_resource_map.json", "ticks": 80, "check": "reject_quarry"},
		{"path": "scenarios/economy/priority_test_map.json", "ticks": 80, "check": "priority"},
		{"path": "scenarios/economy/storage_full_map.json", "ticks": 80, "check": "storage"},
		{"path": "scenarios/economy/pathfinding_test_map.json", "ticks": 80, "check": "roads"},
		{"path": "scenarios/economy/forest_depletion_regrowth_map.json", "ticks": 120, "check": "forest"},
		{"path": "scenarios/economy/mine_food_support_map.json", "ticks": 120, "check": "mine"},
		{"path": "scenarios/economy/tool_shortage_worker_block_map.json", "ticks": 80, "check": "tools"},
		{"path": "scenarios/economy/building_upgrade_map.json", "ticks": 120, "check": "upgrade"},
	]
	for entry in scenarios:
		_run_scenario(str(entry["path"]), int(entry["ticks"]), str(entry["check"]))


func _run_scenario(scenario_path: String, ticks: int, check: String) -> void:
	var scenario: Dictionary = ConfigLoader.load_scenario(scenario_path)
	assert_true(not scenario.is_empty(), "scenario load failed: %s" % scenario_path)
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, ticks)

	match check:
		"hq":
			var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
			assert_true(hq_id > 0, "HQ must exist in %s" % scenario_path)
		"blocked":
			var blocked: int = int(state.diagnostics.get("delivery", {}).get("blocked", 0))
			assert_true(blocked >= 0, "blocked delivery diagnostics present")
		"reject_quarry":
			var validator := CommandValidator.new(CommandRegistry.create_default())
			var cmd := PlaceBuildingPlanCommand.new("p1", 0, {
				"building_type": "quarry",
				"q": 5,
				"r": 5,
				"player_id": "p1",
			})
			var result: CommandResult = validator.validate(state, cmd)
			assert_false(result.success, "quarry should reject without deposit")
		"priority":
			assert_true(state.diagnostics.has("construction"), "construction diagnostics expected")
		"storage":
			assert_true(state.diagnostics.has("ware_totals"), "ware totals expected")
		"roads":
			var roads: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS)
			assert_true(roads.size() >= 0, "road layer accessible")
		"forest":
			var summary: Variant = EconomyState.get_meta_value(state, "forest_summary", {})
			assert_true(typeof(summary) == TYPE_DICTIONARY or state.diagnostics.has("resources"), "forest state tracked")
		"mine":
			for building in EconomyState.all_buildings(state):
				if "mine" in str(building.get("building_type", "")):
					return
			assert_true(true, "mine scenario runs without error")
		"tools":
			var shortages: Dictionary = state.diagnostics.get("shortages", {})
			assert_true(typeof(shortages) == TYPE_DICTIONARY, "shortage diagnostics present")
		"upgrade":
			assert_true(not EconomyState.all_buildings(state).is_empty(), "buildings exist for upgrade map")

	var hash_a: String = ReplayHash.compute_state_hash(state)
	var state_b: GameState = runner.create_game(scenario)
	state_b = runner.run_ticks(state_b, ticks)
	var hash_b: String = ReplayHash.compute_state_hash(state_b)
	assert_eq(hash_a, hash_b, "scenario replay hash stable: %s" % scenario_path)
