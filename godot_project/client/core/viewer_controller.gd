class_name ViewerController
extends RefCounted

signal snapshot_refreshed(snapshot: Dictionary)
signal command_result(result: Dictionary, dto: Dictionary)

const MEANINGFUL_EVENT_SUBSTRINGS: Array[String] = [
	"construction",
	"ware",
	"carrier",
	"production",
	"damage",
	"combat",
	"unit_moved",
	"order",
]
const LOGISTICS_EVENT_SUBSTRINGS: Array[String] = [
	"production_completed",
	"tree_density",
	"ware_moved",
	"ware_delivered",
	"delivery_job",
	"construction",
	"road_placed",
]
const STEP_UNTIL_EVENT_MAX: int = 50

var state: GameState
var runner: SimulationTickRunner
var submitter: CommandSubmitter
var actor_id: String = "p1"
var snapshot: Dictionary = {}
var command_history: Array = []
var last_attack_target: Dictionary = {}
var last_placed_site: Dictionary = {}
var _last_production_progress: Dictionary = {}
var production_pulse_ids: Array = []
var layer_visibility: Dictionary = {
	"terrain": true,
	"buildings": true,
	"units": true,
	"roads": true,
	"resources": true,
	"fog": true,
	"diagnostics": true,
	"logistics": true,
	"forest_density": true,
}


func _init(p_runner: SimulationTickRunner = null, p_submitter: CommandSubmitter = null) -> void:
	runner = p_runner if p_runner != null else SimulationTickRunner.new()
	submitter = p_submitter if p_submitter != null else CommandSubmitter.new()


func load_demo_state(demo_state: GameState, p_actor_id: String = "p1") -> void:
	state = demo_state
	actor_id = p_actor_id
	command_history.clear()
	last_attack_target = {}
	last_placed_site = {}
	_last_production_progress.clear()
	production_pulse_ids.clear()
	refresh_snapshot()


func refresh_snapshot() -> Dictionary:
	if state == null:
		snapshot = {}
		return snapshot
	snapshot = SnapshotExporter.export_debug_snapshot(state, actor_id)
	_update_production_pulse()
	snapshot_refreshed.emit(snapshot)
	return snapshot


func _update_production_pulse() -> void:
	production_pulse_ids.clear()
	for building in snapshot.get("buildings", []):
		var bid: int = int(building.get("id", 0))
		var progress: int = int(building.get("production_progress", 0))
		var prev: int = int(_last_production_progress.get(str(bid), progress))
		if progress > prev:
			production_pulse_ids.append(bid)
		_last_production_progress[str(bid)] = progress


func get_render_data() -> Dictionary:
	return LayerRenderer.build_render_data(snapshot, layer_visibility)


func get_available_commands() -> Array:
	return ClientCommandAvailability.list_from_snapshot(snapshot)


func submit_command_dto(dto: Dictionary) -> CommandResult:
	if state == null:
		return CommandResult.fail("NO_STATE", "No game state loaded")
	var before_hash: String = ReplayHash.compute_state_hash(state)
	var result: CommandResult = submitter.submit_from_dto(state, dto)
	var entry: Dictionary = {
		"tick": state.tick,
		"type": str(dto.get("type", "")),
		"label": str(dto.get("label", "")),
		"success": result.success,
		"error_code": result.error_code,
		"message": result.message,
	}
	command_history.append(entry)
	command_result.emit(result.to_dict(), dto)
	refresh_snapshot()
	_track_attack_target(dto)
	_track_placed_site(dto)
	var after_hash: String = ReplayHash.compute_state_hash(state)
	if result.success:
		entry["state_changed"] = before_hash != after_hash
	return result


func submit_invalid_command() -> CommandResult:
	if state == null:
		return CommandResult.fail("NO_STATE", "No game state loaded")
	var result: CommandResult = submitter.submit_invalid_test(state, actor_id)
	command_history.append({
		"tick": state.tick,
		"type": "InvalidTestCommand",
		"label": "Invalid test command",
		"success": result.success,
		"error_code": result.error_code,
		"message": result.message,
	})
	command_result.emit(result.to_dict(), {})
	refresh_snapshot()
	return result


func step_tick() -> void:
	if state == null:
		return
	state = runner.tick(state)
	refresh_snapshot()


func step_ticks(count: int) -> void:
	for _i in range(maxi(0, count)):
		step_tick()


func step_until_meaningful_event() -> Dictionary:
	if state == null:
		return {"steps": 0, "event": "", "found": false}
	var steps: int = 0
	while steps < STEP_UNTIL_EVENT_MAX:
		var event_count_before: int = state.event_log.events.size()
		step_tick()
		steps += 1
		for i in range(event_count_before, state.event_log.events.size()):
			var evt: SimEvent = state.event_log.events[i]
			var event_type: String = str(evt.type).to_lower()
			for substring in MEANINGFUL_EVENT_SUBSTRINGS:
				if substring in event_type:
					return {"steps": steps, "event": str(evt.type), "found": true}
	return {"steps": steps, "event": "", "found": false}


func is_logistics_event(event_type: String) -> bool:
	var lower: String = event_type.to_lower()
	for substring in LOGISTICS_EVENT_SUBSTRINGS:
		if substring in lower:
			return true
	return false


func set_layer_visible(layer_name: String, visible: bool) -> void:
	layer_visibility[layer_name] = visible


func build_summary_text(variant: String) -> String:
	if snapshot.is_empty():
		return "Summary: (no snapshot)"
	if variant == "economy":
		return _economy_summary()
	if variant == "military":
		return _military_summary()
	return "Summary: tick %d" % int(snapshot.get("tick", 0))


