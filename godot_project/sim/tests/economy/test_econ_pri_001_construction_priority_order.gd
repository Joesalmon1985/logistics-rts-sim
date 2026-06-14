extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_PRI_001_construction_priority_order"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 79,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_flatten_grass_area(state, 0, 0, 7, 7)
	var low_site: Dictionary = EconomyConstructionSystem.create_site(
		state,
		"storehouse",
		4,
		3,
		"p1",
		0,
		{"timber": 5, "builder_work": 0},
	)
	var high_site: Dictionary = EconomyConstructionSystem.create_site(
		state,
		"storehouse",
		6,
		3,
		"p1",
		0,
		{"timber": 5, "builder_work": 0},
	)
	assert_true(low_site.get("ok", false), "low priority site must be created")
	assert_true(high_site.get("ok", false), "high priority site must be created")
	var low_id: int = int(low_site.get("site_id", 0))
	var high_id: int = int(high_site.get("site_id", 0))
	EconomyPrioritySystem.set_construction_priority(state, low_id, 1)
	EconomyPrioritySystem.set_construction_priority(state, high_id, 10)

	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	var hq_store: Dictionary = EconomyState.get_ware_store(state, int(hq.get("store_id", 0)))
	hq_store["wares"] = {"timber": 5}
	hq_store["reserved"] = {}
	EconomyState.set_ware_store(state, int(hq.get("store_id", 0)), hq_store)

	var sorted: Array = EconomyPrioritySystem.sort_construction_sites(EconomyState.all_construction_sites(state))
	assert_eq(int(sorted[0].get("id", 0)), high_id, "higher priority site must sort first")

	EconomyConstructionSystem.deliver_to_site(state, high_id, "timber", 5)
	var high_after: Dictionary = EconomyState.get_construction_site(state, high_id)
	var low_after: Dictionary = EconomyState.get_construction_site(state, low_id)
	assert_eq(int(high_after.get("delivered", {}).get("timber", 0)), 5)
	assert_eq(int(low_after.get("delivered", {}).get("timber", 0)), 0, "lower priority site must not receive scarce timber first")


func _flatten_grass_area(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))
