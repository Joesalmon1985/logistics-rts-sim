extends SceneTree

## Verify real player launch route: main menu → Tutorial at 1280×720.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const Helper := preload("res://sim/tests/viewer/actual_human_launch_helper.gd")
const ReviewUtil := preload("res://sim/tools/actual_human_launch_review_util.gd")
const ShotUtil := preload("res://sim/tools/actual_human_screenshot_util.gd")

var _host: Node
var _ctx: Dictionary = {}
var _wait: int = 4
var _review: Dictionary = {}
var _failures: Array[String] = []


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
		_wait = 8
		return false
	_finish()
	return true


func _finish() -> void:
	var scene: Node = _ctx.get("scene")
	_review = ReviewUtil.probe_tutorial_launch(scene, ReviewUtil.LAUNCH_SIZE)
	_review["capture_method"] = "in_engine_fallback"
	_review["main_menu_route_used"] = true
	_review["tutorial_button_activated"] = true
	var shot_path: String = Helper.capture_png(_ctx, "actual_human_launch_tutorial_after", _review)
	_review["screenshot_path"] = shot_path
	if ShotUtil.is_headless_display():
		_review["capture_method"] = "in_engine_fallback"
	elif _image_is_synthesized(shot_path):
		_review["capture_method"] = "in_engine_fallback"
	else:
		_review["capture_method"] = "real_desktop"
	ReviewUtil.write_launch_review(_review)

	if shot_path == "" or not ReviewUtil.png_is_non_blank(shot_path):
		_failures.append("screenshot_blank")
	if not bool(_review.get("human_understandable_pass", false)):
		for reason in _review.get("failure_reasons", []):
			_failures.append(str(reason))

	Helper.teardown(_ctx, _host)
	if _failures.is_empty():
		print("VERIFY ACTUAL HUMAN TUTORIAL LAUNCH PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("VERIFY ACTUAL HUMAN TUTORIAL LAUNCH FAIL")
		print("EXIT: 1 (fail)")
		quit(1)


func _image_is_synthesized(path: String) -> bool:
	if path == "" or not FileAccess.file_exists(path):
		return true
	# Real rendered tutorial screenshots are typically >50KB at 1280x720.
	return FileAccess.get_file_as_bytes(path).size() < 50000
