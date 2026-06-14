extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_KERNEL_007_replay_hash_reproducible"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "hybrid",
		"seed": 99,
		"map_width": 6,
		"map_height": 6,
		"players": ["p1"],
	}
	var runner := SimulationTickRunner.new()

	var state_a: GameState = runner.create_game(scenario)
	runner.submit_command(state_a, NoopCommand.new("p1", 0))
	state_a = runner.run_ticks(state_a, 5)
	var hash_a: String = ReplayHash.compute_state_hash(state_a)

	var state_b: GameState = runner.create_game(scenario)
	runner.submit_command(state_b, NoopCommand.new("p1", 0))
	state_b = runner.run_ticks(state_b, 5)
	var hash_b: String = ReplayHash.compute_state_hash(state_b)

	assert_eq(hash_a, hash_b, "replay hash must be reproducible")
	assert_true(hash_a.begins_with("sha256:"))
