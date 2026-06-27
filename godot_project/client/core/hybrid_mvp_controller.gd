class_name HybridMvpController
extends RefCounted

## Human-playable Hybrid MVP — delegated military control + economy logistics.

signal snapshot_refreshed(snapshot: Dictionary)
signal command_result(result: Dictionary, dto: Dictionary)
signal message_logged(text: String)
signal victory_achieved()

const SCENARIO_PATH := "scenarios/hybrid/hybrid_mvp.json"

const ECONOMY_BUILD_TYPES: Array[String] = ["woodcutter", "forester", "sawmill", "quarry"]
const MILITARY_BUILD_TYPES: Array[String] = ["power_generator", "guard_post", "barracks"]
const BUILD_LABELS: Dictionary = {
	"woodcutter": "Woodcutter Lodge",
	"forester": "Forester Camp",
	"sawmill": "Sawmill",
	"quarry": "Quarry",
	"power_generator": "Power Generator",
	"guard_post": "Guard Post",
	"barracks": "Barracks",
}

const REASON_MESSAGES: Dictionary = {
	"FOOTPRINT_OVERLAP": "Already occupied.",
	"OUT_OF_BOUNDS": "Outside the map.",
	"OUTSIDE_BUILD_RADIUS": "Too far from Headquarters.",
	"NOT_BUILDABLE": "Not buildable terrain.",
	"TERRAIN_MISMATCH": "Not buildable terrain.",
	"DEPOSIT_REQUIRED": "Needs nearby trees or rock.",
	"UNKNOWN_BUILDING": "Unknown building type.",
	"NOT_ADJACENT": "Road endpoints must be adjacent hexes.",
	"TERRAIN_BLOCKED": "Terrain blocks roads here.",
	"ROAD_EXISTS": "Road already exists.",
	"NOT_OWNER": "You do not own this hex.",
	"ERR_TARGET_NOT_VISIBLE": "Target not visible.",
}

var viewer: ViewerController
var placement_building_type: String = ""
var road_mode_active: bool = false
var attack_mode_active: bool = false
var road_start_hex: Vector2i = Vector2i(-1, -1)
var paused: bool = false
var speed_multiplier: float = 1.0
var victory_shown: bool = false
var hover_q: int = -1
var hover_r: int = -1
var selected_q: int = -1
var selected_r: int = -1


func _init() -> void:
	viewer = ViewerController.new()
	viewer.snapshot_refreshed.connect(_on_viewer_snapshot_refreshed)
	viewer.command_result.connect(_on_viewer_command_result)


func reset_game() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO_PATH)
	var state: GameState = runner.create_game(scenario)
	HybridTestHelper.prepare_playable_map(state, 0, 0, 15, 15, 2, 2)
	state = runner.run_ticks(state, 1)
	viewer.load_demo_state(state, "p1")
	_apply_auto_worker_policy()
	clear_modes()
	victory_shown = false
	selected_q = -1
	selected_r = -1
	message_logged.emit("New Hybrid MVP game started.")


func get_state() -> GameState:
	return viewer.state


func get_snapshot() -> Dictionary:
	return viewer.snapshot


func get_render_data() -> Dictionary:
	return viewer.get_render_data()


func get_military_sprite_states() -> Dictionary:
	return MilitarySpriteStateSelector.select_states(get_render_data(), viewer.snapshot, [])


func get_selection_at(q: int, r: int) -> Dictionary:
	for carrier in viewer.snapshot.get("carriers", []):
		if int(round(float(carrier.get("q", -1)))) == q and int(round(float(carrier.get("r", -1)))) == r:
			return {"kind": "carrier", "data": carrier}
	for unit in viewer.snapshot.get("units", []):
		if int(unit.get("q", -1)) == q and int(unit.get("r", -1)) == r:
			return {"kind": "unit", "data": unit}
	for building in viewer.snapshot.get("buildings", []):
		if BuildingCatalog.occupies_hex(building, q, r):
			return {"kind": "building", "data": building}
	for site in viewer.snapshot.get("construction_sites", []):
		var sq: int = int(site.get("anchor_q", site.get("q", -1)))
		var sr: int = int(site.get("anchor_r", site.get("r", -1)))
		if sq == q and sr == r:
			return {"kind": "construction_site", "data": site}
	for node in viewer.snapshot.get("road_nodes", []):
		if int(node.get("q", -1)) == q and int(node.get("r", -1)) == r:
			return {"kind": "road_node", "data": node}
	return viewer.get_selection_at(q, r)


