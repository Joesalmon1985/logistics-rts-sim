extends "res://sim/tests/test_case.gd"

## MIL-ORDER-004: patrol order keeps a unit moving between waypoints.


func get_name() -> String:
	return "test_MIL_ORDER_004_patrol_loops_waypoints"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 22,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "scout", "q": 3, "r": 3}]}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var unit_id: int = _first_unit_of_type(state, "scout")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, PatrolCommand.new("p1", 0, {
		"unit_id": unit_id,
		"waypoints": [{"q": 3, "r": 3}, {"q": 10, "r": 3}],
	}))
	assert_true(res.success, "patrol accepted")

	state = runner.run_ticks(state, 60)
	var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
	assert_eq(str(unit.get("order", {}).get("type", "")), MilitaryConstants.ORDER_PATROL, "patrol order persists")
	assert_true(int(unit.get("q", 3)) != 3 or int(unit.get("r", 3)) != 3, "patrol drove movement away from start")


func _first_unit_of_type(state: GameState, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
