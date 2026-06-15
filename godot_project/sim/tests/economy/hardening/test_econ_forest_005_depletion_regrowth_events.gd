extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_FOREST_005_depletion_regrowth_events"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 125,
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
	var decrement: int = cfg.int_val("tree_density_decrement_per_cycle", 5)
	var start_density: int = min_density + decrement
	_flatten_grass_area(state, 1, 1, 5, 2)
	_connect_hq_to(state, 5, 2)
	_set_forest_tile(state, 5, 2, start_density)
	EconomyInit._spawn_completed_building(state, "woodcutter", 5, 2, "p1", cfg)

	state = runner.run_ticks(state, 10)
	var depleted_event: bool = false
	var density_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "forest_depleted":
			depleted_event = true
		if str(event.type) == "tree_density_changed":
			density_event = true
	assert_true(density_event, "tree_density_changed event expected during depletion")
	assert_true(depleted_event, "forest_depleted event expected when density falls below threshold")

	state = runner.tick(state)
	var summary: Variant = EconomyState.get_meta_value(state, "forest_summary", {})
	assert_true(typeof(summary) == TYPE_DICTIONARY, "forest summary tracked in meta for diagnostics overlay")
	assert_true(int(summary.get("depleted_hexes", 0)) >= 1, "depleted hex count must reflect forest depletion")


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
