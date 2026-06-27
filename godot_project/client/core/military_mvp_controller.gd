class_name MilitaryMvpController
extends RefCounted

## Human-playable Skirmish MVP controller — wraps ViewerController and map-driven military commands.

signal snapshot_refreshed(snapshot: Dictionary)
signal command_result(result: Dictionary, dto: Dictionary)
signal message_logged(text: String)
signal victory_achieved()

const SCENARIO_PATH := "scenarios/military/military_mvp_skirmish.json"

const BUILD_TYPES: Array[String] = ["power_generator", "barracks", "resource_processor"]
const BUILD_LABELS: Dictionary = {
	"power_generator": "Power Generator",
	"barracks": "Barracks",
	"resource_processor": "Supply Depot",
}

const REASON_MESSAGES: Dictionary = {
	"FOOTPRINT_OVERLAP": "Already occupied.",
	"OUT_OF_BOUNDS": "Outside the map.",
	"OUTSIDE_BUILD_RADIUS": "Too far from your base.",
	"NOT_BUILDABLE": "Not buildable terrain.",
	"MISSING_PREREQUISITE": "Missing prerequisite building or tech.",
	"UNKNOWN_BUILDING": "Unknown building type.",
	"INSUFFICIENT_CREDITS": "Not enough credits.",
	"UNPOWERED": "Building is underpowered.",
	"NOT_PRODUCER": "This building cannot train units.",
	"INVALID_TARGET": "Invalid target.",
	"UNREACHABLE": "Target is unreachable.",
	"NOT_OWNER": "You do not own this unit or building.",
	"DEAD_UNIT": "Unit is destroyed.",
}

var viewer: ViewerController
var placement_building_type: String = ""
var queue_unit_type: String = ""
var selected_unit_id: int = -1
var selected_building_id: int = -1
var paused: bool = true
var speed_multiplier: float = 1.0
var victory_shown: bool = false
var hover_q: int = -1
var hover_r: int = -1
var last_command_message: String = ""


func _init() -> void:
	viewer = ViewerController.new()
	viewer.snapshot_refreshed.connect(_on_viewer_snapshot_refreshed)
	viewer.command_result.connect(_on_viewer_command_result)
	viewer.layer_visibility["fog"] = false


func reset_game() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO_PATH)
	var state: GameState = runner.create_game(scenario)
	viewer.load_demo_state(state, "p1")
	clear_modes()
	victory_shown = false
	last_command_message = ""
	message_logged.emit("New Skirmish MVP game started.")


func get_state() -> GameState:
	return viewer.state


func get_snapshot() -> Dictionary:
	return viewer.snapshot


func get_render_data() -> Dictionary:
	return viewer.get_render_data()


func get_selection_at(q: int, r: int) -> Dictionary:
	for site in viewer.snapshot.get("construction_sites", []):
		var sq: int = int(site.get("anchor_q", site.get("q", -1)))
		var sr: int = int(site.get("anchor_r", site.get("r", -1)))
		if _occupies_footprint(site, q, r):
			return {"kind": "construction_site", "data": site}
	for building in viewer.snapshot.get("buildings", []):
		if _occupies_footprint(building, q, r):
			return {"kind": "building", "data": building}
	for unit in viewer.snapshot.get("units", []):
		if int(unit.get("q", -1)) == q and int(unit.get("r", -1)) == r:
			if str(unit.get("player_id", "")) == viewer.actor_id:
				return {"kind": "unit", "data": unit}
			return {"kind": "enemy_unit", "data": unit}
	return {"kind": "empty", "data": {"q": q, "r": r}}


func step_tick() -> void:
	viewer.step_tick()


func clear_modes() -> void:
	placement_building_type = ""
	queue_unit_type = ""
	hover_q = -1
	hover_r = -1


func clear_unit_selection() -> void:
	selected_unit_id = -1
	selected_building_id = -1


func set_build_mode(building_type: String) -> void:
	if building_type == "":
		placement_building_type = ""
		return
	placement_building_type = building_type
	queue_unit_type = ""
	clear_unit_selection()
	message_logged.emit("Build mode: %s — click a valid hex." % BUILD_LABELS.get(building_type, building_type))


func set_queue_mode(unit_type: String) -> void:
	queue_unit_type = unit_type
	placement_building_type = ""
	clear_unit_selection()
	message_logged.emit("Queue mode: %s — click your Barracks." % MilitaryCatalog.display_name(unit_type))


func set_paused(value: bool) -> void:
	paused = value


func set_speed(multiplier: float) -> void:
	speed_multiplier = clampf(multiplier, 0.25, 8.0)


func handle_hex_hover(q: int, r: int) -> void:
	hover_q = q
	hover_r = r


