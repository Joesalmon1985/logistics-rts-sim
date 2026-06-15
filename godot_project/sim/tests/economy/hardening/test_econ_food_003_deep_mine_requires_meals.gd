extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_FOOD_003_deep_mine_requires_meals"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 133,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_flatten_grass_area(state, 1, 1, 5, 2)
	_connect_hq_to(state, 5, 2)
	_set_coal_deposit_tile(state, 5, 2)
	var building_id: int = EconomyInit._spawn_completed_building(
		state, "deep_coal_mine", 5, 2, "p1", EconomyState.get_config(state),
	)

	var building: Dictionary = EconomyState.get_building(state, building_id)
	var store: Dictionary = EconomyState.get_ware_store(state, int(building.get("store_id", 0)))
	EconomyState.inventory_add(store, "rations", 5)
	EconomyState.set_ware_store(state, int(building.get("store_id", 0)), store)

	state = runner.run_ticks(state, 3)
	building = EconomyState.get_building(state, building_id)
	assert_eq(str(building.get("paused_reason", "")), "food_missing", "deep mine must reject rations-only support")

	store = EconomyState.get_ware_store(state, int(building.get("store_id", 0)))
	EconomyState.inventory_add(store, "meals", 3)
	EconomyState.set_ware_store(state, int(building.get("store_id", 0)), store)

	state = runner.run_ticks(state, 15)
	store = EconomyState.get_ware_store(state, int(building.get("store_id", 0)))
	assert_true(int(store.get("wares", {}).get("coal", 0)) >= 1, "deep mine produces after meals delivered")


func _set_coal_deposit_tile(state: GameState, q: int, r: int, amount: int = 50) -> void:
	for offset in [[0, 0], [1, 0], [0, 1], [1, 1]]:
		var coord := HexCoord.new(q + int(offset[0]), r + int(offset[1]))
		var tile := HexTile.new("rock", 0, 1, 0)
		tile.deposit_type = "coal"
		tile.deposit_amount = amount
		tile.deposit_revealed = 1
		state.map.set_tile(coord, tile)


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
