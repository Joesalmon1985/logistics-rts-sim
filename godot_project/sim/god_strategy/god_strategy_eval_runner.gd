class_name GodStrategyEvalRunner
extends RefCounted

const SCENARIO_MVP_A := "scenarios/god_strategy/god_strategy_4p_mvp_a.json"
const SCENARIO_REGRESSION := "scenarios/god_strategy/god_strategy_domination_regression.json"


static func run_eval_mode(
	mode_name: String,
	max_ticks: int,
	p1_only_scripted: bool = true,
	scenario_path: String = "",
) -> String:
	SimBootstrap.load_all()
	var runner := SimulationTickRunner.new()
	var quad := QuadBotRunner.new(runner)
	var path: String = scenario_path if scenario_path != "" else SCENARIO_MVP_A
	var scenario: Dictionary = ConfigLoader.load_scenario(path)
	scenario["max_ticks"] = max_ticks
	if p1_only_scripted:
		scenario["stall_ticks_threshold"] = 0

	var viability: Dictionary = _validate_scenario_or_fail(scenario, runner, path)
	if not bool(viability.get("ok", false)):
		push_error("Map viability failed: %s" % GodStrategyMapViability.format_failure(viability))
		return ""

	var state: GameState = runner.create_game(scenario)
	_prep_state(state, scenario, path)

	var telemetry: Array = []
	var progression: Dictionary = {}
	for pid in scenario.get("players", ["p1", "p2", "p3", "p4"]):
		var bot: Bot
		if p1_only_scripted and str(pid) == "p1":
			bot = ScriptedGodStrategyBot.new(str(pid))
		elif not p1_only_scripted:
			bot = ScriptedGodStrategyBot.new(str(pid))
		else:
			bot = PassiveBot.new(str(pid))
		quad.set_bot(str(pid), bot)

	state = quad.run_match(state, scenario)
	for pid in scenario.get("players", ["p1", "p2", "p3", "p4"]):
		var bot: Bot = quad.bots.get(str(pid))
		if bot is ScriptedGodStrategyBot:
			var report: Dictionary = (bot as ScriptedGodStrategyBot).get_progression_report()
			telemetry.append(report)
			if str(pid) == "p1":
				progression["p1"] = report

	var match_end: Dictionary = GodStrategyMatchEnd.evaluate(state, scenario)
	var out_dir: String = _out_dir(mode_name)
	DirAccess.make_dir_recursive_absolute(out_dir)

	var p1_report: Dictionary = progression.get("p1", {})
	var stall: Dictionary = _stall_report(p1_report, match_end)
	var end_reason: String = str(match_end.get("end_reason", ""))

	_write_json(out_dir.path_join("match_summary.json"), {
		"mode": mode_name,
		"scenario_path": path,
		"tick": state.tick,
		"proof_tick_cap": max_ticks,
		"ticks_at_end": state.tick,
		"furthest_phase": str(p1_report.get("furthest_phase", "")),
		"match_end": match_end,
		"viability": viability,
		"stall": stall,
	})
	_write_json(out_dir.path_join("bot_phase_progression.json"), progression)
	_write_text(out_dir.path_join("match_end_reason.txt"), end_reason)
	_write_text(out_dir.path_join("replay_hash.txt"), ReplayHash.compute_state_hash(state))

	var winner: String = str(match_end.get("winner_player_id", ""))
	if winner != "":
		_write_text(out_dir.path_join("winner.txt"), winner)
	else:
		_write_text(out_dir.path_join("no_winner_tick_cap.txt"), "tick_cap=%d" % state.tick)

	if _needs_stall_report(p1_report, match_end):
		_write_json(out_dir.path_join("bot_stall_report.json"), stall)

	_write_player_metrics(out_dir.path_join("player_metrics.csv"), telemetry, state)
	return out_dir


static func domination_regression_passes(max_ticks: int = GodStrategyConstants.PROOF_TICK_CAP) -> bool:
	return evaluate_domination_pass(
		run_eval_mode("phase0_domination", max_ticks, true, SCENARIO_REGRESSION),
	)


static func evaluate_domination_pass(out_dir: String) -> bool:
	if out_dir == "":
		return false
	var summary_path: String = out_dir.path_join("match_summary.json")
	if not FileAccess.file_exists(summary_path):
		return false
	var summary: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(summary_path))
	if typeof(summary) != TYPE_DICTIONARY:
		return false
	var match_end: Dictionary = summary.get("match_end", {})
	var end_reason: String = str(match_end.get("end_reason", ""))
	var winner: String = str(match_end.get("winner_player_id", ""))
	return end_reason == "domination" and winner == "p1"


