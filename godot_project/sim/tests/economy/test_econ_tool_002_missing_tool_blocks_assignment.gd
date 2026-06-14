extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_TOOL_002_missing_tool_blocks_assignment"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 53,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	var hq_store: Dictionary = EconomyState.get_ware_store(state, int(hq.get("store_id", 0)))
	hq_store["wares"] = {}
	hq_store["reserved"] = {}
	EconomyState.set_ware_store(state, int(hq.get("store_id", 0)), hq_store)

	var building_q: int = 5
	var building_r: int = 2
	_flatten_grass_area(state, 1, 1, 5, 2)
	_connect_hq_to(state, building_q, building_r)
	_set_forest_tile(state, building_q, building_r, 80)
	var building_id: int = EconomyInit._spawn_completed_building(
		state,
		"woodcutter",
		building_q,
		building_r,
		"p1",
		EconomyState.get_config(state),
	)

	state = runner.run_ticks(state, 2)
	var building: Dictionary = EconomyState.get_building(state, building_id)
	assert_eq(building.get("assigned_workers", []).size(), 0, "worker must not assign without tools")

	var tool_missing_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "tool_missing" and int(event.payload.get("building_id", -1)) == building_id:
			tool_missing_event = true
			break
	assert_true(tool_missing_event, "tool_missing event expected when HQ lacks tools")


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
		_flatten_grass_area(state, current.q, current.r, current.q, current.r)
		_flatten_grass_area(state, next.q, next.r, next.q, next.r)
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
	while current.r != target.r:
		var next := HexCoord.new(current.q, current.r + (1 if target.r > current.r else -1))
		_flatten_grass_area(state, current.q, current.r, current.q, current.r)
		_flatten_grass_area(state, next.q, next.r, next.q, next.r)
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
