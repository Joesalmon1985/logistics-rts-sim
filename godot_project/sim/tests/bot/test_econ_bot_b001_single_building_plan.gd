extends "res://sim/tests/test_case.gd"

const MAX_TICKS: int = 50


func get_name() -> String:
	return "test_ECON_BOT_B001_single_building_plan"


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
		func(current: GameState, runner_ref: BotRunner) -> bool:
			return SimTestLoop.has_accepted_command_type(runner_ref, "PlaceBuildingPlanCommand"),
	)
	state = result["state"]

	var site_count: int = 0
	for site in state.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
		if str(site.get("player_id", "")) == "p1":
			site_count += 1
	assert_true(site_count >= 1, "p1 has construction site within tick budget")

	var found_plan: bool = SimTestLoop.has_accepted_command_type(bot_runner, "PlaceBuildingPlanCommand")
	assert_true(found_plan, "recorder shows accepted PlaceBuildingPlanCommand")
