extends SceneTree

## Evaluate trained modular neural bots and write artefacts.

const SCENARIO := "scenarios/hybrid/hybrid_4p_training_ffa.json"
const OUT_BASE := "res://../docs/audit/neural_hybrid_ai/eval/"


func _initialize() -> void:
	SimBootstrap.load_all()
	var modes: Array = [
		{"name": "1_neural_vs_3_passive", "players": _passive_setup()},
		{"name": "1_neural_vs_3_weak", "players": _weak_setup()},
		{"name": "4_neural_ffa", "players": _all_neural_setup()},
	]
	var ok: bool = true
	for mode in modes:
		if not _run_eval(str(mode.get("name", "")), mode.get("players", {})):
			ok = false
	if ok:
		print("TRAINED EVAL PASS")
		quit(0)
	print("TRAINED EVAL FAIL")
	quit(1)


func _passive_setup() -> Dictionary:
	return {
		"p1": {"trained": true},
		"p2": {"trained": false, "noop": true},
		"p3": {"trained": false, "noop": true},
		"p4": {"trained": false, "noop": true},
	}


func _weak_setup() -> Dictionary:
	return {
		"p1": {"trained": true},
		"p2": {"trained": false, "noop": false},
		"p3": {"trained": false, "noop": false},
		"p4": {"trained": false, "noop": false},
	}


func _all_neural_setup() -> Dictionary:
	return {
		"p1": {"trained": true},
		"p2": {"trained": true},
		"p3": {"trained": true},
		"p4": {"trained": true},
	}


func _run_eval(eval_name: String, player_cfg: Dictionary) -> bool:
	var runner := SimulationTickRunner.new()
	var quad := QuadBotRunner.new(runner)
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	scenario["max_ticks"] = 200
	var state: GameState = runner.create_game(scenario)
	for spawn in scenario.get("spawns", []):
		HybridMapSetup.prepare_playable_map(
			state, 0, 0, state.map.width - 1, state.map.height - 1,
			int(spawn.get("q", 0)), int(spawn.get("r", 0)),
		)
	var telemetry: Array = []
	var actions: Array = []
	for pid in ["p1", "p2", "p3", "p4"]:
		var cfg: Dictionary = player_cfg.get(pid, {})
		var bot: Bot
		if bool(cfg.get("trained", false)):
			bot = ModularNeuralHybridBot.new(pid)
			bot.use_trained_checkpoint = true
			bot.use_heuristic_teacher = false
		elif bool(cfg.get("noop", false)):
			bot = PassiveBot.new(pid)
		else:
			bot = ModularNeuralHybridBot.new(pid)
			bot.use_trained_checkpoint = false
			bot.use_heuristic_teacher = true
		quad.set_bot(pid, bot)

	for _t in range(int(scenario.get("max_ticks", 200))):
		EconomyDiagnosticsSystem.tick(state, "eval")
		for pid in ["p1", "p2", "p3", "p4"]:
			var bot: Bot = quad.bots.get(pid)
			var snap: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, pid)
			bot.observe(snap, pid)
			var cmd: SimCommand = bot.decide(state.tick)
			if cmd != null and cmd.type != "NoopCommand":
				runner.command_validator.validate_and_apply(state, cmd)
				actions.append({"tick": state.tick, "player_id": pid, "command": cmd.type})
			if bot is ModularNeuralHybridBot:
				var telem: Dictionary = (bot as ModularNeuralHybridBot).get_last_telemetry()
				if not telem.is_empty():
					telemetry.append(telem)
		state = runner.tick(state)
		var ffa: Dictionary = HybridFfaObjective.evaluate(state)
		if bool(ffa.get("match_over", false)):
			break

	var out_dir: String = _out_dir(eval_name)
	DirAccess.make_dir_recursive_absolute(out_dir)
	_write_json(out_dir.path_join("match_summary.json"), {
		"eval_name": eval_name,
		"tick": state.tick,
		"ffa": HybridFfaObjective.evaluate(state),
	})
	_write_json(out_dir.path_join("final_snapshot.json"), HybridSnapshotExporter.export_snapshot(state))
	_write_jsonl(out_dir.path_join("decision_telemetry.jsonl"), telemetry)
	_write_csv_actions(out_dir.path_join("action_trace.csv"), actions)
	_write_player_metrics(out_dir.path_join("player_metrics.csv"), telemetry, state)
	_write_reward_trace(out_dir.path_join("reward_trace.csv"), telemetry)
	var winner: String = str(HybridFfaObjective.evaluate(state).get("winner_player_id", ""))
	if winner != "":
		_write_text(out_dir.path_join("winner.txt"), winner)
	else:
		_write_text(out_dir.path_join("no_winner_tick_cap.txt"), "tick_cap=%d" % state.tick)
	return _trained_used(telemetry)


func _trained_used(telemetry: Array) -> bool:
	for row in telemetry:
		if str(row.get("inference_mode", "")) == "trained_neural":
			return true
	return false


func _out_dir(name: String) -> String:
	return ProjectSettings.globalize_path(OUT_BASE.path_join(name))


func _write_json(path: String, data: Variant) -> void:
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(data, "\t"))


func _write_text(path: String, text: String) -> void:
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f != null:
		f.store_string(text)


func _write_jsonl(path: String, rows: Array) -> void:
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f == null:
		return
	for row in rows:
		f.store_line(JSON.stringify(row))


func _write_csv_actions(path: String, rows: Array) -> void:
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f == null:
		return
	f.store_line("tick,player_id,command")
	for row in rows:
		f.store_line("%s,%s,%s" % [row.get("tick", 0), row.get("player_id", ""), row.get("command", "")])


func _write_player_metrics(path: String, telemetry: Array, state: GameState) -> void:
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f == null:
		return
	f.store_line("player_id,inference_mode,decisions,heuristic_fallback")
	var counts: Dictionary = {}
	for row in telemetry:
		var pid: String = str(row.get("player_id", ""))
		if not counts.has(pid):
			counts[pid] = {"decisions": 0, "trained": 0, "fallback": 0, "mode": ""}
		counts[pid]["decisions"] += 1
		if str(row.get("inference_mode", "")) == "trained_neural":
			counts[pid]["trained"] += 1
			counts[pid]["mode"] = "trained_neural"
		if bool(row.get("heuristic_fallback", false)):
			counts[pid]["fallback"] += 1
	for pid in counts.keys():
		var c: Dictionary = counts[pid]
		f.store_line("%s,%s,%d,%d" % [pid, c.get("mode", ""), c.get("decisions", 0), c.get("fallback", 0)])


func _write_reward_trace(path: String, telemetry: Array) -> void:
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f == null:
		return
	f.store_line("tick,player_id,reward_proxy,inference_mode")
	for row in telemetry:
		var reward: float = 1.0 if str(row.get("inference_mode", "")) == "trained_neural" else 0.0
		f.store_line("%d,%s,%.3f,%s" % [int(row.get("tick", 0)), row.get("player_id", ""), reward, row.get("inference_mode", "")])
