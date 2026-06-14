extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_ROAD_002_terrain_blocks_road_on_water"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 23,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var grass := HexCoord.new(2, 2)
	var water := HexCoord.new(3, 2)
	_set_grass_tile(state, grass.q, grass.r, 0)
	_set_water_tile(state, water.q, water.r)

	var check: Dictionary = EconomyMapRules.can_place_road(state, grass, water, "p1")
	assert_false(check.get("ok", true), "road crossing water must be rejected")
	assert_eq(str(check.get("reason", "")), "TERRAIN_BLOCKED")

	var roads_before: int = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS).size()
	var result: Dictionary = EconomyRoadSystem.place_road(state, grass, water, "p1")
	assert_false(result.get("ok", true), "road system must reject water endpoint")
	assert_eq(roads_before, EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS).size())


func _set_grass_tile(state: GameState, q: int, r: int, height: int) -> void:
	state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", height, 1, 1))


func _set_water_tile(state: GameState, q: int, r: int) -> void:
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var tdef: Dictionary = cfg.get_terrain_def("water")
	state.map.set_tile(
		HexCoord.new(q, r),
		HexTile.new("water", 0, int(tdef.get("passability", 0)), int(tdef.get("buildability", 0))),
	)
