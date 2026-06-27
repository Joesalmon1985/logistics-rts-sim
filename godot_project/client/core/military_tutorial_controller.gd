class_name MilitaryTutorialController
extends BaseTutorialController

## Client-side guided military tutorial — reads snapshot/UI only.

const STEPS_PATH := "res://client/data/military_tutorial_steps.json"

var _initial_camera_pan: Vector2 = Vector2.ZERO
var _initial_camera_zoom: float = 1.0
var _camera_baseline_set: bool = false
var _initial_tick: int = 0
var _initial_queue_count: int = 0
var _unit_moved: bool = false
var _attack_issued: bool = false
var _last_unit_q: int = -1
var _last_unit_r: int = -1


func load_steps() -> void:
	var file := FileAccess.open(STEPS_PATH, FileAccess.READ)
	if file == null:
		return
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	if typeof(parsed) == TYPE_DICTIONARY:
		steps = parsed.get("steps", [])


func start(snapshot: Dictionary, map: MapViewport) -> void:
	load_steps()
	step_index = 0
	exited = false
	_initial_tick = int(snapshot.get("tick", 0))
	_initial_queue_count = _queue_item_count(snapshot)
	_unit_moved = false
	_attack_issued = false
	_last_unit_q = -1
	_last_unit_r = -1
	if map != null:
		_initial_camera_pan = map.pan_offset
		_initial_camera_zoom = map.zoom
		_camera_baseline_set = true
	step_changed.emit(step_index, current_step())


func update(snapshot: Dictionary, map: MapViewport, selection: Dictionary, controller: MilitaryMvpController) -> void:
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
		"camera_moved":
			if map != null and _camera_baseline_set:
				done = map.pan_offset.distance_to(_initial_camera_pan) > 20.0 or absf(map.zoom - _initial_camera_zoom) > 0.05
		"hub_selected":
			done = _is_hub_selection(selection)
		"power_generator_placed":
			done = _has_building_or_site(snapshot, "power_generator")
		"barracks_placed":
			done = _has_building_or_site(snapshot, "barracks")
		"rifle_squad_queued":
			done = _queue_item_count(snapshot) > _initial_queue_count
		"rifle_squad_selected":
			done = str(selection.get("kind", "")) == "unit" and str(selection.get("data", {}).get("unit_type", "")) == "rifle_squad"
		"unit_moved":
			done = _unit_moved
		"attack_issued":
			done = _attack_issued
		"victory":
			done = controller.is_victory()
	if done:
		_advance()


func note_unit_move(from_q: int, from_r: int, to_q: int, to_r: int) -> void:
	if from_q != to_q or from_r != to_r:
		_unit_moved = true


func note_attack_issued() -> void:
	_attack_issued = true


static func _is_hub_selection(selection: Dictionary) -> bool:
	if str(selection.get("kind", "")) != "building":
		return false
	return str(selection.get("data", {}).get("building_type", "")) == "command_hub"


static func _has_building_or_site(snapshot: Dictionary, btype: String) -> bool:
	for b in snapshot.get("buildings", []):
		if str(b.get("building_type", "")) == btype:
			return true
	for s in snapshot.get("construction_sites", []):
		if str(s.get("building_type", "")) == btype:
			return true
	return false


static func _queue_item_count(snapshot: Dictionary) -> int:
	var count: int = 0
	for queue in snapshot.get("production_queues", []):
		if typeof(queue) != TYPE_DICTIONARY:
			continue
		count += queue.get("items", []).size()
	return count
