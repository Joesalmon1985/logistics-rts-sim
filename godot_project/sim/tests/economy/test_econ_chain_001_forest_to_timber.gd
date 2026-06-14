extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_CHAIN_001_forest_to_timber"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 83,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var forest_q: int = 5
	var forest_r: int = 2
	_flatten_grass_area(state, 1, 1, 5, 2)
	_connect_hq_to(state, forest_q, forest_r)
	_set_forest_tile(state, forest_q, forest_r, 80)
	var building_id: int = EconomyInit._spawn_completed_building(
		state,
		"woodcutter",
		forest_q,
		forest_r,
		"p1",
		EconomyState.get_config(state),
	)

	state = runner.run_ticks(state, 10)
	var building: Dictionary = EconomyState.get_building(state, building_id)
	var store: Dictionary = EconomyState.get_ware_store(state, int(building.get("store_id", 0)))
	assert_true(int(store.get("wares", {}).get("timber", 0)) >= 1, "forest chain must output timber")

	var produced_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "production_completed" and str(event.payload.get("ware", "")) == "timber":
			produced_event = true
			break
	assert_true(produced_event, "production_completed event for timber expected")


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
