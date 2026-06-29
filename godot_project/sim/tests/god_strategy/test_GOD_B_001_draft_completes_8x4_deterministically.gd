extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_B_001_draft_completes_8x4_deterministically"


func _run_body() -> void:
	var scenario: Dictionary = GodStrategyTestHelper.load_scenario()
	var runner := SimulationTickRunner.new()
	var state_a: GameState = runner.create_game(scenario)
	GodStrategyState.init_match(state_a, scenario)
	CultureDraftSystem.start_draft_for_current_age(state_a)
	CultureDraftSystem.complete_draft_for_all_players(state_a)
	assert_true(CultureDraftSystem.draft_complete(state_a), "draft complete")

	var picks_a: Dictionary = GodStrategyState.get_bucket(state_a, GodStrategyConstants.BUCKET_DRAFT).get("picks", {})

	var state_b: GameState = runner.create_game(scenario)
	GodStrategyState.init_match(state_b, scenario)
	CultureDraftSystem.start_draft_for_current_age(state_b)
	CultureDraftSystem.complete_draft_for_all_players(state_b)
	var picks_b: Dictionary = GodStrategyState.get_bucket(state_b, GodStrategyConstants.BUCKET_DRAFT).get("picks", {})

	assert_eq(JSON.stringify(picks_a), JSON.stringify(picks_b), "deterministic draft picks")
	for pid in ["p1", "p2", "p3", "p4"]:
		assert_eq(picks_a.get(pid, []).size(), 8, "%s must pick 8 cards" % pid)
