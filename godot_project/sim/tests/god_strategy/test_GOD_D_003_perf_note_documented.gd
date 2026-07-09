extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_D_003_perf_note_documented"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_micro_scenario("micro_worker_policy_4p")
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state, scenario)
	state = runner.run_ticks(state, 600)
	var perf: Dictionary = WorkerPolicySystem.get_perf_note(state)
	assert_true(int(perf.get("policy_refresh_count", 0)) >= 1, "policy refresh ran")
	assert_true(perf.has("avg_calls_per_refresh"), "perf note fields present")
