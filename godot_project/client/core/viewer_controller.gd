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
const STEP_UNTIL_EVENT_MAX: int = 50

var state: GameState
var runner: SimulationTickRunner
var submitter: CommandSubmitter
var actor_id: String = "p1"
var snapshot: Dictionary = {}
var command_history: Array = []
var last_attack_target: Dictionary = {}
var layer_visibility: Dictionary = {
	"terrain": true,
	"buildings": true,
	"units": true,
	"roads": true,
	"resources": true,
	"fog": true,
	"diagnostics": true,
}


func _init(p_runner: SimulationTickRunner = null, p_submitter: CommandSubmitter = null) -> void:
	runner = p_runner if p_runner != null else SimulationTickRunner.new()
	submitter = p_submitter if p_submitter != null else CommandSubmitter.new()


func load_demo_state(demo_state: GameState, p_actor_id: String = "p1") -> void:
	state = demo_state
	actor_id = p_actor_id
	command_history.clear()
	last_attack_target = {}
	refresh_snapshot()


func refresh_snapshot() -> Dictionary:
	if state == null:
		snapshot = {}
		return snapshot
	snapshot = SnapshotExporter.export_debug_snapshot(state, actor_id)
	snapshot_refreshed.emit(snapshot)
	return snapshot


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


func _economy_summary() -> String:
	var diag: Dictionary = snapshot.get("diagnostics", {})
	var ware_totals: Dictionary = diag.get("ware_totals", {}).get("by_type", {})
	var ware_parts: PackedStringArray = PackedStringArray()
	for ware_type in ["planks", "timber", "stone", "tools", "food"]:
		if ware_totals.has(ware_type):
			ware_parts.append("%s=%d" % [ware_type, int(ware_totals[ware_type])])
	var sites: int = snapshot.get("construction_sites", []).size()
	var carriers: int = snapshot.get("carrier_markers", []).size()
	var jobs: int = snapshot.get("ware_movement_markers", []).size() + snapshot.get("blocked_job_markers", []).size()
	var ware_text: String = ", ".join(ware_parts) if not ware_parts.is_empty() else "(see diagnostics)"
	return "Summary — wares [%s] | sites:%d carriers:%d jobs:%d" % [ware_text, sites, carriers, jobs]


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
	var layers: Dictionary = snapshot.get("display_layers", {})
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
