class_name Hybrid3dSpectatorController
extends RefCounted

## MRP Phase C6 — authoritative hybrid bot-vs-bot transport for 3D spectator.

signal snapshot_refreshed(snapshot: Dictionary)

const SCENARIO_PATH := "scenarios/hybrid/hybrid_bot_vs_bot_3d_spectator.json"
const MEANINGFUL_EVENT_SUBSTRINGS: Array[String] = [
	"combat",
	"damage",
	"construction",
	"ware",
	"carrier",
	"production",
	"delivery",
	"road",
	"unit",
	"attack",
	"delegated",
]
const STEP_UNTIL_EVENT_MAX: int = 80
const BOT_PLAYERS: Array[String] = ["p1", "p2"]

var state: GameState
var runner: SimulationTickRunner
var dual: DualBotRunner
var scenario: Dictionary = {}
var snapshot: Dictionary = {}
var match_label: String = "Hybrid bot-vs-bot (seed 4242)"
var overlay_visibility: Dictionary = {
	"roads": true,
	"supply": true,
	"construction": true,
	"military_queues": true,
	"attack_targets": true,
	"unit_orders": true,
	"combat_events": true,
}
var selected_q: int = -1
var selected_r: int = -1
var selected_entity_id: String = ""
var follow_target: String = "free"
var camera_mode: String = "free"
var autoplay_enabled: bool = false
var speed_multiplier: float = 1.0
var event_log_lines: PackedStringArray = PackedStringArray()
var recent_change_lines: PackedStringArray = PackedStringArray()
var bot_status: Dictionary = {"p1": {}, "p2": {}}
var match_result: Dictionary = {}
var _prev_summary_sig: String = ""


func reset_match() -> void:
	runner = SimulationTickRunner.new()
	dual = DualBotRunner.new(runner)
	event_log_lines = PackedStringArray()
	recent_change_lines = PackedStringArray()
	bot_status = {"p1": {}, "p2": {}}
	match_result = {}
	_prev_summary_sig = ""
	selected_q = -1
	selected_r = -1
	selected_entity_id = ""
	follow_target = "free"
	camera_mode = "free"
	SimBootstrap.load_all()
	scenario = ConfigLoader.load_scenario(SCENARIO_PATH)
	match_label = "Hybrid bot-vs-bot (seed %d)" % int(scenario.get("seed", 0))
	state = runner.create_game(scenario)
	_prepare_demo_lane_map()
	dual.bot_p1 = EconomyExpansionBot.new("p1")
	dual.bot_p2 = EconomyExpansionBot.new("p2")
	dual.bot_p1.reset("p1")
	dual.bot_p2.reset("p2")
	dual.max_ticks = int(scenario.get("max_ticks", 2500))
	dual.stall_ticks_threshold = int(scenario.get("stall_ticks_threshold", 400))
	_refresh("Match reset — %s" % match_label)


func _prepare_demo_lane_map() -> void:
	if state == null or state.map == null:
		return
	for q in range(state.map.width):
		for r in range(state.map.height):
			var tile: HexTile = state.map.get_tile(HexCoord.new(q, r))
			if tile == null:
				continue
			if tile.terrain_type == "water" or tile.terrain_type == "mountain":
				state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))


func bot_step_once() -> void:
	if state == null or dual == null:
		return
	EconomyDiagnosticsSystem.tick(state, "bot")
	_apply_bot_turn("p1")
	_apply_bot_turn("p2")
	state = runner.tick(state)
	_update_match_result()
	_refresh("Bot step: p1+p2 commands + tick")


func step_tick() -> void:
	bot_step_once()


func autoplay_step() -> void:
	step_tick()


func step_ticks(count: int) -> void:
	for _i in range(maxi(0, count)):
		autoplay_step()


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


func advance_until_combat_or_cap(max_steps: int = 600) -> Dictionary:
	var steps: int = 0
	var saw_combat: bool = false
	var saw_units: bool = false
	var saw_military: bool = false
	var saw_roads: bool = false
	while steps < max_steps:
		autoplay_step()
		steps += 1
		if snapshot.get("units", []).size() > 0:
			saw_units = true
		if snapshot.get("military_buildings", []).size() > 0:
			saw_military = true
		if snapshot.get("roads", []).size() > 0:
			saw_roads = true
		if not snapshot.get("combat_log", []).is_empty():
			saw_combat = true
		if saw_combat or saw_units:
			break
		if match_result.get("finished", false):
			break
	return {
		"steps": steps,
		"saw_units": saw_units,
		"saw_combat": saw_combat,
		"saw_military": saw_military,
		"saw_roads": saw_roads,
		"finished": bool(match_result.get("finished", false)),
		"progress": saw_units or saw_combat or saw_military or saw_roads or bool(match_result.get("finished", false)),
	}


