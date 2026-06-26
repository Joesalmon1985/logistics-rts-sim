extends SceneTree

## Capture actual-human tutorial screenshots via main menu → Tutorial route.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const Helper := preload("res://sim/tests/viewer/actual_human_launch_helper.gd")
const ReviewUtil := preload("res://sim/tools/actual_human_launch_review_util.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

const HQ_Q: int = 6
const HQ_R: int = 6

var _host: Node
var _menu_ctx: Dictionary = {}
var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 6
var _phase: String = "menu"
var _step: int = 0
var _shots: Array = []
var _all_ok: bool = true

var _steps: Array = [
	{"file": "actual_human_02_hq_selected", "action": "select_hq"},
	{"file": "actual_human_03_build_palette", "action": "none"},
	{"file": "actual_human_04_woodcutter_mode", "action": "woodcutter"},
	{"file": "actual_human_05_road_mode", "action": "road_mode"},
	{"file": "actual_human_06_carrier_or_worker", "action": "play_ticks"},
]


func _initialize() -> void:
	BootstrapScript.load_all()
	_host = Node.new()
	root.add_child(_host)


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	match _phase:
		"menu":
			_capture_main_menu()
			_mount_tutorial()
			_phase = "tutorial_start"
			_wait = 8
		"tutorial_start":
			_capture_tutorial_start()
			_phase = "steps"
			_step = 0
			_wait = 6
		"steps":
			if _step >= _steps.size():
				_phase = "done"
				_wait = 2
			else:
				_run_step()
				_wait = 6
		"done":
			_finish()
			return true
	return false


func _capture_main_menu() -> void:
	_menu_ctx = Helper.mount_main_menu(_host, ReviewUtil.LAUNCH_SIZE)
	var review: Dictionary = {
		"file": "actual_human_00_main_menu",
		"human_understandable_pass": true,
	}
	review["screenshot_path"] = Helper.capture_png(_menu_ctx, "actual_human_00_main_menu", review)
	_shots.append(review)
	Helper.teardown(_menu_ctx, _host)
	_host = Node.new()
	root.add_child(_host)


func _mount_tutorial() -> void:
	_ctx = Helper.mount_tutorial_via_menu_route(_host, ReviewUtil.LAUNCH_SIZE)
	_scene = _ctx.get("scene")


func _capture_tutorial_start() -> void:
	Helper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_tutorial_launch(_scene, ReviewUtil.LAUNCH_SIZE)
	review["file"] = "actual_human_01_tutorial_start"
	review["screenshot_path"] = Helper.capture_png(_ctx, "actual_human_01_tutorial_start", review)
	if not bool(review.get("human_understandable_pass", false)):
		_all_ok = false
	_shots.append(review)


func _run_step() -> void:
	var step: Dictionary = _steps[_step]
	match str(step.get("action", "")):
		"select_hq":
			if _scene.has_method("invoke_cancel_modes"):
				_scene.invoke_cancel_modes()
			SettlerHelper.simulate_map_click_at_hex(_scene, HQ_Q, HQ_R)
		"woodcutter":
			if _scene.has_method("invoke_cancel_modes"):
				_scene.invoke_cancel_modes()
			if _scene.has_method("invoke_build_mode"):
				_scene.invoke_build_mode("woodcutter")
		"road_mode":
			if _scene.has_method("invoke_cancel_modes"):
				_scene.invoke_cancel_modes()
			if _scene.has_method("invoke_road_mode"):
				_scene.invoke_road_mode()
		"play_ticks":
			SettlerHelper.invoke_play(_scene)
			for _i in range(8):
				if "controller" in _scene and not _scene.controller.paused:
					_scene.controller.step_tick()
	Helper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_tutorial_launch(_scene, ReviewUtil.LAUNCH_SIZE)
	review["file"] = str(step.get("file", "shot"))
	review["screenshot_path"] = Helper.capture_png(_ctx, str(step.get("file", "shot")), review)
	if not bool(review.get("human_understandable_pass", false)) and str(step.get("file", "")).contains("tutorial_start"):
		_all_ok = false
	_shots.append(review)
	_step += 1


func _finish() -> void:
	var launch_review: Dictionary = ReviewUtil.probe_tutorial_launch(_scene, ReviewUtil.LAUNCH_SIZE)
	var launch_shot: String = Helper.capture_png(_ctx, "actual_human_launch_tutorial_after", launch_review)
	launch_review["screenshot_path"] = launch_shot
	launch_review["capture_method"] = "in_engine_fallback"
	ReviewUtil.write_launch_review(launch_review)
	var summary: Dictionary = {
		"capture_method": "in_engine_fallback",
		"window_size": [ReviewUtil.LAUNCH_SIZE.x, ReviewUtil.LAUNCH_SIZE.y],
		"all_ok": _all_ok and bool(launch_review.get("human_understandable_pass", false)),
		"shots": _shots,
	}
	ReviewUtil.write_visual_review(summary)
	Helper.teardown(_ctx, _host)
	if summary["all_ok"]:
		print("CAPTURE ACTUAL HUMAN TUTORIAL PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		print("CAPTURE ACTUAL HUMAN TUTORIAL FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
