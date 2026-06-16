extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_TOOL_004_shortage_in_diagnostics"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 114,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq_store_id: int = int(EconomyState.get_building(state, hq_id).get("store_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(state, hq_store_id)
	hq_store["wares"] = {}
	hq_store["reserved"] = {}
	EconomyState.set_ware_store(state, hq_store_id, hq_store)

	_flatten_grass_area(state, 1, 1, 5, 2)
	_connect_hq_to(state, 5, 2)
	_set_forest_tile(state, 5, 2, 80)
	var building_id: int = EconomyInit._spawn_completed_building(
		state, "woodcutter", 5, 2, "p1", EconomyState.get_config(state),
	)

	state = runner.run_ticks(state, 3)
	var building: Dictionary = EconomyState.get_building(state, building_id)
	assert_eq(building.get("assigned_workers", []).size(), 0, "tool shortage blocks assignment")

	var tool_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "tool_missing" and int(event.payload.get("building_id", -1)) == building_id:
			tool_event = true
			break
	assert_true(tool_event, "tool_missing event expected")

	state = runner.tick(state)
	var production: Array = state.diagnostics.get("production", [])
	var saw_blocked: bool = false
	for record in production:
		if int(record.get("building_id", 0)) == building_id and int(record.get("workers", 0)) == 0:
			saw_blocked = true
			break
	assert_true(saw_blocked, "production diagnostics must show building stalled without tools")

	var shortages: Dictionary = state.diagnostics.get("shortages", {})
	assert_true(int(shortages.get("tools", 0)) >= 1, "tool shortage counter must increment")


func _set_forest_tile(state: GameState, q: int, r: int, density: int) -> void:
	var tile := HexTile.new("forest", 0, 1, 1)
	tile.tree_density = density
	tile.deposit_type = "forest"
	state.map.set_tile(HexCoord.new(q, r), tile)


func _flatten_grass_area(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))


func _connect_hq_to(state: GameState, q: int, r: int) -> void:
	var hq_q: int = int(EconomyState.get_meta_value(state, "hq_position_q", 0))
	var hq_r: int = int(EconomyState.get_meta_value(state, "hq_position_r", 0))
	var current := HexCoord.new(hq_q, hq_r)
	var target := HexCoord.new(q, r)
	while current.q != target.q:
		var next := HexCoord.new(current.q + (1 if target.q > current.q else -1), current.r)
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
	while current.r != target.r:
		var next := HexCoord.new(current.q, current.r + (1 if target.r > current.r else -1))
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
