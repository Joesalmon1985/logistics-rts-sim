extends TestCase

const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")
const ReviewUtil := preload("res://sim/tools/military_mvp_review_util.gd")


func get_name() -> String:
	return "test_military_mvp_screenshot_capture"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_military(host)
	var scene: Node = ctx.get("scene")
	var review: Dictionary = ReviewUtil.probe_scene(scene)
	assert_true(bool(review.get("scene_loaded", false)), "scene not loaded")
	assert_true(bool(review.get("map_viewport_found", false)), "map missing")
	assert_true(bool(review.get("build_palette_found", false)), "palette missing")
	var dir: String = ReviewUtil.ensure_output_dir()
	var png_path: String = dir.path_join("military_mvp_test_initial.png")
	var img: Image = ViewerTestHarness.capture(ctx)
	assert_true(img != null and not img.is_empty(), "empty capture")
	assert_true(img.save_png(png_path) == OK, "png save failed")
	ReviewUtil.write_review_files("military_mvp_test_initial", review, png_path)
	Helper.teardown(ctx, host)
