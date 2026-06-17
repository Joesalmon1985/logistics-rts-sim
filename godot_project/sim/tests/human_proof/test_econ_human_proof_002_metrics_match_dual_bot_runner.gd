extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_HUMAN_PROOF_002_metrics_match_dual_bot_runner"


func _run_body() -> void:
	var oracle := EconomyHumanProofOracleRunner.new()
	oracle.reset()
	var plan_path: String = ProjectSettings.globalize_path(
		"res://sim/tests/fixtures/human_proof/oracle_one_building_p1.json"
	)
	var loaded: Dictionary = oracle.load_plan_from_path(plan_path)
	assert_true(bool(loaded.get("ok", false)), "oracle plan loads")
	var summary: Dictionary = oracle.run_plan(loaded.get("plan", {}))
	var state: GameState = summary.get("state")
	assert_true(state != null, "state after oracle run")

	for player_id in ["p1", "p2"]:
		var contract: Dictionary = EconomyHumanProofContract.evaluate_player_gates(state, player_id)
		assert_eq(
			int(contract.get("buildings", -1)),
			DualBotRunner.count_player_buildings_built(state, player_id),
			"%s buildings metric" % player_id,
		)
		assert_eq(
			int(contract.get("roads", -1)),
			DualBotRunner.count_player_roads_built(state, player_id),
			"%s roads metric" % player_id,
		)
		assert_eq(
			int(contract.get("deliveries", -1)),
			DualBotRunner.count_player_deliveries_built(state, player_id),
			"%s deliveries metric" % player_id,
		)
