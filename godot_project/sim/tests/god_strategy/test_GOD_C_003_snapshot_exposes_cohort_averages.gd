extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_C_003_snapshot_exposes_cohort_averages"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_micro_scenario("micro_worker_needs")
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state, scenario)
	GodStrategyTestHelper.ensure_god_strategy(state, scenario)
	state = runner.run_ticks(state, 50)
	var needs: Dictionary = WorkerNeedsSystem.export_snapshot(state)
	var averages: Dictionary = needs.get("player_averages", {})
	assert_true(averages.has("p1"), "p1 cohort averages exposed")
	assert_true(int(averages["p1"].get("cohort_count", 0)) >= 1, "cohort count exposed")
