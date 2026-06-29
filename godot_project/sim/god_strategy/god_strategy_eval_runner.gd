class_name GodStrategyEvalRunner
extends RefCounted

const SCENARIO := "scenarios/god_strategy/god_strategy_4p_mvp_a.json"
const OUT_BASE := "res://../docs/audit/god_strategy/eval/"


static func run_eval_mode(mode_name: String, max_ticks: int, p1_only_scripted: bool = true) -> String:
	SimBootstrap.load_all()
	var runner := SimulationTickRunner.new()
	var quad := QuadBotRunner.new(runner)
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	scenario["max_ticks"] = max_ticks
	if p1_only_scripted:
		scenario["stall_ticks_threshold"] = 0

	var viability: Dictionary = _validate_scenario_or_fail(scenario, runner)
	if not bool(viability.get("ok", false)):
		push_error("Map viability failed: %s" % GodStrategyMapViability.format_failure(viability))
		return ""

	var state: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state, scenario)

	var telemetry: Array = []
	for pid in ["p1", "p2", "p3", "p4"]:
		var bot: Bot
		if p1_only_scripted and pid == "p1":
			bot = ScriptedGodStrategyBot.new(pid)
		elif not p1_only_scripted:
			bot = ScriptedGodStrategyBot.new(pid)
		else:
			bot = PassiveBot.new(pid)
		quad.set_bot(pid, bot)

	state = quad.run_match(state, scenario)
	for pid in ["p1", "p2", "p3", "p4"]:
		var bot: Bot = quad.bots.get(pid)
		if bot is ScriptedGodStrategyBot:
			telemetry.append((bot as ScriptedGodStrategyBot).get_last_telemetry())

	var match_end: Dictionary = GodStrategyMatchEnd.evaluate(state, scenario)
	var out_dir: String = _out_dir(mode_name)
	DirAccess.make_dir_recursive_absolute(out_dir)

	_write_json(out_dir.path_join("match_summary.json"), {
		"mode": mode_name,
		"tick": state.tick,
		"match_end": match_end,
		"viability": viability,
	})
	_write_text(out_dir.path_join("replay_hash.txt"), ReplayHash.compute_state_hash(state))

	var winner: String = str(match_end.get("winner_player_id", ""))
	if winner != "":
		_write_text(out_dir.path_join("winner.txt"), winner)
	else:
		_write_text(out_dir.path_join("no_winner_tick_cap.txt"), "tick_cap=%d" % state.tick)
		_write_json(out_dir.path_join("bot_stall_report.json"), _stall_report(telemetry, match_end))

	_write_player_metrics(out_dir.path_join("player_metrics.csv"), telemetry, state)
	return out_dir


static func _validate_scenario_or_fail(scenario: Dictionary, runner: SimulationTickRunner) -> Dictionary:
	var probe: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(probe, scenario)
	return GodStrategyMapViability.validate_all_spawns(probe, scenario)


static func domination_regression_passes(max_ticks: int = 8000) -> bool:
	var out_dir: String = run_eval_mode("1_scripted_vs_3_passive", max_ticks, true)
	if out_dir == "":
		return false
	if FileAccess.file_exists(out_dir.path_join("winner.txt")):
		var winner: String = FileAccess.get_file_as_string(out_dir.path_join("winner.txt")).strip_edges()
		return winner == "p1"
	var stall_path: String = out_dir.path_join("bot_stall_report.json")
	if not FileAccess.file_exists(stall_path):
		return false
	var stall: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(stall_path))
	if typeof(stall) != TYPE_DICTIONARY:
		return false
	return str(stall.get("blocker_class", "")) != "map"


static func _stall_report(telemetry: Array, match_end: Dictionary) -> Dictionary:
	var p1: Dictionary = {}
	for entry in telemetry:
		if str(entry.get("player_id", "")) == "p1":
			p1 = entry
			break
	return {
		"blocker_class": GodStrategyMapViability.classify_stall(p1),
		"p1_telemetry": p1,
		"match_end": match_end,
		"past_barracks_phase": _past_barracks_phase(p1),
	}


static func _past_barracks_phase(telemetry: Dictionary) -> bool:
	var phase: String = str(telemetry.get("phase", ""))
	return phase in ["queue_unit", "attack", "hold", "wait_production"]


static func _out_dir(mode_name: String) -> String:
	var project_root: String = ProjectSettings.globalize_path("res://").path_join("..")
	return project_root.path_join("docs/audit/god_strategy/eval").path_join(mode_name)


static func _write_json(path: String, data: Variant) -> void:
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		return
	file.store_string(JSON.stringify(data, "\t"))
	file.close()


static func _write_text(path: String, text: String) -> void:
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		return
	file.store_string(text)
	file.close()


static func _write_player_metrics(path: String, telemetry: Array, state: GameState) -> void:
	var lines: PackedStringArray = PackedStringArray(["player_id,phase,intent,rejection,units,buildings"])
	for entry in telemetry:
		var pid: String = str(entry.get("player_id", ""))
		var units: int = 0
		for unit in MilitaryState.all_units(state):
			if str(unit.get("player_id", "")) == pid:
				units += 1
		var buildings: int = 0
		for building in MilitaryState.all_buildings(state):
			if str(building.get("player_id", "")) == pid:
				buildings += 1
		lines.append("%s,%s,%s,%s,%d,%d" % [
			pid,
			str(entry.get("phase", "")),
			str(entry.get("intent", "")),
			str(entry.get("last_rejection_reason", "")),
			units,
			buildings,
		])
	_write_text(path, "\n".join(lines))
