class_name NeuralHybridTestHelper
extends RefCounted


static func load_4p_state(ticks: int = 20) -> GameState:
	SimBootstrap.load_all()
	var runner := SimulationTickRunner.new()
	var quad := QuadBotRunner.new(runner)
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/hybrid/hybrid_4p_training_ffa.json")
	var state: GameState = runner.create_game(scenario)
	for spawn in scenario.get("spawns", []):
		HybridMapSetup.prepare_playable_map(
			state, 0, 0, state.map.width - 1, state.map.height - 1,
			int(spawn.get("q", 0)), int(spawn.get("r", 0)),
		)
	for pid in ["p1", "p2", "p3", "p4"]:
		var bot := ModularNeuralHybridBot.new(pid)
		bot.use_trained_checkpoint = true
		bot.use_heuristic_teacher = false
		quad.set_bot(pid, bot)
	scenario["max_ticks"] = ticks
	return quad.run_match(state, scenario)
