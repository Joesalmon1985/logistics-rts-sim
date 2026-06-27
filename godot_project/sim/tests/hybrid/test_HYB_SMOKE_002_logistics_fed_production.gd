extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_HYB_SMOKE_002_logistics_fed_production"


func _run_body() -> void:
	var scenario: Dictionary = HybridTestHelper.base_scenario(702)
	scenario["spawns"][0]["buildings"] = [
		{"type": "power_generator", "q": 6, "r": 2},
		{"type": "barracks", "q": 9, "r": 2},
	]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	HybridTestHelper.flatten_grass(state, 0, 0, 15, 15)
	state = runner.run_ticks(state, 2)
	var barracks_id: int = HybridTestHelper.building_of_type(state, "barracks")
	assert_true(barracks_id > 0, "barracks required")
	assert_eq(int(MilitaryState.get_player(state, "p1").get("credits", -1)), 0, "hybrid must have no credits")
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {
		"building_id": barracks_id, "unit_type": "rifle_squad",
	}))
	assert_true(res.success, "ware-fed queue must succeed: %s" % res.reason_code)
	state = runner.run_ticks(state, 40)
	assert_true(HybridTestHelper.count_units(state, "rifle_squad") >= 1, "unit must spawn after ware payment")
	assert_true(HybridTestHelper.has_event(state, "unit_spawned"), "unit_spawned expected")
