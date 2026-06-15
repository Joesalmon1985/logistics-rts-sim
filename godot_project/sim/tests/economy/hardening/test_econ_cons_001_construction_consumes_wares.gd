extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_CONS_001_construction_consumes_wares"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 201,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass_area(state, 1, 1, 7, 4)

	var result: Dictionary = EconomyConstructionSystem.create_site(
		state, "storehouse", 5, 2, "p1", 0, {"timber": 3, "builder_work": 2},
	)
	var site_id: int = int(result.get("site_id", 0))
	EconomyConstructionSystem.deliver_to_site(state, site_id, "timber", 3)

	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	assert_eq(int(site.get("delivered", {}).get("timber", 0)), 3, "delivered wares must accumulate on site")
	assert_eq(int(site.get("builder_work_remaining", 0)), 2)

	state = runner.tick(state)
	site = EconomyState.get_construction_site(state, site_id)
	assert_eq(int(site.get("builder_work_remaining", 0)), 1, "builder work must be consumed each active tick")

	var progress_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "construction_progressed" and int(event.payload.get("site_id", -1)) == site_id:
			progress_event = true
			break
	assert_true(progress_event, "construction_progressed event expected while consuming builder work")


func _flatten_grass_area(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))
