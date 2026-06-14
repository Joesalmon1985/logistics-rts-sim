extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_PATH_001_shortest_path_on_network"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 31,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var coords: Array[HexCoord] = [
		HexCoord.new(1, 1),
		HexCoord.new(2, 1),
		HexCoord.new(3, 1),
		HexCoord.new(4, 1),
		HexCoord.new(4, 2),
	]
	for coord in coords:
		_set_grass_tile(state, coord.q, coord.r, 0)

	for i in range(coords.size() - 1):
		var result: Dictionary = EconomyRoadSystem.place_road(state, coords[i], coords[i + 1], "p1")
		assert_true(result.get("ok", false), "road chain segment must succeed")

	var start_node: int = EconomyRoadSystem._find_node_at(state, 1, 1)
	var end_node: int = EconomyRoadSystem._find_node_at(state, 4, 2)
	var route: Array = EconomyPathfinder.find_route(state, start_node, end_node)
	assert_false(route.is_empty(), "connected network must yield a route")
	assert_eq(route.size(), 5, "shortest path must traverse five nodes on this chain")
	assert_eq(int(route[0]), start_node)
	assert_eq(int(route[-1]), end_node)


func _set_grass_tile(state: GameState, q: int, r: int, height: int) -> void:
	state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", height, 1, 1))
