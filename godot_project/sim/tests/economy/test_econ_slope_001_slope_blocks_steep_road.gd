extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_SLOPE_001_slope_blocks_steep_road"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 13,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var low := HexCoord.new(3, 3)
	var high := HexCoord.new(4, 3)
	_set_grass_tile(state, low.q, low.r, 0)
	_set_grass_tile(state, high.q, high.r, 5)

	var check: Dictionary = EconomyMapRules.can_place_road(state, low, high, "p1")
	assert_false(check.get("ok", true), "steep slope must block road placement")
	assert_eq(str(check.get("reason", "")), "SLOPE_TOO_STEEP")

	var roads_before: int = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS).size()
	var result: Dictionary = EconomyRoadSystem.place_road(state, low, high, "p1")
	assert_false(result.get("ok", true), "road system must reject steep slope")
	assert_eq(roads_before, EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS).size(), "state unchanged on rejection")


func _set_grass_tile(state: GameState, q: int, r: int, height: int) -> void:
	var coord := HexCoord.new(q, r)
	state.map.set_tile(coord, HexTile.new("grass", height, 1, 1))
