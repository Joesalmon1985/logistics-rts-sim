extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_PRI_002_production_priority_order"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 182,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 14,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_clear_workers_of_type(state, "woodcutter")
	_flatten_grass_area(state, 1, 1, 7, 3)
	_set_forest_tile(state, 4, 2, 80)
	_set_forest_tile(state, 6, 2, 80)
	_connect_hq_to(state, 4, 2)
	_connect_hq_to(state, 6, 2)

	var low_id: int = EconomyInit._spawn_completed_building(state, "woodcutter", 4, 2, "p1", EconomyState.get_config(state))
	var high_id: int = EconomyInit._spawn_completed_building(state, "woodcutter", 6, 2, "p1", EconomyState.get_config(state))
	EconomyPrioritySystem.set_production_priority(state, low_id, 1)
	EconomyPrioritySystem.set_production_priority(state, high_id, 10)

	var worker_id: int = state.entity_ids.allocate()
	var hq_store_id: int = int(EconomyState.get_building(state, int(EconomyState.get_meta_value(state, "hq_building_id", 0))).get("store_id", 0))
	EconomyState.add_worker(state, {
		"id": worker_id,
		"worker_type": "woodcutter",
		"player_id": "p1",
		"state": EconomyConstants.WORKER_IDLE,
		"home_store_id": hq_store_id,
		"assigned_building_id": 0,
		"has_tool": 0,
	})

	var sorted: Array = EconomyPrioritySystem.sort_buildings(EconomyState.all_buildings(state))
	var woodcutters: Array = []
	for building in sorted:
		if str(building.get("building_type", "")) == "woodcutter":
			woodcutters.append(building)
	assert_eq(int(woodcutters[0].get("id", 0)), high_id, "higher production priority must sort first")

	state = runner.run_ticks(state, 2)
	var high_building: Dictionary = EconomyState.get_building(state, high_id)
	var low_building: Dictionary = EconomyState.get_building(state, low_id)
	assert_true(high_building.get("assigned_workers", []).size() >= 1, "high priority building receives scarce worker")
	assert_eq(low_building.get("assigned_workers", []).size(), 0, "low priority building waits when worker scarce")


func _clear_workers_of_type(state: GameState, worker_type: String) -> void:
	var workers: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_WORKERS)
	for key in workers.keys():
		if str(workers[key].get("worker_type", "")) == worker_type:
			workers.erase(key)


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
