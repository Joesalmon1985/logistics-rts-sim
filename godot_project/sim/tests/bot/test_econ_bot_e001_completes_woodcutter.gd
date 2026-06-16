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
	bot_runner.max_ticks = MAX_TICKS
	state = bot_runner.run_until_complete(state, bot)

	assert_true(
		DualBotRunner.count_player_buildings_built(state, "p1") >= 1,
		"p1 completed at least one bot-built building",
	)

	var hq_id: int = EconomyState.get_hq_building_id(state, "p1")
	var found_woodcutter: bool = false
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != "p1":
			continue
		if int(building.get("id", 0)) == hq_id:
			continue
		if str(building.get("building_type", "")) != "woodcutter":
			continue
		var state_name: String = str(building.get("state", ""))
		if state_name in [
			EconomyConstants.BLD_PLANNED,
			EconomyConstants.BLD_UNDER_CONSTRUCTION,
		]:
			continue
		found_woodcutter = true
		break
	assert_true(found_woodcutter, "completed bot-built woodcutter exists")
