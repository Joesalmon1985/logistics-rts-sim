extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_REP_SMOKE_001_export_import_identical_hash"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 7,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var validator := CommandValidator.new(CommandRegistry.create_default())
	runner.command_validator = validator
	var state: GameState = runner.create_game(scenario)
	var recorder := ReplayRecorder.new()

	for _i in range(20):
		var cmd := PlaceRoadCommand.new("p1", state.tick, {
			"from_q": 2 + (_i % 3),
			"from_r": 2,
			"to_q": 2 + (_i % 3) + 1,
			"to_r": 2,
		})
		var result: CommandResult = validator.validate_and_apply(state, cmd)
		recorder.record(state.tick, cmd, result)
		state = runner.tick(state)

	var replay_path: String = SimPaths.default_export_file("run04_test_replay.json")
	assert_true(ReplayFile.save_from_run(state, recorder, scenario, replay_path), "export replay")

	var verify: Dictionary = ReplayFile.load_and_run(replay_path)
	assert_true(bool(verify.get("success", false)), "import replay: %s" % str(verify.get("error", "")))
	assert_eq(str(verify.get("final_hash", "")), ReplayHash.compute_state_hash(state), "hash match after import")

	# Tamper detection
	var tampered: Dictionary = ReplayFile.load_from_path(replay_path)
	tampered["expected_hash"] = "sha256:definitely_wrong"
	var tamper_path: String = SimPaths.default_export_file("run04_test_replay_tampered.json")
	assert_true(SimPaths.write_text_file(tamper_path, JSON.stringify(tampered, "\t")), "write tamper path")
	var tamper_result: Dictionary = ReplayFile.load_and_run(tamper_path)
	assert_false(bool(tamper_result.get("success", true)), "tampered hash must fail")
