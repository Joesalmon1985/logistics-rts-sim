extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_EVT_018_tree_density_changed_event"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 218,
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
	var start_density: int = 80
	_flatten_grass_area(state, 1, 1, 5, 2)
	_connect_hq_to(state, forest_q, forest_r)
	_set_forest_tile(state, forest_q, forest_r, start_density)
	EconomyInit._spawn_completed_building(state, "woodcutter", forest_q, forest_r, "p1", EconomyState.get_config(state))

	state = runner.run_ticks(state, 10)
	var tile: HexTile = state.map.get_tile(HexCoord.new(forest_q, forest_r))
	assert_true(tile.tree_density < start_density, "production must reduce tree density")

	var density_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "tree_density_changed":
			assert_eq(int(event.payload.get("q", -1)), forest_q)
			assert_eq(int(event.payload.get("r", -1)), forest_r)
			assert_true(int(event.payload.get("tree_density", -1)) < start_density)
			density_event = true
			break
	assert_true(density_event, "tree_density_changed event must report new density")


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
