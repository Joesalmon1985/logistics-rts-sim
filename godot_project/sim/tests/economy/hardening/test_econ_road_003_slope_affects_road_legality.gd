extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_ROAD_003_slope_affects_road_legality"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 163,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var moderate_a := HexCoord.new(3, 3)
	var moderate_b := HexCoord.new(4, 3)
	_set_grass_tile(state, moderate_a.q, moderate_a.r, 0)
	_set_grass_tile(state, moderate_b.q, moderate_b.r, 1)
	var moderate_check: Dictionary = EconomyMapRules.can_place_road(state, moderate_a, moderate_b, "p1")
	assert_true(moderate_check.get("ok", false), "moderate slope within max_slope_for_road must be legal")

	var steep_a := HexCoord.new(5, 3)
	var steep_b := HexCoord.new(6, 3)
	_set_grass_tile(state, steep_a.q, steep_a.r, 0)
	_set_grass_tile(state, steep_b.q, steep_b.r, 5)
	var steep_check: Dictionary = EconomyMapRules.can_place_road(state, steep_a, steep_b, "p1")
	assert_false(steep_check.get("ok", true), "steep slope must block road placement")
	assert_eq(str(steep_check.get("reason", "")), "SLOPE_TOO_STEEP")

	var roads_before: int = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS).size()
	var moderate_result: Dictionary = EconomyRoadSystem.place_road(state, moderate_a, moderate_b, "p1")
	assert_true(moderate_result.get("ok", false), "legal slope road must be placed")
	assert_true(EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS).size() > roads_before)


func _set_grass_tile(state: GameState, q: int, r: int, height: int) -> void:
	state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", height, 1, 1))
