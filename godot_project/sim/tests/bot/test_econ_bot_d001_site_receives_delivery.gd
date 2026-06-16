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
	bot_runner.max_ticks = MAX_TICKS
	state = bot_runner.run_until_complete(state, bot)

	var saw_site: bool = false
	var saw_delivery_progress: bool = false
	for site in state.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
		if str(site.get("player_id", "")) != "p1":
			continue
		saw_site = true
		for ware_type in site.get("delivered", {}):
			if int(site.get("delivered", {}).get(ware_type, 0)) > 0:
				saw_delivery_progress = true
				break
		if int(site.get("progress_percent", 0)) > 0:
			saw_delivery_progress = true
		var required_work: int = int(site.get("required", {}).get("builder_work", 0))
		if required_work > 0 and int(site.get("builder_work_remaining", required_work)) < required_work:
			saw_delivery_progress = true

	assert_true(saw_site or saw_delivery_progress, "p1 construction site existed during run")
	assert_true(saw_delivery_progress, "construction site received material or builder progress")
