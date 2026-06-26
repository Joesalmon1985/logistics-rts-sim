extends SceneTree

## Capture Settler MVP interaction screenshot sequence + per-step review JSON.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/settler_mvp_review_util.gd")
const HQ_Q: int = 6
const HQ_R: int = 6

var _ctx: Dictionary = {}
var _scene: Node
var _map: MapViewport
var _controller: SettlerMvpController
var _wait_frames: int = 8
var _step_index: int = 0
var _steps: Array = []
var _prev_state: Dictionary = {}
var _prev_hash: String = ""
var _all_ok: bool = true


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "settler_mvp")
	if not bool(_ctx.get("ok", false)):
		print("SETTLER INTERACTION CAPTURE FAIL: mount")
		quit(1)
		return
	_scene = _ctx.get("scene")
	if _scene is Control:
		(_scene as Control).set_anchors_preset(Control.PRESET_TOP_LEFT)
		(_scene as Control).position = Vector2.ZERO
		(_scene as Control).size = ViewerTestHarness.SIZE
	if _scene != null and _scene.has_method("initialize_for_tests"):
		_scene.call("initialize_for_tests")
	_wait_frames = 12
	_map = _ctx.get("map")
	_controller = _ctx.get("controller")
	_steps = [
		{"name": "initial", "action": "none"},
		{"name": "after_zoom", "action": "zoom"},
		{"name": "after_pan", "action": "pan"},
		{"name": "after_select_hq", "action": "select_hq"},
		{"name": "build_mode", "action": "build_woodcutter"},
	]


func _process(_delta: float) -> bool:
	if _wait_frames > 0:
		_wait_frames -= 1
		return false
	if _step_index >= _steps.size():
		_finish()
		return true
	_run_step()
	_wait_frames = 4
	return false


func _run_step() -> void:
	var step: Dictionary = _steps[_step_index]
	match str(step.get("action", "")):
		"zoom":
			_map.zoom_by_factor(1.2)
		"pan":
			_map.pan_by_screen_delta(Vector2(80, 40))
		"select_hq":
			if _scene.has_method("simulate_map_click_at_hex"):
				_scene.simulate_map_click_at_hex(HQ_Q, HQ_R)
		"build_woodcutter":
			if _scene.has_method("invoke_build_mode"):
				_scene.invoke_build_mode("woodcutter")
	var state: Dictionary = _collect_state(step)
	var dir: String = ReviewUtil.ensure_output_dir()
	var file_id: String = "%02d_%s" % [_step_index, str(step.get("name", "step"))]
	var png_path: String = dir.path_join("settler_mvp_%s.png" % file_id)
	var img: Image = ViewerTestHarness.capture(_ctx)
	var saved: bool = img != null and not img.is_empty() and img.save_png(png_path) == OK
	var file_hash: String = FileAccess.get_md5(png_path) if saved else ""
	state["screenshot_path"] = png_path
	state["changed_from_previous"] = _step_index == 0 or state != _prev_state or file_hash != _prev_hash
	state["screenshot_hash"] = file_hash
	var json_path: String = dir.path_join("settler_mvp_%s_review.json" % file_id)
	SimPaths.write_text_file(json_path, JSON.stringify(state, "\t"))
	if not saved or (_step_index > 0 and not bool(state.get("changed_from_previous", false)) and str(step.get("action", "")) != "none"):
		_all_ok = false
	_prev_state = state.duplicate(true)
	_prev_hash = file_hash
	_step_index += 1


func _collect_state(step: Dictionary) -> Dictionary:
	var inspector: String = ""
	var status: String = ""
	if _scene.has_method("get_inspector_text"):
		inspector = _scene.get_inspector_text()
	if _scene.has_method("get_status_text"):
		status = _scene.get_status_text()
	var selected := Vector2i(_map.selected_q, _map.selected_r)
	var cam: Dictionary = _map.get_camera_state()
	return {
		"step_name": step.get("name", ""),
		"input_action_attempted": step.get("action", ""),
		"selected_hex": {"q": selected.x, "r": selected.y},
		"active_mode": _controller.placement_building_type,
		"road_mode": _controller.road_mode_active,
		"camera_zoom_or_scale": cam.get("zoom", 1.0),
		"camera_offset_or_position": cam.get("pan_offset", Vector2.ZERO),
		"inspector_text_summary": inspector.substr(0, 120),
		"objective_text_summary": _controller.build_objective_text().substr(0, 120),
		"message_log_summary": status,
	}


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	if _all_ok:
		print("SETTLER INTERACTION CAPTURE PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		print("SETTLER INTERACTION CAPTURE FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
