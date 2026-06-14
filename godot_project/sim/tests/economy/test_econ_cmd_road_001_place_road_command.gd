extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_CMD_ROAD_001_place_road_command"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 107,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
	}
	var registry := CommandRegistry.create_default()
	var validator := CommandValidator.new(registry)
	var runner := SimulationTickRunner.new(null, validator)
	var state: GameState = runner.create_game(scenario)

	_set_grass_tile(state, 3, 2, 0)
	var roads_before: int = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS).size()

	var cmd: SimCommand = registry.create("PlaceRoadCommand", {
		"player_id": "p1",
		"tick": 0,
		"from_q": 2,
		"from_r": 2,
		"to_q": 3,
		"to_r": 2,
	})
	assert_not_null(cmd, "CommandRegistry must create PlaceRoadCommand")
	runner.submit_command(state, cmd)
	state = runner.tick(state)

	assert_true(EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS).size() > roads_before, "road must be added to state")

	var placed_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "road_placed":
			placed_event = true
			break
	assert_true(placed_event, "road_placed event expected after command apply")

	assert_true(
		EconomyRoadSystem.has_road_between(state, HexCoord.new(2, 2), HexCoord.new(3, 2)),
		"command must connect adjacent hexes",
	)


func _set_grass_tile(state: GameState, q: int, r: int, height: int) -> void:
	state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", height, 1, 1))
