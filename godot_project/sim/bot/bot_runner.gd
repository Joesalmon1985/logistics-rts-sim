class_name BotRunner
extends RefCounted

## Drives a Bot through the same command validator path as humans/tests.

var runner: SimulationTickRunner
var validator: CommandValidator
var bot: Bot
var recorder: ReplayRecorder
var decisions: Array = []
var max_ticks: int = 20000


func _init(
	p_runner: SimulationTickRunner = null,
	p_validator: CommandValidator = null,
) -> void:
	runner = p_runner if p_runner != null else SimulationTickRunner.new()
	validator = p_validator if p_validator != null else CommandValidator.new(CommandRegistry.create_default())
	runner.command_validator = validator
	recorder = ReplayRecorder.new()


func run_until_complete(state: GameState, p_bot: Bot) -> GameState:
	bot = p_bot
	decisions.clear()
	recorder.clear()
	bot.reset(bot.player_id)

	for _i in range(max_ticks):
		EconomyDiagnosticsSystem.tick(state, "bot")
		var snapshot: Dictionary = _snapshot_for_bot(state, bot)
		if bot.is_complete(snapshot):
			break

		bot.observe(snapshot, bot.player_id)
		var command: SimCommand = bot.decide(state.tick)
		if command != null:
			var result: CommandResult = validator.validate_and_apply(state, command)
			recorder.record(state.tick, command, result)
			_record_decision(state, command, result)
			if bot.has_method("on_command_applied"):
				bot.on_command_applied(command, result)

		state = runner.tick(state)

		if _variant_victory(state):
			break

	decisions = bot.get_decisions()
	return state


func get_recorder() -> ReplayRecorder:
	return recorder


func get_decisions() -> Array:
	return decisions


func _snapshot_for_bot(state: GameState, p_bot: Bot) -> Dictionary:
	var snapshot: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, p_bot.player_id)
	var candidates: Array = EconomyCommandAvailability.list_bot_candidate_commands(state, p_bot.player_id)
	snapshot["available_commands"] = {p_bot.player_id: candidates}
	return snapshot


func _record_decision(state: GameState, command: SimCommand, result: CommandResult) -> void:
	decisions.append({
		"tick": state.tick,
		"actor_id": bot.actor_id,
		"legal_actions_count": 1,
		"chosen_command": command.type,
		"reason": "bot decision",
		"expected_effect": "applied" if result.success else str(result.error_code),
	})


func _variant_victory(state: GameState) -> bool:
	if state.variant_id == "military_trial":
		return int(MilitaryState.get_meta_value(state, "victory_decided", 0)) == 1
	if state.variant_id == "economy_trial":
		return false
	if state.variant_id == "hybrid":
		var snap: Dictionary = SnapshotExporter.export_snapshot(state)
		return bool(snap.get("objective", {}).get("complete", false))
	return false
