extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_KERNEL_004_invalid_command_does_not_mutate_state"


func run() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 7,
		"map_width": 4,
		"map_height": 4,
		"players": ["p1"],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var before_hash: String = ReplayHash.compute_state_hash(state)
	var before_tick: int = state.tick

	var invalid := InvalidTestCommand.new("p1", state.tick)
	var result: CommandResult = runner.command_validator.validate_and_apply(state, invalid)

	assert_false(result.success)
	assert_eq(before_hash, ReplayHash.compute_state_hash(state), "hash must not change")
	assert_eq(before_tick, state.tick, "tick must not change")
