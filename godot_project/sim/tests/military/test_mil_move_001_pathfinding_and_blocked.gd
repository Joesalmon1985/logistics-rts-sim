extends "res://sim/tests/test_case.gd"

## MIL-MOVE-001/002: a unit paths across the shared hex map and reaches its
## destination; an unreachable destination (walled off by water) is rejected by the
## move command without mutating the unit's order.


func get_name() -> String:
	return "test_MIL_MOVE_001_pathfinding_and_blocked"


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
	assert_true(unit_id > 0, "scout spawned")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, MoveUnitCommand.new("p1", 0, {"unit_id": unit_id, "q": 14, "r": 10}))
	assert_true(res.success, "move command accepted")

	state = runner.run_ticks(state, 150)
	var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
	assert_eq(int(unit.get("q", -1)), 14, "reached destination q")
	assert_eq(int(unit.get("r", -1)), 10, "reached destination r")
	assert_eq(str(unit.get("order", {}).get("type", "")), MilitaryConstants.ORDER_NONE, "order cleared on arrival")

	# Wall off the map with a column of water and verify an unreachable move fails.
	for r in range(0, 16):
		state.map.set_tile(HexCoord.new(8, r), HexTile.new("water", 0, 0, 0))
	var u2: Dictionary = MilitaryState.get_unit(state, unit_id)
	# Move the unit back to the left side first by direct placement for the check.
	u2["q"] = 3
	u2["r"] = 3
	var blocked: CommandResult = validator.validate_and_apply(state, MoveUnitCommand.new("p1", 0, {"unit_id": unit_id, "q": 13, "r": 3}))
	assert_false(blocked.success, "unreachable move rejected")
	assert_eq(blocked.error_code, MilitaryConstants.ERR_UNREACHABLE)


func _first_unit_of_type(state: GameState, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
