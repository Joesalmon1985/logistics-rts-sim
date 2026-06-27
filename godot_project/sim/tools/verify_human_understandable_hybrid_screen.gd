extends SceneTree

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/human_understandable_hybrid_review_util.gd")
const Helper := preload("res://sim/tests/viewer/hybrid_mvp_test_helper.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _phase: int = 0
var _failures: Array[String] = []


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "hybrid_mvp")
	_scene = _ctx.get("scene")
	if _scene.has_method("initialize_for_tests"):
		_scene.call("initialize_for_tests")
	Helper.settle_frames(_ctx)


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	match _phase:
		0:
			if _scene.has_method("enable_tutorial_for_tests"):
				_scene.call("enable_tutorial_for_tests")
			_record("tutorial_start")
			_phase = 1
			_wait = 6
		1:
			Helper.simulate_map_click_at_hq(_scene)
			_record("hq_selected")
			_phase = 2
			_wait = 6
		2:
			if _scene.has_method("invoke_build_mode"):
				_scene.invoke_build_mode("guard_post")
			_record("military_site")
			_finish()
			return true
	return false


func _record(context: String) -> void:
	Helper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_game_context(_scene, context)
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("%s:%s" % [context, str(review.get("failures", []))])


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	if _failures.is_empty():
		print("VERIFY HUMAN UNDERSTANDABLE HYBRID SCREEN PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("VERIFY HUMAN UNDERSTANDABLE HYBRID SCREEN FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
