class_name HybridMobileFfaController
extends HybridMvpController

## Human Player 1 vs three modular neural AI opponents (PRD Part D).

const FFA_SCENARIO_PATH := "scenarios/hybrid/hybrid_4p_human_vs_neural_ffa.json"

var ai_bots: Dictionary = {}
var ffa_result: Dictionary = {}
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
	for spawn in scenario.get("spawns", []):
		var hq_q: int = int(spawn.get("q", 0))
		var hq_r: int = int(spawn.get("r", 0))
		HybridMapSetup.prepare_playable_map(state, 0, 0, state.map.width - 1, state.map.height - 1, hq_q, hq_r)
	state = runner.run_ticks(state, 1)
	viewer.load_demo_state(state, human_player_id)
	_apply_auto_worker_policy()
	clear_modes()
	ai_bots.clear()
	for pid in ["p2", "p3", "p4"]:
		var bot := ModularNeuralHybridBot.new(pid)
		match difficulty:
			"easy":
				bot.configure_difficulty(true, false)
			"hard":
				bot.configure_difficulty(false, true)
			_:
				bot.configure_difficulty(false, false)
		ai_bots[pid] = bot
	ffa_result = HybridFfaObjective.evaluate(state)
	message_logged.emit("Hybrid FFA started — difficulty %s" % difficulty)


func step_tick() -> void:
	if match_over:
		return
	_apply_ai_turns()
	viewer.step_tick()
	ffa_result = HybridFfaObjective.evaluate(viewer.state)
	match_over = bool(ffa_result.get("match_over", false))
	if match_over:
		var winner: String = str(ffa_result.get("winner_player_id", ""))
		if winner == human_player_id:
			victory_shown = true
			victory_achieved.emit()
			message_logged.emit("Victory! You are the last faction standing.")
		else:
			defeat_shown = true
			message_logged.emit("Defeat — eliminated from the match.")


func get_match_summary() -> Dictionary:
	return {
		"difficulty": difficulty,
		"winner": str(ffa_result.get("winner_player_id", "")),
		"alive": ffa_result.get("alive_players", []),
		"eliminated": ffa_result.get("eliminated_players", []),
		"tick": int(viewer.snapshot.get("tick", 0)),
		"human_player_id": human_player_id,
	}


func get_ai_telemetry() -> Array:
	var rows: Array = []
	for pid in ai_bots.keys():
		var bot: ModularNeuralHybridBot = ai_bots[pid] as ModularNeuralHybridBot
		if bot != null:
			rows.append(bot.get_last_telemetry())
	return rows


func _apply_ai_turns() -> void:
	var state: GameState = viewer.state
	if state == null:
		return
	var validator: CommandValidator = viewer.runner.command_validator
	for pid in ai_bots.keys():
		var bot: Bot = ai_bots[pid]
		var snap: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, pid)
		bot.observe(snap, pid)
		var cmd: SimCommand = bot.decide(state.tick)
		if cmd != null and cmd.type != "NoopCommand":
			validator.validate_and_apply(state, cmd)
