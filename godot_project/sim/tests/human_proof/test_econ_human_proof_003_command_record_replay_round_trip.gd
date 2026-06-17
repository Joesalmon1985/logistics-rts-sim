extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_HUMAN_PROOF_003_command_record_replay_round_trip"


func _run_body() -> void:
	var oracle := EconomyHumanProofOracleRunner.new()
	oracle.reset()
	var plan_path: String = ProjectSettings.globalize_path(
		"res://sim/tests/fixtures/human_proof/oracle_one_building_p1.json"
	)
	var loaded: Dictionary = oracle.load_plan_from_path(plan_path)
	assert_true(bool(loaded.get("ok", false)), "plan load")
	var summary: Dictionary = oracle.run_plan(loaded.get("plan", {}))
	var state: GameState = summary.get("state")
	var export_path: String = SimPaths.default_export_file("human_proof_round_trip.json")
	assert_true(oracle.export_replay(export_path), "export replay")

	var replay: Dictionary = EconomyHumanProofReplay.run_replay_file(export_path)
	assert_true(bool(replay.get("ok", false)), "replay ok: %s" % str(replay.get("error", "")))
	var replay_state: GameState = replay.get("state")
	assert_eq(
		DualBotRunner.count_player_buildings_built(state, "p1"),
		DualBotRunner.count_player_buildings_built(replay_state, "p1"),
		"p1 buildings round trip",
	)