func step_tick() -> void:
	viewer.step_tick()


func clear_modes() -> void:
	placement_building_type = ""
	road_mode_active = false
	attack_mode_active = false
	road_start_hex = Vector2i(-1, -1)
	hover_q = -1
	hover_r = -1


func set_build_mode(building_type: String) -> void:
	if building_type == "":
		placement_building_type = ""
		road_mode_active = false
		attack_mode_active = false
		return
	placement_building_type = building_type
	road_mode_active = false
	attack_mode_active = false
	road_start_hex = Vector2i(-1, -1)
	message_logged.emit("Build mode: %s — click a valid hex." % BUILD_LABELS.get(building_type, building_type))


func set_road_mode(active: bool) -> void:
	road_mode_active = active
	placement_building_type = ""
	attack_mode_active = false
	road_start_hex = Vector2i(-1, -1)
	if active:
		message_logged.emit("Road mode — click start then destination on your network.")
	else:
		message_logged.emit("Road mode cancelled.")


func set_attack_mode(active: bool) -> void:
	attack_mode_active = active
	placement_building_type = ""
	road_mode_active = false
	road_start_hex = Vector2i(-1, -1)
	if active:
		message_logged.emit("Attack posture — click an enemy hex to assign delegated attack.")
	else:
		message_logged.emit("Attack mode cancelled.")


func set_selection(q: int, r: int) -> void:
	selected_q = q
	selected_r = r


func queue_rifle_squad_at_selection() -> Dictionary:
	if viewer.state == null or selected_q < 0:
		return {"success": false, "message": "Select a Barracks or Guard Post first."}
	var sel: Dictionary = get_selection_at(selected_q, selected_r)
	if str(sel.get("kind", "")) != "building":
		return {"success": false, "message": "Select a military building to queue units."}
	var btype: String = str(sel.get("data", {}).get("building_type", ""))
	if btype not in ["barracks", "guard_post"]:
		return {"success": false, "message": "Only Barracks or Guard Post can queue infantry."}
	var building_id: int = int(sel.get("data", {}).get("id", 0))
	var dto: Dictionary = {
		"type": "QueueUnitCommand",
		"label": "Queue Rifle Squad",
		"actor_id": viewer.actor_id,
		"payload": {
			"building_id": building_id,
			"unit_type": "rifle_squad",
			"player_id": viewer.actor_id,
			"tick": viewer.state.tick,
		},
	}
	var result: CommandResult = viewer.submit_command_dto(dto)
	var msg: String = "Queued Rifle Squad." if result.success else human_reason(result.error_code if result.error_code != "" else result.message)
	message_logged.emit(msg)
	return {"success": result.success, "message": msg}


func handle_hex_click(q: int, r: int) -> Dictionary:
	if viewer.state == null:
		return {"handled": false, "message": "No game loaded."}
	set_selection(q, r)
	if attack_mode_active:
		return _handle_attack_click(q, r)
	if placement_building_type != "":
		return _handle_build_click(q, r)
	if road_mode_active:
		return _handle_road_click(q, r)
	return {"handled": false, "message": ""}


func handle_hex_hover(q: int, r: int) -> void:
	hover_q = q
	hover_r = r


func build_status_text() -> String:
	if viewer.snapshot.is_empty():
		return "Simulation: Paused | Speed: 1x | Tick: —"
	return "Simulation: %s | Speed: %.0fx | Tick: %d" % [
		"Paused" if paused else "Playing",
		speed_multiplier,
		int(viewer.snapshot.get("tick", 0)),
	]


func build_objective_text() -> String:
	return HybridCatalog.format_objective(viewer.snapshot.get("objective", {}))


func build_supply_text() -> String:
	return HybridCatalog.format_supply_panel(viewer.snapshot, viewer.actor_id)


func build_production_text() -> String:
	return HybridCatalog.format_production_queues(viewer.snapshot, viewer.actor_id)


func build_orders_text() -> String:
	return HybridCatalog.format_delegated_orders(viewer.snapshot, viewer.actor_id)


func build_inspector_text() -> String:
	return HybridCatalog.format_selection(get_selection_at(selected_q, selected_r), viewer.snapshot)


