extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_DET_002_replay_hash_stable"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 103,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
	}
	var runner := SimulationTickRunner.new()

	var state_a: GameState = runner.create_game(scenario)
	runner.submit_command(state_a, NoopCommand.new("p1", 0))
	state_a = runner.run_ticks(state_a, 5)
	var hash_a: String = _authoritative_replay_hash(state_a)

	var state_b: GameState = runner.create_game(scenario)
	runner.submit_command(state_b, NoopCommand.new("p1", 0))
	state_b = runner.run_ticks(state_b, 5)
	var hash_b: String = _authoritative_replay_hash(state_b)

	assert_eq(hash_a, hash_b, "economy replay hash must be stable for identical scenario and commands")
	assert_true(hash_a.begins_with("sha256:"))

	var map_hash_a: String = MapGenerator.compute_map_hash(state_a.map)
	var map_hash_b: String = MapGenerator.compute_map_hash(state_b.map)
	assert_eq(map_hash_a, map_hash_b, "economy map hash must be stable for identical seed")


func _authoritative_replay_hash(state: GameState) -> String:
	var meta: Dictionary = EconomyState.get_economy_meta(state)
	var cached_config: Variant = meta.get("config", null)
	meta.erase("config")
	var hash: String = ReplayHash.compute_state_hash(state)
	if cached_config != null:
		meta["config"] = cached_config
	return hash
