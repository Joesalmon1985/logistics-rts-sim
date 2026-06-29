extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_B_004_multi_vp_advances_to_age4"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_scenario()
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	GodStrategyState.init_match(state, scenario)
	var match_bucket: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_MATCH)
	match_bucket["current_age_id"] = "modern"
	match_bucket["current_age_index"] = GodStrategyConstants.AGES.find("modern")
	VictoryPointSystem.set_vp(state, "p1", 12)
	VictoryPointSystem.set_vp(state, "p2", 11)
	var result: Dictionary = AgeSystem.resolve_age3_gate(state)
	assert_eq(str(result.get("action", "")), "advance_age4", "multi VP tie advances to Age 4")
	assert_eq(GodStrategyState.current_age_id(state), "futuristic_collapse", "Age 4 active")
