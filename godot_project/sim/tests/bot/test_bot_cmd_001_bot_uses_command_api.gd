extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_BOT_CMD_001_bot_uses_command_api"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 11,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var validator := CommandValidator.new(CommandRegistry.create_default())
	runner.command_validator = validator
	var state: GameState = runner.create_game(scenario)

	var before_hash: String = ReplayHash.compute_state_hash(state)
	var bot := HeuristicEconomyBot.new()
	bot.reset("p1")
	bot.observe(SnapshotExporter.export_snapshot(state), "p1")
	var command: SimCommand = bot.decide(0)
	assert_not_null(command, "bot should emit a command")

	var result: CommandResult = validator.validate_and_apply(state, command)
	assert_true(result.success or str(result.error_code) != "UNKNOWN_COMMAND", "command uses registered type")

	var invalid := PlaceRoadCommand.new("p1", 0, {"from_q": 99, "from_r": 99, "to_q": 100, "to_r": 100})
	var invalid_result: CommandResult = validator.validate_and_apply(state, invalid)
	assert_false(invalid_result.success, "invalid road should reject")
	var after_invalid_hash: String = ReplayHash.compute_state_hash(state)
	assert_eq(after_invalid_hash, ReplayHash.compute_state_hash(state), "rejected command must not mutate hashed state")