func handle_hex_click(q: int, r: int) -> Dictionary:
	if viewer.state == null:
		return {"handled": false, "message": "No game loaded."}
	if placement_building_type != "":
		return _handle_build_click(q, r)
	if queue_unit_type != "":
		return _handle_queue_click(q, r)
	return _handle_select_or_command(q, r)


func handle_command_click(q: int, r: int) -> Dictionary:
	if selected_unit_id < 0:
		return {"handled": false, "message": "Select a unit first."}
	var enemy: Dictionary = _enemy_at(q, r)
	if not enemy.is_empty():
		return _issue_attack(selected_unit_id, int(enemy.get("id", 0)))
	return _issue_move(selected_unit_id, q, r)


func get_placement_preview() -> Dictionary:
	if viewer.state == null or placement_building_type == "" or hover_q < 0:
		return {"active": false}
	var check: Dictionary = MilitaryMapRules.can_place_building(
		viewer.state, placement_building_type, hover_q, hover_r, viewer.actor_id,
	)
	return {
		"active": true,
		"q": hover_q,
		"r": hover_r,
		"building_type": placement_building_type,
		"valid": bool(check.get("ok", false)),
		"reason": human_reason(str(check.get("reason", ""))),
	}


func build_status_text() -> String:
	if viewer.snapshot.is_empty():
		return "Simulation: Paused | Speed: 1x | Tick: —"
	var tick: int = int(viewer.snapshot.get("tick", 0))
	var play_state: String = "Paused" if paused else "Playing"
	return "Simulation: %s | Speed: %.0fx | Tick: %d" % [play_state, speed_multiplier, tick]


func get_current_tick() -> int:
	if viewer.snapshot.is_empty():
		return 0
	return int(viewer.snapshot.get("tick", 0))


func build_objective_text() -> String:
	var lines: PackedStringArray = PackedStringArray(["[b]Objective[/b]"])
	if is_victory():
		lines.append("[color=#55cc66]Victory![/color] Enemy Command Hub destroyed.")
		return "\n".join(lines)
	lines.append("Destroy the enemy Command Hub to win the skirmish.")
	lines.append("")
	lines.append("1. Build Power Generator and Barracks near your hub.")
	lines.append("2. Queue a Rifle Squad at the Barracks.")
	lines.append("3. Select your squad and attack the enemy base.")
	return "\n".join(lines)


func build_summary_text() -> String:
	if viewer.snapshot.is_empty():
		return "Credits: — | Power: —"
	var credits: int = 0
	var supply: int = 0
	var demand: int = 0
	for player in viewer.snapshot.get("military_players", []):
		if str(player.get("player_id", "")) != viewer.actor_id:
			continue
		credits = int(player.get("credits", 0))
		supply = int(player.get("power_supply", 0))
		demand = int(player.get("power_demand", 0))
		break
	return "Credits: %d | Power: %d / %d" % [credits, supply, demand]


func build_production_text() -> String:
	if viewer.snapshot.is_empty():
		return "Production queues: (loading)"
	var lines: PackedStringArray = PackedStringArray(["[b]Production queues[/b]"])
	var found: bool = false
	for queue in viewer.snapshot.get("production_queues", []):
		if typeof(queue) != TYPE_DICTIONARY:
			continue
		if str(queue.get("player_id", "")) != viewer.actor_id:
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
				suffix = " [%s]" % human_reason(blocked)
			item_labels.append("%s %d/%d%s" % [MilitaryCatalog.display_name(utype), prog, req, suffix])
		lines.append("- %s: %s" % [MilitaryCatalog.display_name(building_type), ", ".join(item_labels)])
	if not found:
		lines.append("- (idle — build Barracks and queue units)")
	return "\n".join(lines)


func build_event_text() -> String:
	return last_command_message


func get_mode_banner_text() -> Dictionary:
	if placement_building_type != "":
		var label: String = str(BUILD_LABELS.get(placement_building_type, placement_building_type))
		return {
			"title": "Placing %s" % label,
			"instruction": "Click a green hex to place it. Red means it cannot be built there. Esc cancels.\n%s" % MilitaryCatalog.role_text(placement_building_type),
		}
	if queue_unit_type != "":
		return {
			"title": "Queue %s" % MilitaryCatalog.display_name(queue_unit_type),
			"instruction": "Click your Barracks to add this unit to its production queue.",
		}
	if selected_unit_id >= 0:
		return {
			"title": "Unit selected",
			"instruction": "Click a hex to move. Click an enemy to attack. Esc clears selection.",
		}
	return {"title": "", "instruction": ""}


func get_mode_context() -> Dictionary:
	return {
		"build_mode": placement_building_type != "",
		"queue_mode": queue_unit_type != "",
		"building_type": placement_building_type,
		"unit_type": queue_unit_type,
	}


