extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_HUMAN_PROOF_001_same_start_as_econ_bot_002"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	var bot_state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(bot_state, scenario)
	var proof_state: GameState = EconomyHumanProofContract.create_proof_state(runner)

	assert_eq(int(proof_state.seed), 28001, "proof seed")
	assert_eq(proof_state.map.width, 24, "map width")
	assert_eq(proof_state.map.height, 24, "map height")
	assert_eq(int(EconomyHumanProofContract.proof_scenario().get("max_ticks", 0)), 800, "scenario max ticks")

	for player_id in ["p1", "p2"]:
		var hq: Vector2i = EconomyState.get_hq_position(proof_state, player_id)
		var baseline: int = EconomyBvbSetup.get_baseline(proof_state, player_id, "buildings")
		assert_eq(baseline, 0, "%s baseline buildings" % player_id)
		if player_id == "p1":
			assert_eq(hq.x, 4, "p1 hq q")
			assert_eq(hq.y, 4, "p1 hq r")
		else:
			assert_eq(hq.x, 19, "p2 hq q")
			assert_eq(hq.y, 19, "p2 hq r")

	var p1_roads: int = DualBotRunner.count_player_roads_built(proof_state, "p1")
	assert_true(p1_roads == 0, "HQ spine roads in baseline not delta")
