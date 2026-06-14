extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_DETERMINISM_001_no_float_in_hashed_state_fixture"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 21,
		"map_width": 5,
		"map_height": 5,
		"players": ["p1"],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 4)

	var hash_input: Dictionary = state.to_hash_dict()
	assert_false(CanonicalJson.contains_float(hash_input), "hashed state must not contain floats")

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_false(CanonicalJson.contains_float(snapshot), "snapshot must not contain floats")
