extends "res://sim/tests/test_case.gd"

const MAX_TICKS: int = 200


func get_name() -> String:
	return "test_ECON_BOT_D001_site_receives_delivery"


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
			return SimTestLoop.has_delivery_progress(current, "p1"),
	)
	state = result["state"]

	var saw_site: bool = false
	var saw_delivery_progress: bool = SimTestLoop.has_delivery_progress(state, "p1")
	for site in state.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
		if str(site.get("player_id", "")) != "p1":
			continue
		saw_site = true

	assert_true(saw_site or saw_delivery_progress, "p1 construction site existed during run")
	assert_true(saw_delivery_progress, "construction site received material or builder progress")
