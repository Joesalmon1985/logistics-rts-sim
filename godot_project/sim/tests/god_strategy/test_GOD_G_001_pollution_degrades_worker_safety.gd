extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_G_001_pollution_degrades_worker_safety"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_micro_scenario("micro_worker_needs")
	scenario["initial_global_pollution"] = 6000
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state, scenario)
	var match_bucket: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_MATCH)
	match_bucket["current_age_index"] = 2
	match_bucket["current_age_id"] = "modern"
	WorkerNeedsSystem.set_cohort_needs(state, "p1", "builder", 800, 200, 800, 800)
	PollutionSystem.set_global_pollution(state, 6000)
	var before: int = int(WorkerNeedsSystem.get_cohort(state, "p1", "builder").get("safety", 1000))
	state = runner.run_ticks(state, 30)
	var after: int = int(WorkerNeedsSystem.get_cohort(state, "p1", "builder").get("safety", 1000))
	assert_true(after < before, "pollution reduces worker safety (before=%d after=%d)" % [before, after])
