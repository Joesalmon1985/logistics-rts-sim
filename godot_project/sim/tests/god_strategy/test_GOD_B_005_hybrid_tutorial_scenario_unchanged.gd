extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_B_005_hybrid_tutorial_scenario_unchanged"


func _run_body() -> void:
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/hybrid/hybrid_mvp.json")
	assert_eq(str(scenario.get("variant", "")), "hybrid", "hybrid mvp loads")
	assert_false(bool(scenario.get("god_strategy_mvp_a", false)), "legacy scenario has no god-strategy flag")
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	assert_eq(str(state.variant_id), "hybrid", "variant unchanged")
	assert_false(GodStrategyMatchEnd.is_god_strategy_scenario(scenario), "legacy path uses HybridFfaObjective routing")
