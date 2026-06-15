extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_WORKER_004_tool_required_before_assign"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 104,
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
	hq_store["wares"] = {"tools": 0}
	hq_store["reserved"] = {}
	EconomyState.set_ware_store(state, int(hq.get("store_id", 0)), hq_store)

	_flatten_grass_area(state, 1, 1, 5, 2)
	_connect_hq_to(state, 5, 2)
	_set_forest_tile(state, 5, 2, 80)
	var building_id: int = EconomyInit._spawn_completed_building(
		state, "woodcutter", 5, 2, "p1", EconomyState.get_config(state),
	)

	state = runner.run_ticks(state, 2)
	var building: Dictionary = EconomyState.get_building(state, building_id)
	assert_eq(building.get("assigned_workers", []).size(), 0, "no assignment without tools")

	hq_store = EconomyState.get_ware_store(state, int(hq.get("store_id", 0)))
	EconomyState.inventory_add(hq_store, "tools", 2)
	EconomyState.set_ware_store(state, int(hq.get("store_id", 0)), hq_store)

	state = runner.run_ticks(state, 2)
	building = EconomyState.get_building(state, building_id)
	assert_true(building.get("assigned_workers", []).size() >= 1, "worker assigns after tool becomes available")

	var worker_id: int = int(building.get("assigned_workers", [])[0])
	var worker: Dictionary = _find_worker(state, worker_id)
	assert_eq(int(worker.get("has_tool", 0)), 1, "assigned worker must consume tool before assignment")

	var consumed_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "tool_consumed" and int(event.payload.get("worker_id", -1)) == worker_id:
			consumed_event = true
			break
	assert_true(consumed_event, "tool_consumed event expected before assignment")


func _find_worker(state: GameState, worker_id: int) -> Dictionary:
	for worker in EconomyState.all_workers(state):
		if int(worker.get("id", 0)) == worker_id:
			return worker
	return {}


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
