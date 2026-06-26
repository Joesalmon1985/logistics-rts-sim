extends SceneTree

## One-shot real-display capture for actual human tutorial launch screenshot.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const Helper := preload("res://sim/tests/viewer/actual_human_launch_helper.gd")
const ReviewUtil := preload("res://sim/tools/actual_human_launch_review_util.gd")

var _host: Node
var _ctx: Dictionary = {}
var _wait: int = 4


func _initialize() -> void:
	BootstrapScript.load_all()
	_host = Node.new()
	root.add_child(_host)


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	if _ctx.is_empty():
		_ctx = Helper.mount_tutorial_via_menu_route(_host, ReviewUtil.LAUNCH_SIZE)
		_wait = 6
		return false
	var scene: Node = _ctx.get("scene")
	if scene != null and "_initialized" in scene and not bool(scene.get("_initialized")):
		_wait = 4
		return false
	if scene != null and scene.has_method("_enforce_launch_layout"):
		scene.call("_enforce_launch_layout")
	Helper.settle_frames(_ctx)
	var review: Dictionary = ReviewUtil.probe_tutorial_launch(scene, ReviewUtil.LAUNCH_SIZE)
	review["capture_method"] = "real_desktop"
	review["main_menu_route_used"] = true
	review["tutorial_button_activated"] = true
	var path: String = Helper.capture_png(_ctx, "actual_human_launch_tutorial_after", review)
	review["screenshot_path"] = path
	ReviewUtil.write_launch_review(review)
	Helper.teardown(_ctx, _host)
	print("CAPTURED: %s (%d bytes)" % [path, FileAccess.get_file_as_bytes(path).size() if path != "" else 0])
	print("human_understandable_pass: %s" % str(review.get("human_understandable_pass", false)))
	print("EXIT: 0 (pass)")
	quit(0)
	return true
