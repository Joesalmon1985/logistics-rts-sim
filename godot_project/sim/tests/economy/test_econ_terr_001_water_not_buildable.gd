extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_TERR_001_water_not_buildable"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 11,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_set_water_tile(state, 4, 4)
	var check: Dictionary = EconomyMapRules.can_place_building(state, "storehouse", 4, 4, "p1")
	assert_false(check.get("ok", true), "water hex must reject building placement")
	assert_eq(str(check.get("reason", "")), "NOT_BUILDABLE")

	var cmd := PlaceBuildingPlanCommand.new("p1", 0, {
		"building_type": "storehouse",
		"q": 4,
		"r": 4,
		"player_id": "p1",
	})
	var result: CommandResult = cmd.validate(state)
	assert_false(result.success, "PlaceBuildingPlanCommand must reject water tile")
	assert_eq(result.error_code, "NOT_BUILDABLE")


func _set_water_tile(state: GameState, q: int, r: int) -> void:
	var coord := HexCoord.new(q, r)
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var tdef: Dictionary = cfg.get_terrain_def("water")
	state.map.set_tile(
		coord,
		HexTile.new("water", 0, int(tdef.get("passability", 0)), int(tdef.get("buildability", 0))),
	)
