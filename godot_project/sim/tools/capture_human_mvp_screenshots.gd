extends SceneTree

## Capture Human MVP screenshot sequence + visibility review artefacts.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/human_mvp_review_util.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")
const HQ_Q: int = 6
const HQ_R: int = 6

var _ctx: Dictionary = {}
var _menu_ctx: Dictionary = {}
var _scene: Node
var _map: MapViewport
var _controller: SettlerMvpController
var _wait_frames: int = 10
var _phase: String = "menu"
var _step_index: int = 0
var _shots: Array = []
var _all_ok: bool = true


func _initialize() -> void:
	BootstrapScript.load_all()
	_menu_ctx = ViewerTestHarness.mount(root, "main_menu")
	if not bool(_menu_ctx.get("ok", false)):
		print("HUMAN MVP CAPTURE FAIL: main menu mount")
		quit(1)
		return
	_wait_frames = 12


func _process(_delta: float) -> bool:
	if _wait_frames > 0:
		_wait_frames -= 1
		return false
	match _phase:
		"menu":
			_capture_menu()
			_mount_game(false)
			_phase = "game"
			_wait_frames = 12
		"game":
			_run_game_step()
			_wait_frames = 8
		_:
			_finish()
			return true
	return false


func _capture_menu() -> void:
	SettlerHelper._force_layout(_menu_ctx, true)
	SettlerHelper.settle_frames(_menu_ctx)
	var scene: Node = _menu_ctx.get("scene")
	var review: Dictionary = ReviewUtil.probe_main_menu(scene)
	review["file"] = "human_mvp_00_main_menu.png"
	review["visibility_gate_pass"] = bool(review.get("all_visible", false))
	_save_shot(scene, _menu_ctx, "human_mvp_00_main_menu", review, "main_menu")
	ViewerTestHarness.teardown(_menu_ctx)


func _mount_game(tutorial: bool) -> void:
	_ctx = ViewerTestHarness.mount(root, "settler_mvp")
	_scene = _ctx.get("scene")
	_map = _ctx.get("map")
	_controller = _ctx.get("controller")
	if _scene != null:
		if "tutorial_enabled" in _scene:
			_scene.set("tutorial_enabled", tutorial)
		if _scene.has_method("initialize_for_tests"):
			_scene.call("initialize_for_tests")
	SettlerHelper._force_layout(_ctx)
	SettlerHelper.settle_frames(_ctx)
	_steps = [
		{"file": "human_mvp_01_tutorial_start", "context": "tutorial_start", "tutorial": true, "action": "none"},
		{"file": "human_mvp_02_hq_selected_inspector", "context": "hq_inspector", "action": "select_hq"},
		{"file": "human_mvp_03_build_menu_open", "context": "build_menu", "action": "none"},
		{"file": "human_mvp_04_woodcutter_placement_preview", "context": "woodcutter_preview", "action": "woodcutter_hover"},
		{"file": "human_mvp_05_road_mode_active", "context": "road_mode", "action": "road_mode"},
		{"file": "human_mvp_06_building_or_site_selected", "context": "building_selected", "action": "select_hq"},
		{"file": "human_mvp_07_free_play_hud", "context": "free_play_hud", "action": "none"},
	]
	_step_index = 0


var _steps: Array = []


func _run_game_step() -> void:
	if _step_index >= _steps.size():
		_phase = "done"
		return
	var step: Dictionary = _steps[_step_index]
	if bool(step.get("tutorial", false)) and _scene != null:
		if _scene.has_method("enable_tutorial_for_tests"):
			_scene.call("enable_tutorial_for_tests")
		elif "tutorial_enabled" in _scene:
			_scene.set("tutorial_enabled", true)
	match str(step.get("action", "")):
		"select_hq":
			if _scene.has_method("simulate_map_click_at_hex"):
				_scene.simulate_map_click_at_hex(HQ_Q, HQ_R)
		"woodcutter_hover":
			if _scene.has_method("invoke_build_mode"):
				_scene.invoke_build_mode("woodcutter")
			if _map != null:
				_map.hover_q = HQ_Q + 1
				_map.hover_r = HQ_R
				_controller.handle_hex_hover(HQ_Q + 1, HQ_R)
		"road_mode":
			if _scene.has_method("invoke_road_mode"):
				_scene.invoke_road_mode()
	SettlerHelper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_game_scene(_scene, str(step.get("context", "free_play")))
	review["file"] = str(step.get("file", "shot"))
	_save_shot(_scene, _ctx, str(step.get("file", "shot")), review, str(step.get("context", "")))
	_step_index += 1


func _save_shot(scene: Node, ctx: Dictionary, file_id: String, review: Dictionary, _context: String) -> void:
	var dir: String = ReviewUtil.ensure_output_dir()
	var png_path: String = dir.path_join("%s.png" % file_id)
	var img: Image = ViewerTestHarness.capture(ctx)
	var saved: bool = img != null and not img.is_empty() and img.save_png(png_path) == OK
	review["screenshot_path"] = png_path
	review["screenshot_bytes"] = FileAccess.get_file_as_bytes(png_path).size() if saved else 0
	review["screenshot_non_blank"] = review["screenshot_bytes"] >= ReviewUtil.MIN_PNG_BYTES
	if not review.get("visibility_gate_pass", false):
		_all_ok = false
	elif not review.get("screenshot_non_blank", false):
		review["screenshot_headless_fallback"] = true
	_shots.append(review)


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	var summary: Dictionary = {"shots": _shots, "all_ok": _all_ok}
	ReviewUtil.write_visual_review(summary)
	if _all_ok:
		print("HUMAN MVP CAPTURE PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		print("HUMAN MVP CAPTURE FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
