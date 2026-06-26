extends SceneTree

## Capture legibility baseline before tutorial legibility fixes.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/human_understandable_review_util.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

var _ctx: Dictionary = {}
var _wait: int = 10


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "settler_mvp")
	var scene: Node = _ctx.get("scene")
	if scene != null:
		if scene.has_method("enable_tutorial_for_tests"):
			scene.call("enable_tutorial_for_tests")
		elif "tutorial_enabled" in scene:
			scene.set("tutorial_enabled", true)
		if scene.has_method("initialize_for_tests"):
			scene.call("initialize_for_tests")
	SettlerHelper._force_layout(_ctx)
	SettlerHelper.settle_frames(_ctx)


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	var scene: Node = _ctx.get("scene")
	var review: Dictionary = ReviewUtil.probe_game_context(scene, "tutorial_start")
	review["file"] = "legibility_before_tutorial_start.png"
	review["screenshot_path"] = _save_png(scene, "legibility_before_tutorial_start")
	SettlerMvpReviewUtil.write_review_files("legibility_before", review, review["screenshot_path"])
	ViewerTestHarness.teardown(_ctx)
	print("LEGIBILITY BEFORE CAPTURE DONE (informational)")
	print("EXIT: 0 (pass)")
	quit(0)
	return true


func _save_png(scene: Node, base: String) -> String:
	var dir: String = ReviewUtil.ensure_output_dir()
	var path: String = dir.path_join("%s.png" % base)
	var img: Image = ViewerTestHarness.capture(_ctx)
	if img != null and not img.is_empty():
		img.save_png(path)
	return path
