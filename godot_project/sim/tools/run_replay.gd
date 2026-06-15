extends SceneTree

## CLI replay export/import verifier — REP-SMOKE-001.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var mode: String = "verify"
	var replay_path: String = ""
	var export_path: String = ""
	var scenario_path: String = "scenarios/economy/tiny_success_map.json"
	var seed_value: int = 42
	var ticks: int = 50

	for arg in args:
		if arg.begins_with("--mode="):
			mode = arg.substr("--mode=".length())
		elif arg.begins_with("--replay="):
			replay_path = arg.substr("--replay=".length())
		elif arg.begins_with("--export="):
			export_path = arg.substr("--export=".length())
		elif arg.begins_with("--scenario="):
			scenario_path = arg.substr("--scenario=".length())
		elif arg.begins_with("--seed="):
			seed_value = int(arg.substr("--seed=".length()))
		elif arg.begins_with("--ticks="):
			ticks = int(arg.substr("--ticks=".length()))

	var exit_code: int = 0
	match mode:
		"verify":
			if replay_path == "":
				print("REPLAY FAIL: --replay= required for verify mode")
				exit_code = 1
			else:
				exit_code = 0 if _verify_replay(replay_path) else 1
		"export-economy-bot":
			exit_code = 0 if _export_economy_bot_run(scenario_path, seed_value, ticks, export_path) else 1
		_:
			print("REPLAY FAIL: unknown mode '%s'" % mode)
			exit_code = 1

	if exit_code == 0:
		print("REPLAY PASS")
		print("EXIT: 0 (pass)")
	else:
		print("REPLAY FAIL")
		print("EXIT: 1 (fail)")
	call_deferred("quit", exit_code)


func _verify_replay(path: String) -> bool:
	var result: Dictionary = ReplayFile.verify(path)
	if not bool(result.get("success", false)):
		print("REP verify failed: %s" % str(result.get("error", "unknown")))
		if result.has("expected_hash"):
			print("  expected: %s" % result.get("expected_hash"))
			print("  actual:   %s" % result.get("actual_hash"))
		return false
	print("REP verify OK hash=%s" % str(result.get("final_hash", "")))
	return true


func _export_economy_bot_run(scenario_path: String, seed_value: int, ticks: int, export_path: String) -> bool:
	var scenario: Dictionary = ConfigLoader.load_scenario(scenario_path)
	if scenario.is_empty():
		return false
	scenario["seed"] = seed_value

	var runner := SimulationTickRunner.new()
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = ticks
	var state: GameState = runner.create_game(scenario)
	var bot := HeuristicEconomyBot.new()
	bot.reset("p1")
	state = bot_runner.run_until_complete(state, bot)

	if export_path == "":
		export_path = SimPaths.default_export_file("run04_replay.json")
	var ok: bool = ReplayFile.save_from_run(state, bot_runner.get_recorder(), scenario, export_path)
	if ok:
		print("Exported replay to %s" % export_path)
	return ok