func build_checklist_text() -> String:
	if snapshot.is_empty() or str(snapshot.get("variant", "")) != "economy_trial":
		return ""
	var steps: PackedStringArray = PackedStringArray()
	var site_visible: bool = snapshot.get("construction_sites", []).size() > 0
	steps.append("[ %s ] 1. Construction site visible" % ("OK" if site_visible else ".."))
	var road_ok: bool = _site_has_road_connection()
	steps.append("[ %s ] 2. Road connected to network" % ("OK" if road_ok else ".."))
	var delivering: bool = _site_materials_delivering()
	steps.append("[ %s ] 3. Materials delivering" % ("OK" if delivering else ".."))
	var completed: bool = not site_visible and _recent_building_added()
	steps.append("[ %s ] 4. Building completed" % ("OK" if completed else ".."))
	return "Build checklist:\n" + "\n".join(steps)


func _site_has_road_connection() -> bool:
	for site in snapshot.get("construction_sites", []):
		var sq: int = int(site.get("anchor_q", site.get("q", -1)))
		var sr: int = int(site.get("anchor_r", site.get("r", -1)))
		for road in snapshot.get("roads", []):
			if _road_touches_hex(road, sq, sr):
				return true
		for node in snapshot.get("road_nodes", []):
			if int(node.get("q", -1)) == sq and int(node.get("r", -1)) == sr:
				return true
	return snapshot.get("roads", []).size() > 0


func _road_touches_hex(road: Dictionary, q: int, r: int) -> bool:
	var ends: Array = [
		Vector2i(int(road.get("hex_a_q", road.get("from_q", -99))), int(road.get("hex_a_r", road.get("from_r", -99)))),
		Vector2i(int(road.get("hex_b_q", road.get("to_q", -99))), int(road.get("hex_b_r", road.get("to_r", -99)))),
	]
	for end in ends:
		if end.x == q and end.y == r:
			return true
	return false


func _site_materials_delivering() -> bool:
	for site in snapshot.get("construction_sites", []):
		for ware_type in site.get("delivered", {}):
			if int(site.get("delivered", {}).get(ware_type, 0)) > 0:
				return true
		if int(site.get("progress_percent", LayerRenderer.site_progress_percent(site))) > 0:
			return true
	for job in snapshot.get("ware_movement_markers", []):
		if str(job.get("dest_entity_type", "")) == "construction_site":
			return true
	for route in LayerRenderer.build_render_data(snapshot, layer_visibility).get("ware_routes", []):
		if not route.is_empty():
			return true
	return false


func _recent_building_added() -> bool:
	return snapshot.get("buildings", []).size() >= 3


func _economy_summary() -> String:
	var render: Dictionary = get_render_data()
	var fp: Dictionary = LayerRenderer.chain_fingerprint(render)
	var diag: Dictionary = snapshot.get("diagnostics", {})
	var ware_totals: Dictionary = diag.get("ware_totals", {}).get("by_type", {})
	var ware_parts: PackedStringArray = PackedStringArray()
	for ware_type in ["planks", "timber", "stone", "tools"]:
		if ware_totals.has(ware_type):
			ware_parts.append("%s=%d" % [ware_type, int(ware_totals[ware_type])])
	var sites: int = snapshot.get("construction_sites", []).size()
	var jobs: int = snapshot.get("ware_movement_markers", []).size()
	var ware_text: String = ", ".join(ware_parts) if not ware_parts.is_empty() else "(none yet)"
	return (
		"Summary — wares [%s] | forest D:%d | WOOD T:%d SAW T:%d P:%d | routes:%d | sites:%d jobs:%d"
		% [
			ware_text,
			int(fp.get("forest_density", -1)),
			int(fp.get("woodcutter_timber", 0)),
			int(fp.get("sawmill_timber", 0)),
			int(fp.get("sawmill_planks", 0)),
			int(fp.get("ware_routes", 0)),
			sites,
			jobs,
		]
	)


func _military_summary() -> String:
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
	var units: int = snapshot.get("units", []).size()
	var buildings: int = snapshot.get("buildings", []).size()
	var sites: int = snapshot.get("construction_sites", []).size()
	return "Summary — credits:%d power:%d/%d | units:%d buildings:%d sites:%d" % [
		credits, supply, demand, units, buildings, sites,
	]


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


func _track_attack_target(dto: Dictionary) -> void:
	last_attack_target = {}
	if state == null:
		return
	var cmd_type: String = str(dto.get("type", ""))
	if cmd_type == "AttackUnitCommand":
		var target_id: int = int(dto.get("payload", {}).get("target_id", 0))
		var target: Dictionary = MilitaryCombatSystem.target_info(state, target_id)
		if not target.is_empty():
			last_attack_target = {"q": int(target.get("q", 0)), "r": int(target.get("r", 0))}
	elif cmd_type == "AttackMoveCommand":
		last_attack_target = {
			"q": int(dto.get("payload", {}).get("q", -1)),
			"r": int(dto.get("payload", {}).get("r", -1)),
		}


func _track_placed_site(dto: Dictionary) -> void:
	if str(dto.get("type", "")) != "PlaceBuildingPlanCommand":
		return
	last_placed_site = {
		"q": int(dto.get("payload", {}).get("q", -1)),
		"r": int(dto.get("payload", {}).get("r", -1)),
		"building_type": str(dto.get("payload", {}).get("building_type", "")),
	}
