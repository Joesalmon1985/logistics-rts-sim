extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_G_002_shared_collapse_loss"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_micro_scenario("micro_pollution_collapse")
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state, scenario)
	PollutionSystem.set_global_pollution(state, 9500)
	var end: Dictionary = GodStrategyMatchEnd.evaluate(state, scenario)
	assert_eq(str(end.get("end_reason", "")), "shared_collapse", "extreme pollution triggers shared collapse")
	assert_true(bool(end.get("match_over", false)), "match over on collapse")
