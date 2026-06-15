extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_UPGRADE_005_upgrade_emits_events"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 155,
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
	var upgrade_result: Dictionary = EconomyUpgradeSystem.start_upgrade(state, sawmill_id, "p1")
	var site_id: int = int(upgrade_result.get("site_id", 0))
	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	for ware_type in site.get("required", {}):
		if ware_type == "builder_work":
			continue
		EconomyConstructionSystem.deliver_to_site(state, site_id, str(ware_type), int(site["required"][ware_type]))
	site = EconomyState.get_construction_site(state, site_id)
	site["builder_work_remaining"] = 0
	EconomyState.add_construction_site(state, site)
	EconomyConstructionSystem.tick(state)

	var started: bool = false
	var completed: bool = false
	for event in state.event_log.events:
		if str(event.type) == "upgrade_started" and int(event.payload.get("building_id", -1)) == sawmill_id:
			started = true
		if str(event.type) == "upgrade_completed" and int(event.payload.get("building_id", -1)) == sawmill_id:
			completed = true
	assert_true(started, "upgrade_started event expected")
	assert_true(completed, "upgrade_completed event expected after site finishes")


func _flatten_grass_area(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))
