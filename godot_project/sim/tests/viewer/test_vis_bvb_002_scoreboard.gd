extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_VIS_BVB_002_scoreboard"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/economy/two_player_init_test.json")
	var state: GameState = runner.create_game(scenario)
	EconomyDiagnosticsSystem.tick(state)
	var controller := ViewerController.new()
	controller.load_demo_state(state, "p1")
	var summary: String = controller.build_summary_text("economy")
	assert_true(summary.find("Scores") >= 0, "summary mentions scores")
	assert_true(summary.find("p1=") >= 0, "summary includes p1 score")
	assert_true(summary.find("p2=") >= 0, "summary includes p2 score")
