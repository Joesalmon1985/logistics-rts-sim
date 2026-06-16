extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_MP_002_cross_player_isolation"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/economy/two_player_init_test.json")
	var state: GameState = runner.create_game(scenario)

	var p1_hq: int = EconomyState.get_hq_building_id(state, "p1")
	var p2_hq: int = EconomyState.get_hq_building_id(state, "p2")
	var p1_hq_building: Dictionary = EconomyState.get_building(state, p1_hq)
	var p2_pos: Vector2i = EconomyState.get_hq_position(state, "p2")

	var cross_build := PlaceBuildingPlanCommand.new("p1", 0, {
		"building_type": "storehouse",
		"q": p2_pos.x,
		"r": p2_pos.y,
		"player_id": "p1",
	})
	var cross_result: CommandResult = validator.validate_and_apply(state, cross_build)
	assert_false(cross_result.success, "p1 cannot build in p2 territory")

	var p1_node: int = int(p1_hq_building.get("road_node_id", 0))
	var p2_hq_building: Dictionary = EconomyState.get_building(state, p2_hq)
	var p2_node: int = int(p2_hq_building.get("road_node_id", 0))
	assert_true(p1_node > 0 and p2_node > 0, "both HQ nodes exist")

	var cross_road := PlaceRoadCommand.new("p1", 0, {
		"from_q": EconomyState.get_hq_position(state, "p1").x,
		"from_r": EconomyState.get_hq_position(state, "p1").y,
		"to_q": p2_pos.x,
		"to_r": p2_pos.y,
		"player_id": "p1",
	})
	var road_result: CommandResult = validator.validate_and_apply(state, cross_road)
	assert_false(road_result.success, "p1 cannot bridge to p2 HQ hex")

	assert_true(
		EconomyPathfinder.find_route(state, p1_node, p2_node, "p1").is_empty(),
		"no cross-player route for p1",
	)

	_assert_worker_isolation(state)


func _assert_worker_isolation(state: GameState) -> void:
	var p2_building_id: int = 0
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) == "p2" and str(building.get("building_type", "")) != "headquarters":
			p2_building_id = int(building.get("id", 0))
			break
	if p2_building_id <= 0:
		return
	var p2_building: Dictionary = EconomyState.get_building(state, p2_building_id)
	p2_building["worker_type"] = "builder"
	p2_building["enabled"] = 1
	EconomyState.add_building(state, p2_building)
	EconomyWorkerSystem.tick(state)
	for worker in EconomyState.all_workers(state):
		if str(worker.get("player_id", "")) == "p1":
			assert_true(
				int(worker.get("assigned_building_id", 0)) != p2_building_id,
				"p1 worker not assigned to p2 building",
			)
