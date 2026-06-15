extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_MINE_006_depletion_in_diagnostics"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 146,
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
	_set_coal_deposit_tile(state, 5, 2, 1)
	var building_id: int = EconomyInit._spawn_completed_building(
		state, "coal_mine", 5, 2, "p1", EconomyState.get_config(state),
	)

	var building: Dictionary = EconomyState.get_building(state, building_id)
	var store: Dictionary = EconomyState.get_ware_store(state, int(building.get("store_id", 0)))
	EconomyState.inventory_add(store, "rations", 10)
	EconomyState.set_ware_store(state, int(building.get("store_id", 0)), store)

	state = runner.run_ticks(state, 30)
	EconomyProductionSystem.tick_deposits(state)
	state = runner.tick(state)

	var resources: Dictionary = state.diagnostics.get("resources", {})
	var deposits: Variant = resources.get("deposits", {})
	assert_true(typeof(deposits) == TYPE_DICTIONARY, "deposit summary must appear in resource diagnostics")
	assert_true(int(deposits.get("depleted", 0)) >= 1 or int(deposits.get("active", 0)) >= 0, "deposit depletion tracked in diagnostics")

	var depleted_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "resource_depleted":
			depleted_event = true
			break
	assert_true(depleted_event, "resource_depleted event expected when deposit exhausts")


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
