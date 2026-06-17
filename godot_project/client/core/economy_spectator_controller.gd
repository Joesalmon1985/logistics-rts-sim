class_name EconomySpectatorController
extends RefCounted

## Spectator transport for RUN_05C — production-chain demo (default) or optional dual-bot view.

signal snapshot_refreshed(snapshot: Dictionary)
signal bot_action_applied(player_id: String, command_type: String, success: bool, message: String)

const DEMO_PRODUCTION_CHAIN: String = "production_chain"
const DEMO_DUAL_BOT: String = "dual_bot"

const MEANINGFUL_EVENT_SUBSTRINGS: Array[String] = [
	"construction",
	"ware",
	"carrier",
	"production",
	"delivery",
	"road_placed",
	"tree_density",
]
const STEP_UNTIL_EVENT_MAX: int = 50
const PRODUCTION_CHAIN_TICKS_RECOMMENDED: int = 30

var state: GameState
var runner: SimulationTickRunner
var dual: DualBotRunner
var scenario: Dictionary = {}
var snapshot: Dictionary = {}
var render_bridge: EconomySpectatorRenderBridge = EconomySpectatorRenderBridge.new()
var demo_mode: String = DEMO_PRODUCTION_CHAIN
var demo_label: String = "Production chain (seed 42)"
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
	"sprites": true,
	"routes": true,
}
var last_bot_status: Dictionary = {"p1": {}}
var event_log_lines: PackedStringArray = PackedStringArray()
var recent_change_lines: PackedStringArray = PackedStringArray()
var _prev_fp: Dictionary = {}
var _prev_sprite_sig: String = ""
var autoplay_enabled: bool = false
var speed_multiplier: float = 1.0


func reset_demo(mode: String = DEMO_PRODUCTION_CHAIN) -> void:
	demo_mode = mode
	dual = null
	runner = SimulationTickRunner.new()
	event_log_lines = PackedStringArray()
	recent_change_lines = PackedStringArray()
	_prev_fp = {}
	_prev_sprite_sig = ""
	render_bridge.reset()
	last_bot_status = {"p1": {}}
	if demo_mode == DEMO_DUAL_BOT:
		_reset_dual_bot()
	else:
		_reset_production_chain()
	_refresh("Demo reset — %s" % demo_label)


func _reset_production_chain() -> void:
	demo_label = "Production chain (seed 42)"
	state = EconomyDemo.build_production_chain()
	last_bot_status["p1"] = {
		"decision": "auto logistics (sim tick)",
		"command_type": "(sim tick)",
		"success": true,
		"message": "HQ woodcutter → sawmill chain",
	}


func _reset_dual_bot() -> void:
	demo_label = "Dual-bot view (manual diagnostic — not expansion proof)"
	scenario = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_VISUAL)
	dual = DualBotRunner.new(runner)
	state = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	last_bot_status = {"p1": {}, "p2": {}}


func get_render_data() -> Dictionary:
	return render_bridge.last_render_data if not render_bridge.last_render_data.is_empty() else LayerRenderer.build_render_data(snapshot, layer_visibility)


func get_sprite_states() -> Dictionary:
	return render_bridge.sprite_states


func autoplay_step() -> void:
	if demo_mode == DEMO_DUAL_BOT:
		bot_step_once()
	else:
		step_tick()


func bot_step_once() -> void:
	if state == null:
		return
	if demo_mode != DEMO_DUAL_BOT:
		step_tick()
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
	_refresh("Sim tick" if demo_mode == DEMO_PRODUCTION_CHAIN else "Step tick")


func step_ticks(count: int) -> void:
	for _i in range(maxi(0, count)):
		autoplay_step()


func step50() -> void:
	step_ticks(50)


func step_until_event() -> Dictionary:
	if state == null:
		return {"steps": 0, "event": "", "found": false}
	var steps: int = 0
	while steps < STEP_UNTIL_EVENT_MAX:
		var event_count_before: int = state.event_log.events.size()
		autoplay_step()
		steps += 1
		for i in range(event_count_before, state.event_log.events.size()):
			var evt: SimEvent = state.event_log.events[i]
			var event_type: String = str(evt.type).to_lower()
			for substring in MEANINGFUL_EVENT_SUBSTRINGS:
				if substring in event_type:
					return {"steps": steps, "event": str(evt.type), "found": true}
	return {"steps": steps, "event": "", "found": false}