func build_help_text() -> String:
	return (
		"[b]Skirmish MVP[/b]\n"
		+ "Press Space or Play to run the battle simulation.\n"
		+ "You can inspect, pan, zoom, build, and queue units while paused.\n\n"
		+ "1. Build Power Generator and Barracks near your Command Hub.\n"
		+ "2. Queue a Rifle Squad at the Barracks.\n"
		+ "3. Select your squad and click enemies to attack.\n"
		+ "4. Win by destroying the enemy Command Hub.\n\n"
		+ "[b]Time[/b]: Space Play/Pause · . or Enter Step · F1/F2/F3 speed 1x/2x/4x\n"
		+ "[b]Map[/b]: Right-drag or WASD pan · Wheel or +/- zoom · Left-click select/place\n"
		+ "[b]Commands[/b]: Select unit, click destination or enemy · Esc cancel · H toggle help"
	)


static func human_reason(code: String) -> String:
	if code == "":
		return ""
	if REASON_MESSAGES.has(code):
		return str(REASON_MESSAGES[code])
	return MilitaryCatalog.human_reason(code) if code != "" else code


func is_victory() -> bool:
	var military: Dictionary = viewer.snapshot.get("diagnostics", {}).get("military", {})
	if str(military.get("victory_state", "")) != MilitaryConstants.VICTORY_WON:
		return false
	var winner: String = str(MilitaryState.get_meta_value(viewer.state, "winner_player_id", ""))
	return winner == viewer.actor_id or winner == ""


func find_first_unit_hex(unit_type: String, player_id: String = "p1") -> Vector2i:
	for unit in viewer.snapshot.get("units", []):
		if str(unit.get("unit_type", "")) != unit_type:
			continue
		if str(unit.get("player_id", "")) != player_id:
			continue
		return Vector2i(int(unit.get("q", -1)), int(unit.get("r", -1)))
	return Vector2i(-1, -1)


func find_first_building_hex(building_type: String, player_id: String = "p1") -> Vector2i:
	for building in viewer.snapshot.get("buildings", []):
		if str(building.get("building_type", "")) != building_type:
			continue
		if str(building.get("player_id", "")) != player_id:
			continue
		return Vector2i(int(building.get("anchor_q", building.get("q", -1))), int(building.get("anchor_r", building.get("r", -1))))
	return Vector2i(-1, -1)


func _handle_build_click(q: int, r: int) -> Dictionary:
	var check: Dictionary = MilitaryMapRules.can_place_building(
		viewer.state, placement_building_type, q, r, viewer.actor_id,
	)
	if not check.get("ok", false):
		var msg: String = human_reason(str(check.get("reason", "REJECTED")))
		last_command_message = "Cannot build: %s" % msg
		message_logged.emit(last_command_message)
		return {"handled": true, "success": false, "message": msg}
	var dto: Dictionary = {
		"type": "PlaceBuildingCommand",
		"label": "Place %s at (%d,%d)" % [placement_building_type, q, r],
		"actor_id": viewer.actor_id,
		"payload": {
			"building_type": placement_building_type,
			"q": q,
			"r": r,
			"player_id": viewer.actor_id,
			"tick": viewer.state.tick,
		},
	}
	var result: CommandResult = viewer.submit_command_dto(dto)
	if result.success:
		last_command_message = "Construction site placed: %s." % BUILD_LABELS.get(placement_building_type, placement_building_type)
		message_logged.emit(last_command_message)
	else:
		last_command_message = "Build failed: %s" % human_reason(result.error_code if result.error_code != "" else result.message)
		message_logged.emit(last_command_message)
	return {"handled": true, "success": result.success, "message": result.message}


func _handle_queue_click(q: int, r: int) -> Dictionary:
	var selection: Dictionary = get_selection_at(q, r)
	if str(selection.get("kind", "")) != "building":
		last_command_message = "Click your Barracks to queue units."
		message_logged.emit(last_command_message)
		return {"handled": true, "success": false, "message": last_command_message}
	var building: Dictionary = selection.get("data", {})
	if str(building.get("building_type", "")) != "barracks":
		last_command_message = "Only Barracks can queue infantry."
		message_logged.emit(last_command_message)
		return {"handled": true, "success": false, "message": last_command_message}
	var dto: Dictionary = {
		"type": "QueueUnitCommand",
		"label": "Queue %s" % queue_unit_type,
		"actor_id": viewer.actor_id,
		"payload": {
			"building_id": int(building.get("id", 0)),
			"unit_type": queue_unit_type,
			"player_id": viewer.actor_id,
			"tick": viewer.state.tick,
		},
	}
	var result: CommandResult = viewer.submit_command_dto(dto)
	if result.success:
		last_command_message = "Queued %s at Barracks." % MilitaryCatalog.display_name(queue_unit_type)
		message_logged.emit(last_command_message)
		queue_unit_type = ""
	else:
		last_command_message = "Queue failed: %s" % human_reason(result.error_code if result.error_code != "" else result.message)
		message_logged.emit(last_command_message)
	return {"handled": true, "success": result.success, "message": result.message}


