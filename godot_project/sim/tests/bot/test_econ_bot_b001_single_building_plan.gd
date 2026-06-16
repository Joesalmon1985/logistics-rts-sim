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
	bot_runner.max_ticks = MAX_TICKS
	state = bot_runner.run_until_complete(state, bot)

	var site_count: int = 0
	for site in state.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
		if str(site.get("player_id", "")) == "p1":
			site_count += 1
	assert_true(site_count >= 1, "p1 has construction site within tick budget")

	var found_plan: bool = false
	for entry in bot_runner.get_recorder().get_command_entries():
		if not bool(entry.get("accepted", false)):
			continue
		var cmd: Dictionary = entry.get("command", {})
		if str(cmd.get("type", "")) == "PlaceBuildingPlanCommand":
			found_plan = true
			break
	assert_true(found_plan, "recorder shows accepted PlaceBuildingPlanCommand")
