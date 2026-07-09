extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_E_001_culture_paths_diverge"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_micro_scenario("micro_societal_selection")
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state, scenario)
	WorkerSelectionSystem.set_culture_path(state, "p1", "welfare")
	WorkerSelectionSystem.set_culture_path(state, "p2", "industrial")
	state = runner.run_ticks(state, 600)
	var snap: Dictionary = WorkerSelectionSystem.export_snapshot(state)
	var hist: Dictionary = snap.get("behaviour_histogram", {})
	assert_true(hist.has("p1") and hist.has("p2"), "both players in histogram")
	var p1_prod: int = int(hist["p1"].get("avg_productivity_trait", 0))
	var p2_prod: int = int(hist["p2"].get("avg_productivity_trait", 0))
	assert_true(abs(p1_prod - p2_prod) >= 80, "culture paths diverge productivity traits (p1=%d p2=%d)" % [p1_prod, p2_prod])
