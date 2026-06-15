extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_ROAD_005_carrier_moves_along_road"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 165,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_flatten_grass_area(state, 1, 1, 4, 2)
	var building_id: int = EconomyInit._spawn_completed_building(
		state, "sawmill", 3, 1, "p1", EconomyState.get_config(state),
	)
	_connect_road_chain(state, HexCoord.new(1, 1), HexCoord.new(3, 1))

	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq_store_id: int = int(EconomyState.get_building(state, hq_id).get("store_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(state, hq_store_id)
	EconomyState.inventory_add(hq_store, "timber", 3)
	EconomyState.set_ware_store(state, hq_store_id, hq_store)

	var job_id: int = EconomyLogisticsSystem.request_delivery(state, "timber", 1, building_id, "building", "p1", 0)
	assert_true(job_id > 0)

	state = runner.run_ticks(state, 1)
	var carrier_moving: bool = false
	for carrier in EconomyState.all_carriers(state):
		if str(carrier.get("state", "")) in [EconomyConstants.CARRIER_ASSIGNED, EconomyConstants.CARRIER_MOVING]:
			assert_true(carrier.get("route_node_ids", []).size() >= 2, "carrier must follow road node route")
			carrier_moving = true
	assert_true(carrier_moving, "carrier must enter transit on road network")

	var moved_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "ware_moved":
			moved_event = true
			break
	assert_true(moved_event, "ware_moved event expected while carrier travels road edges")


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
