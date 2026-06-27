extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_HYB_SMOKE_006_repair_priority"


func _run_body() -> void:
	var scenario: Dictionary = HybridTestHelper.base_scenario(706)
	scenario["spawns"][0]["buildings"] = [
		{"type": "power_generator", "q": 5, "r": 2},
		{"type": "repair_depot", "q": 8, "r": 2},
	]
	scenario["spawns"][0]["units"] = [{"type": "rifle_squad", "q": 8, "r": 4, "count": 1}]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	HybridTestHelper.flatten_grass(state, 0, 0, 15, 15)
	state = runner.run_ticks(state, 2)
	var unit_id: int = 0
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == "rifle_squad":
			unit_id = int(unit.get("id", 0))
			unit["hp"] = 20
			MilitaryState.add_unit(state, unit)
			break
	assert_true(unit_id > 0, "damaged unit required")
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, SetRepairPriorityCommand.new("p1", 0, {
		"target_id": unit_id, "priority": 1,
	}))
	assert_true(res.success, "repair priority accepted: %s" % res.reason_code)
	state = runner.run_ticks(state, 40)
	var healed: Dictionary = MilitaryState.get_unit(state, unit_id)
	assert_true(int(healed.get("hp", 0)) > 20, "repair depot must heal with wares")
