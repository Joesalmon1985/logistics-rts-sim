extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_FOREST_004_low_density_blocks_timber"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 124,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var cfg: EconomyConfig = EconomyState.get_config(state)
	var min_density: int = cfg.int_val("tree_density_min_for_timber", 20)
	_flatten_grass_area(state, 1, 1, 5, 2)
	_connect_hq_to(state, 5, 2)
	_set_forest_tile(state, 5, 2, min_density - 5)
	var building_id: int = EconomyInit._spawn_completed_building(
		state, "woodcutter", 5, 2, "p1", cfg,
	)

	state = runner.run_ticks(state, 10)
	var building: Dictionary = EconomyState.get_building(state, building_id)
	assert_eq(str(building.get("paused_reason", "")), "input_missing")
	var store: Dictionary = EconomyState.get_ware_store(state, int(building.get("store_id", 0)))
	assert_eq(int(store.get("wares", {}).get("timber", 0)), 0, "low density must block timber output")

	var missing_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "input_missing" and int(event.payload.get("building_id", -1)) == building_id:
			missing_event = true
			break
	assert_true(missing_event, "input_missing event expected when forest too sparse")


func _set_forest_tile(state: GameState, q: int, r: int, density: int) -> void:
	var tile := HexTile.new("forest", 0, 1, 1)
	tile.tree_density = density
	tile.deposit_type = "forest"
	state.map.set_tile(HexCoord.new(q, r), tile)


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
