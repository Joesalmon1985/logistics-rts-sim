extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_JOB_003_job_lifecycle_transitions"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 41,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var sawmill_q: int = 3
	var sawmill_r: int = 1
	_flatten_grass_area(state, 1, 1, 4, 2)
	var building_id: int = EconomyInit._spawn_completed_building(
		state,
		"sawmill",
		sawmill_q,
		sawmill_r,
		"p1",
		EconomyState.get_config(state),
	)
	_connect_road_chain(state, HexCoord.new(1, 1), HexCoord.new(3, 1))

	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	var hq_store_id: int = int(hq.get("store_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(state, hq_store_id)
	EconomyState.inventory_add(hq_store, "timber", 5)
	EconomyState.set_ware_store(state, hq_store_id, hq_store)

	var job_id: int = EconomyLogisticsSystem.request_delivery(state, "timber", 1, building_id, "building", "p1", 0)
	assert_true(job_id > 0, "delivery job must be created")

	var job: Dictionary = EconomyState.get_delivery_job(state, job_id)
	assert_eq(str(job.get("status", "")), EconomyConstants.JOB_CREATED)

	state = runner.run_ticks(state, 1)
	job = EconomyState.get_delivery_job(state, job_id)
	var status_after_first_tick: String = str(job.get("status", ""))
	assert_true(
		status_after_first_tick in [
			EconomyConstants.JOB_IN_TRANSIT,
			EconomyConstants.JOB_WAITING_CARRIER,
			EconomyConstants.JOB_WAITING_WARE,
		],
		"job must advance from created (got %s)" % status_after_first_tick,
	)

	state = runner.run_ticks(state, 20)
	job = EconomyState.get_delivery_job(state, job_id)
	var final_status: String = str(job.get("status", ""))
	if final_status == "":
		final_status = EconomyConstants.JOB_DELIVERED
	assert_eq(final_status, EconomyConstants.JOB_DELIVERED, "job must reach delivered when route and wares exist")

	var delivered_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "ware_delivered" and int(event.payload.get("job_id", -1)) == job_id:
			delivered_event = true
			break
	assert_true(delivered_event, "ware_delivered event expected for completed job")


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
