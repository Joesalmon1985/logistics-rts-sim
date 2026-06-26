class_name SettlerMvpController
extends RefCounted

## Human-playable Settler MVP controller — wraps ViewerController and map-driven commands.

signal snapshot_refreshed(snapshot: Dictionary)
signal command_result(result: Dictionary, dto: Dictionary)
signal message_logged(text: String)
signal victory_achieved()

const SCENARIO_PATH := "scenarios/economy/settler_mvp.json"

const BUILD_TYPES: Array[String] = ["woodcutter", "forester", "sawmill", "quarry"]
const BUILD_LABELS: Dictionary = {
	"woodcutter": "Woodcutter Lodge",
	"forester": "Forester Camp",
	"sawmill": "Sawmill",
	"quarry": "Quarry",
}

const REASON_MESSAGES: Dictionary = {
	"FOOTPRINT_OVERLAP": "Another building or site occupies this hex.",
	"OUT_OF_BOUNDS": "Outside the map.",
	"OUTSIDE_BUILD_RADIUS": "Too far from headquarters.",
	"NOT_BUILDABLE": "Terrain cannot support this building.",
	"TERRAIN_MISMATCH": "Wrong terrain type for this building.",
	"DEPOSIT_REQUIRED": "Needs forest, rock, or another deposit here.",
	"SLOPE_TOO_STEEP": "Slope is too steep to build.",
	"UNKNOWN_BUILDING": "Unknown building type.",
	"NOT_ADJACENT": "Road endpoints must be adjacent hexes.",
	"TERRAIN_BLOCKED": "Terrain blocks roads here.",
	"ROAD_EXISTS": "Road already exists.",
	"NOT_OWNER": "You do not own this hex.",
}

var viewer: ViewerController
var placement_building_type: String = ""
var road_mode_active: bool = false
var road_start_hex: Vector2i = Vector2i(-1, -1)
var paused: bool = true
var speed_multiplier: float = 1.0
var victory_shown: bool = false
var hover_q: int = -1
var hover_r: int = -1


func _init() -> void:
	viewer = ViewerController.new()
	viewer.snapshot_refreshed.connect(_on_viewer_snapshot_refreshed)
	viewer.command_result.connect(_on_viewer_command_result)


func reset_game() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO_PATH)
	var state: GameState = runner.create_game(scenario)
	viewer.load_demo_state(state, "p1")
	_apply_auto_worker_policy()
	clear_modes()
	victory_shown = false
	message_logged.emit("New Settler MVP game started.")


func get_state() -> GameState:
	return viewer.state


func get_snapshot() -> Dictionary:
	return viewer.snapshot


func get_render_data() -> Dictionary:
	return viewer.get_render_data()


func get_selection_at(q: int, r: int) -> Dictionary:
	return viewer.get_selection_at(q, r)


func step_tick() -> void:
	viewer.step_tick()


func clear_modes() -> void:
	placement_building_type = ""
	road_mode_active = false
	road_start_hex = Vector2i(-1, -1)
	hover_q = -1
	hover_r = -1


func set_build_mode(building_type: String) -> void:
	if building_type == "":
		placement_building_type = ""
		road_mode_active = false
		return
	placement_building_type = building_type
	road_mode_active = false
	road_start_hex = Vector2i(-1, -1)
	message_logged.emit("Build mode: %s — click a valid hex." % BUILD_LABELS.get(building_type, building_type))


func set_road_mode(active: bool) -> void:
	road_mode_active = active
	placement_building_type = ""
	road_start_hex = Vector2i(-1, -1)
	if active:
		message_logged.emit("Road mode — click a start hex on your road network, then a destination.")
	else:
		message_logged.emit("Road mode cancelled.")


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
	if road_mode_active:
		return _handle_road_click(q, r)
	return {"handled": false, "message": ""}


