extends "res://sim/tests/test_case.gd"

const MAX_TICKS: int = 80
const MAX_ROADS_BUILT: int = 45


func get_name() -> String:
	return "test_ECON_BOT_C001_road_connects_target"


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
			for site in current.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
				if str(site.get("player_id", "")) != "p1":
					continue
				if SimTestLoop.site_road_adjacent(current, site, "p1"):
					return true
			return false,
	)
	state = result["state"]

	var sites: Array = []
	for site in state.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
		if str(site.get("player_id", "")) == "p1":
			sites.append(site)
	assert_true(not sites.is_empty(), "p1 has a construction site")

	var connected: bool = false
	for site in sites:
		if SimTestLoop.site_road_adjacent(state, site, "p1"):
			connected = true
			break
	assert_true(connected, "construction site hex is road-adjacent")

	var roads_built: int = DualBotRunner.count_player_roads_built(state, "p1")
	assert_true(roads_built <= MAX_ROADS_BUILT, "roads built within connect budget")
