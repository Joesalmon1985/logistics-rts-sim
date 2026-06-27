class_name SettlerTutorialController
extends BaseTutorialController

## Client-side guided tutorial — reads snapshot/UI only; never mutates sim directly.

const STEPS_PATH := "res://client/data/settler_tutorial_steps.json"

var _initial_camera_pan: Vector2 = Vector2.ZERO
var _initial_camera_zoom: float = 1.0
var _camera_baseline_set: bool = false
var _initial_tick: int = 0
var _initial_road_count: int = 0
var _economy_watch_tick: int = -1
var _buildings_at_start: Dictionary = {}


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
	_initial_road_count = int(snapshot.get("roads", []).size())
	_buildings_at_start = _building_flags(snapshot)
	if map != null:
		_initial_camera_pan = map.pan_offset
		_initial_camera_zoom = map.zoom
		_camera_baseline_set = true
	step_changed.emit(step_index, current_step())


func update(snapshot: Dictionary, map: MapViewport, selection: Dictionary, controller: SettlerMvpController) -> void:
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
		"hq_selected":
			done = _is_hq_selection(selection)
		"tick_advanced":
			done = int(snapshot.get("tick", 0)) > _initial_tick
		"road_placed":
			done = int(snapshot.get("roads", []).size()) > _initial_road_count
		"woodcutter_placed":
			done = _has_building_or_site(snapshot, "woodcutter")
		"forester_placed":
			done = _has_building_or_site(snapshot, "forester")
		"sawmill_placed":
			done = _has_building_or_site(snapshot, "sawmill")
		"quarry_placed":
			done = _has_building_or_site(snapshot, "quarry")
		"economy_watched":
			if not controller.paused and int(snapshot.get("tick", 0)) >= _initial_tick + 5:
				done = true
		"objective_complete":
			done = bool(snapshot.get("objective", {}).get("complete", false))
	if done:
		_advance()


static func _is_hq_selection(selection: Dictionary) -> bool:
	if str(selection.get("kind", "")) != "building":
		return false
	return str(selection.get("data", {}).get("building_type", "")) in ["headquarters", "command_hub"]


static func _has_building_or_site(snapshot: Dictionary, btype: String) -> bool:
	for b in snapshot.get("buildings", []):
		if str(b.get("building_type", "")) == btype:
			return true
	for s in snapshot.get("construction_sites", []):
		if str(s.get("building_type", "")) == btype:
			return true
	return false


static func _building_flags(snapshot: Dictionary) -> Dictionary:
	var out: Dictionary = {}
	for key in ["woodcutter", "forester", "sawmill", "quarry"]:
		out[key] = _has_building_or_site(snapshot, key)
	return out
