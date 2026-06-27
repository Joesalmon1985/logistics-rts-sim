extends SceneTree

## Verify Skirmish MVP screen legibility gates.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/military_mvp_review_util.gd")
const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _phase: int = 0
var _failures: Array[String] = []


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
	match _phase:
		0:
			_check_overview()
			_phase = 1
			_wait = 6
		1:
			_check_hub_selected()
			_phase = 2
			_wait = 6
		2:
			_check_build_mode()
			_phase = 3
			_wait = 6
		3:
			_check_queue_mode()
			_finish()
			return true
	return false


func _check_overview() -> void:
	var review: Dictionary = ReviewUtil.probe_scene(_scene)
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("overview:%s" % str(review.get("failures", [])))


func _check_hub_selected() -> void:
	Helper.simulate_map_click_at_hex(_scene, Helper.HUB_Q, Helper.HUB_R)
	Helper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_game_context(_scene, "hub_selected")
	if not bool(review.get("plain_english_gate_pass", false)):
		_failures.append("hub_selected:%s" % str(review.get("failures", [])))


func _check_build_mode() -> void:
	if _scene.has_method("invoke_build_mode"):
		_scene.invoke_build_mode("power_generator")
	if _scene.has_method("_refresh_mode_ui"):
		_scene.call("_refresh_mode_ui")
	Helper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_game_context(_scene, "build_mode")
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("build_mode:%s" % str(review.get("failures", [])))


func _check_queue_mode() -> void:
	if _scene.has_method("invoke_cancel_modes"):
		_scene.invoke_cancel_modes()
	if _scene.has_method("invoke_queue_mode"):
		_scene.invoke_queue_mode("rifle_squad")
	Helper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_scene(_scene)
	var production: RichTextLabel = _scene.get_node_or_null("%ProductionQueuesPanel") as RichTextLabel
	if production == null:
		_failures.append("queue_mode:missing_production_panel")
	elif not bool(review.get("production_panel_found", false)):
		_failures.append("queue_mode:production_not_found")


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	if _failures.is_empty():
		print("VERIFY MILITARY MVP SCREEN PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("VERIFY MILITARY MVP SCREEN FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
