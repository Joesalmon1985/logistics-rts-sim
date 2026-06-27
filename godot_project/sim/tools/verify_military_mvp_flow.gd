extends SceneTree

## Step through menu and Skirmish MVP screens and verify each gate.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/military_mvp_review_util.gd")
const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")

var _menu_ctx: Dictionary = {}
var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _phase: String = "menu"
var _step: int = 0
var _failures: Array[String] = []
var _shots: Array = []
var _steps: Array = []


func _initialize() -> void:
	BootstrapScript.load_all()
	_menu_ctx = ViewerTestHarness.mount(root, "main_menu")
	Helper._force_layout(_menu_ctx, true)
	Helper.settle_frames(_menu_ctx)


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	match _phase:
		"menu":
			_verify_menu()
			_mount_game()
			_phase = "game"
			_wait = 10
		"game":
			_run_game_step()
			_wait = 8
		_:
			_finish()
			return true
	return false


func _verify_menu() -> void:
	var scene: Node = _menu_ctx.get("scene")
	var btn: Button = scene.get_node_or_null("%SkirmishMvpBtn") as Button if scene != null else null
	if btn == null:
		_failures.append("menu:missing_skirmish_button")
	var review: Dictionary = {"file": "military_mvp_00_main_menu", "visibility_gate_pass": btn != null}
	_record_shot(review)
	ViewerTestHarness.teardown(_menu_ctx)


func _mount_game() -> void:
	_ctx = ViewerTestHarness.mount(root, "military_mvp")
	_scene = _ctx.get("scene")
	if _scene != null and _scene.has_method("initialize_for_tests"):
		_scene.call("initialize_for_tests")
	Helper.settle_frames(_ctx)
	_steps = [
		{"file": "military_mvp_01_skirmish_start", "context": "overview", "action": "none"},
		{"file": "military_mvp_02_unit_selected", "context": "overview", "action": "select_unit"},
		{"file": "military_mvp_03_build_palette", "context": "build_mode", "action": "build_power"},
		{"file": "military_mvp_04_production_queue", "context": "overview", "action": "select_hub"},
	]


func _run_game_step() -> void:
	if _step >= _steps.size():
		_phase = "done"
		return
	var step: Dictionary = _steps[_step]
	match str(step.get("action", "")):
		"select_unit":
			var hex: Vector2i = get_controller().find_first_unit_hex("rifle_squad", "p1")
			if hex.x >= 0:
				Helper.simulate_map_click_at_hex(_scene, hex.x, hex.y)
		"build_power":
			if _scene.has_method("invoke_build_mode"):
				_scene.invoke_build_mode("power_generator")
		"select_hub":
			Helper.simulate_map_click_at_hex(_scene, Helper.HUB_Q, Helper.HUB_R)
	Helper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_game_context(_scene, str(step.get("context", "overview")))
	review["file"] = str(step.get("file", "shot"))
	review["screenshot_path"] = _save_png(str(step.get("file", "shot")))
	_record_shot(review)
	_step += 1


func get_controller() -> MilitaryMvpController:
	return _scene.controller if _scene != null and "controller" in _scene else null


func _record_shot(review: Dictionary) -> void:
	_shots.append(review)
	if not bool(review.get("visibility_gate_pass", true)):
		_failures.append("%s:%s" % [review.get("file", "?"), str(review.get("failures", []))])


func _save_png(base: String) -> String:
	var dir: String = ReviewUtil.ensure_output_dir()
	var path: String = dir.path_join("%s.png" % base)
	var img: Image = ViewerTestHarness.capture(_ctx)
	if img != null and not img.is_empty():
		img.save_png(path)
	return path


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	ReviewUtil.write_visual_review({"shots": _shots, "all_ok": _failures.is_empty()})
	if _failures.is_empty():
		print("VERIFY MILITARY MVP FLOW PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("VERIFY MILITARY MVP FLOW FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