func get_placement_preview() -> Dictionary:
	if viewer.state == null or placement_building_type == "" or hover_q < 0:
		return {"active": false}
	var check: Dictionary = EconomyMapRules.can_place_building(
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


func get_road_preview() -> Dictionary:
	if viewer.state == null or not road_mode_active or road_start_hex.x < 0 or hover_q < 0:
		return {"active": false, "hexes": [], "valid": false, "reason": ""}
	var path: Array = find_road_extension_path(
		viewer.state, viewer.actor_id,
		road_start_hex.x, road_start_hex.y, hover_q, hover_r,
	)
	if path.is_empty():
		return {
			"active": true,
			"hexes": [{"q": road_start_hex.x, "r": road_start_hex.y}, {"q": hover_q, "r": hover_r}],
			"valid": false,
			"reason": "No legal road route to this hex.",
		}
	return {"active": true, "hexes": path, "valid": true, "reason": ""}


func find_road_extension_path(
	state: GameState,
	player_id: String,
	from_q: int,
	from_r: int,
	to_q: int,
	to_r: int,
) -> Array:
	var start := Vector2i(from_q, from_r)
	var goal := Vector2i(to_q, to_r)
	if start == goal:
		return [{"q": from_q, "r": from_r}]
	var queue: Array = [start]
	var came_from: Dictionary = {}
	came_from[start] = null
	while not queue.is_empty():
		var current: Vector2i = queue.pop_front()
		if current == goal:
			break
		var coord := HexCoord.new(current.x, current.y)
		for nb in coord.neighbors():
			if not state.map.in_bounds(nb):
				continue
			var next := Vector2i(nb.q, nb.r)
			if came_from.has(next):
				continue
			if not _edge_traversable(state, player_id, coord, nb):
				continue
			came_from[next] = current
			queue.append(next)
	if not came_from.has(goal):
		return []
	var path: Array = []
	var cur: Variant = goal
	while cur != null:
		path.insert(0, {"q": cur.x, "r": cur.y})
		cur = came_from.get(cur)
	return path


static func _edge_traversable(state: GameState, player_id: String, hex_a: HexCoord, hex_b: HexCoord) -> bool:
	if EconomyRoadSystem.has_road_between(state, hex_a, hex_b):
		var road: Dictionary = EconomyRoadSystem.get_road_between(state, hex_a, hex_b)
		return str(road.get("player_id", "")) == player_id
	var check: Dictionary = EconomyMapRules.can_place_road(state, hex_a, hex_b, player_id)
	return bool(check.get("ok", false))


func submit_road_path(path: Array) -> Dictionary:
	if viewer.state == null:
		return {"success": false, "message": "No game loaded."}
	var placed: int = 0
	for i in range(path.size() - 1):
		var a_q: int = int(path[i].get("q", -1))
		var a_r: int = int(path[i].get("r", -1))
		var b_q: int = int(path[i + 1].get("q", -1))
		var b_r: int = int(path[i + 1].get("r", -1))
		var hex_a := HexCoord.new(a_q, a_r)
		var hex_b := HexCoord.new(b_q, b_r)
		if EconomyRoadSystem.has_road_between(viewer.state, hex_a, hex_b):
			continue
		var dto: Dictionary = {
			"type": "PlaceRoadCommand",
			"label": "Road %d,%d -> %d,%d" % [a_q, a_r, b_q, b_r],
			"actor_id": viewer.actor_id,
			"payload": {
				"from_q": a_q,
				"from_r": a_r,
				"to_q": b_q,
				"to_r": b_r,
				"player_id": viewer.actor_id,
				"tick": viewer.state.tick,
			},
		}
		var result: CommandResult = viewer.submit_command_dto(dto)
		if not result.success:
			return {
				"success": false,
				"message": human_reason(result.error_code if result.error_code != "" else result.message),
				"placed": placed,
			}
		placed += 1
	return {"success": true, "message": "Road extended (%d new segment(s))." % placed, "placed": placed}


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
	var objective: Dictionary = viewer.snapshot.get("objective", {})
	if objective.is_empty():
		return "Objective: (loading)"
	if bool(objective.get("complete", false)):
		return "[b]Victory![/b] Starter economy complete."
	var lines: PackedStringArray = PackedStringArray(["[b]Objective[/b]"])
	for req in objective.get("requirements", []):
		var mark: String = "[color=#55cc66]✓[/color]" if bool(req.get("done", false)) else "[color=#cccccc]…[/color]"
		var label: String = str(req.get("label", "?"))
		var current: int = int(req.get("current", 0))
		var target: int = int(req.get("target", 1))
		if str(req.get("key", "")).begins_with("ware_"):
			lines.append("%s %s: %d / %d" % [mark, label, current, target])
		else:
			var detail: String = str(req.get("detail", ""))
			var suffix: String = " — %s" % detail if detail != "" and not bool(req.get("done", false)) else ""
			lines.append("%s %s%s" % [mark, label, suffix])
	return "\n".join(lines)


func build_stockpile_text() -> String:
	var totals: Dictionary = viewer.snapshot.get("diagnostics", {}).get("ware_totals", {}).get("by_type", {})
	var parts: PackedStringArray = PackedStringArray()
	for ware_type in ["timber", "planks", "stone", "tools", "builder_work"]:
		if totals.has(ware_type):
			parts.append("%s: %d" % [ware_type, int(totals[ware_type])])
	return "Stock: %s" % (", ".join(parts) if not parts.is_empty() else "(empty)")


func build_warnings_text() -> String:
	var lines: PackedStringArray = PackedStringArray()
	var diag: Dictionary = viewer.snapshot.get("diagnostics", {})
	for site in diag.get("construction", {}).get("sites", []):
		var missing: Dictionary = site.get("missing_wares", {})
		for ware_type in missing:
			var need: int = int(missing[ware_type])
			lines.append(
				"Site %s: waiting for %s (%d more needed)."
				% [str(site.get("building_type", "?")), ware_type, need],
			)
	for prod in diag.get("production", []):
		var reason: String = str(prod.get("paused_reason", ""))
		if reason != "":
			lines.append("%s: %s" % [str(prod.get("building_type", "?")), human_reason(reason)])
	for job in viewer.snapshot.get("blocked_job_markers", []):
		var block: String = str(job.get("block_reason", ""))
		if block != "":
			lines.append("Delivery blocked: %s" % human_reason(block))
	if lines.is_empty():
		return "No warnings."
	return "\n".join(lines)


func build_help_text() -> String:
	return (
		"[b]Settler MVP[/b]\n"
		+ "Press Space or Play to start the settlement simulation.\n"
		+ "You can inspect, pan, zoom and place plans while paused.\n\n"
		+ "1. Connect roads from HQ to forest and rock areas.\n"
		+ "2. Build Woodcutter, Forester, Sawmill, and Quarry on suitable terrain.\n"
		+ "3. Press Play and watch carriers deliver materials.\n"
		+ "4. Win when all four buildings are complete, connected, and you store 10 planks + 10 stone.\n\n"
		+ "[b]Time[/b]: Space Play/Pause · . or Enter Step · F1/F2/F3 speed 1x/2x/4x\n"
		+ "[b]Map[/b]: Right-drag or WASD pan · Wheel or +/- zoom · Left-click inspect/place\n"
		+ "[b]Build[/b]: 1–4 building modes · R road · Esc cancel · H toggle this help"
	)


static func human_reason(code: String) -> String:
	if code == "":
		return ""
	if REASON_MESSAGES.has(code):
		return str(REASON_MESSAGES[code])
	return code.replace("_", " ").to_lower().capitalize()


func _handle_build_click(q: int, r: int) -> Dictionary:
	var check: Dictionary = EconomyMapRules.can_place_building(
		viewer.state, placement_building_type, q, r, viewer.actor_id,
	)
	if not check.get("ok", false):
		var msg: String = human_reason(str(check.get("reason", "REJECTED")))
		message_logged.emit("Cannot build: %s" % msg)
		return {"handled": true, "success": false, "message": msg}
	var dto: Dictionary = {
		"type": "PlaceBuildingPlanCommand",
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
		message_logged.emit("Construction site placed: %s." % BUILD_LABELS.get(placement_building_type, placement_building_type))
	else:
		message_logged.emit("Build failed: %s" % human_reason(result.error_code if result.error_code != "" else result.message))
	return {"handled": true, "success": result.success, "message": result.message}


func _handle_road_click(q: int, r: int) -> Dictionary:
	if road_start_hex.x < 0:
		road_start_hex = Vector2i(q, r)
		message_logged.emit("Road start: (%d, %d) — click destination." % [q, r])
		return {"handled": true, "success": true, "message": "Road start selected."}
	var path: Array = find_road_extension_path(viewer.state, viewer.actor_id, road_start_hex.x, road_start_hex.y, q, r)
	if path.is_empty():
		var msg: String = "No legal road route to (%d, %d)." % [q, r]
		message_logged.emit(msg)
		road_start_hex = Vector2i(-1, -1)
		return {"handled": true, "success": false, "message": msg}
	var outcome: Dictionary = submit_road_path(path)
	road_start_hex = Vector2i(-1, -1)
	if outcome.get("success", false):
		message_logged.emit(str(outcome.get("message", "Road placed.")))
	else:
		message_logged.emit(str(outcome.get("message", "Road failed.")))
	return {"handled": true, "success": outcome.get("success", false), "message": str(outcome.get("message", ""))}


func _apply_auto_worker_policy() -> void:
	if viewer.state == null:
		return
	var dto: Dictionary = {
		"type": "SetWorkerPolicyCommand",
		"label": "Auto worker policy",
		"actor_id": viewer.actor_id,
		"payload": {
			"policy": "auto",
			"player_id": viewer.actor_id,
			"tick": viewer.state.tick,
		},
	}
	viewer.submit_command_dto(dto)


func _on_viewer_snapshot_refreshed(snapshot: Dictionary) -> void:
	if not victory_shown and bool(snapshot.get("objective", {}).get("complete", false)):
		victory_shown = true
		victory_achieved.emit()
		message_logged.emit("Victory! Starter economy objective complete.")
	snapshot_refreshed.emit(snapshot)


func _on_viewer_command_result(result: Dictionary, dto: Dictionary) -> void:
	command_result.emit(result, dto)
