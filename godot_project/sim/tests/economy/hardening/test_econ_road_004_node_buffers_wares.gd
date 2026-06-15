extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_ROAD_004_node_buffers_wares"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 164,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var cfg: EconomyConfig = EconomyState.get_config(state)
	var expected_capacity: int = cfg.int_val("road_node_buffer_capacity", 5)
	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	var node: Dictionary = EconomyState.get_road_node(state, int(hq.get("road_node_id", 0)))
	assert_has_keys(node, ["inventory"], "road node must expose buffer inventory")
	var inventory: Dictionary = node.get("inventory", {})
	assert_eq(int(inventory.get("capacity", 0)), expected_capacity, "node buffer capacity must match config")

	EconomyState.inventory_add(inventory, "timber", 2)
	EconomyState.add_road_node(state, node)
	node = EconomyState.get_road_node(state, int(hq.get("road_node_id", 0)))
	assert_eq(int(node.get("inventory", {}).get("wares", {}).get("timber", 0)), 2, "node inventory must stage wares")
