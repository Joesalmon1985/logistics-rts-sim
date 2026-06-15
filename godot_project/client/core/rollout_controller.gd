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
var interface_mode: String = "world"
var command_target: Dictionary = {}


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


func build_status_text() -> String:
	return (
		"Scenario: %s  |  Policy: %s  |  Seed: %s  |  Tick: %d  |  Step: %d / %d"
		% [
			str(metadata.get("scenario_name", "?")),
			str(metadata.get("policy_type", "?")),
			str(metadata.get("seed", "?")),
			int(snapshot.get("tick", 0)),
			step_index,
			get_command_count(),
		]
	)


func build_metrics_text() -> String:
	var parts: PackedStringArray = PackedStringArray()
	parts.append("Steps: %d" % int(metrics.get("steps", get_command_count())))
	parts.append("No-ops: %d" % int(metrics.get("no_ops", 0)))
	parts.append("Rejected: %d" % int(metrics.get("rejected_commands", 0)))
	parts.append("Outcome: %s" % str(metrics.get("final_outcome", "?")))
	var score: Variant = metrics.get("total_score")
	if score != null:
		parts.append("Score: %s" % str(score))
	return "Metrics — " + " | ".join(parts)


func build_action_text() -> String:
	var prev: Dictionary = get_previous_command()
	var cur: Dictionary = get_current_decision()
	var lines: PackedStringArray = PackedStringArray()
	if not prev.is_empty():
		lines.append(
			"[b]Previous command[/b] (produced this snapshot): %s — %s"
			% [str(prev.get("command_label", "?")), str(prev.get("result_message", ""))]
		)
	else:
		lines.append("[b]Previous command[/b]: (none — initial snapshot)")
	if not cur.is_empty():
		lines.append(
			"[b]Current / next decision[/b] (from this snapshot): %s — %s"
			% [str(cur.get("command_label", "?")), str(cur.get("result_message", ""))]
		)
	else:
		lines.append("[b]Current / next decision[/b]: (terminal — no further command)")
	return "\n".join(lines)


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
	return JSON.stringify(preview, "\t")


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


func _refresh_snapshot() -> void:
	command_target = {}
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
