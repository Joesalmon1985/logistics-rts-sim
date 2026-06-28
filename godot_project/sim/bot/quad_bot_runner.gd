class_name QuadBotRunner
extends RefCounted

## Four-bot runner: each bot submits one command per tick, then sim ticks once.

var runner: SimulationTickRunner
var validator: CommandValidator
var recorder: ReplayRecorder
var bots: Dictionary = {}
var player_order: Array[String] = ["p1", "p2", "p3", "p4"]
var max_ticks: int = 50000
var stall_ticks_threshold: int = 1000


func _init(
	p_runner: SimulationTickRunner = null,
	p_validator: CommandValidator = null,
) -> void:
	runner = p_runner if p_runner != null else SimulationTickRunner.new()
	validator = p_validator if p_validator != null else CommandValidator.new(CommandRegistry.create_default())
	runner.command_validator = validator
	recorder = ReplayRecorder.new()


func set_bot(player_id: String, bot: Bot) -> void:
	bots[player_id] = bot
	if bot != null:
		bot.reset(player_id)


func run_match(state: GameState, scenario: Dictionary) -> GameState:
	recorder.clear()
	max_ticks = int(scenario.get("max_ticks", max_ticks))
	stall_ticks_threshold = int(scenario.get("stall_ticks_threshold", stall_ticks_threshold))
	player_order = []
	for pid in scenario.get("players", ["p1", "p2", "p3", "p4"]):
		player_order.append(str(pid))

	for _i in range(max_ticks):
		EconomyDiagnosticsSystem.tick(state, "bot")
		for pid in player_order:
			var bot: Bot = bots.get(pid)
			if bot == null:
				continue
			_apply_bot_turn(state, pid, bot)
		state = runner.tick(state)
		var stall: int = int(EconomyState.get_meta_value(state, "stall_ticks", 0))
		if stall >= stall_ticks_threshold and stall_ticks_threshold > 0:
			break
		var ffa: Dictionary = HybridFfaObjective.evaluate(state)
		if bool(ffa.get("match_over", false)):
			break
	return state


func _apply_bot_turn(state: GameState, player_id: String, bot: Bot) -> void:
	var snapshot: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, player_id)
	bot.observe(snapshot, player_id)
	var command: SimCommand = bot.decide(state.tick)
	if command != null and command.type != "NoopCommand":
		var result: CommandResult = validator.validate_and_apply(state, command)
		recorder.record(state.tick, command, result)
		if bot.has_method("on_command_applied"):
			bot.on_command_applied(command, result)


func get_recorder() -> ReplayRecorder:
	return recorder