func set_overlay_visible(name: String, visible: bool) -> void:
	overlay_visibility[name] = visible


func set_selection(q: int, r: int, entity_id: String = "") -> void:
	selected_q = q
	selected_r = r
	selected_entity_id = entity_id


func set_follow_target(target: String) -> void:
	follow_target = target
	match target:
		"p1", "bot_a":
			camera_mode = "follow_bot_a"
		"p2", "bot_b":
			camera_mode = "follow_bot_b"
		"battle", "objective":
			camera_mode = target
		_:
			camera_mode = "free"


func get_selection() -> Dictionary:
	return get_selection_at(selected_q, selected_r, selected_entity_id)


func get_selection_at(q: int, r: int, entity_id: String = "") -> Dictionary:
	if snapshot.is_empty():
		return {"kind": "empty", "data": {"q": q, "r": r}}
	if entity_id != "":
		for unit in snapshot.get("units", []):
			if str(unit.get("id", "")) == entity_id or str(unit.get("entity_id", "")) == entity_id:
				return {"kind": "unit", "data": unit}
		for carrier in snapshot.get("carriers", snapshot.get("carrier_markers", [])):
			if str(carrier.get("id", "")) == entity_id:
				return {"kind": "carrier", "data": carrier}
	if q < 0:
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
			return {"kind": "economy_building", "data": building}
	for building in snapshot.get("military_buildings", []):
		var bq2: int = int(building.get("anchor_q", building.get("q", -1)))
		var br2: int = int(building.get("anchor_r", building.get("r", -1)))
		if bq2 == q and br2 == r:
			return {"kind": "military_building", "data": building}
	for unit in snapshot.get("units", []):
		if int(unit.get("q", -1)) == q and int(unit.get("r", -1)) == r:
			return {"kind": "unit", "data": unit}
	for carrier in snapshot.get("carriers", snapshot.get("carrier_markers", [])):
		if int(carrier.get("q", -1)) == q and int(carrier.get("r", -1)) == r:
			return {"kind": "carrier", "data": carrier}
	var terrain: Dictionary = _terrain_at(q, r)
	if not terrain.is_empty():
		return {"kind": "terrain", "data": terrain}
	return {"kind": "empty", "data": {"q": q, "r": r}}


func build_transport_bar_text() -> String:
	if snapshot.is_empty():
		return "Tick — | PAUSED | 1.0x | camera free"
	return "Tick %d | %s | %.1fx | camera %s | follow %s" % [
		int(snapshot.get("tick", 0)),
		"AUTOPLAY" if autoplay_enabled else "PAUSED",
		speed_multiplier,
		camera_mode,
		follow_target,
	]


func build_bot_summary(player_id: String) -> String:
	var counts: Dictionary = _entity_counts(player_id)
	var stock: Dictionary = _stockpile(player_id)
	var stock_parts: PackedStringArray = PackedStringArray()
	for ware in ["planks", "stone", "tools", "rations"]:
		if int(stock.get(ware, 0)) > 0:
			stock_parts.append("%s=%d" % [ware, int(stock.get(ware, 0))])
	var status: Dictionary = bot_status.get(player_id, {})
	var plan: String = str(status.get("decision", "expanding settlement"))
	var blocked: String = str(status.get("blocked_reason", ""))
	var blocked_line: String = ""
	if blocked != "":
		blocked_line = " | blocked: %s" % blocked
	return (
		"Bot %s — stock [%s] | econ %d mil %d units %d | plan: %s%s"
		% [
			"A" if player_id == "p1" else "B" if player_id == "p2" else player_id.to_upper(),
			", ".join(stock_parts) if not stock_parts.is_empty() else "—",
			int(counts.get("economy_buildings", 0)),
			int(counts.get("military_buildings", 0)),
			int(counts.get("units", 0)),
			plan,
			blocked_line,
		]
	)


func build_objective_text() -> String:
	if bool(match_result.get("finished", false)):
		return "Result: %s" % str(match_result.get("summary", "match ended"))
	return str(scenario.get("objective_text", "Destroy the enemy Command Hub or control the central outpost."))


func build_recent_events_text() -> String:
	if recent_change_lines.is_empty():
		return "[i]Step ticks to watch economy, supply, and combat.[/i]"
	return "\n".join(recent_change_lines)


func build_event_log_text() -> String:
	return "\n".join(event_log_lines)


func get_follow_position() -> Vector3:
	var target: Vector3 = Vector3.ZERO
	match follow_target:
		"p1", "bot_a":
			target = _player_base_position("p1")
		"p2", "bot_b":
			target = _player_base_position("p2")
		"battle":
			target = _battle_center_position()
		"objective":
			target = _objective_position()
		_:
			if selected_q >= 0:
				target = Hybrid3dWorldRenderer.hex_to_world(selected_q, selected_r)
	return target


