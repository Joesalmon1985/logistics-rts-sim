extends SceneTree

## Capture human_understandable screenshot set + visual review artefacts.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/human_understandable_review_util.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")
const HQ_Q: int = 6
const HQ_R: int = 6

var _menu_ctx: Dictionary = {}
var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _phase: String = "menu"
var _step: int = 0
var _shots: Array = []
var _all_ok: bool = true


func _initialize() -> void:
	BootstrapScript.load_all()
	_menu_ctx = ViewerTestHarness.mount(root, "main_menu")
	SettlerHelper._force_layout(_menu_ctx, true)
	SettlerHelper.settle_frames(_menu_ctx)


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	match _phase:
		"menu":
			_capture_menu()
			_mount_game()
			_phase = "game"
			_wait = 10
		"game":
			_run_step()
			_wait = 8
		_:
			_finish()
			return true
	return false


func _capture_menu() -> void:
	var scene: Node = _menu_ctx.get("scene")
	var review: Dictionary = ReviewUtil.probe_main_menu(scene)
	review["file"] = "human_understandable_00_main_menu"
	review["screenshot_path"] = _save_shot(_menu_ctx, "human_understandable_00_main_menu")
	_shots.append(review)
	if not review.get("visibility_gate_pass", false):
		_all_ok = false
	ViewerTestHarness.teardown(_menu_ctx)


func _mount_game() -> void:
	_ctx = ViewerTestHarness.mount(root, "settler_mvp")
	_scene = _ctx.get("scene")
	if _scene != null and _scene.has_method("initialize_for_tests"):
		_scene.call("initialize_for_tests")
	SettlerHelper._force_layout(_ctx)
	SettlerHelper.settle_frames(_ctx)
	_steps = [
		{"file": "human_understandable_01_tutorial_start", "context": "tutorial_start", "action": "tutorial"},
		{"file": "human_understandable_02_hq_selected_inspector", "context": "hq_selected", "action": "select_hq"},
		{"file": "human_understandable_03_build_palette_visible", "context": "tutorial_start", "action": "none"},
		{"file": "human_understandable_04_road_mode_instruction", "context": "road_mode", "action": "road_mode"},
		{"file": "human_understandable_05_woodcutter_build_mode", "context": "build_mode", "action": "woodcutter"},
		{"file": "human_understandable_06_construction_site_inspector", "context": "hq_selected", "action": "select_hq"},
		{"file": "human_understandable_07_carrier_with_ware", "context": "tutorial_start", "action": "none"},
		{"file": "human_understandable_08_watch_economy_running", "context": "tutorial_start", "action": "play"},
	]
	_step = 0


var _steps: Array = []


func _run_step() -> void:
	if _step >= _steps.size():
		_phase = "done"
		return
	var step: Dictionary = _steps[_step]
	match str(step.get("action", "")):
		"tutorial":
			if _scene.has_method("enable_tutorial_for_tests"):
				_scene.call("enable_tutorial_for_tests")
		"select_hq":
			if _scene.has_method("invoke_cancel_modes"):
				_scene.invoke_cancel_modes()
			SettlerHelper.simulate_map_click_at_hex(_scene, HQ_Q, HQ_R)
		"road_mode":
			if _scene.has_method("invoke_cancel_modes"):
				_scene.invoke_cancel_modes()
			if _scene.has_method("invoke_road_mode"):
				_scene.invoke_road_mode()
		"woodcutter":
			if _scene.has_method("invoke_cancel_modes"):
				_scene.invoke_cancel_modes()
			if _scene.has_method("invoke_build_mode"):
				_scene.invoke_build_mode("woodcutter")
		"play":
			SettlerHelper.invoke_play(_scene)
			for _i in range(5):
				if not _scene.controller.paused:
					_scene.controller.step_tick()
	SettlerHelper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_game_context(_scene, str(step.get("context", "tutorial_start")))
	review["file"] = str(step.get("file", "shot"))
	review["screenshot_path"] = _save_shot(_ctx, str(step.get("file", "shot")))
	if not review.get("visibility_gate_pass", false):
		_all_ok = false
	_shots.append(review)
	_step += 1


func _save_shot(ctx: Dictionary, base: String) -> String:
	var dir: String = ReviewUtil.ensure_output_dir()
	var path: String = dir.path_join("%s.png" % base)
	var img: Image = ViewerTestHarness.capture(ctx)
	if img != null and not img.is_empty():
		img.save_png(path)
	return path


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	ReviewUtil.write_visual_review({"shots": _shots, "all_ok": _all_ok})
	if _all_ok:
		print("HUMAN UNDERSTANDABLE CAPTURE PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		print("HUMAN UNDERSTANDABLE CAPTURE FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
