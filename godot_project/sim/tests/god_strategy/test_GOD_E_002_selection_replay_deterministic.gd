extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_E_002_selection_replay_deterministic"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_micro_scenario("micro_societal_selection")
	var runner := SimulationTickRunner.new()
	var state_a: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state_a, scenario)
	state_a = runner.run_ticks(state_a, 600)
	var snap_a: Dictionary = WorkerSelectionSystem.export_snapshot(state_a)

	var state_b: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state_b, scenario)
	state_b = runner.run_ticks(state_b, 600)
	var snap_b: Dictionary = WorkerSelectionSystem.export_snapshot(state_b)
	assert_eq(JSON.stringify(snap_a.get("cohort_traits", {})), JSON.stringify(snap_b.get("cohort_traits", {})), "selection deterministic")
