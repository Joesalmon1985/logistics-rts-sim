extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_WORKER_002_worker_supplied_from_storehouse"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 102,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 14,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_clear_workers_of_type(state, "woodcutter")
	_flatten_grass_area(state, 1, 1, 8, 4)

	var store_id: int = EconomyInit._spawn_completed_building(state, "storehouse", 3, 2, "p1", EconomyState.get_config(state))
	var store_building: Dictionary = EconomyState.get_building(state, store_id)
	var storehouse_store_id: int = int(store_building.get("store_id", 0))
	var storehouse_store: Dictionary = EconomyState.get_ware_store(state, storehouse_store_id)
	EconomyState.inventory_add(storehouse_store, "tools", 5)
	EconomyState.set_ware_store(state, storehouse_store_id, storehouse_store)

	var worker_id: int = state.entity_ids.allocate()
	EconomyState.add_worker(state, {
		"id": worker_id,
		"worker_type": "woodcutter",
		"player_id": "p1",
		"state": EconomyConstants.WORKER_IDLE,
		"home_store_id": storehouse_store_id,
		"assigned_building_id": 0,
		"has_tool": 0,
	})

	_connect_road_chain(state, HexCoord.new(1, 1), HexCoord.new(3, 2))
	_connect_road_chain(state, HexCoord.new(3, 2), HexCoord.new(6, 2))
	_set_forest_tile(state, 6, 2, 80)
	var woodcutter_id: int = EconomyInit._spawn_completed_building(
		state, "woodcutter", 6, 2, "p1", EconomyState.get_config(state),
	)

	state = runner.run_ticks(state, 3)
	var woodcutter: Dictionary = EconomyState.get_building(state, woodcutter_id)
	assert_true(woodcutter.get("assigned_workers", []).has(worker_id), "storehouse worker must supply remote woodcutter")

	var worker: Dictionary = _find_worker(state, worker_id)
	assert_eq(int(worker.get("home_store_id", 0)), storehouse_store_id, "assigned worker must originate from storehouse pool")
	assert_eq(str(worker.get("state", "")), EconomyConstants.WORKER_ASSIGNED)


func _clear_workers_of_type(state: GameState, worker_type: String) -> void:
	var workers: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_WORKERS)
	for key in workers.keys():
		if str(workers[key].get("worker_type", "")) == worker_type:
			workers.erase(key)


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


func _connect_road_chain(state: GameState, from_hex: HexCoord, to_hex: HexCoord) -> void:
	var current := from_hex
	var guard: int = 0
	while (current.q != to_hex.q or current.r != to_hex.r) and guard < 64:
		guard += 1
		var best_neighbor: HexCoord = current
		var best_dist: int = current.distance(to_hex)
		for neighbour in current.neighbors():
			var dist: int = neighbour.distance(to_hex)
			if dist < best_dist:
				best_dist = dist
				best_neighbor = neighbour
		if best_neighbor.q == current.q and best_neighbor.r == current.r:
			break
		EconomyRoadSystem.place_road(state, current, best_neighbor, "p1")
		current = best_neighbor
