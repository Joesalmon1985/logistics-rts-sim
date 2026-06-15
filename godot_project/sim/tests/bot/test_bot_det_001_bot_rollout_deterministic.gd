extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_BOT_DET_001_bot_rollout_deterministic"


func _run_body() -> void:
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/economy/tiny_success_map.json")
	scenario["seed"] = 99

	var run_once := func() -> Dictionary:
		var runner := SimulationTickRunner.new()
		var bot_runner := BotRunner.new(runner)
		bot_runner.max_ticks = 3000
		var state: GameState = runner.create_game(scenario)
		var bot := HeuristicEconomyBot.new()
		state = bot_runner.run_until_complete(state, bot)
		return {
			"hash": ReplayHash.compute_state_hash(state),
			"commands": bot_runner.get_recorder().get_command_entries(),
		}

	var first: Dictionary = run_once.call()
	var second: Dictionary = run_once.call()
	assert_eq(first["hash"], second["hash"], "identical bot rollout hash")
	assert_eq(JSON.stringify(first["commands"]), JSON.stringify(second["commands"]), "identical command log")