func _handle_select_or_command(q: int, r: int) -> Dictionary:
	var selection: Dictionary = get_selection_at(q, r)
	var kind: String = str(selection.get("kind", ""))
	if selected_unit_id >= 0:
		if kind == "enemy_unit":
			return _issue_attack(selected_unit_id, int(selection.get("data", {}).get("id", 0)))
		if kind in ["empty", "building", "construction_site"]:
			return _issue_move(selected_unit_id, q, r)
	if kind == "unit":
		selected_unit_id = int(selection.get("data", {}).get("id", -1))
		selected_building_id = -1
		last_command_message = "Selected %s." % MilitaryCatalog.display_name(str(selection.get("data", {}).get("unit_type", "unit")))
		message_logged.emit(last_command_message)
		return {"handled": true, "success": true, "message": "unit_selected"}
	if kind in ["building", "construction_site", "enemy_unit"]:
		selected_unit_id = -1
		selected_building_id = int(selection.get("data", {}).get("id", -1))
		return {"handled": true, "success": true, "message": "selected"}
	selected_unit_id = -1
	selected_building_id = -1
	return {"handled": true, "success": true, "message": ""}


func _issue_move(unit_id: int, q: int, r: int) -> Dictionary:
	var dto: Dictionary = {
		"type": "MoveUnitCommand",
		"label": "Move unit %d to (%d,%d)" % [unit_id, q, r],
		"actor_id": viewer.actor_id,
		"payload": {
			"unit_id": unit_id,
			"q": q,
			"r": r,
			"player_id": viewer.actor_id,
			"tick": viewer.state.tick,
		},
	}
	var result: CommandResult = viewer.submit_command_dto(dto)
	if result.success:
		last_command_message = "Move order issued."
		message_logged.emit(last_command_message)
	else:
		last_command_message = "Move failed: %s" % human_reason(result.error_code if result.error_code != "" else result.message)
		message_logged.emit(last_command_message)
	return {"handled": true, "success": result.success, "message": result.message}


func _issue_attack(unit_id: int, target_id: int) -> Dictionary:
	var dto: Dictionary = {
		"type": "AttackUnitCommand",
		"label": "Attack target %d" % target_id,
		"actor_id": viewer.actor_id,
		"payload": {
			"unit_id": unit_id,
			"target_id": target_id,
			"player_id": viewer.actor_id,
			"tick": viewer.state.tick,
		},
	}
	var result: CommandResult = viewer.submit_command_dto(dto)
	if result.success:
		last_command_message = "Attack order issued."
		message_logged.emit(last_command_message)
	else:
		last_command_message = "Attack failed: %s" % human_reason(result.error_code if result.error_code != "" else result.message)
		message_logged.emit(last_command_message)
	return {"handled": true, "success": result.success, "message": result.message}


func _enemy_at(q: int, r: int) -> Dictionary:
	for unit in viewer.snapshot.get("units", []):
		if int(unit.get("q", -1)) == q and int(unit.get("r", -1)) == r:
			if str(unit.get("player_id", "")) != viewer.actor_id:
				return unit
	for building in viewer.snapshot.get("buildings", []):
		if _occupies_footprint(building, q, r):
			if str(building.get("player_id", "")) != viewer.actor_id:
				return building
	return {}


func _building_type_by_id(building_id: int) -> String:
	for building in viewer.snapshot.get("buildings", []):
		if int(building.get("id", 0)) == building_id:
			return str(building.get("building_type", "?"))
	return "?"


static func _occupies_footprint(entity: Dictionary, q: int, r: int) -> bool:
	var anchor_q: int = int(entity.get("anchor_q", entity.get("q", -999)))
	var anchor_r: int = int(entity.get("anchor_r", entity.get("r", -999)))
	if anchor_q == q and anchor_r == r:
		return true
	for offset in entity.get("footprint", []):
		if typeof(offset) != TYPE_ARRAY or offset.size() < 2:
			continue
		if anchor_q + int(offset[0]) == q and anchor_r + int(offset[1]) == r:
			return true
	return false


func _on_viewer_snapshot_refreshed(snapshot: Dictionary) -> void:
	if not victory_shown and is_victory():
		victory_shown = true
		victory_achieved.emit()
		message_logged.emit("Victory! Enemy Command Hub destroyed.")
	snapshot_refreshed.emit(snapshot)


func _on_viewer_command_result(result: Dictionary, dto: Dictionary) -> void:
	command_result.emit(result, dto)
