class_name RolloutController
extends RefCounted

signal snapshot_refreshed(snapshot: Dictionary)

var folder_path: String = ""
var metadata: Dictionary = {}
var metrics: Dictionary = {}
var commands: Array = []
var events: Array = []
var snapshot_paths: Array = []
var step_index: int = 0
var snapshot: Dictionary = {}
var layer_visibility: Dictionary = InterfaceModePresets.apply_mode("world")
var ui_flags: Dictionary = InterfaceModePresets.apply_ui_flags("world")
var interface_mode: String = "world"
var command_target: Dictionary = {}
var _cached_prev_snapshot: Dictionary = {}


func load_rollout(path: String) -> Dictionary:
	var loaded: Dictionary = RolloutLoader.load_folder(path)
	if not loaded.get("ok", false):
		return loaded
	folder_path = path
	metadata = loaded.get("metadata", {})
	metrics = loaded.get("metrics", {})
	commands = loaded.get("commands", [])
	events = loaded.get("events", [])
	snapshot_paths = loaded.get("snapshot_paths", [])
	step_index = 0
	_refresh_snapshot()
	return loaded


func get_command_count() -> int:
	return commands.size()


func get_snapshot_count() -> int:
	return snapshot_paths.size()


func step_forward() -> bool:
	if step_index >= get_command_count():
		return false
	step_index += 1
	_refresh_snapshot()
	return true


func step_back() -> bool:
	if step_index <= 0:
		return false
	step_index -= 1
	_refresh_snapshot()
	return true


func jump_to_start() -> void:
	step_index = 0
	_refresh_snapshot()


func jump_to_end() -> void:
	step_index = maxi(0, get_command_count())
	_refresh_snapshot()


func set_interface_mode(mode: String) -> void:
	interface_mode = mode
	layer_visibility = InterfaceModePresets.apply_mode(mode)
	ui_flags = InterfaceModePresets.apply_ui_flags(mode)
	snapshot_refreshed.emit(snapshot)


func get_render_data() -> Dictionary:
	return LayerRenderer.build_render_data(snapshot, layer_visibility)


func get_previous_command() -> Dictionary:
	if step_index <= 0 or commands.is_empty():
		return {}
	var idx: int = step_index - 1
	if idx < 0 or idx >= commands.size():
		return {}
	return commands[idx]


func get_current_decision() -> Dictionary:
	if step_index >= commands.size():
		return {}
	return commands[step_index]


func get_events_for_step() -> Array:
	var rows: Array = []
	for event in events:
		if int(event.get("step_index", -1)) == step_index:
			rows.append(event)
	return rows


func get_events_for_previous_step() -> Array:
	if step_index <= 0:
		return []
	var rows: Array = []
	var prev_step: int = step_index - 1
	for event in events:
		if int(event.get("step_index", -1)) == prev_step:
			rows.append(event)
	return rows


func get_command_overlays() -> Array:
	var show_prev: bool = bool(ui_flags.get("show_previous_command_overlay", false))
	var show_cur: bool = bool(ui_flags.get("show_current_command_overlay", false))
	var overlays: Array = RolloutCommandOverlay.build_command_overlays(
		get_previous_command(),
		get_current_decision(),
		show_prev,
		show_cur,
	)
	if bool(ui_flags.get("show_military_combat_overlay", false)):
		for overlay in RolloutCommandOverlay.military_combat_overlays(snapshot):
			overlays.append(overlay)
	return overlays


func get_economy_badges() -> Array:
	if not bool(ui_flags.get("show_economy_badges", false)):
		return []
	return RolloutCommandOverlay.economy_badges(snapshot)


func get_focus_hexes() -> Array:
	var hexes: Array = RolloutCommandOverlay.focus_hexes_from_overlays(get_command_overlays())
	if bool(ui_flags.get("show_military_combat_overlay", false)):
		hexes = RolloutCommandOverlay.military_focus_hexes(snapshot)
	elif hexes.is_empty() and not snapshot.is_empty():
		for building in snapshot.get("buildings", []):
			if typeof(building) != TYPE_DICTIONARY:
				continue
			hexes.append({
				"q": int(building.get("anchor_q", building.get("q", -1))),
				"r": int(building.get("anchor_r", building.get("r", -1))),
			})
	return hexes


