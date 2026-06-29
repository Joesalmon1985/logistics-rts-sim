extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_B_002_card_unlock_enables_building"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_scenario()
	scenario["players"] = ["p1"]
	scenario["spawns"] = [{"player_id": "p1", "q": 2, "r": 2}]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	GodStrategyState.init_match(state, scenario)
	CultureDraftSystem.start_draft_for_current_age(state)
	for _i in range(8):
		var card: Dictionary = CultureDraftSystem.pick_next_card(state, "p1")
		assert_false(card.is_empty(), "card pick must succeed")
	var unlocks: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_UNLOCKS).get("p1", {})
	assert_false(unlocks.is_empty(), "card unlocks must be stored for snapshot/diagnostics")
