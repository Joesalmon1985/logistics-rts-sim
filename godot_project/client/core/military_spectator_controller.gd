class_name MilitarySpectatorController
extends RefCounted

## Spectator transport for MRP Phase B1 — military skirmish watch mode.

signal snapshot_refreshed(snapshot: Dictionary)

const MEANINGFUL_EVENT_SUBSTRINGS: Array[String] = [
	"combat",
	"damage",
	"production",
	"unit",
	"attack",
	"harvest",
	"construction",
	"fired",
	"queued",
]
const STEP_UNTIL_EVENT_MAX: int = 50

var state: GameState
var runner: SimulationTickRunner
var snapshot: Dictionary = {}
var render_bridge: MilitarySpectatorRenderBridge = MilitarySpectatorRenderBridge.new()
var actor_id: String = "p1"
var demo_label: String = "Military skirmish (seed 7)"
var layer_visibility: Dictionary = {
	"terrain": true,
	"buildings": true,
	"units": true,
	"resources": true,
	"fog": false,
	"diagnostics": true,
	"sprites": true,
}
var selected_q: int = -1
var selected_r: int = -1
var event_log_lines: PackedStringArray = PackedStringArray()
var recent_change_lines: PackedStringArray = PackedStringArray()
var autoplay_enabled: bool = false
var speed_multiplier: float = 1.0
var _prev_summary_sig: String = ""


func reset_demo() -> void:
	runner = SimulationTickRunner.new()
	event_log_lines = PackedStringArray()
	recent_change_lines = PackedStringArray()
	_prev_summary_sig = ""
	selected_q = -1
	selected_r = -1
	render_bridge.reset()
	state = MilitaryDemo.build_state()
	_seed_demo_production_queue()
	_refresh("Demo reset — %s" % demo_label)


func _seed_demo_production_queue() -> void:
	if state == null:
		return
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) != "barracks":
			continue
		if str(building.get("player_id", "")) != actor_id:
			continue
		var cmd := QueueUnitCommand.new(actor_id, state.tick, {
			"building_id": int(building.get("id", 0)),
			"unit_type": "rifle_squad",
		})
		var result: CommandResult = cmd.validate(state)
		if result.success:
			cmd.apply(state)
		break


func get_render_data() -> Dictionary:
	return render_bridge.last_render_data if not render_bridge.last_render_data.is_empty() else LayerRenderer.build_render_data(snapshot, layer_visibility)


func get_sprite_states() -> Dictionary:
	return render_bridge.sprite_states


func autoplay_step() -> void:
	step_tick()


func step_tick() -> void:
	if state == null:
		return
	state = runner.tick(state)
	_refresh("Sim tick")


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


func set_layer_visible(layer_name: String, visible: bool) -> void:
	layer_visibility[layer_name] = visible
	if not snapshot.is_empty():
		render_bridge.update(state, snapshot, layer_visibility)


func set_selection(q: int, r: int) -> void:
	selected_q = q
	selected_r = r


func get_selection() -> Dictionary:
	return get_selection_at(selected_q, selected_r)


func get_selection_at(q: int, r: int) -> Dictionary:
	if q < 0 or snapshot.is_empty():
		return {"kind": "empty", "data": {"q": q, "r": r}}
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
	return {"kind": "empty", "data": {"q": q, "r": r}}


func build_summary_text() -> String:
	if snapshot.is_empty():
		return "Military summary: (no snapshot)"
	var credits: int = 0
	var supply: int = 0
	var demand: int = 0
	for player in snapshot.get("military_players", []):
		if str(player.get("player_id", "")) != actor_id:
			continue
		credits = int(player.get("credits", 0))
		supply = int(player.get("power_supply", 0))
		demand = int(player.get("power_demand", 0))
		break
	var unit_counts: Dictionary = _unit_counts_for_player(actor_id)
	var unit_parts: PackedStringArray = PackedStringArray()
	for utype in unit_counts.keys():
		unit_parts.append("%s=%d" % [MilitaryCatalog.display_name(str(utype)), int(unit_counts[utype])])
	var unit_line: String = ", ".join(unit_parts) if not unit_parts.is_empty() else "(none)"
	return (
		"Credits: %d | Power: %d / %d | Units: %s | Buildings: %d | Sites: %d"
		% [
			credits,
			supply,
			demand,
			unit_line,
			snapshot.get("buildings", []).size(),
			snapshot.get("construction_sites", []).size(),
		]
	)