func build_status_text() -> String:
	var checkpoint: String = str(metadata.get("checkpoint_id", metadata.get("checkpoint_path", "")))
	var ckpt_part: String = ""
	if checkpoint != "" and checkpoint != "null":
		ckpt_part = "  |  Checkpoint: %s" % checkpoint
	return (
		"Scenario: %s  |  Policy: %s  |  Seed: %s  |  Mode: %s  |  Tick: %d  |  Step: %d / %d%s"
		% [
			str(metadata.get("scenario_name", "?")),
			str(metadata.get("policy_type", "?")),
			str(metadata.get("seed", "?")),
			interface_mode.capitalize(),
			int(snapshot.get("tick", 0)),
			step_index,
			get_command_count(),
			ckpt_part,
		]
	)


func build_metrics_text() -> String:
	var parts: PackedStringArray = PackedStringArray()
	parts.append("Steps: %d" % int(metrics.get("steps", get_command_count())))
	parts.append("No-ops: %d" % int(metrics.get("no_ops", 0)))
	parts.append("Rejected: %d" % int(metrics.get("rejected_commands", 0)))
	parts.append("Valid cmds: %d" % int(metrics.get("valid_commands", 0)))
	parts.append("Outcome: %s" % str(metrics.get("final_outcome", "?")))
	var objective: Dictionary = metrics.get("objective_progress", {})
	if typeof(objective) == TYPE_DICTIONARY and not objective.is_empty():
		parts.append("Result: %s (%s)" % [str(objective.get("outcome", "?")), str(objective.get("reason", "?"))])
	var score: Variant = metrics.get("total_score")
	if score != null:
		parts.append("Fitness/score: %s" % str(score))
	var mode_summary: String = build_mode_summary_text()
	if mode_summary != "":
		parts.append(mode_summary)
	return "Metrics — " + " | ".join(parts)


func build_mode_summary_text() -> String:
	match interface_mode:
		"economy":
			return _economy_summary_one_line()
		"military":
			return _military_summary_one_line()
		"neural":
			return _neural_diagnostics_one_line()
		_:
			return ""


func build_action_text() -> String:
	var prev: Dictionary = get_previous_command()
	var cur: Dictionary = get_current_decision()
	var lines: PackedStringArray = PackedStringArray()
	if not prev.is_empty():
		lines.append(
			"[b]Previous command[/b] (produced this snapshot): %s — %s"
			% [str(prev.get("command_label", "?")), str(prev.get("result_message", ""))]
		)
		if prev.has("selected_action_index"):
			lines.append(_policy_line(prev))
	else:
		lines.append("[b]Previous command[/b]: (none — initial snapshot)")
	if not cur.is_empty():
		lines.append(
			"[b]Current / next decision[/b] (from this snapshot): %s — %s"
			% [str(cur.get("command_label", "?")), str(cur.get("result_message", ""))]
		)
		if cur.has("selected_action_index"):
			lines.append(_policy_line(cur))
	else:
		lines.append("[b]Current / next decision[/b]: (terminal — no further command)")
	if interface_mode == "neural":
		var neural: String = build_neural_panel_text()
		if neural != "":
			lines.append(neural)
	return "\n".join(lines)


func build_neural_panel_text() -> String:
	var cur: Dictionary = get_current_decision()
	if cur.is_empty():
		return "[i]Neural[/i]: terminal snapshot — no pending decision"
	var lines: PackedStringArray = PackedStringArray()
	lines.append("[b]Policy diagnostics[/b]")
	lines.append(_policy_line(cur))
	var reason: String = str(cur.get("policy_confidence_unavailable_reason", ""))
	if reason != "" and reason != "null":
		lines.append("Confidence note: %s" % reason)
	elif cur.get("policy_confidence") == null:
		lines.append("Confidence note: not exported for this policy type")
	return "\n".join(lines)


