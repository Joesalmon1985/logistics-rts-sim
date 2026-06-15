extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_CARR_001_carrier_delivers_one_ware"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 191,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_flatten_grass_area(state, 1, 1, 4, 2)
	var building_id: int = EconomyInit._spawn_completed_building(state, "sawmill", 3, 1, "p1", EconomyState.get_config(state))
	_connect_road_chain(state, HexCoord.new(1, 1), HexCoord.new(3, 1))

	var hq_store_id: int = int(EconomyState.get_building(state, int(EconomyState.get_meta_value(state, "hq_building_id", 0))).get("store_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(state, hq_store_id)
	EconomyState.inventory_add(hq_store, "timber", 5)
	EconomyState.set_ware_store(state, hq_store_id, hq_store)

	var job_id: int = EconomyLogisticsSystem.request_delivery(state, "timber", 1, building_id, "building", "p1", 0)
	state = runner.run_ticks(state, 20)

	var building: Dictionary = EconomyState.get_building(state, building_id)
	var store: Dictionary = EconomyState.get_ware_store(state, int(building.get("store_id", 0)))
	assert_eq(int(store.get("wares", {}).get("timber", 0)), 1, "carrier delivers exactly one ware unit")

	for carrier in EconomyState.all_carriers(state):
		if str(carrier.get("state", "")) == EconomyConstants.CARRIER_IDLE:
			assert_eq(str(carrier.get("ware_type", "")), "", "carrier carries one ware type at a time then clears load")
			break

	var delivered_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "ware_delivered" and str(event.payload.get("ware", "")) == "timber":
			assert_eq(int(event.payload.get("amount", 0)), 1)
			delivered_event = true
			break
	assert_true(delivered_event, "ware_delivered event for single timber unit expected")


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
