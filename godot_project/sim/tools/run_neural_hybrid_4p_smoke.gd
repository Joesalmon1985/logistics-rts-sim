extends SceneTree

## Neural hybrid 4P smoke — four modular bots tick without crash (PRD A.14).

const SCENARIO := "scenarios/hybrid/hybrid_4p_training_ffa.json"


func _initialize() -> void:
	SimBootstrap.load_all()
	var runner := SimulationTickRunner.new()
	var quad := QuadBotRunner.new(runner)
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	var state: GameState = runner.create_game(scenario)
	for spawn in scenario.get("spawns", []):
		HybridMapSetup.prepare_playable_map(
			state, 0, 0, state.map.width - 1, state.map.height - 1,
			int(spawn.get("q", 0)), int(spawn.get("r", 0)),
		)
	for pid in ["p1", "p2", "p3", "p4"]:
		quad.set_bot(pid, ModularNeuralHybridBot.new(pid))
	scenario["max_ticks"] = 80
	quad.run_match(state, scenario)
	var ffa: Dictionary = HybridFfaObjective.evaluate(state)
	print("SMOKE PASS: neural_hybrid_4p ticks=%d alive=%s" % [
		state.tick,
		str(ffa.get("alive_players", [])),
	])
	quit(0)
