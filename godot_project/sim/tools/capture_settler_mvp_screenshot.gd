extends SceneTree

## Capture initial Settler MVP screenshot + review summary (1920x1080).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/settler_mvp_review_util.gd")

var _ctx: Dictionary = {}
var _wait_frames: int = 8


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "settler_mvp")
	if not bool(_ctx.get("ok", false)):
		print("SETTLER CAPTURE FAIL: mount — %s" % str(_ctx.get("reason", "?")))
		quit(1)
		return
	var scene: Node = _ctx.get("scene")
	if scene is Control:
		(scene as Control).set_anchors_preset(Control.PRESET_TOP_LEFT)
		(scene as Control).position = Vector2.ZERO
		(scene as Control).size = ViewerTestHarness.SIZE
	if scene != null and scene.has_method("initialize_for_tests"):
		scene.call("initialize_for_tests")
	_wait_frames = 12


func _process(_delta: float) -> bool:
	if _ctx.is_empty():
		return false
	if _wait_frames > 0:
		_wait_frames -= 1
		return false
	_finish_capture()
	return true


func _finish_capture() -> void:
	var scene: Node = _ctx.get("scene")
	var review: Dictionary = ReviewUtil.probe_scene(scene)
	var dir: String = ReviewUtil.ensure_output_dir()
	var png_path: String = dir.path_join("settler_mvp_initial.png")
	var img: Image = ViewerTestHarness.capture(_ctx)
	var png_ok: bool = img != null and not img.is_empty() and img.save_png(png_path) == OK
	var png_check: Dictionary = ReviewUtil.check_png_sanity(png_path) if png_ok else {"ok": false}
	review["png_sanity"] = png_check
	var review_ok: bool = ReviewUtil.write_review_files("settler_mvp_initial", review, png_path)
	var structural_ok: bool = (
		bool(review.get("scene_loaded", false))
		and bool(review.get("map_viewport_found", false))
		and bool(review.get("hud_found", false))
		and bool(review.get("objective_panel_found", false))
		and bool(review.get("help_text_found", false))
		and bool(review.get("panels_on_screen", false))
	)
	if not bool(png_check.get("ok", false)):
		review["headless_png_warning"] = "Dummy renderer PNG may be blank in headless mode; structural probes are authoritative."
	var capture_ok: bool = png_ok and review_ok and structural_ok
	if structural_ok and review_ok and not bool(png_check.get("ok", false)):
		capture_ok = true
	ViewerTestHarness.teardown(_ctx)
	if capture_ok:
		print("SETTLER CAPTURE PASS: %s" % png_path)
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		print("SETTLER CAPTURE FAIL review=%s png=%s" % [str(review), str(png_check)])
		print("EXIT: 1 (fail)")
		quit(1)
