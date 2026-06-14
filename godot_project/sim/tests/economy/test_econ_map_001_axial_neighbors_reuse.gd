extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_MAP_001_axial_neighbors_reuse"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 7,
		"map_width": 6,
		"map_height": 6,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var center := HexCoord.new(2, 2)
	var neighbors: Array[HexCoord] = center.neighbors()
	assert_eq(neighbors.size(), 6, "axial hex must expose six neighbours")

	for neighbour in neighbors:
		assert_true(state.map.in_bounds(neighbour), "in-bounds neighbour must be queryable")
		var tile: HexTile = state.map.get_tile(neighbour)
		assert_not_null(tile, "map tile must exist for in-bounds neighbour")

	var hq: Dictionary = EconomyState.get_building(
		state,
		int(EconomyState.get_meta_value(state, "hq_building_id", 0)),
	)
	var hq_node_id: int = int(hq.get("road_node_id", 0))
	assert_true(hq_node_id > 0, "HQ must create a road node on its hex")

	var hq_q: int = int(hq.get("anchor_q", 0))
	var hq_r: int = int(hq.get("anchor_r", 0))
	var neighbour_coord := HexCoord.new(hq_q + 1, hq_r)
	_configure_tile(state, neighbour_coord.q, neighbour_coord.r, "grass", 0)

	var result: Dictionary = EconomyRoadSystem.place_road(
		state,
		HexCoord.new(hq_q, hq_r),
		neighbour_coord,
		"p1",
	)
	assert_true(result.get("ok", false), "road placement on adjacent hex should succeed")

	var node_at_hq: int = EconomyRoadSystem._find_node_at(state, hq_q, hq_r)
	assert_eq(node_at_hq, hq_node_id, "road node must reuse existing hex node instead of duplicating")


func _configure_tile(state: GameState, q: int, r: int, terrain: String, height: int) -> void:
	var coord := HexCoord.new(q, r)
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var tdef: Dictionary = cfg.get_terrain_def(terrain)
	state.map.set_tile(
		coord,
		HexTile.new(
			terrain,
			height,
			int(tdef.get("passability", 1)),
			int(tdef.get("buildability", 1)),
		),
	)
