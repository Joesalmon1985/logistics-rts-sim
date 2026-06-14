extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_MINE_001_requires_matching_deposit"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 67,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_set_rock_without_deposit(state, 5, 2)
	var check: Dictionary = EconomyMapRules.can_place_building(state, "coal_mine", 5, 2, "p1")
	assert_false(check.get("ok", true), "coal mine must require coal deposit")
	assert_eq(str(check.get("reason", "")), "DEPOSIT_REQUIRED")

	var cmd := PlaceBuildingPlanCommand.new("p1", 0, {
		"building_type": "coal_mine",
		"q": 5,
		"r": 2,
		"player_id": "p1",
	})
	assert_false(cmd.validate(state).success, "command must reject missing deposit")


func _set_rock_without_deposit(state: GameState, q: int, r: int) -> void:
	for offset in [[0, 0], [1, 0], [0, 1], [1, 1]]:
		var coord := HexCoord.new(q + int(offset[0]), r + int(offset[1]))
		state.map.set_tile(coord, HexTile.new("rock", 0, 1, 1))
