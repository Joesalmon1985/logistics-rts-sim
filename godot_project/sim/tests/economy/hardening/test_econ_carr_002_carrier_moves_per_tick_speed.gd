extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_CARR_002_carrier_moves_per_tick_speed"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 192,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 14,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_flatten_grass_area(state, 1, 1, 6, 2)
	var building_id: int = EconomyInit._spawn_completed_building(state, "sawmill", 6, 1, "p1", EconomyState.get_config(state))
	_connect_road_chain(state, HexCoord.new(1, 1), HexCoord.new(6, 1))

	var hq_store_id: int = int(EconomyState.get_building(state, int(EconomyState.get_meta_value(state, "hq_building_id", 0))).get("store_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(state, hq_store_id)
	hq_store["wares"] = {}
	hq_store["reserved"] = {}
	EconomyState.inventory_add(hq_store, "timber", 5)
	EconomyState.set_ware_store(state, hq_store_id, hq_store)

	EconomyLogisticsSystem.request_delivery(state, "timber", 1, building_id, "building", "p1", 0)
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var ticks_per_edge: int = cfg.int_val("carrier_speed_ticks_per_edge", 1)

	state = runner.run_ticks(state, 1)
	var carrier: Dictionary = _first_busy_carrier(state)
	assert_false(carrier.is_empty(), "carrier must be assigned to a delivery job")
	var start_index: int = int(carrier.get("route_index", 0))
	var start_node: int = int(carrier.get("current_node_id", 0))

	state = runner.run_ticks(state, ticks_per_edge)
	carrier = _find_carrier(state, int(carrier.get("id", 0)))
	assert_true(
		int(carrier.get("route_index", 0)) > start_index or int(carrier.get("current_node_id", 0)) != start_node,
		"carrier movement must advance per configured ticks-per-edge",
	)


func _first_busy_carrier(state: GameState) -> Dictionary:
	for carrier in EconomyState.all_carriers(state):
		if str(carrier.get("state", "")) in [EconomyConstants.CARRIER_ASSIGNED, EconomyConstants.CARRIER_MOVING]:
			return carrier
	return {}


func _find_carrier(state: GameState, carrier_id: int) -> Dictionary:
	for carrier in EconomyState.all_carriers(state):
		if int(carrier.get("id", 0)) == carrier_id:
			return carrier
	return {}


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
