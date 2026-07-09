extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_C_001_needs_deterministic_replay_hash"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_micro_scenario("micro_worker_needs")
	var runner := SimulationTickRunner.new()
	var state_a: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state_a, scenario)
	GodStrategyTestHelper.ensure_god_strategy(state_a, scenario)
	state_a = runner.run_ticks(state_a, 120)
	var snap_a: Dictionary = WorkerNeedsSystem.export_snapshot(state_a)

	var state_b: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state_b, scenario)
	GodStrategyTestHelper.ensure_god_strategy(state_b, scenario)
	state_b = runner.run_ticks(state_b, 120)
	var snap_b: Dictionary = WorkerNeedsSystem.export_snapshot(state_b)
	assert_eq(JSON.stringify(snap_a), JSON.stringify(snap_b), "worker needs snapshot stable")
