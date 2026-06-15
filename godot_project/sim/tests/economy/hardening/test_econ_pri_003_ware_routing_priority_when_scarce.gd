extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_PRI_003_ware_routing_priority_when_scarce"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 183,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_flatten_grass_area(state, 1, 1, 5, 2)
	var low_building: int = EconomyInit._spawn_completed_building(state, "sawmill", 3, 1, "p1", EconomyState.get_config(state))
	var high_building: int = EconomyInit._spawn_completed_building(state, "sawmill", 5, 2, "p1", EconomyState.get_config(state))
	_connect_road_chain(state, HexCoord.new(1, 1), HexCoord.new(3, 1))
	_connect_road_chain(state, HexCoord.new(1, 1), HexCoord.new(5, 2))

	var hq_store_id: int = int(EconomyState.get_building(state, int(EconomyState.get_meta_value(state, "hq_building_id", 0))).get("store_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(state, hq_store_id)
	hq_store["wares"] = {}
	hq_store["reserved"] = {}
	EconomyState.inventory_add(hq_store, "timber", 1)
	EconomyState.set_ware_store(state, hq_store_id, hq_store)

	var low_job: int = EconomyLogisticsSystem.request_delivery(state, "timber", 1, low_building, "building", "p1", 1)
	var high_job: int = EconomyLogisticsSystem.request_delivery(state, "timber", 1, high_building, "building", "p1", 10)
	assert_true(low_job > 0 and high_job > 0)

	var sorted: Array = EconomyPrioritySystem.sort_delivery_jobs(EconomyState.all_delivery_jobs(state))
	assert_eq(int(sorted[0].get("id", 0)), high_job, "higher ware priority job must sort first")

	state = runner.run_ticks(state, 20)
	var high_store: Dictionary = EconomyState.get_ware_store(
		state, int(EconomyState.get_building(state, high_building).get("store_id", 0)),
	)
	var low_store: Dictionary = EconomyState.get_ware_store(
		state, int(EconomyState.get_building(state, low_building).get("store_id", 0)),
	)
	assert_eq(int(high_store.get("wares", {}).get("timber", 0)), 1, "high priority destination receives scarce timber")
	assert_eq(int(low_store.get("wares", {}).get("timber", 0)), 0, "low priority destination must not receive timber first")


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