func uses_authoritative_snapshots_only() -> bool:
	return true


func _apply_bot_turn(player_id: String) -> void:
	if dual == null or state == null:
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
		"blocked_reason": "",
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
			"blocked_reason": result.message if not result.success else "",
		}
	bot_status[player_id] = status


func _entity_counts(player_id: String) -> Dictionary:
	var counts: Dictionary = {"economy_buildings": 0, "military_buildings": 0, "units": 0}
	for building in snapshot.get("buildings", []):
		if str(building.get("player_id", "")) == player_id:
			counts["economy_buildings"] = int(counts.get("economy_buildings", 0)) + 1
	for building in snapshot.get("military_buildings", []):
		if str(building.get("player_id", "")) == player_id:
			counts["military_buildings"] = int(counts.get("military_buildings", 0)) + 1
	for unit in snapshot.get("units", []):
		if str(unit.get("player_id", "")) == player_id:
			counts["units"] = int(counts.get("units", 0)) + 1
	return counts


func _stockpile(player_id: String) -> Dictionary:
	for player in snapshot.get("players", []):
		if typeof(player) != TYPE_DICTIONARY:
			continue
		if str(player.get("player_id", "")) == player_id:
			return player.get("stockpile", {})
	return {}


func _terrain_at(q: int, r: int) -> Dictionary:
	for tile in _terrain_tiles(snapshot):
		if int(tile.get("q", -1)) == q and int(tile.get("r", -1)) == r:
			return tile
	return {}


static func _terrain_tiles(data: Dictionary) -> Array:
	if not data.get("terrain", []).is_empty():
		return data.get("terrain", [])
	var layers: Dictionary = data.get("display_layers", {})
	if typeof(layers) == TYPE_DICTIONARY:
		return layers.get("terrain", [])
	return []


func _player_base_position(player_id: String) -> Vector3:
	for building in snapshot.get("buildings", []):
		if str(building.get("building_type", "")) in ["headquarters", "command_hub"]:
			if str(building.get("player_id", "")) == player_id:
				return Hybrid3dWorldRenderer.hex_to_world(
					int(building.get("anchor_q", building.get("q", 0))),
					int(building.get("anchor_r", building.get("r", 0))),
				)
	for spawn in scenario.get("spawns", []):
		if str(spawn.get("player_id", "")) == player_id:
			return Hybrid3dWorldRenderer.hex_to_world(int(spawn.get("q", 0)), int(spawn.get("r", 0)))
	return Vector3.ZERO


func _battle_center_position() -> Vector3:
	var mid_q: float = float(scenario.get("battle_center_q", int(scenario.get("map_width", 14)) / 2))
	var mid_r: float = float(scenario.get("battle_center_r", int(scenario.get("map_height", 14)) / 2))
	return Hybrid3dWorldRenderer.hex_to_world(mid_q, mid_r)


func _objective_position() -> Vector3:
	return _battle_center_position()


func _update_match_result() -> void:
	if state == null:
		return
	var max_ticks: int = int(scenario.get("max_ticks", 2500))
	if state.tick >= max_ticks:
		match_result = {"finished": true, "summary": "Timeout at tick %d" % state.tick}
		return
	var p1_hub: bool = _player_has_hub("p1")
	var p2_hub: bool = _player_has_hub("p2")
	if not p1_hub and p2_hub:
		match_result = {"finished": true, "summary": "Bot B wins — Bot A Command Hub destroyed"}
	elif not p2_hub and p1_hub:
		match_result = {"finished": true, "summary": "Bot A wins — Bot B Command Hub destroyed"}
	elif not p1_hub and not p2_hub:
		match_result = {"finished": true, "summary": "Draw — both Command Hubs destroyed"}


func _player_has_hub(player_id: String) -> bool:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) in ["command_hub", "headquarters"]:
			return true
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) == "headquarters":
			return true
	return false


func _record_recent_changes() -> void:
	var sig: String = "%s|%s" % [build_bot_summary("p1"), build_bot_summary("p2")]
	if _prev_summary_sig != "" and sig != _prev_summary_sig:
		_push_recent_change("Bot summaries updated")
	_prev_summary_sig = sig


func _push_recent_change(text: String) -> void:
	var line: String = "[tick %d] %s" % [int(snapshot.get("tick", 0)), text]
	recent_change_lines.insert(0, line)
	while recent_change_lines.size() > 14:
		recent_change_lines.remove_at(recent_change_lines.size() - 1)


func _refresh(note: String = "") -> void:
	if state == null:
		snapshot = {}
		return
	snapshot = HybridSnapshotExporter.export_snapshot(state)
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
