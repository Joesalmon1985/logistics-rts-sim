class_name HybridObserverController
extends RefCounted

## Spectator transport for MRP Phase C2 — hybrid economy feeding military watch mode.

signal snapshot_refreshed(snapshot: Dictionary)

const SCENARIO_PATH := "scenarios/hybrid/hybrid_mvp_observer.json"
const MEANINGFUL_EVENT_SUBSTRINGS: Array[String] = [
	"construction",
	"ware",
	"carrier",
	"production",
	"unit",
	"attack",
	"delegated",
	"military",
	"damage",
	"delivery",
]
const STEP_UNTIL_EVENT_MAX: int = 50

var state: GameState
var runner: SimulationTickRunner
var submitter: CommandSubmitter
var snapshot: Dictionary = {}
var render_bridge: EconomySpectatorRenderBridge = EconomySpectatorRenderBridge.new()
var actor_id: String = "p1"
var demo_label: String = "Hybrid observer (seed 707)"
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
var selected_q: int = -1
var selected_r: int = -1
var event_log_lines: PackedStringArray = PackedStringArray()
var recent_change_lines: PackedStringArray = PackedStringArray()
var autoplay_enabled: bool = false
var speed_multiplier: float = 1.0
var _prev_summary_sig: String = ""
var _scripted_attack: bool = false
var _scripted_queue: bool = false


func reset_demo() -> void:
	runner = SimulationTickRunner.new()
	submitter = CommandSubmitter.new()
	event_log_lines = PackedStringArray()
	recent_change_lines = PackedStringArray()
	_prev_summary_sig = ""
	selected_q = -1
	selected_r = -1
	_scripted_attack = false
	_scripted_queue = false
	render_bridge.reset()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO_PATH)
	state = runner.create_game(scenario)
	HybridTestHelper.flatten_grass(state, 0, 0, 15, 15)
	state = runner.run_ticks(state, 1)
	_refresh("Hybrid observer reset — %s" % demo_label)


func get_render_data() -> Dictionary:
	return render_bridge.last_render_data if not render_bridge.last_render_data.is_empty() else LayerRenderer.build_render_data(snapshot, layer_visibility)


func get_military_sprite_states() -> Dictionary:
	return MilitarySpriteStateSelector.select_states(get_render_data(), snapshot, [])


func autoplay_step() -> void:
	_maybe_script_commands()
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
		if BuildingCatalog.occupies_hex(building, q, r):
			return {"kind": "building", "data": building}
	for unit in snapshot.get("units", []):
		if int(unit.get("q", -1)) == q and int(unit.get("r", -1)) == r:
			return {"kind": "unit", "data": unit}
	for carrier in snapshot.get("carriers", []):
		if int(round(float(carrier.get("q", -1)))) == q and int(round(float(carrier.get("r", -1)))) == r:
			return {"kind": "carrier", "data": carrier}
	return {"kind": "terrain", "data": {"q": q, "r": r}}


func build_summary_text() -> String:
	if snapshot.is_empty():
		return "Hybrid summary: (no snapshot)"
	return HybridCatalog.format_supply_panel(snapshot, actor_id)


func build_chain_text() -> String:
	var lines: PackedStringArray = PackedStringArray([
		"[b]Logistics chain[/b]",
		"timber/stone/planks/tools → military site → building → unit queue → attack",
	])
	var sites: int = snapshot.get("construction_sites", []).size()
	var mil_buildings: int = 0
	for b in snapshot.get("buildings", []):
		var btype: String = str(b.get("building_type", ""))
		if HybridConfig.load_all().is_military_building(btype):
			mil_buildings += 1
	lines.append("Construction sites: %d | Military buildings: %d | Units: %d" % [
		sites, mil_buildings, snapshot.get("units", []).size(),
	])
	return "\n".join(lines)


func build_transport_bar_text() -> String:
	if snapshot.is_empty():
		return "Tick — | PAUSED | 1.0x"
	return "Tick %d | %s | %.1fx | events %d" % [
		int(snapshot.get("tick", 0)),
		"AUTOPLAY" if autoplay_enabled else "PAUSED",
		speed_multiplier,
		event_log_lines.size(),
	]


func build_production_text() -> String:
	return HybridCatalog.format_production_queues(snapshot, actor_id)


func build_orders_text() -> String:
	return HybridCatalog.format_delegated_orders(snapshot, actor_id)


func build_inspector_text() -> String:
	return HybridCatalog.format_selection(get_selection(), snapshot)


func build_recent_changes_panel_text() -> String:
	if recent_change_lines.is_empty():
		return "[i]Step ticks to watch carriers supply military construction and production.[/i]"
	var lines: PackedStringArray = PackedStringArray()
	for line in recent_change_lines:
		lines.append("[b]%s[/b]" % line)
	return "\n".join(lines)


func build_event_log_text() -> String:
	return "\n".join(event_log_lines)


func build_activity_hint() -> String:
	var hints: PackedStringArray = PackedStringArray()
	if snapshot.get("carriers", []).size() > 0:
		hints.append("carriers active")
	if not snapshot.get("production_queues", []).is_empty():
		hints.append("production queued")
	if not snapshot.get("delegated_orders", []).is_empty():
		hints.append("delegated attack")
	if hints.is_empty():
		return "Hybrid activity: quiet — step ticks or enable autoplay"
	return "Hybrid activity: " + ", ".join(hints)


func _maybe_script_commands() -> void:
	if state == null:
		return
	if not _scripted_queue:
		var barracks_id: int = HybridTestHelper.building_of_type(state, "barracks", actor_id)
		if barracks_id > 0:
			var cmd := QueueUnitCommand.new(actor_id, state.tick, {
				"building_id": barracks_id,
				"unit_type": "rifle_squad",
			})
			var result: CommandResult = submitter.submit(state, cmd)
			if result.success:
				_scripted_queue = true
				_append_event("Queued Rifle Squad at Barracks")
	if not _scripted_attack and int(snapshot.get("tick", 0)) >= 8:
		var enemy_hub: int = HybridTestHelper.building_of_type(state, "command_hub", "p2")
		if enemy_hub > 0:
			var enemy: Dictionary = MilitaryState.get_building(state, enemy_hub)
			var cmd2 := DelegatedAttackCommand.new(actor_id, state.tick, {
				"target_q": int(enemy.get("anchor_q", 0)),
				"target_r": int(enemy.get("anchor_r", 0)),
			})
			var res2: CommandResult = submitter.submit(state, cmd2)
			if res2.success:
				_scripted_attack = true
				_append_event("Delegated attack on enemy Command Hub")


func _record_recent_changes() -> void:
	var sig: String = build_summary_text()
	if _prev_summary_sig != "" and sig != _prev_summary_sig:
		_push_recent_change("Supply or power changed")
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