func _policy_line(cmd: Dictionary) -> String:
	var parts: PackedStringArray = PackedStringArray()
	if cmd.has("selected_action_index"):
		parts.append("action=%s" % str(cmd.get("selected_action_index")))
	if cmd.has("valid_action_count"):
		parts.append("valid=%s" % str(cmd.get("valid_action_count")))
	if cmd.has("selected_action_rank"):
		parts.append("rank=%s" % str(cmd.get("selected_action_rank")))
	var conf: Variant = cmd.get("policy_confidence")
	if conf != null:
		parts.append("confidence=%.3f" % float(conf))
	if parts.is_empty():
		return "Policy: (no diagnostics exported)"
	return "Policy: " + " | ".join(parts)


func build_debug_text() -> String:
	var counts: Dictionary = LayerRenderer.count_entities(get_render_data())
	return (
		"Debug — variant:%s tick:%d bld:%d sites:%d roads:%d units:%d | rollout:%s"
		% [
			str(snapshot.get("variant", "")),
			int(snapshot.get("tick", 0)),
			int(counts.get("buildings", 0)),
			int(counts.get("construction_sites", 0)),
			int(counts.get("roads", 0)),
			int(counts.get("units", 0)),
			folder_path.get_file(),
		]
	)


func build_debug_json_preview() -> String:
	if snapshot.is_empty():
		return "(no snapshot)"
	var keys: Array = ["schema_version", "variant", "tick", "seed", "buildings", "units", "diagnostics"]
	var preview: Dictionary = {}
	for key in keys:
		if snapshot.has(key):
			preview[key] = snapshot[key]
	preview["available_commands_count"] = snapshot.get("available_commands", []).size()
	if bool(ui_flags.get("show_raw_events", false)):
		preview["events_this_step"] = get_events_for_step()
	return JSON.stringify(preview, "\t")


func build_event_text() -> String:
	if bool(ui_flags.get("show_raw_events", false)):
		return _format_raw_events()
	return _format_aggregated_events()


func build_delta_summary_text() -> String:
	if step_index <= 0:
		return "[b]Changed since previous step[/b]: (initial snapshot)"
	var prev: Dictionary = _cached_prev_snapshot
	if prev.is_empty():
		return "[b]Changed since previous step[/b]: (previous snapshot unavailable)"
	var lines: PackedStringArray = PackedStringArray()
	lines.append("[b]Changed since previous step[/b]")
	lines.append("- roads: %d -> %d" % [prev.get("roads", []).size(), snapshot.get("roads", []).size()])
	lines.append(
		"- construction sites: %d -> %d"
		% [prev.get("construction_sites", []).size(), snapshot.get("construction_sites", []).size()]
	)
	lines.append("- units: %d -> %d" % [prev.get("units", []).size(), snapshot.get("units", []).size()])
	var prev_obj: Dictionary = metrics.get("objective_progress", {})
	if typeof(prev_obj) == TYPE_DICTIONARY:
		pass
	var event_delta: Dictionary = _event_type_counts(get_events_for_step())
	var prev_event_delta: Dictionary = _event_type_counts(get_events_for_previous_step())
	for event_type in event_delta.keys():
		var cur_count: int = int(event_delta[event_type])
		var prev_count: int = int(prev_event_delta.get(event_type, 0))
		if cur_count != prev_count:
			lines.append("- %s: +%d this step (prev step had %d)" % [event_type, cur_count, prev_count])
	if int(metrics.get("rejected_commands", 0)) > 0:
		var cur_cmd: Dictionary = get_current_decision()
		if not cur_cmd.is_empty() and not bool(cur_cmd.get("was_accepted", true)):
			lines.append("- command rejected at this step")
	return "\n".join(lines)


