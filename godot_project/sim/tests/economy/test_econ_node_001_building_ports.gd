extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_NODE_001_building_ports"


func _run_body() -> void:
	_test_default_anchor_port_fallback()
	_test_hq_port_routing()


func _test_default_anchor_port_fallback() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 301,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass_area(state, 1, 1, 7, 4)

	var building_id: int = EconomyInit._spawn_completed_building(
		state, "woodcutter", 5, 2, "p1", EconomyState.get_config(state),
	)
	var building: Dictionary = EconomyState.get_building(state, building_id)
	var ports: Array = building.get("port_node_ids", [])
	assert_eq(ports.size(), 1, "default single port expected")
	assert_eq(int(ports[0].get("dq", -1)), 0, "default port dq")
	assert_eq(int(ports[0].get("dr", -1)), 0, "default port dr")
	assert_eq(int(building.get("road_node_id", 0)), int(ports[0].get("node_id", -1)), "primary node at anchor")


func _test_hq_port_routing() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 302,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass_area(state, 1, 1, 9, 6)

	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	var hq_ports: Array = hq.get("port_node_ids", [])
	assert_true(hq_ports.size() >= 1, "HQ must have configured port nodes")

	var port_hex: Vector2i = _hq_secondary_port_hex(hq)
	assert_true(
		port_hex.x != int(hq.get("anchor_q", -1)) or port_hex.y != int(hq.get("anchor_r", -1)),
		"secondary port must differ from anchor for routing test",
	)
	var sawmill_q: int = 6
	var sawmill_r: int = 2
	_connect_hex_path(state, port_hex.x, port_hex.y, sawmill_q, sawmill_r)
	var sawmill_id: int = EconomyInit._spawn_completed_building(
		state, "sawmill", sawmill_q, sawmill_r, "p1", EconomyState.get_config(state),
	)
	var sawmill: Dictionary = EconomyState.get_building(state, sawmill_id)
	var sawmill_store: Dictionary = EconomyState.get_ware_store(state, int(sawmill.get("store_id", 0)))
	EconomyState.inventory_add(sawmill_store, "timber", 0)
	EconomyState.set_ware_store(state, int(sawmill.get("store_id", 0)), sawmill_store)

	EconomyLogisticsSystem.request_delivery(
		state, "planks", 1, sawmill_id, "building", "p1", 0,
	)
	state = runner.run_ticks(state, 40)

	sawmill = EconomyState.get_building(state, sawmill_id)
	sawmill_store = EconomyState.get_ware_store(state, int(sawmill.get("store_id", 0)))
	assert_true(
		int(sawmill_store.get("wares", {}).get("planks", 0)) >= 1,
		"delivery must route through HQ port hex, not anchor-only teleport",
	)


func _hq_secondary_port_hex(hq: Dictionary) -> Vector2i:
	for port in hq.get("port_node_ids", []):
		if typeof(port) != TYPE_DICTIONARY:
			continue
		if int(port.get("dq", 0)) == 0 and int(port.get("dr", 0)) == 0:
			continue
		return Vector2i(int(port.get("q", 0)), int(port.get("r", 0)))
	return Vector2i(int(hq.get("anchor_q", 0)), int(hq.get("anchor_r", 0)))


func _flatten_grass_area(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))


func _connect_hex_path(state: GameState, from_q: int, from_r: int, to_q: int, to_r: int) -> void:
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