func measure_activity_after_steps(step_count: int, mode: String = DEMO_PRODUCTION_CHAIN) -> Dictionary:
	reset_demo(mode)
	var counts_before: Dictionary = LayerRenderer.count_entities(get_render_data())
	var fp_before: Dictionary = EconomySpectatorMetrics.chain_fingerprint(get_render_data())
	var sprite_before: Dictionary = get_sprite_states().duplicate(true)
	var snapshot_before: Dictionary = snapshot.duplicate(true)
	var events_before: int = state.event_log.events.size() if state != null else 0
	for _i in range(step_count):
		autoplay_step()
	var counts_after: Dictionary = LayerRenderer.count_entities(get_render_data())
	var fp_after: Dictionary = EconomySpectatorMetrics.chain_fingerprint(get_render_data())
	var sprite_after: Dictionary = get_sprite_states().duplicate(true)
	var snapshot_after: Dictionary = snapshot.duplicate(true)
	var events_after: int = state.event_log.events.size() if state != null else 0
	return EconomySpectatorMetrics.measure_activity(
		counts_before,
		counts_after,
		fp_before,
		fp_after,
		sprite_before,
		sprite_after,
		snapshot_before,
		snapshot_after,
		events_before,
		events_after,
	)


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
	var counts: Dictionary = LayerRenderer.count_entities(render)
	var stalls: Dictionary = diag.get("player_stalls", {})
	var stall_line: String = ""
	if not stalls.is_empty():
		stall_line = " | stalls %s" % str(stalls)
	return (
		"Demo: %s | tick %d | roads %d | sites %d | carriers %d | jobs %d | sprites %d | forest D:%s%s"
		% [
			demo_label,
			int(snapshot.get("tick", 0)),
			int(counts.get("roads", 0)),
			int(counts.get("construction_sites", 0)),
			int(counts.get("carriers", 0)),
			int(counts.get("delivery_jobs", 0)),
			render_bridge.sprite_states.size(),
			str(fp.get("forest_density", "?")),
			stall_line,
		]
	)


func build_bot_status_text() -> String:
	var lines: PackedStringArray = PackedStringArray()
	lines.append("Mode: %s | transport %s | speed %.1fx" % [
		demo_label,
		"AUTOPLAY" if autoplay_enabled else "MANUAL",
		speed_multiplier,
	])
	if demo_mode == DEMO_DUAL_BOT:
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
	else:
		var status: Dictionary = last_bot_status.get("p1", {})
		lines.append(
			"Activity: %s | %s"
			% [str(status.get("decision", "?")), str(status.get("message", ""))]
		)
	return "\n".join(lines)


func build_activity_hint() -> String:
	var lines: PackedStringArray = PackedStringArray()
	lines.append("Current visual mode: production-chain demo")
	var fp: Dictionary = LayerRenderer.chain_fingerprint(get_render_data())
	var hints: PackedStringArray = PackedStringArray()
	if int(fp.get("forest_density", -1)) >= 0:
		hints.append("Forest D:%s" % str(fp.get("forest_density", "?")))
	if int(fp.get("carrier_moving", 0)) > 0:
		hints.append("carriers moving")
	if int(fp.get("ware_routes", 0)) > 0:
		hints.append("ware routes active")
	if snapshot.get("construction_sites", []).size() > 0:
		hints.append("construction sites open")
	if hints.is_empty():
		lines.append("Now: (idle — step ticks to observe chain)")
	else:
		lines.append("Now: " + ", ".join(hints))
	var blocked: PackedStringArray = PackedStringArray()
	for job in snapshot.get("blocked_job_markers", []):
		if typeof(job) == TYPE_DICTIONARY:
			blocked.append(str(job.get("reason", "blocked")))
	if not blocked.is_empty():
		lines.append("Warning: " + ", ".join(blocked))
	lines.append("Recent changes:")
	var recent: String = build_recent_changes_text()
	lines.append(recent if recent != "" else "(none yet — step ticks)")
	return "\n".join(lines)


func build_recent_changes_text() -> String:
	if recent_change_lines.is_empty():
		return ""
	return "\n".join(recent_change_lines)


func _record_recent_changes() -> void:
	var fp: Dictionary = EconomySpectatorMetrics.chain_fingerprint(get_render_data())
	var sig: String = EconomySpectatorMetrics.sprite_state_signature(get_sprite_states())
	if not _prev_fp.is_empty():
		if int(fp.get("forest_density", 0)) != int(_prev_fp.get("forest_density", 0)):
			_push_recent_change("Forest D %s -> %s" % [str(_prev_fp.get("forest_density", "?")), str(fp.get("forest_density", "?"))])
		if int(fp.get("carrier_moving", 0)) != int(_prev_fp.get("carrier_moving", 0)):
			_push_recent_change("Carriers moving %d -> %d" % [int(_prev_fp.get("carrier_moving", 0)), int(fp.get("carrier_moving", 0))])
		if int(fp.get("ware_routes", 0)) != int(_prev_fp.get("ware_routes", 0)):
			_push_recent_change("Ware routes %d -> %d" % [int(_prev_fp.get("ware_routes", 0)), int(fp.get("ware_routes", 0))])
	if _prev_sprite_sig != "" and sig != _prev_sprite_sig:
		_push_recent_change("Sprite states updated")
	_prev_fp = fp.duplicate(true)
	_prev_sprite_sig = sig


func _push_recent_change(text: String) -> void:
	var line: String = "[tick %d] %s" % [int(snapshot.get("tick", 0)), text]
	recent_change_lines.insert(0, line)
	while recent_change_lines.size() > 12:
		recent_change_lines.remove_at(recent_change_lines.size() - 1)


func build_event_log_text() -> String:
	return "\n".join(event_log_lines)


func _apply_bot_turn(player_id: String) -> void:
	if dual == null:
		return
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
	_record_recent_changes()
	if note != "":
		_append_event(note)
	_collect_recent_sim_events()
	snapshot_refreshed.emit(snapshot)


func _collect_recent_sim_events() -> void:
	if state == null:
		return
	var start: int = maxi(0, state.event_log.events.size() - 8)
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