func build_economy_summary_text() -> String:
	var lines: PackedStringArray = PackedStringArray()
	lines.append("[b]Economy summary[/b]")
	lines.append(_economy_summary_one_line())
	var diag: Dictionary = snapshot.get("diagnostics", {})
	if typeof(diag) == TYPE_DICTIONARY:
		var top_issue: String = str(diag.get("top_issue", ""))
		if top_issue != "":
			lines.append("Top issue: %s" % top_issue)
		var construction: Dictionary = diag.get("construction", {})
		if typeof(construction) == TYPE_DICTIONARY:
			lines.append(
				"Construction diag — active:%s blocked:%s"
				% [str(construction.get("active", "?")), str(construction.get("blocked", "?"))]
			)
		var delivery: Dictionary = diag.get("delivery", {})
		if typeof(delivery) == TYPE_DICTIONARY:
			lines.append(
				"Delivery diag — active:%s blocked:%s"
				% [str(delivery.get("active", "?")), str(delivery.get("blocked", "?"))]
			)
	var route_blocked: int = _event_type_counts(get_events_for_step()).get("route_blocked", 0)
	if route_blocked > 0:
		lines.append("Route blocked events this step: %d" % route_blocked)
	return "\n".join(lines)


func build_military_summary_text() -> String:
	var lines: PackedStringArray = PackedStringArray()
	lines.append("[b]Military summary[/b]")
	lines.append(_military_summary_one_line())
	var p1_units: int = 0
	var p2_units: int = 0
	var p2_hub_hp: String = "?"
	for unit in snapshot.get("units", []):
		if str(unit.get("player_id", "")) == "p1":
			p1_units += 1
		elif str(unit.get("player_id", "")) == "p2":
			p2_units += 1
	for building in snapshot.get("buildings", []):
		if str(building.get("player_id", "")) == "p2" and str(building.get("building_type", "")) in [
			"command_hub",
			"headquarters",
		]:
			p2_hub_hp = "%d/%d" % [int(building.get("hp", 0)), int(building.get("max_hp", 0))]
	lines.append("Friendly units: %d | Enemy units: %d | Enemy hub HP: %s" % [p1_units, p2_units, p2_hub_hp])
	return "\n".join(lines)


func get_selection_at(q: int, r: int) -> Dictionary:
	for site in snapshot.get("construction_sites", []):
		var sq: int = int(site.get("anchor_q", site.get("q", -1)))
		var sr: int = int(site.get("anchor_r", site.get("r", -1)))
		if sq == q and sr == r:
			return {"kind": "construction_site", "data": site}
	for building in snapshot.get("buildings", []):
		var bq: int = int(building.get("anchor_q", building.get("q", -1)))
		var br: int = int(building.get("anchor_r", building.get("r", -1)))
		if bq == q and br == r:
			return {"kind": "building", "data": building}
	for unit in snapshot.get("units", []):
		if int(unit.get("q", -1)) == q and int(unit.get("r", -1)) == r:
			return {"kind": "unit", "data": unit}
	var render: Dictionary = get_render_data()
	for carrier in render.get("carriers", []):
		if int(round(float(carrier.get("q", -1)))) == q and int(round(float(carrier.get("r", -1)))) == r:
			return {"kind": "carrier", "data": carrier}
	var layers: Dictionary = snapshot.get("display_layers", {})
	for td in layers.get("tree_density", []):
		if int(td.get("q", -1)) == q and int(td.get("r", -1)) == r:
			return {"kind": "forest", "data": td}
	for tile in layers.get("terrain", []):
		if int(tile.get("q", -1)) == q and int(tile.get("r", -1)) == r:
			return {"kind": "tile", "data": tile}
	return {"kind": "empty", "data": {"q": q, "r": r}}


func capture_manifest_step_entry() -> Dictionary:
	var prev: Dictionary = get_previous_command()
	var cur: Dictionary = get_current_decision()
	var overlays: Array = get_command_overlays()
	var modes: Dictionary = {}
	for mode in InterfaceModePresets.MODES:
		var flags: Dictionary = InterfaceModePresets.apply_ui_flags(mode)
		modes[mode] = {
			"ui_flags": flags,
			"layers": InterfaceModePresets.apply_mode(mode),
		}
	return {
		"step_index": step_index,
		"tick": int(snapshot.get("tick", 0)),
		"interface_mode": interface_mode,
		"overlay": RolloutCommandOverlay.overlay_manifest_entry(prev, cur, overlays),
		"modes": modes,
	}