func build_warnings_text() -> String:
	var lines: PackedStringArray = PackedStringArray()
	for queue in viewer.snapshot.get("production_queues", []):
		for item in queue.get("items", []):
			var blocked: String = str(item.get("blocked_reason", ""))
			if blocked != "":
				lines.append("Production blocked: %s" % MilitaryCatalog.human_reason(blocked))
	if lines.is_empty():
		return "No warnings."
	return "\n".join(lines)


func get_mode_banner_text() -> Dictionary:
	if attack_mode_active:
		return {
			"title": "Attack posture",
			"instruction": "Click an enemy building or unit hex. Your forces will attack automatically (delegated control).",
		}
	if placement_building_type != "":
		var label: String = str(BUILD_LABELS.get(placement_building_type, placement_building_type))
		return {
			"title": "Placing %s" % label,
			"instruction": "Click a valid hex. Military sites need roads so carriers can deliver planks, stone and tools.",
		}
	if road_mode_active:
		return {
			"title": "Road mode",
			"instruction": "Connect roads from HQ to resources and military construction sites.",
		}
	return {"title": "Inspect mode", "instruction": "Select buildings, carriers, or units. Use build palette for economy and military structures."}


func build_help_text() -> String:
	return (
		"[b]Hybrid MVP[/b]\n"
		+ "Build economy, supply a Guard Post or Barracks, queue a Rifle Squad, then set Attack Posture on the enemy base.\n\n"
		+ "Delegated control — you set intent; units execute automatically.\n"
		+ "Space Play/Pause · R road · A attack posture · Q queue squad at selected military building"
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
	return {"handled": true, "success": result.success, "message": result.message}


func _handle_attack_click(q: int, r: int) -> Dictionary:
	var dto: Dictionary = {
		"type": "DelegatedAttackCommand",
		"label": "Attack (%d,%d)" % [q, r],
		"actor_id": viewer.actor_id,
		"payload": {
			"target_q": q,
			"target_r": r,
			"player_id": viewer.actor_id,
			"tick": viewer.state.tick,
		},
	}
	var result: CommandResult = viewer.submit_command_dto(dto)
	var msg: String = "Attack posture set." if result.success else human_reason(result.error_code if result.error_code != "" else result.message)
	message_logged.emit(msg)
	attack_mode_active = false
	return {"handled": true, "success": result.success, "message": msg}


func _handle_road_click(q: int, r: int) -> Dictionary:
	if road_start_hex.x < 0:
		road_start_hex = Vector2i(q, r)
		message_logged.emit("Road start: (%d, %d)" % [q, r])
		return {"handled": true, "success": true, "message": "Road start selected."}
	var path: Array = find_road_extension_path(
		viewer.state, viewer.actor_id, road_start_hex.x, road_start_hex.y, q, r,
	)
	if path.is_empty():
		road_start_hex = Vector2i(-1, -1)
		return {"handled": true, "success": false, "message": "No legal road route."}
	var outcome: Dictionary = _submit_road_path(path)
	road_start_hex = Vector2i(-1, -1)
	return {"handled": true, "success": outcome.get("success", false), "message": str(outcome.get("message", ""))}


func _submit_road_path(path: Array) -> Dictionary:
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
			"label": "Road segment",
			"actor_id": viewer.actor_id,
			"payload": {
				"from_q": a_q, "from_r": a_r, "to_q": b_q, "to_r": b_r,
				"player_id": viewer.actor_id, "tick": viewer.state.tick,
			},
		}
		var result: CommandResult = viewer.submit_command_dto(dto)
		if not result.success:
			return {"success": false, "message": human_reason(result.error_code), "placed": placed}
		placed += 1
	return {"success": true, "message": "Road extended (%d segment(s))." % placed, "placed": placed}


func _apply_auto_worker_policy() -> void:
	if viewer.state == null:
		return
	viewer.submit_command_dto({
		"type": "SetWorkerPolicyCommand",
		"label": "Auto workers",
		"actor_id": viewer.actor_id,
		"payload": {"policy": "auto", "player_id": viewer.actor_id, "tick": viewer.state.tick},
	})


func _on_viewer_snapshot_refreshed(snapshot: Dictionary) -> void:
	if not victory_shown and bool(snapshot.get("objective", {}).get("complete", false)):
		victory_shown = true
		victory_achieved.emit()
		message_logged.emit("Victory! Hybrid objective complete.")
	snapshot_refreshed.emit(snapshot)


func _on_viewer_command_result(result: Dictionary, dto: Dictionary) -> void:
	command_result.emit(result, dto)


static func find_road_extension_path(
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
