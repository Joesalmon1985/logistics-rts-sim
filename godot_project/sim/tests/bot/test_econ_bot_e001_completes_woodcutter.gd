extends "res://sim/tests/test_case.gd"

const MAX_TICKS: int = 400


func get_name() -> String:
	return "test_ECON_BOT_E001_completes_woodcutter"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)

	var bot := EconomyExpansionBot.new("p1")
	var bot_runner := BotRunner.new(runner)
	var result: Dictionary = SimTestLoop.run_bot_until(
		state,
		bot_runner,
		bot,
		MAX_TICKS,
		func(current: GameState, _runner_ref: BotRunner) -> bool:
			return SimTestLoop.has_completed_building_type(current, "p1", "woodcutter"),
	)
	state = result["state"]

	assert_true(
		DualBotRunner.count_player_buildings_built(state, "p1") >= 1,
		"p1 completed at least one bot-built building",
	)

	var found_woodcutter: bool = SimTestLoop.has_completed_building_type(state, "p1", "woodcutter")
	assert_true(found_woodcutter, "completed bot-built woodcutter exists")
