class_name DualBotRunner
extends RefCounted

## Strict alternation dual-bot runner: p1 command → p2 command → tick (RUN_02.8).

var runner: SimulationTickRunner
var validator: CommandValidator
var recorder: ReplayRecorder
var bot_p1: Bot
var bot_p2: Bot
var decisions: Array = []
var max_ticks: int = 4500
var stall_ticks_threshold: int = 300
var timing_enabled: bool = false
var timing_totals: Dictionary = {}
var timing_tick_count: int = 0


func _init(
	p_runner: SimulationTickRunner = null,
	p_validator: CommandValidator = null,
) -> void:
	runner = p_runner if p_runner != null else SimulationTickRunner.new()
	validator = p_validator if p_validator != null else CommandValidator.new(CommandRegistry.create_default())
	runner.command_validator = validator
	recorder = ReplayRecorder.new()
	_reset_timing()


func _reset_timing() -> void:
	timing_totals = {
		"diagnostics_us": 0,
		"snapshot_us": 0,
		"shortlist_us": 0,
		"bot_apply_us": 0,
		"sim_tick_us": 0,
		"total_us": 0,
	}
	timing_tick_count = 0


func run_match(
	state: GameState,
	scenario: Dictionary,
	checkpoint_ticks: Array = [],
	on_checkpoint: Callable = Callable(),
) -> GameState:
	decisions.clear()
	recorder.clear()
	if timing_enabled:
		_reset_timing()
	max_ticks = int(scenario.get("max_ticks", max_ticks))
	stall_ticks_threshold = int(scenario.get("stall_ticks_threshold", stall_ticks_threshold))

	bot_p1 = EconomyExpansionBot.new("p1")
	bot_p2 = EconomyExpansionBot.new("p2")
	bot_p1.reset("p1")
	bot_p2.reset("p2")

	var pending_checkpoints: Array = []
	for tick in checkpoint_ticks:
		pending_checkpoints.append(int(tick))
	pending_checkpoints.sort()
	_maybe_emit_checkpoint(state, pending_checkpoints, on_checkpoint)

	for _i in range(max_ticks):
		var tick_start: int = Time.get_ticks_usec() if timing_enabled else 0
		var t_diag: int = Time.get_ticks_usec() if timing_enabled else 0
		EconomyDiagnosticsSystem.tick(state, "bot")
		if timing_enabled:
			timing_totals["diagnostics_us"] += Time.get_ticks_usec() - t_diag

		var snapshot: Dictionary = _snapshot_for_bot(state, bot_p1)
		var t_apply1: int = Time.get_ticks_usec() if timing_enabled else 0
		_apply_bot_turn(state, snapshot, bot_p1)
		if timing_enabled:
			timing_totals["bot_apply_us"] += Time.get_ticks_usec() - t_apply1

		snapshot = _snapshot_for_bot(state, bot_p2)
		var t_apply2: int = Time.get_ticks_usec() if timing_enabled else 0
		_apply_bot_turn(state, snapshot, bot_p2)
		if timing_enabled:
			timing_totals["bot_apply_us"] += Time.get_ticks_usec() - t_apply2

		var t_sim: int = Time.get_ticks_usec() if timing_enabled else 0
		state = runner.tick(state)
		if timing_enabled:
			timing_totals["sim_tick_us"] += Time.get_ticks_usec() - t_sim
			timing_totals["total_us"] += Time.get_ticks_usec() - tick_start
			timing_tick_count += 1

		var stall: int = int(EconomyState.get_meta_value(state, "stall_ticks", 0))
		if stall >= stall_ticks_threshold and stall_ticks_threshold > 0:
			break

		_maybe_emit_checkpoint(state, pending_checkpoints, on_checkpoint)

	decisions = bot_p1.get_decisions() + bot_p2.get_decisions()
	return state


func _maybe_emit_checkpoint(
	state: GameState,
	pending_checkpoints: Array,
	on_checkpoint: Callable,
) -> void:
	if not on_checkpoint.is_valid() or pending_checkpoints.is_empty():
		return
	while not pending_checkpoints.is_empty() and int(pending_checkpoints[0]) <= state.tick:
		var target: int = int(pending_checkpoints[0])
		if state.tick == target:
			on_checkpoint.call(state, target)
		pending_checkpoints.remove_at(0)


