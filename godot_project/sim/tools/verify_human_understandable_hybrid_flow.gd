extends SceneTree

## Flow verification for hybrid tutorial legibility (MRP C4).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/human_understandable_hybrid_review_util.gd")
const Helper := preload("res://sim/tests/viewer/hybrid_mvp_test_helper.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _failures: Array[String] = []
var _finished: bool = false


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "hybrid_mvp")
	_scene = _ctx.get("scene")
	if _scene.has_method("initialize_for_tests"):
		_scene.call("initialize_for_tests")
	if _scene.has_method("enable_tutorial_for_tests"):
		_scene.call("enable_tutorial_for_tests")
	Helper.settle_frames(_ctx)


func _process(_delta: float) -> bool:
	if _finished:
		return true
	_finished = true
	_run_checks()
	_finish()
	return true


func _run_checks() -> void:
	if not _scene.has_method("get_tutorial_title_text"):
		_failures.append("missing_tutorial_api")
	elif _scene.get_tutorial_title_text().length() < 5:
		_failures.append("tutorial_title_missing")
	if _scene.has_method("invoke_attack_mode"):
		_scene.invoke_attack_mode()
	Helper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_game_context(_scene, "tutorial_start")
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("flow:%s" % str(review.get("failures", [])))


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	if _failures.is_empty():
		print("VERIFY HUMAN UNDERSTANDABLE HYBRID FLOW PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("VERIFY HUMAN UNDERSTANDABLE HYBRID FLOW FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
