extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_WORKER_001_building_requests_worker_type"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 101,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var forest_q: int = 5
	var forest_r: int = 2
	_flatten_grass_area(state, 1, 1, 5, 2)
	_connect_hq_to(state, forest_q, forest_r)
	_set_forest_tile(state, forest_q, forest_r, 80)
	var building_id: int = EconomyInit._spawn_completed_building(
		state,
		"woodcutter",
		forest_q,
		forest_r,
		"p1",
		EconomyState.get_config(state),
	)

	var building: Dictionary = EconomyState.get_building(state, building_id)
	var bdef: Dictionary = EconomyState.get_config(state).get_building_def("woodcutter")
	assert_eq(str(building.get("worker_type", "")), str(bdef.get("worker_type", "")), "building must request configured worker type")

	state = runner.run_ticks(state, 2)
	building = EconomyState.get_building(state, building_id)
	assert_true(building.get("assigned_workers", []).size() >= 1, "woodcutter worker must be assigned to fill slot")

	var assigned_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "worker_assigned" and int(event.payload.get("building_id", -1)) == building_id:
			assert_eq(str(event.payload.get("worker_type", "")), "woodcutter")
			assigned_event = true
			break
	assert_true(assigned_event, "worker_assigned event must name requested worker type")


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
