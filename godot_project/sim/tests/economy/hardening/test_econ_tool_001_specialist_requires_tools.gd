extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_TOOL_001_specialist_requires_tools"


func _run_body() -> void:
	var cfg: EconomyConfig = EconomyConfig.load_all()
	var woodcutter_req: Dictionary = cfg.get_tool_requirement("woodcutter")
	var miner_req: Dictionary = cfg.get_tool_requirement("miner")
	var carrier_req: Dictionary = cfg.get_tool_requirement("carrier")

	assert_true(int(woodcutter_req.get("amount", 0)) > 0, "woodcutter specialist must require tools")
	assert_true(int(miner_req.get("amount", 0)) > 0, "miner specialist must require tools")
	assert_eq(int(carrier_req.get("amount", 0)), 0, "carrier must not require tools")

	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 111,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq_store_id: int = int(EconomyState.get_building(state, hq_id).get("store_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(state, hq_store_id)
	hq_store["wares"] = {}
	hq_store["reserved"] = {}
	EconomyState.set_ware_store(state, hq_store_id, hq_store)

	_flatten_grass_area(state, 1, 1, 5, 2)
	_connect_hq_to(state, 5, 2)
	_set_coal_deposit_tile(state, 5, 2)
	var mine_id: int = EconomyInit._spawn_completed_building(state, "coal_mine", 5, 2, "p1", cfg)

	state = runner.run_ticks(state, 2)
	var mine: Dictionary = EconomyState.get_building(state, mine_id)
	assert_eq(mine.get("assigned_workers", []).size(), 0, "miner must not assign without tools in HQ store")


func _set_coal_deposit_tile(state: GameState, q: int, r: int) -> void:
	for offset in [[0, 0], [1, 0], [0, 1], [1, 1]]:
		var coord := HexCoord.new(q + int(offset[0]), r + int(offset[1]))
		var tile := HexTile.new("rock", 0, 1, 0)
		tile.deposit_type = "coal"
		tile.deposit_amount = 50
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