func build_transport_bar_text() -> String:
	if snapshot.is_empty():
		return "Tick — | PAUSED | 1.0x"
	return "Tick %d | %s | %.1fx | events %d" % [
		int(snapshot.get("tick", 0)),
		"AUTOPLAY" if autoplay_enabled else "PAUSED",
		speed_multiplier,
		event_log_lines.size(),
	]


func build_production_queues_text() -> String:
	if snapshot.is_empty():
		return "Production queues: (no snapshot)"
	var lines: PackedStringArray = PackedStringArray(["Production queues:"])
	var found: bool = false
	for queue in snapshot.get("production_queues", []):
		if typeof(queue) != TYPE_DICTIONARY:
			continue
		if str(queue.get("player_id", "")) != actor_id:
			continue
		var building_id: int = int(queue.get("building_id", 0))
		var building_type: String = _building_type_by_id(building_id)
		var items: Array = queue.get("items", [])
		if items.is_empty():
			continue
		found = true
		var item_labels: PackedStringArray = PackedStringArray()
		for item in items:
			if typeof(item) != TYPE_DICTIONARY:
				continue
			var utype: String = str(item.get("unit_type", "?"))
			var prog: int = int(item.get("progress", 0))
			var req: int = int(item.get("required_ticks", 0))
			var blocked: String = str(item.get("blocked_reason", ""))
			var suffix: String = ""
			if blocked != "":
				suffix = " [%s]" % MilitaryCatalog.human_reason(blocked)
			item_labels.append("%s %d/%d%s" % [MilitaryCatalog.display_name(utype), prog, req, suffix])
		lines.append("- %s: %s" % [MilitaryCatalog.display_name(building_type), ", ".join(item_labels)])
	if not found:
		lines.append("- (idle — step ticks to watch production)")
	return "\n".join(lines)


func build_inspector_text() -> String:
	return MilitaryCatalog.format_selection(get_selection(), snapshot)


func build_recent_changes_panel_text() -> String:
	if recent_change_lines.is_empty():
		return "[i]No grounded changes yet — step ticks to watch the skirmish.[/i]"
	var lines: PackedStringArray = PackedStringArray()
	for line in recent_change_lines:
		lines.append("[b]%s[/b]" % line)
	return "\n".join(lines)


func build_event_log_text() -> String:
	return "\n".join(event_log_lines)


func build_activity_hint() -> String:
	var hints: PackedStringArray = PackedStringArray()
	if snapshot.get("projectiles", []).size() > 0:
		hints.append("projectiles in flight")
	if not snapshot.get("combat_log", []).is_empty():
		hints.append("recent combat")
	for queue in snapshot.get("production_queues", []):
		if typeof(queue) == TYPE_DICTIONARY and not queue.get("items", []).is_empty():
			hints.append("production active")
			break
	if hints.is_empty():
		return "Battlefield activity: quiet — step ticks or enable autoplay"
	return "Battlefield activity: " + ", ".join(hints)


func _unit_counts_for_player(player_id: String) -> Dictionary:
	var counts: Dictionary = {}
	for unit in snapshot.get("units", []):
		if typeof(unit) != TYPE_DICTIONARY:
			continue
		if str(unit.get("player_id", "")) != player_id:
			continue
		var utype: String = str(unit.get("unit_type", "unit"))
		counts[utype] = int(counts.get(utype, 0)) + 1
	return counts


func _building_type_by_id(building_id: int) -> String:
	for building in snapshot.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		if int(building.get("id", 0)) == building_id:
			return str(building.get("building_type", "?"))
	return "?"


func _record_recent_changes() -> void:
	var sig: String = build_summary_text()
	if _prev_summary_sig != "" and sig != _prev_summary_sig:
		_push_recent_change("Summary updated")
	_prev_summary_sig = sig


func _push_recent_change(text: String) -> void:
	var line: String = "[tick %d] %s" % [int(snapshot.get("tick", 0)), text]
	recent_change_lines.insert(0, line)
	while recent_change_lines.size() > 12:
		recent_change_lines.remove_at(recent_change_lines.size() - 1)


func _refresh(note: String = "") -> void:
	if state == null:
		snapshot = {}
		return
	snapshot = SnapshotExporter.export_debug_snapshot(state, actor_id)
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
