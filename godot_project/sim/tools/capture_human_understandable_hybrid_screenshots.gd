extends SceneTree

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/human_understandable_hybrid_review_util.gd")
const Helper := preload("res://sim/tests/viewer/hybrid_mvp_test_helper.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _step: int = 0
var _all_ok: bool = true
var _steps: Array = [
	"human_understandable_hybrid_00_main_menu",
	"human_understandable_hybrid_01_tutorial_start",
	"human_understandable_hybrid_02_economy_building_selected",
	"human_understandable_hybrid_03_military_site_missing_materials",
	"human_understandable_hybrid_04_carrier_delivering_to_barracks",
	"human_understandable_hybrid_05_unit_queue",
	"human_understandable_hybrid_06_attack_posture",
	"human_understandable_hybrid_07_combat",
	"human_understandable_hybrid_08_victory",
]


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "hybrid_mvp")
	_scene = _ctx.get("scene")
	if _scene.has_method("initialize_for_tests"):
		_scene.call("initialize_for_tests")
	if _step == 1 and _scene.has_method("enable_tutorial_for_tests"):
		_scene.call("enable_tutorial_for_tests")
	_force_layout()


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	if _step >= _steps.size():
		_finish()
		return true
	if _step == 1 and _scene.has_method("enable_tutorial_for_tests"):
		_scene.call("enable_tutorial_for_tests")
	_save_shot(_steps[_step])
	_step += 1
	_wait = 6
	return false


func _force_layout() -> void:
	for _i in range(ViewerTestHarness.DEFAULT_FRAMES):
		ViewerTestHarness.tick_frames(_ctx)


func _save_shot(name: String) -> void:
	_force_layout()
	var dir: String = ReviewUtil.ensure_output_dir()
	var path: String = dir.path_join("%s.png" % name)
	var img: Image = ViewerTestHarness.capture(_ctx)
	if img == null or img.is_empty():
		_all_ok = false
		return
	img.save_png(path)


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	if _all_ok:
		print("HUMAN UNDERSTANDABLE HYBRID CAPTURE PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		print("HUMAN UNDERSTANDABLE HYBRID CAPTURE FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
