extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_BOT_004_observation_boundary"


func _run_body() -> void:
	var bot := EconomyExpansionBot.new("p1")
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/economy/tiny_success_map.json")
	var state: GameState = runner.create_game(scenario)
	EconomyDiagnosticsSystem.tick(state)
	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	snapshot["available_commands"] = {
		"p1": CommandAvailabilityProvider.enumerate_legal_commands(state, "p1"),
	}
	bot.observe(snapshot, "p1")
	var cmd: SimCommand = bot.decide(0)
	assert_true(cmd != null, "bot returns a command object")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var before_hash: String = ReplayHash.compute_state_hash(state)
	var result: CommandResult = validator.validate_and_apply(state, cmd)
	if result.success:
		assert_true(before_hash != ReplayHash.compute_state_hash(state), "applied command mutates state via validator")
	else:
		assert_eq(before_hash, ReplayHash.compute_state_hash(state), "rejected command leaves hash unchanged")

	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = 400
	var progress_state: GameState = bot_runner.run_until_complete(state, bot)
	var roads: int = DualBotRunner._count_player_roads(progress_state, "p1")
	var buildings: int = DualBotRunner._count_player_buildings(progress_state, "p1")
	assert_true(roads + buildings > 0, "expansion bot makes progress")
