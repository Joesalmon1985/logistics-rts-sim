extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_ROAD_007_blocked_route_in_diagnostics"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 29,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var isolated_a: int = EconomyRoadSystem.create_building_road_node(state, 0, 5, 1, "p1")
	var isolated_b: int = EconomyRoadSystem.create_building_road_node(state, 0, 5, 3, "p1")
	_set_grass_tile(state, 5, 1, 0)
	_set_grass_tile(state, 5, 3, 0)

	var route: Array = EconomyPathfinder.find_route(state, isolated_a, isolated_b)
	assert_true(route.is_empty(), "disconnected nodes must have no route")

	var blocked: Dictionary = EconomyState.get_meta_value(state, "blocked_routes", {})
	assert_false(blocked.is_empty(), "pathfinder must record blocked route metadata")

	var had_route_blocked_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "route_blocked":
			had_route_blocked_event = true
			break
	assert_true(had_route_blocked_event, "route_blocked event expected")

	runner.tick(state)
	assert_has_keys(state.diagnostics, ["routing"], "diagnostics must expose routing section")
	var routing: Dictionary = state.diagnostics.get("routing", {})
	assert_false(routing.is_empty(), "blocked route must appear in diagnostics.routing")


func _set_grass_tile(state: GameState, q: int, r: int, height: int) -> void:
	state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", height, 1, 1))
