class_name ViewerController
extends RefCounted

signal snapshot_refreshed(snapshot: Dictionary)
signal command_result(result: Dictionary, dto: Dictionary)

var state: GameState
var runner: SimulationTickRunner
var submitter: CommandSubmitter
var actor_id: String = "p1"
var snapshot: Dictionary = {}
var command_history: Array = []
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


func set_layer_visible(layer_name: String, visible: bool) -> void:
	layer_visibility[layer_name] = visible


func get_selection_at(q: int, r: int) -> Dictionary:
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