func _economy_summary_one_line() -> String:
	var sites: int = snapshot.get("construction_sites", []).size()
	var blocked_sites: int = 0
	for site in snapshot.get("construction_sites", []):
		if str(site.get("state", "")) == "blocked":
			blocked_sites += 1
	var routes: int = get_render_data().get("ware_routes", []).size()
	var blocked_jobs: int = snapshot.get("blocked_job_markers", []).size()
	var idle_carriers: int = 0
	for carrier in snapshot.get("carrier_markers", []):
		if str(carrier.get("state", "")) in ["idle", "waiting"]:
			idle_carriers += 1
	return (
		"Economy — sites:%d blocked:%d roads:%d routes:%d blocked_jobs:%d idle_carriers:%d"
		% [
			sites,
			blocked_sites,
			snapshot.get("roads", []).size(),
			routes,
			blocked_jobs,
			idle_carriers,
		]
	)


func _military_summary_one_line() -> String:
	var p1: int = 0
	var p2: int = 0
	for unit in snapshot.get("units", []):
		if str(unit.get("player_id", "")) == "p1":
			p1 += 1
		elif str(unit.get("player_id", "")) == "p2":
			p2 += 1
	return "Military — p1 units:%d p2 units:%d buildings:%d" % [
		p1,
		p2,
		snapshot.get("buildings", []).size(),
	]


func _neural_diagnostics_one_line() -> String:
	var cur: Dictionary = get_current_decision()
	if cur.is_empty():
		return "Neural — terminal snapshot"
	return "Neural — " + _policy_line(cur)


func _format_raw_events() -> String:
	var lines: PackedStringArray = PackedStringArray()
	for event in get_events_for_step():
		lines.append("- %s (tick %s)" % [str(event.get("event_type", "?")), str(event.get("tick", "?"))])
	if lines.is_empty():
		return "[b]Events[/b] — (none this step)"
	return "[b]Events (raw)[/b] — step %d\n" % step_index + "\n".join(lines)


func _format_aggregated_events() -> String:
	var counts: Dictionary = _event_type_counts(get_events_for_step())
	var prev_counts: Dictionary = _event_type_counts(get_events_for_previous_step())
	if counts.is_empty():
		return "[b]Events[/b] — (none this step)"
	var lines: PackedStringArray = PackedStringArray()
	lines.append("[b]Events[/b] — step %d (aggregated)" % step_index)
	var keys: Array = counts.keys()
	keys.sort()
	for key in keys:
		var cur: int = int(counts[key])
		var prev: int = int(prev_counts.get(key, 0))
		var delta: int = cur - prev
		var delta_text: String = ""
		if delta != 0:
			delta_text = " (%+d vs prev step count)" % delta
		lines.append("- %s × %d%s" % [str(key), cur, delta_text])
	return "\n".join(lines)


func _event_type_counts(rows: Array) -> Dictionary:
	var counts: Dictionary = {}
	for event in rows:
		var event_type: String = str(event.get("event_type", "unknown"))
		counts[event_type] = int(counts.get(event_type, 0)) + 1
	return counts


func _refresh_snapshot() -> void:
	command_target = {}
	_cached_prev_snapshot = {}
	if step_index > 0:
		var prev_parsed: Dictionary = RolloutLoader.load_snapshot_at(folder_path, step_index - 1)
		if prev_parsed.get("ok", false):
			_cached_prev_snapshot = prev_parsed.get("snapshot", {})
	var parsed: Dictionary = RolloutLoader.load_snapshot_at(folder_path, step_index)
	if not parsed.get("ok", false):
		snapshot = {}
		snapshot_refreshed.emit(snapshot)
		return
	snapshot = parsed.get("snapshot", {})
	var decision: Dictionary = get_current_decision()
	if not decision.is_empty():
		var target: Variant = decision.get("command_target")
		if typeof(target) == TYPE_DICTIONARY:
			command_target = target
	snapshot_refreshed.emit(snapshot)
