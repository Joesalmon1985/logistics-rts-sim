extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_STALL_001_stall_emits_diagnostic"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 210,
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
	EconomyInit._spawn_completed_building(state, "woodcutter", 4, 2, "p1", EconomyState.get_config(state))

	var threshold: int = EconomyState.get_config(state).int_val("stall_ticks_threshold", 50)
	state = runner.run_ticks(state, threshold + 2)

	var stall: Dictionary = state.diagnostics.get("stall", {})
	assert_true(int(stall.get("stall_ticks", 0)) >= threshold, "stall tick counter must exceed threshold")
	assert_true(str(stall.get("primary_reason", "")) != "", "stall diagnostic must record primary reason")

	var stalled_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "economy_stalled":
			stalled_event = true
			assert_true(int(event.payload.get("stall_ticks", 0)) >= threshold)
			break
	assert_true(stalled_event, "economy_stalled event expected when progress stalls")


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
