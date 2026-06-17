class_name EconomySpectatorController
extends RefCounted

## Dual-bot spectator transport: live DualBotRunner stepping for RUN_05C.

signal snapshot_refreshed(snapshot: Dictionary)
signal bot_action_applied(player_id: String, command_type: String, success: bool, message: String)

const MEANINGFUL_EVENT_SUBSTRINGS: Array[String] = [
	"construction",
	"ware",
	"carrier",
	"production",
	"delivery",
	"road_placed",
]
const STEP_UNTIL_EVENT_MAX: int = 50

var state: GameState
var runner: SimulationTickRunner
var dual: DualBotRunner
var scenario: Dictionary = {}
var snapshot: Dictionary = {}
var render_bridge: EconomySpectatorRenderBridge = EconomySpectatorRenderBridge.new()
var layer_visibility: Dictionary = {
	"terrain": true,
	"buildings": true,
	"units": true,
	"roads": true,
	"resources": true,
	"fog": false,
	"diagnostics": true,
	"logistics": true,
	"forest_density": true,
	"ownership": true,
}
var last_bot_status: Dictionary = {"p1": {}, "p2": {}}
var event_log_lines: PackedStringArray = PackedStringArray()
var autoplay_enabled: bool = false
var speed_multiplier: float = 1.0


func reset_demo() -> void:
	scenario = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_VISUAL)
	runner = SimulationTickRunner.new()
	dual = DualBotRunner.new(runner)
	state = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	event_log_lines = PackedStringArray()
	render_bridge.reset()
	last_bot_status = {"p1": {}, "p2": {}}
	_refresh("Demo reset — dual-bot expansion duel loaded")


func get_render_data() -> Dictionary:
	return render_bridge.last_render_data if not render_bridge.last_render_data.is_empty() else LayerRenderer.build_render_data(snapshot, layer_visibility)


func get_sprite_states() -> Dictionary:
	return render_bridge.sprite_states


func bot_step_once() -> void:
	if state == null:
		return
	EconomyDiagnosticsSystem.tick(state, "bot")
	_apply_bot_turn("p1")
	_apply_bot_turn("p2")
	state = runner.tick(state)
	_refresh("Bot step: p1+p2 commands + tick")


func step_tick() -> void:
	if state == null:
		return
	state = runner.tick(state)
	_refresh("Step tick")


func step_ticks(count: int) -> void:
	for _i in range(maxi(0, count)):
		step_tick()


func step50() -> void:
	step_ticks(50)


func step_until_event() -> Dictionary:
	if state == null:
		return {"steps": 0, "event": "", "found": false}
	var steps: int = 0
	while steps < STEP_UNTIL_EVENT_MAX:
		var event_count_before: int = state.event_log.events.size()
		bot_step_once()
		steps += 1
		for i in range(event_count_before, state.event_log.events.size()):
			var evt: SimEvent = state.event_log.events[i]
			var event_type: String = str(evt.type).to_lower()
			for substring in MEANINGFUL_EVENT_SUBSTRINGS:
				if substring in event_type:
					return {"steps": steps, "event": str(evt.type), "found": true}
	return {"steps": steps, "event": "", "found": false}


func set_layer_visible(layer_name: String, visible: bool) -> void:
	layer_visibility[layer_name] = visible
	if not snapshot.is_empty():
		render_bridge.update(state, snapshot, layer_visibility)


func build_summary_text() -> String:
	if snapshot.is_empty():
		return "Summary: (no snapshot)"
	var render: Dictionary = get_render_data()
	var fp: Dictionary = LayerRenderer.chain_fingerprint(render)
	var diag: Dictionary = snapshot.get("diagnostics", {})
	var scores: Dictionary = diag.get("economy_scores", {})
	var score_line: String = ""
	if not scores.is_empty():
		var parts: PackedStringArray = PackedStringArray()
		for player_id in scores.keys():
			parts.append("%s=%d" % [str(player_id), int(scores[player_id])])
		score_line = "Scores [%s] | " % ", ".join(parts)
	var counts: Dictionary = LayerRenderer.count_entities(render)
	return (
		"%sTick %d | roads %d | sites %d | carriers %d | jobs %d | sprites %d | forest D:%s"
		% [
			score_line,
			int(snapshot.get("tick", 0)),
			int(counts.get("roads", 0)),
			int(counts.get("construction_sites", 0)),
			int(counts.get("carriers", 0)),
			int(counts.get("delivery_jobs", 0)),
			render_bridge.sprite_states.size(),
			str(fp.get("forest_density", "?")),
		]
	)


func build_bot_status_text() -> String:
	var lines: PackedStringArray = PackedStringArray()
	lines.append("Bot mode: %s | speed %.1fx" % [
		"AUTOPLAY" if autoplay_enabled else "MANUAL",
		speed_multiplier,
	])
	for player_id in ["p1", "p2"]:
		var status: Dictionary = last_bot_status.get(player_id, {})
		lines.append(
			"%s last: %s | cmd: %s | ok=%s"
			% [
				player_id,
				str(status.get("decision", "(none)")),
				str(status.get("command_type", "(none)")),
				str(status.get("success", "?")),
			]
		)
	return "\n".join(lines)


func build_event_log_text() -> String:
	return "\n".join(event_log_lines)


func _apply_bot_turn(player_id: String) -> void:
	var bot: EconomyExpansionBot = dual.bot_p1 if player_id == "p1" else dual.bot_p2
	if bot == null:
		return
	var snap: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, player_id)
	bot.observe(snap, player_id)
	var command: SimCommand = bot.decide(state.tick)
	var status: Dictionary = {
		"decision": "(noop)",
		"command_type": "NoopCommand",
		"success": true,
		"message": "",
	}
	if command != null and command.type != "NoopCommand":
		var result: CommandResult = dual.validator.validate_and_apply(state, command)
		dual.recorder.record(state.tick, command, result)
		if bot.has_method("on_command_applied"):
			bot.on_command_applied(command, result)
		var decisions: Array = bot.get_decisions()
		var last_decision: String = "(none)"
		if not decisions.is_empty():
			var d: Dictionary = decisions[-1]
			last_decision = str(d.get("reason", d.get("action", command.type)))
		status = {
			"decision": last_decision,
			"command_type": command.type,
			"success": result.success,
			"message": result.message,
		}
		bot_action_applied.emit(player_id, command.type, result.success, result.message)
	last_bot_status[player_id] = status


func _refresh(note: String = "") -> void:
	if state == null:
		snapshot = {}
		return
	snapshot = SnapshotExporter.export_debug_snapshot(state, "p1")
	render_bridge.update(state, snapshot, layer_visibility)
	if note != "":
		_append_event(note)
	_collect_recent_sim_events()
	snapshot_refreshed.emit(snapshot)


func _collect_recent_sim_events() -> void:
	if state == null:
		return
	var start: int = maxi(0, state.event_log.events.size() - 6)
	for i in range(start, state.event_log.events.size()):
		var evt: SimEvent = state.event_log.events[i]
		var line: String = "[tick %d] %s" % [state.tick, str(evt.type)]
		if not event_log_lines.has(line):
			event_log_lines.insert(0, line)
	while event_log_lines.size() > 40:
		event_log_lines.remove_at(event_log_lines.size() - 1)


func _append_event(text: String) -> void:
	var stamp: String = "[tick %d] " % int(snapshot.get("tick", 0))
	event_log_lines.insert(0, stamp + text)
	while event_log_lines.size() > 40:
		event_log_lines.remove_at(event_log_lines.size() - 1)
