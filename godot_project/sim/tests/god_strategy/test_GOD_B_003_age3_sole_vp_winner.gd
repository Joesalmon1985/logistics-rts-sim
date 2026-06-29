extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_B_003_age3_sole_vp_winner"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_scenario()
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	GodStrategyState.init_match(state, scenario)
	var match_bucket: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_MATCH)
	match_bucket["current_age_id"] = "modern"
	match_bucket["current_age_index"] = GodStrategyConstants.AGES.find("modern")
	VictoryPointSystem.set_vp(state, "p1", 10)
	VictoryPointSystem.set_vp(state, "p2", 4)
	var result: Dictionary = AgeSystem.resolve_age3_gate(state)
	assert_true(bool(result.get("action", "") == "win"), "sole 10+ VP winner")
	assert_eq(str(result.get("winner_player_id", "")), "p1", "p1 wins Age 3 VP check")
