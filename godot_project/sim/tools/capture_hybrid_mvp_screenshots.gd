extends SceneTree

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/hybrid_mvp_review_util.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _step: int = 0
var _shots: Array = []
var _all_ok: bool = true
var _steps: Array = [
	{"file": "hybrid_mvp_00_start", "action": "none"},
	{"file": "hybrid_mvp_01_objective", "action": "none"},
	{"file": "hybrid_mvp_02_build_palette", "action": "build_mode"},
]


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
	if _step >= _steps.size():
		_finish()
		return true
	_run_step()
	_wait = 8
	return false


func _force_layout() -> void:
	var scene: Control = _scene as Control
	if scene != null:
		scene.size = ViewerTestHarness.SIZE
	for _i in range(ViewerTestHarness.DEFAULT_FRAMES):
		ViewerTestHarness.tick_frames(_ctx)


func _run_step() -> void:
	var step: Dictionary = _steps[_step]
	if str(step.get("action", "")) == "build_mode" and _scene.has_method("invoke_build_mode"):
		_scene.invoke_build_mode("guard_post")
	_force_layout()
	var review: Dictionary = ReviewUtil.probe_game(_scene)
	review["file"] = str(step.get("file", "shot"))
	var dir: String = ReviewUtil.ensure_output_dir()
	var path: String = dir.path_join("%s.png" % str(step.get("file", "shot")))
	var img: Image = ViewerTestHarness.capture(_ctx)
	if img != null and not img.is_empty():
		img.save_png(path)
	review["screenshot_path"] = path
	if not bool(review.get("visibility_gate_pass", false)):
		_all_ok = false
	_shots.append(review)
	_step += 1


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	if _all_ok:
		print("HYBRID MVP CAPTURE PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		print("HYBRID MVP CAPTURE FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
