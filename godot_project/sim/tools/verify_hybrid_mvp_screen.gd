extends SceneTree

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/hybrid_mvp_review_util.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _phase: int = 0
var _failures: Array[String] = []


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "hybrid_mvp")
	_scene = _ctx.get("scene")
	if _scene != null and _scene.has_method("initialize_for_tests"):
		_scene.call("initialize_for_tests")
	_force_layout()


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	match _phase:
		0:
			_check_start()
			_phase = 1
			_wait = 6
		1:
			_check_build_palette()
			_phase = 2
			_wait = 6
		2:
			_check_inspector()
			_finish()
			return true
	return false


func _force_layout() -> void:
	var scene: Control = _scene as Control
	if scene != null:
		scene.size = ViewerTestHarness.SIZE
	for _i in range(ViewerTestHarness.DEFAULT_FRAMES):
		ViewerTestHarness.tick_frames(_ctx)


func _check_start() -> void:
	var review: Dictionary = ReviewUtil.probe_game(_scene)
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("start:%s" % str(review.get("failures", [])))


func _check_build_palette() -> void:
	var palette: Control = _scene.get_node_or_null("%BuildPalette") as Control
	if palette == null or not palette.visible:
		_failures.append("build_palette:not_visible")


func _check_inspector() -> void:
	if _scene.has_method("invoke_build_mode"):
		_scene.invoke_build_mode("guard_post")
	_force_layout()
	var review: Dictionary = ReviewUtil.probe_game(_scene)
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("build_mode:%s" % str(review.get("failures", [])))


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	if _failures.is_empty():
		print("VERIFY HYBRID MVP SCREEN PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("VERIFY HYBRID MVP SCREEN FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
