extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_ROAD_001_roads_connect_adjacent_hexes"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 19,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var from_hex := HexCoord.new(5, 1)
	var to_hex := HexCoord.new(6, 1)
	_set_grass_tile(state, from_hex.q, from_hex.r, 0)
	_set_grass_tile(state, to_hex.q, to_hex.r, 0)

	var result: Dictionary = EconomyRoadSystem.place_road(state, from_hex, to_hex, "p1")
	assert_true(result.get("ok", false), "adjacent legal hexes must accept a road")
	var road_id: int = int(result.get("road_id", 0))
	assert_true(road_id > 0, "road entity must be allocated")

	assert_true(EconomyRoadSystem.has_road_between(state, from_hex, to_hex), "road must connect the two hexes")

	var road: Dictionary = EconomyState.get_road(state, road_id)
	assert_eq(int(road.get("hex_a_q", -1)), from_hex.q)
	assert_eq(int(road.get("hex_b_q", -1)), to_hex.q)

	var node_a: int = EconomyRoadSystem._find_node_at(state, from_hex.q, from_hex.r)
	var node_b: int = EconomyRoadSystem._find_node_at(state, to_hex.q, to_hex.r)
	assert_true(node_a > 0 and node_b > 0, "road endpoints must create nodes")
	assert_true(
		EconomyRoadSystem.is_network_connected(state, node_a, node_b),
		"placed road must connect its endpoint nodes",
	)


func _set_grass_tile(state: GameState, q: int, r: int, height: int) -> void:
	state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", height, 1, 1))
