class_name HybridTutorialController
extends RefCounted

## Client-side hybrid tutorial — reads snapshot/UI only.

signal step_changed(step_index: int, step: Dictionary)
signal tutorial_completed()
signal tutorial_exited()

const STEPS_PATH := "res://client/data/hybrid_tutorial_steps.json"

var enabled: bool = false
var steps: Array = []
var step_index: int = 0
var exited: bool = false

var _initial_tick: int = 0
var _initial_road_count: int = 0
var _military_site_placed: bool = false


func load_steps() -> void:
	var file := FileAccess.open(STEPS_PATH, FileAccess.READ)
	if file == null:
		return
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	if typeof(parsed) == TYPE_DICTIONARY:
		steps = parsed.get("steps", [])


func start(snapshot: Dictionary, _map: MapViewport) -> void:
	load_steps()
	step_index = 0
	exited = false
	_initial_tick = int(snapshot.get("tick", 0))
	_initial_road_count = int(snapshot.get("roads", []).size())
	_military_site_placed = false
	step_changed.emit(step_index, current_step())


func current_step() -> Dictionary:
	if step_index < 0 or step_index >= steps.size():
		return {}
	return steps[step_index]


func advance_manual() -> void:
	if current_step().get("completion", "") == "manual_next":
		_advance()


func skip_or_exit() -> void:
	exited = true
	tutorial_exited.emit()


func update(snapshot: Dictionary, _map: MapViewport, selection: Dictionary, controller: HybridMvpController) -> void:
	if not enabled or exited or steps.is_empty():
		return
	var step: Dictionary = current_step()
	if step.is_empty():
		return
	var kind: String = str(step.get("completion", ""))
	var done: bool = false
	match kind:
		"manual_next":
			done = false
		"hq_selected":
			done = _is_hq_selection(selection)
		"road_placed":
			done = int(snapshot.get("roads", []).size()) > _initial_road_count
		"military_site_placed":
			done = _has_military_site(snapshot)
		"tick_advanced":
			done = int(snapshot.get("tick", 0)) > _initial_tick + 3
		"military_building_ready":
			done = _has_military_building(snapshot, str(step.get("building_type", "guard_post")))
		"unit_queued_or_ready":
			done = _has_unit(snapshot, "rifle_squad")
		"attack_posture_set":
			done = not snapshot.get("delegated_orders", []).is_empty()
		"objective_complete":
			done = bool(snapshot.get("objective", {}).get("complete", false))
	if done:
		_advance()


func _advance() -> void:
	step_index += 1
	if step_index >= steps.size():
		tutorial_completed.emit()
		return
	step_changed.emit(step_index, current_step())


static func _is_hq_selection(selection: Dictionary) -> bool:
	if str(selection.get("kind", "")) != "building":
		return false
	return str(selection.get("data", {}).get("building_type", "")) in ["headquarters", "command_hub"]


static func _has_military_site(snapshot: Dictionary) -> bool:
	for site in snapshot.get("construction_sites", []):
		var btype: String = str(site.get("building_type", ""))
		if HybridConfig.load_all().is_military_building(btype):
			return true
	return false


static func _has_military_building(snapshot: Dictionary, building_type: String) -> bool:
	for building in snapshot.get("buildings", []):
		if str(building.get("building_type", "")) == building_type:
			return true
	return false


static func _has_unit(snapshot: Dictionary, unit_type: String) -> bool:
	for unit in snapshot.get("units", []):
		if str(unit.get("unit_type", "")) == unit_type:
			return true
	for queue in snapshot.get("production_queues", []):
		for item in queue.get("items", []):
			if str(item.get("unit_type", "")) == unit_type:
				return true
	return false
