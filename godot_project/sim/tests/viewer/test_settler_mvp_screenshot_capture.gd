extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")
const SettlerMvpReviewUtil := preload("res://sim/tools/settler_mvp_review_util.gd")

## SETTLER-MVP-SCREEN-001 — initial screenshot capture smoke.


func get_name() -> String:
	return "test_SETTLER_MVP_SCREEN_001_screenshot_capture"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	var review: Dictionary = SettlerMvpReviewUtil.probe_scene(scene)
	assert_true(bool(review.get("scene_loaded", false)), "scene not loaded")
	assert_true(bool(review.get("map_viewport_found", false)), "map missing")
	assert_true(bool(review.get("hud_found", false)), "hud missing")
	assert_true(bool(review.get("objective_panel_found", false)), "objective missing")
	assert_true(bool(review.get("help_text_found", false)), "help missing")
	var dir: String = SettlerMvpReviewUtil.ensure_output_dir()
	var png_path: String = dir.path_join("settler_mvp_test_initial.png")
	var img: Image = ViewerTestHarness.capture(ctx)
	assert_true(img != null and not img.is_empty(), "empty capture image")
	assert_true(img.get_width() > 0 and img.get_height() > 0, "capture image invalid")
	assert_true(img.save_png(png_path) == OK, "png save failed")
	assert_true(SettlerMvpReviewUtil.write_review_files("settler_mvp_test_initial", review, png_path), "review write failed")
	SettlerMvpTestHelper.teardown(ctx, host)
