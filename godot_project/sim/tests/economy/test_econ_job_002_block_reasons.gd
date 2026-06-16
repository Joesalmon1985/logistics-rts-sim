extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_JOB_002_block_reasons"


func _run_body() -> void:
	_test_no_route_block_reason()
	_test_no_carrier_block_reason()


func _test_no_route_block_reason() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 402,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass_area(state, 1, 1, 6, 3)

	var isolated_id: int = EconomyInit._spawn_completed_building(
		state, "sawmill", 6, 3, "p1", EconomyState.get_config(state),
	)
	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq_store_id: int = int(EconomyState.get_building(state, hq_id).get("store_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(state, hq_store_id)
	EconomyState.inventory_add(hq_store, "timber", 5)
	EconomyState.set_ware_store(state, hq_store_id, hq_store)

	var job_id: int = state.entity_ids.allocate()
	var job := {
		"id": job_id,
		"status": EconomyConstants.JOB_CREATED,
		"ware_type": "timber",
		"amount": 1,
		"source_store_id": hq_store_id,
		"dest_store_id": int(EconomyState.get_building(state, isolated_id).get("store_id", 0)),
		"dest_entity_id": isolated_id,
		"dest_entity_type": "building",
		"player_id": "p1",
		"priority": 0,
		"carrier_id": 0,
		"block_reason": "",
	}
	EconomyState.add_delivery_job(state, job)

	state = runner.tick(state)
	job = EconomyState.get_delivery_job(state, job_id)
	assert_eq(str(job.get("status", "")), EconomyConstants.JOB_WAITING_ROUTE)
	assert_eq(str(job.get("block_reason", "")), "no_route")


func _test_no_carrier_block_reason() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 403,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
		"starting_workers_per_type": {"carrier": 0},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass_area(state, 1, 1, 6, 3)
	_connect_hq_to(state, 5, 2)

	var carriers: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_CARRIERS)
	for key in carriers.keys():
		carriers.erase(key)

	var building_id: int = EconomyInit._spawn_completed_building(
		state, "sawmill", 5, 2, "p1", EconomyState.get_config(state),
	)
	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(
		state, int(EconomyState.get_building(state, hq_id).get("store_id", 0)),
	)
	EconomyState.inventory_add(hq_store, "timber", 5)
	EconomyState.set_ware_store(state, int(EconomyState.get_building(state, hq_id).get("store_id", 0)), hq_store)

	var job_id: int = EconomyLogisticsSystem.request_delivery(state, "timber", 1, building_id, "building", "p1", 0)
	state = runner.tick(state)
	var job: Dictionary = EconomyState.get_delivery_job(state, job_id)
	assert_eq(str(job.get("status", "")), EconomyConstants.JOB_WAITING_CARRIER)
	assert_eq(str(job.get("block_reason", "")), "no_carrier")


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
