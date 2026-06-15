extends "res://sim/tests/test_case.gd"

## MIL-MOVE-002: a move command paths the unit and updates its hex position.


func get_name() -> String:
	return "test_MIL_MOVE_002_move_command_updates_position"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 7,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [{"player_id": "p1", "q": 2, "r": 2, "units": [{"type": "scout", "q": 10, "r": 10}]}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var unit_id: int = _first_unit_of_type(state, "scout")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, MoveUnitCommand.new("p1", 0, {"unit_id": unit_id, "q": 14, "r": 10}))
	assert_true(res.success, "move accepted")

	state = runner.run_ticks(state, 150)
	var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
	assert_eq(int(unit.get("q", -1)), 14, "unit reached destination q")
	assert_eq(int(unit.get("r", -1)), 10, "unit reached destination r")
	assert_eq(str(unit.get("order", {}).get("type", "")), MilitaryConstants.ORDER_NONE, "order cleared on arrival")


func _first_unit_of_type(state: GameState, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
