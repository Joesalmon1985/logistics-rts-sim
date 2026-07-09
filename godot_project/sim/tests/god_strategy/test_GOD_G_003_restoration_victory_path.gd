extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_G_003_restoration_victory_path"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_micro_scenario("micro_restoration_victory")
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state, scenario)
	var match_bucket: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_MATCH)
	match_bucket["current_age_index"] = 3
	match_bucket["current_age_id"] = "futuristic_collapse"
	PollutionSystem.set_global_pollution(state, 1500)
	PollutionSystem.apply_restoration(state, 6000)
	var end: Dictionary = GodStrategyMatchEnd.evaluate(state, scenario)
	assert_eq(str(end.get("end_reason", "")), "restoration_victory", "restoration victory when goals met")
	assert_eq(str(end.get("winner_player_id", "")), "p1", "p1 wins restoration")
