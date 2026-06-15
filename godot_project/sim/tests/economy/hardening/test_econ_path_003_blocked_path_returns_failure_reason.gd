extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_PATH_003_blocked_path_returns_failure_reason"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 173,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var isolated_a: int = EconomyRoadSystem.create_building_road_node(state, 0, 5, 1, "p1")
	var isolated_b: int = EconomyRoadSystem.create_building_road_node(state, 0, 5, 3, "p1")
	var route: Array = EconomyPathfinder.find_route(state, isolated_a, isolated_b)
	assert_true(route.is_empty(), "disconnected nodes must return no route")

	var failure: Variant = EconomyState.get_meta_value(state, "last_route_failure", {})
	assert_true(typeof(failure) == TYPE_DICTIONARY)
	assert_eq(str(failure.get("reason", "")), "NO_PATH")

	var blocked_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "route_blocked":
			blocked_event = true
			assert_eq(str(event.payload.get("reason", "")), "NO_PATH")
			break
	assert_true(blocked_event, "route_blocked event expected with failure reason")
