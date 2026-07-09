extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_C_002_throughput_delta_neglected_vs_satisfied"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_micro_scenario("micro_worker_needs")
	var runner := SimulationTickRunner.new()
	var good: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(good, scenario)
	GodStrategyTestHelper.ensure_god_strategy(good, scenario)
	WorkerNeedsSystem.set_cohort_needs(good, "p1", "woodcutter", 1000, 0, 1000, 1000)
	var good_mult: int = WorkerNeedsSystem.get_throughput_multiplier(good, "p1", "woodcutter")

	var bad: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(bad, scenario)
	GodStrategyTestHelper.ensure_god_strategy(bad, scenario)
	WorkerNeedsSystem.set_cohort_needs(bad, "p1", "woodcutter", 100, 900, 100, 100)
	var bad_mult: int = WorkerNeedsSystem.get_throughput_multiplier(bad, "p1", "woodcutter")

	assert_true(good_mult > bad_mult, "satisfied cohort higher throughput (good=%d bad=%d)" % [good_mult, bad_mult])
	assert_true(bad_mult < good_mult / 2, "measurable throughput delta")
