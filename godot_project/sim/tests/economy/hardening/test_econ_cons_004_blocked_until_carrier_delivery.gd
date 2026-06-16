extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_CONS_004_blocked_until_carrier_delivery"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 501,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass_area(state, 0, 0, 9, 9)
	_connect_roads(state, 2, 2, 6, 3)

	var result: Dictionary = EconomyConstructionSystem.create_site(
		state, "storehouse", 6, 3, "p1", 0, {"planks": 2, "builder_work": 3},
	)
	var site_id: int = int(result.get("site_id", 0))
	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	var initial_builder_work: int = int(site.get("builder_work_remaining", 0))
	assert_eq(initial_builder_work, 3)

	state = runner.run_ticks(state, 5)
	site = EconomyState.get_construction_site(state, site_id)
	assert_true(int(site.get("delivered", {}).get("planks", 0)) < 2, "materials incomplete early in scenario")
	assert_eq(int(site.get("builder_work_remaining", 0)), initial_builder_work, "builder_work before all materials")

	state = runner.run_ticks(state, 80)
	site = EconomyState.get_construction_site(state, site_id)
	assert_true(int(site.get("delivered", {}).get("planks", 0)) >= 2, "carrier must deliver planks")
	assert_true(
		int(site.get("builder_work_remaining", 0)) < initial_builder_work,
		"builder_work must progress after materials delivered",
	)


func _flatten_grass_area(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))


func _connect_roads(state: GameState, from_q: int, from_r: int, to_q: int, to_r: int) -> void:
	var current := HexCoord.new(from_q, from_r)
	var target := HexCoord.new(to_q, to_r)
	while current.q != target.q:
		var next := HexCoord.new(current.q + (1 if target.q > current.q else -1), current.r)
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
	while current.r != target.r:
		var next := HexCoord.new(current.q, current.r + (1 if target.r > current.r else -1))
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
