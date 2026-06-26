extends TestCase

const HumanMvpReviewUtil := preload("res://sim/tools/human_mvp_review_util.gd")
const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_mvp_screenshot_capture"


func _run_body() -> void:
	var menu_host := Node.new()
	var menu_ctx: Dictionary = ViewerTestHarness.mount(menu_host, "main_menu")
	assert_true(bool(menu_ctx.get("ok", false)), "menu mount")
	SettlerMvpTestHelper._force_layout(menu_ctx, true)
	SettlerMvpTestHelper.settle_frames(menu_ctx)
	var menu_review: Dictionary = HumanMvpReviewUtil.probe_main_menu(menu_ctx.get("scene"))
	assert_true(bool(menu_review.get("all_visible", false)), "menu buttons visible: %s" % str(menu_review.get("off_screen", [])))
	SettlerMvpTestHelper.teardown(menu_ctx, menu_host)
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	SettlerMvpTestHelper.scene_call(scene, "invoke_build_mode", ["woodcutter"])
	SettlerMvpTestHelper.settle_frames(ctx)
	var review: Dictionary = HumanMvpReviewUtil.probe_game_scene(scene, "woodcutter_preview")
	assert_true(bool(review.get("visibility_gate_pass", false)), "visibility gate: %s" % str(review.get("off_screen_panels", [])))
	var dir: String = HumanMvpReviewUtil.ensure_output_dir()
	var png: String = dir.path_join("human_mvp_test_capture.png")
	var img: Image = ViewerTestHarness.capture(ctx)
	assert_true(img != null and not img.is_empty(), "empty capture image")
	assert_true(img.get_width() > 0 and img.get_height() > 0, "capture image invalid")
	assert_true(img.save_png(png) == OK, "png save")
	HumanMvpReviewUtil.write_visual_review({"shots": [review]})
	SettlerMvpTestHelper.teardown(ctx, host)
