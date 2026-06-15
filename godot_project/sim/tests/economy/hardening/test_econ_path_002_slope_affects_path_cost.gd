extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_PATH_002_slope_affects_path_cost"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 172,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_set_grass_tile(state, 1, 1, 0)
	_set_grass_tile(state, 2, 1, 0)
	_set_grass_tile(state, 3, 1, 0)
	_set_grass_tile(state, 3, 2, 0)
	_set_grass_tile(state, 2, 2, 2)
	EconomyRoadSystem.place_road(state, HexCoord.new(1, 1), HexCoord.new(2, 1), "p1")
	EconomyRoadSystem.place_road(state, HexCoord.new(2, 1), HexCoord.new(3, 1), "p1")
	EconomyRoadSystem.place_road(state, HexCoord.new(3, 1), HexCoord.new(3, 2), "p1")
	EconomyRoadSystem.place_road(state, HexCoord.new(2, 1), HexCoord.new(2, 2), "p1")

	var start_node: int = EconomyRoadSystem._find_node_at(state, 1, 1)
	var end_node: int = EconomyRoadSystem._find_node_at(state, 3, 2)
	var flat_mid: int = EconomyRoadSystem._find_node_at(state, 2, 1)
	var slope_mid: int = EconomyRoadSystem._find_node_at(state, 2, 2)

	var flat_cost: int = (
		EconomyPathfinder._edge_cost(state, start_node, flat_mid)
		+ EconomyPathfinder._edge_cost(state, flat_mid, EconomyRoadSystem._find_node_at(state, 3, 1))
		+ EconomyPathfinder._edge_cost(state, EconomyRoadSystem._find_node_at(state, 3, 1), end_node)
	)
	var slope_cost: int = EconomyPathfinder._edge_cost(state, flat_mid, slope_mid) + EconomyPathfinder._edge_cost(state, slope_mid, end_node)
	assert_true(slope_cost > flat_cost / 2, "slope edge must cost more than flat edge on same network")

	var route: Array = EconomyPathfinder.find_route(state, start_node, end_node)
	assert_false(route.is_empty())
	assert_eq(int(route[1]), flat_mid, "pathfinder must prefer lower-cost flat route over sloped shortcut")


func _set_grass_tile(state: GameState, q: int, r: int, height: int) -> void:
	state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", height, 1, 1))
