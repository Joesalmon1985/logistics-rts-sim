extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_CONS_003_completion_spawns_building"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 73,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_flatten_grass_area(state, 1, 1, 7, 4)
	var buildings_before: int = EconomyState.all_buildings(state).size()
	var result: Dictionary = EconomyConstructionSystem.create_site(
		state,
		"storehouse",
		5,
		2,
		"p1",
		0,
		{"builder_work": 1},
	)
	assert_true(result.get("ok", false), "construction site must be created")
	var site_id: int = int(result.get("site_id", 0))

	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	site["builder_work_remaining"] = 1
	EconomyState.add_construction_site(state, site)

	state = runner.tick(state)
	site = EconomyState.get_construction_site(state, site_id)
	assert_eq(str(site.get("state", "")), EconomyConstants.CONS_COMPLETED)
	assert_true(EconomyState.all_buildings(state).size() > buildings_before, "completion must spawn a building")

	var completed_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "construction_completed" and int(event.payload.get("site_id", -1)) == site_id:
			completed_event = true
			break
	assert_true(completed_event, "construction_completed event expected")


func _flatten_grass_area(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))
