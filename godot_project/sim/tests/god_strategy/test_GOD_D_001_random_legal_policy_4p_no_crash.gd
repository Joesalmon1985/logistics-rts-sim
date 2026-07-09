extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_D_001_random_legal_policy_4p_no_crash"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_micro_scenario("micro_worker_policy_4p")
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state, scenario)
	state = runner.run_ticks(state, int(scenario.get("max_ticks", 500)))
	assert_true(state.tick > 0, "4P random legal policy completed ticks")
