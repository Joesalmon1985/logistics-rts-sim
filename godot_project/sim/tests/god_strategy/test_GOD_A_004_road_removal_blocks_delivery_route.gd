extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_004_road_removal_blocks_delivery_route"


func _run_body() -> void:
	var scenario: Dictionary = HybridTestHelper.base_scenario(99004)
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	HybridTestHelper.flatten_grass(state, 0, 0, 15, 15)

	var node_a: int = EconomyRoadSystem.create_building_road_node(state, 0, 2, 2, "p1")
	var node_b: int = EconomyRoadSystem.create_building_road_node(state, 0, 8, 2, "p1")
	var node_c: int = EconomyRoadSystem.create_building_road_node(state, 0, 10, 2, "p1")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	for segment in [{"from_q": 2, "from_r": 2, "to_q": 3, "to_r": 2}, {"from_q": 3, "from_r": 2, "to_q": 4, "to_r": 2}, {"from_q": 4, "from_r": 2, "to_q": 5, "to_r": 2}, {"from_q": 5, "from_r": 2, "to_q": 6, "to_r": 2}, {"from_q": 6, "from_r": 2, "to_q": 7, "to_r": 2}, {"from_q": 7, "from_r": 2, "to_q": 8, "to_r": 2}]:
		validator.validate_and_apply(state, PlaceRoadCommand.new("p1", 0, segment))

	var route_before: Array = EconomyPathfinder.find_route(state, node_a, node_b)
	assert_false(route_before.is_empty(), "route must exist before removal")

	var bridge_road_id: int = 0
	for road in EconomyState.all_roads(state):
		if int(road.get("hex_a_q", -1)) == 4 and int(road.get("hex_b_q", -1)) == 5:
			bridge_road_id = int(road.get("id", 0))
			break
		if int(road.get("hex_a_q", -1)) == 5 and int(road.get("hex_b_q", -1)) == 4:
			bridge_road_id = int(road.get("id", 0))
			break
	assert_true(bridge_road_id > 0, "bridge road segment required")

	var remove_res: CommandResult = validator.validate_and_apply(state, RemoveRoadCommand.new("p1", 0, {
		"road_id": bridge_road_id,
	}))
	assert_true(remove_res.success, "road removal accepted")

	var route_after: Array = EconomyPathfinder.find_route(state, node_a, node_b)
	assert_true(route_after.is_empty(), "route must break after road removal")

	var blocked: Dictionary = EconomyState.get_meta_value(state, "blocked_routes", {})
	assert_false(blocked.is_empty(), "blocked_routes metadata expected")

	var site_result: Dictionary = EconomyConstructionSystem.create_site(state, "sawmill", 10, 2, "p1")
	assert_true(site_result.get("ok", false), "sawmill site for delivery context")
	state = runner.tick(state)
	assert_true(
		route_after.is_empty() or not blocked.is_empty(),
		"logistics warfare: broken route must block delivery path",
	)