func get_timing_summary() -> Dictionary:
	if timing_tick_count <= 0:
		return {"tick_count": 0, "ms_per_tick": 0.0}
	var ms_per_tick: float = float(timing_totals.get("total_us", 0)) / float(timing_tick_count) / 1000.0
	return {
		"tick_count": timing_tick_count,
		"ms_per_tick": ms_per_tick,
		"totals_us": timing_totals.duplicate(true),
	}


func _apply_bot_turn(state: GameState, snapshot: Dictionary, bot: Bot) -> void:
	bot.observe(snapshot, bot.player_id)
	var command: SimCommand = bot.decide(state.tick)
	if command != null and command.type != "NoopCommand":
		var result: CommandResult = validator.validate_and_apply(state, command)
		recorder.record(state.tick, command, result)
		if bot.has_method("on_command_applied"):
			bot.on_command_applied(command, result)


func _snapshot_for_bot(state: GameState, bot: Bot) -> Dictionary:
	var t_snap: int = Time.get_ticks_usec() if timing_enabled else 0
	var snapshot: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, bot.player_id)
	if timing_enabled:
		timing_totals["snapshot_us"] += Time.get_ticks_usec() - t_snap
	var t_list: int = Time.get_ticks_usec() if timing_enabled else 0
	var candidates: Array = EconomyCommandAvailability.list_bot_candidate_commands(state, bot.player_id)
	if timing_enabled:
		timing_totals["shortlist_us"] += Time.get_ticks_usec() - t_list
	snapshot["available_commands"] = {bot.player_id: candidates}
	return snapshot


func get_recorder() -> ReplayRecorder:
	return recorder


func get_summary(state: GameState) -> Dictionary:
	EconomyDiagnosticsSystem.tick(state)
	var diag: Dictionary = state.diagnostics
	return {
		"tick": state.tick,
		"economy_scores": diag.get("economy_scores", {}),
		"economy_score_components": diag.get("economy_score_components", {}),
		"command_count": recorder.get_command_entries().size(),
		"hash": ReplayHash.compute_state_hash(state),
	}


static func count_player_metric(state: GameState, player_id: String, metric: String) -> int:
	match metric:
		"buildings":
			return _count_player_buildings(state, player_id)
		"roads":
			return _count_player_roads(state, player_id)
		"deliveries":
			return EconomyState.get_player_stat(state, player_id, "completed_deliveries_count")
	return 0


static func count_player_buildings_built(state: GameState, player_id: String) -> int:
	return maxi(0, _count_player_buildings(state, player_id) - EconomyBvbSetup.get_baseline(state, player_id, "buildings"))


static func count_player_roads_built(state: GameState, player_id: String) -> int:
	return maxi(0, _count_player_roads(state, player_id) - EconomyBvbSetup.get_baseline(state, player_id, "roads"))


static func count_player_deliveries_built(state: GameState, player_id: String) -> int:
	var current: int = EconomyState.get_player_stat(state, player_id, "completed_deliveries_count")
	return maxi(0, current - EconomyBvbSetup.get_baseline(state, player_id, "deliveries"))


static func _count_player_buildings(state: GameState, player_id: String) -> int:
	var hq_id: int = EconomyState.get_hq_building_id(state, player_id)
	var count: int = 0
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if int(building.get("id", 0)) == hq_id:
			continue
		var state_name: String = str(building.get("state", ""))
		if state_name in [
			EconomyConstants.BLD_PLANNED,
			EconomyConstants.BLD_UNDER_CONSTRUCTION,
			EconomyConstants.BLD_DISABLED,
			EconomyConstants.BLD_BLOCKED,
		]:
			continue
		count += 1
	return maxi(0, count)


static func _count_player_roads(state: GameState, player_id: String) -> int:
	var segments: int = 0
	for road in EconomyState.all_roads(state):
		if str(road.get("player_id", "")) == player_id:
			segments += 1
	var nodes: int = 0
	for node in EconomyState.all_road_nodes(state):
		if str(node.get("player_id", "")) == player_id:
			nodes += 1
	return segments + nodes
