extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_JOB_001_job_created_on_missing_input"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 401,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass_area(state, 1, 1, 6, 3)
	_connect_hq_to(state, 5, 2)

	var building_id: int = EconomyInit._spawn_completed_building(
		state, "sawmill", 5, 2, "p1", EconomyState.get_config(state),
	)
	var building: Dictionary = EconomyState.get_building(state, building_id)
	var store: Dictionary = EconomyState.get_ware_store(state, int(building.get("store_id", 0)))
	store["wares"] = {}
	store["reserved"] = {}
	EconomyState.set_ware_store(state, int(building.get("store_id", 0)), store)

	state = runner.tick(state)
	var saw_job: bool = false
	for job in EconomyState.all_delivery_jobs(state):
		if int(job.get("dest_entity_id", 0)) == building_id and str(job.get("ware_type", "")) == "timber":
			saw_job = true
			break
	assert_true(saw_job, "missing timber input must create delivery job")


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
