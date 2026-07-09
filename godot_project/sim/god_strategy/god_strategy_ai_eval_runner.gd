class_name GodStrategyAiEvalRunner
extends RefCounted

const REGRESSION := "scenarios/god_strategy/god_strategy_ai_eval.json"
const SEEDS: Array = [99001]


static func run_win_rate_table(bot_factory: Callable, opponent_passive: bool = true) -> Dictionary:
	SimBootstrap.load_all()
	var wins: int = 0
	var domination: int = 0
	var rows: Array = []
	for seed_val in SEEDS:
		var row: Dictionary = _run_single_seed(bot_factory, seed_val, opponent_passive)
		rows.append(row)
		if str(row.get("winner", "")) == "p1":
			wins += 1
		if str(row.get("end_reason", "")) == "domination":
			domination += 1
	return {
		"seeds": SEEDS.size(),
		"wins_p1": wins,
		"win_rate": float(wins) / float(SEEDS.size()),
		"domination_endings": domination,
		"rows": rows,
	}


static func _run_single_seed(bot_factory: Callable, seed_val: int, opponent_passive: bool) -> Dictionary:
	var runner := SimulationTickRunner.new()
	var quad := QuadBotRunner.new(runner)
	var scenario: Dictionary = ConfigLoader.load_scenario(REGRESSION)
	scenario["seed"] = seed_val
	scenario["max_ticks"] = mini(int(scenario.get("max_ticks", 120)), 120)
	scenario["stall_ticks_threshold"] = 0
	var state: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_domination_regression(state, scenario)
	quad.set_bot("p1", bot_factory.call())
	for pid in ["p2", "p3", "p4"]:
		if opponent_passive:
			quad.set_bot(pid, PassiveBot.new(pid))
		else:
			quad.set_bot(pid, HeuristicModularGodBot.new(pid))
	state = quad.run_match(state, scenario)
	var end: Dictionary = GodStrategyMatchEnd.evaluate(state, scenario)
	return {
		"seed": seed_val,
		"winner": str(end.get("winner_player_id", "")),
		"end_reason": str(end.get("end_reason", "")),
		"tick": state.tick,
		"survival_score_p1": int(end.get("survival_scores", {}).get("p1", 0)),
		"alive": end.get("alive_players", []),
	}


static func write_artefacts(phase_dir: String, tables: Dictionary) -> void:
	DirAccess.make_dir_recursive_absolute(phase_dir)
	var file := FileAccess.open(phase_dir.path_join("win_rate_tables.json"), FileAccess.WRITE)
	if file != null:
		file.store_string(JSON.stringify(tables, "\t"))
		file.close()
