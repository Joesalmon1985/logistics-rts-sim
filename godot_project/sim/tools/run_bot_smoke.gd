extends SceneTree

## RUN_04 bot/replay smoke runner — BOT-SMOKE-001/002, REP-SMOKE-001.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")

const VALID_SMOKES: Array[String] = ["all", "001", "002", "003", "004", "BOT-SMOKE-001", "BOT-SMOKE-002", "BOT-SMOKE-004", "REP-SMOKE-001"]


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var smoke_id: String = "all"
	var seed_value: int = 42
	var export_dir: String = ""
	var artifacts: bool = false
	for arg in args:
		if arg.begins_with("--smoke="):
			smoke_id = arg.substr("--smoke=".length())
		elif arg.begins_with("--seed="):
			seed_value = int(arg.substr("--seed=".length()))
		elif arg.begins_with("--export-dir="):
			export_dir = arg.substr("--export-dir=".length())
		elif arg == "--artifacts":
			artifacts = true

	smoke_id = smoke_id.replace("BOT-SMOKE-", "").replace("REP-SMOKE-", "00")

	if artifacts and export_dir == "":
		export_dir = SimPaths.default_export_dir()

	if smoke_id not in VALID_SMOKES:
		print("BOT SMOKE FAIL: unknown smoke id '%s'" % smoke_id)
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
		return

	var failed: bool = false
	if smoke_id == "all" or smoke_id == "001":
		failed = failed or not _run_smoke_001(seed_value, export_dir)
	if smoke_id == "all" or smoke_id == "002":
		failed = failed or not _run_smoke_002(seed_value, export_dir)
	if smoke_id == "all" or smoke_id == "003":
		failed = failed or not _run_smoke_003(seed_value, export_dir)
	if smoke_id == "all" or smoke_id == "004":
		failed = failed or not _run_smoke_004(seed_value, export_dir)

	if failed:
		print("BOT SMOKE FAIL")
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
	else:
		print("BOT SMOKE PASS")
		print("EXIT: 0 (pass)")
		call_deferred("quit", 0)


func _run_smoke_001(seed_value: int, export_dir: String) -> bool:
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/economy/tiny_success_map.json")
	if scenario.is_empty():
		print("BOT-SMOKE-001 FAIL: scenario load failed")
		return false
	scenario["seed"] = seed_value

	var runner := SimulationTickRunner.new()
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = 20000
	var state: GameState = runner.create_game(scenario)
	var bot := HeuristicEconomyBot.new()
	state = bot_runner.run_until_complete(state, bot)

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	if not bot.is_complete(snapshot):
		print("BOT-SMOKE-001 FAIL: economy bot did not complete tutorial")
		return false

	if export_dir != "":
		DirAccess.make_dir_recursive_absolute(export_dir)
		EconomyExporter.export_all(state, export_dir, bot_runner.get_decisions())

	print("BOT-SMOKE-001 PASS: economy bot completed tutorial")
	return true


func _run_smoke_002(seed_value: int, export_dir: String) -> bool:
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/military/skirmish_1v1.json")
	if scenario.is_empty():
		print("BOT-SMOKE-002 FAIL: scenario load failed")
		return false
	scenario["seed"] = seed_value
	scenario["fog_enabled"] = 0

	var runner := SimulationTickRunner.new()
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = 8000
	var state: GameState = runner.create_game(scenario)
	var bot := HeuristicMilitaryBot.new()
	bot.reset("p1")
	state = bot_runner.run_until_complete(state, bot)

	if int(MilitaryState.get_meta_value(state, "victory_decided", 0)) != 1:
		print("BOT-SMOKE-002 FAIL: military bot did not win skirmish")
		return false
	if str(MilitaryState.get_meta_value(state, "winner_player_id", "")) != "p1":
		print("BOT-SMOKE-002 FAIL: wrong winner")
		return false

	if export_dir != "":
		DirAccess.make_dir_recursive_absolute(export_dir)
		MilitaryExporter.export_all(state, export_dir, bot_runner.get_decisions())

	print("BOT-SMOKE-002 PASS: military bot won skirmish")
	return true


func _run_smoke_003(seed_value: int, export_dir: String) -> bool:
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/economy/tiny_success_map.json")
	scenario["seed"] = seed_value

	var runner := SimulationTickRunner.new()
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = 500
	var state: GameState = runner.create_game(scenario)
	var bot := HeuristicEconomyBot.new()
	state = bot_runner.run_until_complete(state, bot)

	var replay_dir: String = export_dir
	if replay_dir == "":
		replay_dir = SimPaths.default_export_dir().path_join("rep_smoke_tmp")
	var replay_path: String = replay_dir.path_join("replay.json")
	DirAccess.make_dir_recursive_absolute(replay_dir)

	if not ReplayFile.save_from_run(state, bot_runner.get_recorder(), scenario, replay_path):
		print("REP-SMOKE-001 FAIL: export failed")
		return false

	var result: Dictionary = ReplayFile.load_and_run(replay_path)
	if not bool(result.get("success", false)):
		print("REP-SMOKE-001 FAIL: import/re-run failed: %s" % str(result.get("error", "")))
		return false

	var expected: String = ReplayHash.compute_state_hash(state)
	var actual: String = str(result.get("final_hash", ""))
	if expected != actual:
		print("REP-SMOKE-001 FAIL: hash mismatch after import")
		print("  recorded: %s" % expected)
		print("  replayed: %s" % actual)
		return false

	print("REP-SMOKE-001 PASS: export/import identical hash")
	return true


func _run_smoke_004(seed_value: int, export_dir: String) -> bool:
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	if seed_value != 42:
		scenario["seed"] = seed_value
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	var dual := DualBotRunner.new(runner)
	state = dual.run_match(state, scenario)
	var hash_a: String = ReplayHash.compute_state_hash(state)

	for player_id in ["p1", "p2"]:
		if DualBotRunner.count_player_buildings_built(state, player_id) < 5:
			print("BOT-SMOKE-004 FAIL: %s bot-built buildings below minimum" % player_id)
			return false
		if DualBotRunner.count_player_roads_built(state, player_id) < 10:
			print("BOT-SMOKE-004 FAIL: %s bot-built roads below minimum" % player_id)
			return false
		if DualBotRunner.count_player_deliveries_built(state, player_id) < 10:
			print("BOT-SMOKE-004 FAIL: %s bot-built deliveries below minimum" % player_id)
			return false

	var runner_b := SimulationTickRunner.new()
	var scenario_b: Dictionary = scenario.duplicate(true)
	var state_b: GameState = runner_b.create_game(scenario_b)
	EconomyBvbSetup.prepare_expansion_duel_map(state_b, scenario_b)
	var dual_b := DualBotRunner.new(runner_b)
	state_b = dual_b.run_match(state_b, scenario_b)
	var hash_b: String = ReplayHash.compute_state_hash(state_b)
	if hash_a != hash_b:
		print("BOT-SMOKE-004 FAIL: replay hash mismatch")
		print("  first:  %s" % hash_a)
		print("  second: %s" % hash_b)
		return false

	if export_dir != "":
		DirAccess.make_dir_recursive_absolute(export_dir)
		EconomyExporter.export_all(state, export_dir, dual.get_recorder().get_command_entries())

	print("BOT-SMOKE-004 PASS: dual-bot economy expansion hash=%s" % hash_a)
	return true
