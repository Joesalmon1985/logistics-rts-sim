extends "res://sim/tests/test_case.gd"

## MIL-CMD-001: an invalid military command is rejected during validation and
## leaves authoritative state byte-for-byte unchanged (replay hash stable).


func get_name() -> String:
	return "test_MIL_CMD_001_invalid_command_no_mutation"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game({
		"variant": "military_trial",
		"seed": 11,
		"map_width": 12,
		"map_height": 12,
		"players": ["p1"],
	})

	var before_hash: String = ReplayHash.compute_state_hash(state)
	var validator := CommandValidator.new(CommandRegistry.create_default())

	# Move a non-existent unit — must fail validation, no mutation.
	var move_bad := MoveUnitCommand.new("p1", 0, {"unit_id": 999999, "q": 1, "r": 1})
	var r1: CommandResult = validator.validate_and_apply(state, move_bad)
	assert_false(r1.success, "move of missing unit must fail")
	assert_eq(r1.error_code, MilitaryConstants.ERR_NO_SUCH_ENTITY)

	# Place an unknown building type — must fail validation, no mutation.
	var place_bad := PlaceBuildingCommand.new("p1", 0, {"building_type": "death_star", "q": 2, "r": 2})
	var r2: CommandResult = validator.validate_and_apply(state, place_bad)
	assert_false(r2.success, "unknown building must fail")

	# Queue a unit at a non-producer / missing building — must fail.
	var queue_bad := QueueUnitCommand.new("p1", 0, {"building_id": 888888, "unit_type": "tank"})
	var r3: CommandResult = validator.validate_and_apply(state, queue_bad)
	assert_false(r3.success, "queue at missing building must fail")

	var after_hash: String = ReplayHash.compute_state_hash(state)
	assert_eq(after_hash, before_hash, "rejected commands must not mutate state")
