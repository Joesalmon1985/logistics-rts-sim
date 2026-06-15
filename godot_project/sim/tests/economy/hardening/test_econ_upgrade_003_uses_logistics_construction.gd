extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_UPGRADE_003_uses_logistics_construction"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 153,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass_area(state, 1, 1, 7, 4)

	var sawmill_id: int = EconomyInit._spawn_completed_building(
		state, "sawmill", 5, 2, "p1", EconomyState.get_config(state),
	)
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var result: CommandResult = validator.validate_and_apply(
		state, UpgradeBuildingCommand.new("p1", 0, {"building_id": sawmill_id}),
	)
	assert_true(result.success, "upgrade command must start construction site")

	var sites: Array = EconomyState.all_construction_sites(state)
	assert_eq(sites.size(), 1)
	var site: Dictionary = sites[0]
	assert_eq(int(site.get("upgrade_source_id", 0)), sawmill_id, "upgrade must use construction/logistics site at same footprint")
	assert_eq(str(site.get("building_type", "")), "enhanced_sawmill")

	var sawmill: Dictionary = EconomyState.get_building(state, sawmill_id)
	assert_eq(str(sawmill.get("state", "")), EconomyConstants.BLD_UNDER_CONSTRUCTION)

	var started_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "upgrade_started" and int(event.payload.get("building_id", -1)) == sawmill_id:
			started_event = true
			break
	assert_true(started_event, "upgrade_started event expected")


func _flatten_grass_area(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))
