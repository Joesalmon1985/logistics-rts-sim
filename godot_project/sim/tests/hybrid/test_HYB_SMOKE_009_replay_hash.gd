extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_HYB_SMOKE_009_replay_hash"


func _run_body() -> void:
	MilitaryState.reset_config_cache()
	EconomyState.reset_config_cache()
	HybridState.reset_config_cache()

	var scenario: Dictionary = HybridTestHelper.base_scenario(709)
	scenario["spawns"][0]["buildings"] = [{"type": "power_generator", "q": 6, "r": 2}]

	var runner_a := SimulationTickRunner.new()
	var state_a: GameState = runner_a.create_game(scenario.duplicate(true))
	HybridTestHelper.flatten_grass(state_a, 0, 0, 15, 15)
	state_a = runner_a.run_ticks(state_a, 25)
	var hash_a: String = ReplayHash.compute_state_hash(state_a)

	var runner_b := SimulationTickRunner.new()
	var state_b: GameState = runner_b.create_game(scenario.duplicate(true))
	HybridTestHelper.flatten_grass(state_b, 0, 0, 15, 15)
	state_b = runner_b.run_ticks(state_b, 25)
	var hash_b: String = ReplayHash.compute_state_hash(state_b)

	assert_eq(hash_a, hash_b, "replay hash must match for identical hybrid scenario")
	assert_true(hash_a.begins_with("sha256:"), "hash format")

	var snap: Dictionary = HybridSnapshotExporter.export_snapshot(state_a)
	assert_eq(str(snap.get("variant", "")), "hybrid", "snapshot variant must be hybrid")
	assert_true(snap.has("delegated_orders"), "snapshot must include delegated_orders")
