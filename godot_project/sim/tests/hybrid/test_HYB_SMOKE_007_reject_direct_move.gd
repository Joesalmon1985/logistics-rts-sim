extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_HYB_SMOKE_007_reject_direct_move"


func _run_body() -> void:
	var scenario: Dictionary = HybridTestHelper.base_scenario(707)
	scenario["spawns"][0]["units"] = [{"type": "scout", "q": 4, "r": 4, "count": 1}]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var unit_id: int = 0
	for unit in MilitaryState.all_units(state):
		unit_id = int(unit.get("id", 0))
		break
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var move_res: CommandResult = validator.validate_and_apply(state, MoveUnitCommand.new("p1", 0, {
		"unit_id": unit_id, "q": 8, "r": 8,
	}))
	assert_false(move_res.success, "MoveUnitCommand must be rejected")
	assert_eq(move_res.reason_code, HybridConstants.ERR_FORBIDDEN_DIRECT)
	var attack_res: CommandResult = validator.validate_and_apply(state, AttackUnitCommand.new("p1", 0, {
		"unit_id": unit_id, "target_id": unit_id,
	}))
	assert_false(attack_res.success, "AttackUnitCommand must be rejected")
