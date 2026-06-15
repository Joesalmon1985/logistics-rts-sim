extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_FOREST_003_forester_increases_density"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 123,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var target_q: int = 4
	var target_r: int = 2
	var forester_q: int = 5
	var forester_r: int = 2
	var start_density: int = 40
	_set_forest_tile(state, target_q, target_r, start_density)
	_set_forest_tile(state, forester_q, forester_r, 50)
	var building_id: int = EconomyInit._spawn_completed_building(
		state, "forester", forester_q, forester_r, "p1", EconomyState.get_config(state),
	)

	var forester_worker_id: int = 0
	for worker in EconomyState.all_workers(state):
		if str(worker.get("worker_type", "")) == "forester":
			forester_worker_id = int(worker.get("id", 0))
			break
	assert_true(forester_worker_id > 0, "forester worker must exist in HQ pool")

	var building: Dictionary = EconomyState.get_building(state, building_id)
	building["assigned_workers"] = [forester_worker_id]
	building["state"] = EconomyConstants.BLD_ACTIVE
	building["production_progress"] = 4
	EconomyState.add_building(state, building)
	var worker: Dictionary = _find_worker(state, forester_worker_id)
	worker["state"] = EconomyConstants.WORKER_ASSIGNED
	worker["assigned_building_id"] = building_id
	worker["has_tool"] = 1
	EconomyState.get_bucket(state, EconomyConstants.BUCKET_WORKERS)[str(forester_worker_id)] = worker

	EconomyProductionSystem.tick(state)
	var tile: HexTile = state.map.get_tile(HexCoord.new(target_q, target_r))
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var increment: int = cfg.int_val("tree_density_increment_per_cycle", 3)
	assert_true(tile.tree_density >= start_density + increment, "forester must increase adjacent forest density")

	var renewed_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "forest_renewed" and int(event.payload.get("q", -1)) == target_q:
			renewed_event = true
			break
	assert_true(renewed_event, "forest_renewed event expected")


func _find_worker(state: GameState, worker_id: int) -> Dictionary:
	for w in EconomyState.all_workers(state):
		if int(w.get("id", 0)) == worker_id:
			return w
	return {}


func _set_forest_tile(state: GameState, q: int, r: int, density: int) -> void:
	var tile := HexTile.new("forest", 0, 1, 1)
	tile.tree_density = density
	tile.deposit_type = "forest"
	state.map.set_tile(HexCoord.new(q, r), tile)
