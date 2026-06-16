extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_SCORE_001_per_player_scoring"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/economy/two_player_init_test.json")
	var state: GameState = runner.create_game(scenario)
	EconomyDiagnosticsSystem.tick(state)

	var diag: Dictionary = state.diagnostics
	assert_true(diag.has("economy_scores"), "economy_scores present")
	var scores: Dictionary = diag.get("economy_scores", {})
	assert_true(scores.has("p1"), "p1 score present")
	assert_true(scores.has("p2"), "p2 score present")
	assert_eq(typeof(scores["p1"]), TYPE_INT, "p1 score is int")
	assert_eq(typeof(scores["p2"]), TYPE_INT, "p2 score is int")

	var components: Dictionary = diag.get("economy_score_components", {})
	assert_true(components.has("p1"), "p1 components present")

	var tick0_scores: Dictionary = scores.duplicate()

	EconomyState.increment_player_stat(state, "p1", "completed_deliveries_count", 3)
	EconomyDiagnosticsSystem.tick(state)
	var score_after: int = int(state.diagnostics.get("economy_scores", {}).get("p1", 0))
	var score_before: int = int(scores.get("p1", 0))
	assert_eq(score_after, score_before + 3, "delivery increment affects score")

	var blocked_job := {
		"id": state.entity_ids.allocate(),
		"status": EconomyConstants.JOB_BLOCKED,
		"ware_type": "planks",
		"amount": 1,
		"source_store_id": EconomyState.get_hq_store_id(state, "p2"),
		"dest_store_id": 0,
		"dest_entity_id": 0,
		"dest_entity_type": "building",
		"player_id": "p2",
		"priority": 0,
		"carrier_id": 0,
		"block_reason": "no_route",
	}
	EconomyState.add_delivery_job(state, blocked_job)
	EconomyDiagnosticsSystem.tick(state)
	var p2_score: int = int(state.diagnostics.get("economy_scores", {}).get("p2", 0))
	assert_true(p2_score <= int(scores.get("p2", 0)), "blocked jobs penalise score")

	var state_b: GameState = runner.create_game(scenario)
	EconomyDiagnosticsSystem.tick(state_b)
	assert_eq(
		tick0_scores,
		state_b.diagnostics.get("economy_scores", {}),
		"scores deterministic at tick 0",
	)
	var hash_b: String = ReplayHash.compute_state_hash(state_b)
	var state_c: GameState = runner.create_game(scenario)
	EconomyDiagnosticsSystem.tick(state_c)
	assert_eq(hash_b, ReplayHash.compute_state_hash(state_c), "initial state hash stable before mutations")
