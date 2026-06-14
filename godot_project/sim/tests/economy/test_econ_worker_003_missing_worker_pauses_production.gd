extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_WORKER_003_missing_worker_pauses_production"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 47,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_clear_workers_of_type(state, "woodcutter")
	_set_forest_tile(state, 4, 2, 80)
	var building_id: int = EconomyInit._spawn_completed_building(
		state,
		"woodcutter",
		4,
		2,
		"p1",
		EconomyState.get_config(state),
	)

	state = runner.run_ticks(state, 2)
	var building: Dictionary = EconomyState.get_building(state, building_id)
	assert_eq(str(building.get("state", "")), EconomyConstants.BLD_PAUSED_MISSING_WORKER)
	assert_eq(str(building.get("paused_reason", "")), "missing_worker")

	var unavailable_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "worker_unavailable" and int(event.payload.get("building_id", -1)) == building_id:
			unavailable_event = true
			break
	assert_true(unavailable_event, "worker_unavailable event expected")


func _clear_workers_of_type(state: GameState, worker_type: String) -> void:
	var workers: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_WORKERS)
	for key in workers.keys():
		var worker: Dictionary = workers[key]
		if str(worker.get("worker_type", "")) == worker_type:
			workers.erase(key)


func _set_forest_tile(state: GameState, q: int, r: int, density: int) -> void:
	var tile := HexTile.new("forest", 0, 1, 1)
	tile.tree_density = density
	tile.deposit_type = "forest"
	state.map.set_tile(HexCoord.new(q, r), tile)