static func evaluate_pass(out_dir: String, max_ticks: int = GodStrategyConstants.PROOF_TICK_CAP) -> bool:
	if out_dir == "":
		return false
	var summary_path: String = out_dir.path_join("match_summary.json")
	if not FileAccess.file_exists(summary_path):
		return false
	var summary: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(summary_path))
	if typeof(summary) != TYPE_DICTIONARY:
		return false
	var match_end: Dictionary = summary.get("match_end", {})
	var end_reason: String = str(match_end.get("end_reason", ""))
	var winner: String = str(match_end.get("winner_player_id", ""))

	var progression_path: String = out_dir.path_join("bot_phase_progression.json")
	var progression: Dictionary = {}
	if FileAccess.file_exists(progression_path):
		progression = JSON.parse_string(FileAccess.get_file_as_string(progression_path))
	var p1: Dictionary = progression.get("p1", {}) if typeof(progression) == TYPE_DICTIONARY else {}
	var milestones: Dictionary = p1.get("milestones", {})
	var furthest: String = str(p1.get("furthest_phase", ""))
	var blocker: String = str(summary.get("stall", {}).get("blocker_class", ""))

	if end_reason == "domination" and winner == "p1":
		return true
	if end_reason == "tick_cap":
		var furthest_idx: int = GodStrategyConstants.PHASE_ORDER.find(furthest)
		var queue_idx: int = GodStrategyConstants.PHASE_ORDER.find("queue_unit")
		if furthest_idx >= queue_idx and bool(milestones.get("unit_produced", false)) and blocker != "road_ownership":
			return true
	return false


static func _needs_stall_report(p1: Dictionary, match_end: Dictionary) -> bool:
	var furthest: String = str(p1.get("furthest_phase", "init"))
	var furthest_idx: int = GodStrategyConstants.PHASE_ORDER.find(furthest)
	var barracks_idx: int = GodStrategyConstants.PHASE_ORDER.find("barracks")
	if furthest_idx < barracks_idx:
		return true
	if str(match_end.get("end_reason", "")) == "tick_cap" and not bool(p1.get("milestones", {}).get("unit_produced", false)):
		return true
	return false


static func _prep_state(state: GameState, scenario: Dictionary, scenario_path: String) -> void:
	if scenario_path.ends_with("god_strategy_domination_regression.json"):
		GodStrategyTestHelper.prep_domination_regression(state, scenario)
	else:
		GodStrategyTestHelper.prep_all_spawns(state, scenario)


static func _validate_scenario_or_fail(
	scenario: Dictionary,
	runner: SimulationTickRunner,
	scenario_path: String,
) -> Dictionary:
	var probe: GameState = runner.create_game(scenario)
	_prep_state(probe, scenario, scenario_path)
	return GodStrategyMapViability.validate_all_spawns(probe, scenario)


static func _stall_report(p1: Dictionary, match_end: Dictionary) -> Dictionary:
	return {
		"blocker_class": GodStrategyMapViability.classify_stall(p1),
		"p1_telemetry": p1,
		"match_end": match_end,
		"past_barracks_phase": _past_barracks_phase(p1),
		"furthest_phase": str(p1.get("furthest_phase", "")),
		"milestones": p1.get("milestones", {}),
	}


static func _past_barracks_phase(telemetry: Dictionary) -> bool:
	var furthest: String = str(telemetry.get("furthest_phase", telemetry.get("phase", "")))
	var idx: int = GodStrategyConstants.PHASE_ORDER.find(furthest)
	return idx >= GodStrategyConstants.PHASE_ORDER.find("barracks")


static func _out_dir(mode_name: String) -> String:
	var project_root: String = ProjectSettings.globalize_path("res://").path_join("..")
	if mode_name == "phase0_domination":
		return project_root.path_join("docs/audit/god_strategy/final/phase0")
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
	var lines: PackedStringArray = PackedStringArray([
		"player_id,phase,furthest_phase,intent,rejection,units,buildings",
	])
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
		lines.append("%s,%s,%s,%s,%s,%d,%d" % [
			pid,
			str(entry.get("phase", "")),
			str(entry.get("furthest_phase", "")),
			str(entry.get("intent", "")),
			str(entry.get("last_rejection_reason", "")),
			units,
			buildings,
		])
	_write_text(path, "\n".join(lines))
