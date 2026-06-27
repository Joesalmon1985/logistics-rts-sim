extends SceneTree

## Capture Skirmish MVP screenshot set + visual review artefacts.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/military_mvp_review_util.gd")
const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _step: int = 0
var _shots: Array = []
var _all_ok: bool = true
var _steps: Array = [
	{"file": "military_mvp_01_skirmish_start", "action": "none"},
	{"file": "military_mvp_02_unit_selected", "action": "select_unit"},
	{"file": "military_mvp_03_build_palette", "action": "build_power"},
	{"file": "military_mvp_04_production_queue", "action": "select_hub"},
]


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "military_mvp")
	_scene = _ctx.get("scene")
	if _scene != null and _scene.has_method("initialize_for_tests"):
		_scene.call("initialize_for_tests")
	Helper.settle_frames(_ctx)


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	if _step >= _steps.size():
		_finish()
		return true
	_run_step()
	_wait = 8
	return false


func _run_step() -> void:
	var step: Dictionary = _steps[_step]
	match str(step.get("action", "")):
		"select_unit":
			if _scene != null and "controller" in _scene:
				var hex: Vector2i = _scene.controller.find_first_unit_hex("rifle_squad", "p1")
				if hex.x >= 0:
					Helper.simulate_map_click_at_hex(_scene, hex.x, hex.y)
		"build_power":
			if _scene.has_method("invoke_build_mode"):
				_scene.invoke_build_mode("power_generator")
		"select_hub":
			Helper.simulate_map_click_at_hex(_scene, Helper.HUB_Q, Helper.HUB_R)
	Helper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_scene(_scene)
	review["file"] = str(step.get("file", "shot"))
	review["screenshot_path"] = _save_shot(str(step.get("file", "shot")))
	if not bool(review.get("visibility_gate_pass", false)):
		_all_ok = false
	_shots.append(review)
	_step += 1


func _save_shot(base: String) -> String:
	var dir: String = ReviewUtil.ensure_output_dir()
	var path: String = dir.path_join("%s.png" % base)
	var img: Image = ViewerTestHarness.capture(_ctx)
	if img != null and not img.is_empty():
		img.save_png(path)
	return path


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	ReviewUtil.write_visual_review({"shots": _shots, "all_ok": _all_ok})
	if _all_ok:
		print("MILITARY MVP CAPTURE PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		print("MILITARY MVP CAPTURE FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
