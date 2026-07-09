class_name GodStrategyMobileFfaController
extends HybridMvpController

## Human P1 vs three learned modular god opponents — God-Strategy product mode.

const FFA_SCENARIO_PATH := "scenarios/god_strategy/god_strategy_4p_human_vs_neural_ffa.json"

var ai_bots: Dictionary = {}
var match_result: Dictionary = {}
var difficulty: String = "normal"
var match_over: bool = false
var human_player_id: String = "p1"
var defeat_shown: bool = false


func reset_ffa(diff: String = "normal") -> void:
	difficulty = diff
	defeat_shown = false
	victory_shown = false
	match_over = false
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(FFA_SCENARIO_PATH)
	human_player_id = str(scenario.get("human_player_id", "p1"))
	var state: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_domination_regression(state, scenario)
	state = runner.run_ticks(state, 1)
	viewer.load_demo_state(state, human_player_id)
	_apply_auto_worker_policy()
	clear_modes()
	ai_bots.clear()
	for pid in ["p2", "p3", "p4"]:
		var bot := LearnedModularGodBot.new(pid)
		bot.use_trained_checkpoint = true
		bot.reset(pid)
		ai_bots[pid] = bot
	match_result = GodStrategyMatchEnd.evaluate(state, scenario)
	message_logged.emit("God-Strategy FFA started — neural opponents active")


func step_tick() -> void:
	if match_over:
		return
	_apply_ai_turns()
	viewer.step_tick()
	var scenario: Dictionary = ConfigLoader.load_scenario(FFA_SCENARIO_PATH)
	match_result = GodStrategyMatchEnd.evaluate(viewer.state, scenario)
	match_over = bool(match_result.get("match_over", false))
	if match_over:
		var winner: String = str(match_result.get("winner_player_id", ""))
		var reason: String = str(match_result.get("end_reason", ""))
		if winner == human_player_id:
			victory_shown = true
			victory_achieved.emit()
			message_logged.emit("Victory (%s)!" % reason)
		else:
			defeat_shown = true
			message_logged.emit("Defeat — %s" % reason)


func get_match_summary() -> Dictionary:
	return {
		"difficulty": difficulty,
		"winner": str(match_result.get("winner_player_id", "")),
		"end_reason": str(match_result.get("end_reason", "")),
		"alive": match_result.get("alive_players", []),
		"eliminated": match_result.get("eliminated_players", []),
		"tick": int(viewer.snapshot.get("tick", 0)),
		"human_player_id": human_player_id,
		"opponent_kind": "learned_modular_god",
	}


func get_ai_telemetry() -> Array:
	var rows: Array = []
	for pid in ai_bots.keys():
		var bot: LearnedModularGodBot = ai_bots[pid] as LearnedModularGodBot
		if bot != null:
			rows.append(bot.get_last_telemetry())
	return rows


func get_god_strategy_overlay() -> Dictionary:
	if viewer.state == null:
		return {}
	var state: GameState = viewer.state
	if int(EconomyState.get_meta_value(state, "god_strategy_active", 0)) <= 0:
		return {}
	return {
		"match": GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_MATCH),
		"pollution": state.entities.get("god_strategy_pollution", {}),
		"worker_needs": state.entities.get("god_strategy_worker_needs", {}),
	}


func _apply_ai_turns() -> void:
	var state: GameState = viewer.state
	if state == null:
		return
	var validator: CommandValidator = viewer.runner.command_validator
	for pid in ai_bots.keys():
		var bot: Bot = ai_bots[pid]
		var snap: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, pid)
		if bot is ScriptedGodStrategyBot:
			(bot as ScriptedGodStrategyBot).bind_state(state)
		bot.observe(snap, pid)
		var cmd: SimCommand = bot.decide(state.tick)
		if cmd != null and cmd.type != "NoopCommand":
			validator.validate_and_apply(state, cmd)
