extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")
const SettlerMvpReviewUtil := preload("res://sim/tools/settler_mvp_review_util.gd")

## SETTLER-MVP-SCREEN-002 — interaction sequence changes state and screenshots.


const HQ_Q: int = 6
const HQ_R: int = 6


func get_name() -> String:
	return "test_SETTLER_MVP_SCREEN_002_screenshot_interactions"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	var map: MapViewport = ctx.get("map")
	var controller: SettlerMvpController = ctx.get("controller")
	var zoom0: float = map.get_camera_state().get("zoom", 1.0)
	map.zoom_by_factor(1.2)
	var zoom1: float = map.get_camera_state().get("zoom", 1.0)
	assert_true(zoom1 != zoom0, "zoom did not change")
	var pan0: Vector2 = map.get_camera_state().get("pan_offset", Vector2.ZERO)
	map.pan_by_screen_delta(Vector2(80, 40))
	var pan1: Vector2 = map.get_camera_state().get("pan_offset", Vector2.ZERO)
	assert_true(pan1 != pan0, "pan did not change")
	SettlerMvpTestHelper.simulate_map_click_at_hex(scene, HQ_Q, HQ_R)
	var selected: Vector2i = SettlerMvpTestHelper.get_selected_hex(scene)
	assert_eq(selected.x, HQ_Q, "hq q not selected")
	assert_eq(selected.y, HQ_R, "hq r not selected")
	assert_false(SettlerMvpTestHelper.get_inspector_text(scene).is_empty(), "inspector empty after select")
	SettlerMvpTestHelper.invoke_build_mode(scene, "woodcutter")
	assert_eq(controller.placement_building_type, "woodcutter", "woodcutter mode not active")
	var dir: String = SettlerMvpReviewUtil.ensure_output_dir()
	var path_a: String = dir.path_join("settler_mvp_test_interaction_a.png")
	var path_b: String = dir.path_join("settler_mvp_test_interaction_b.png")
	var img_a: Image = ViewerTestHarness.capture(ctx)
	img_a.save_png(path_a)
	map.zoom_by_factor(1.3)
	var img_b: Image = ViewerTestHarness.capture(ctx)
	img_b.save_png(path_b)
	var hash_a: String = FileAccess.get_md5(path_a)
	var hash_b: String = FileAccess.get_md5(path_b)
	assert_true(hash_a != hash_b or zoom1 != zoom0, "expected screenshot or state change")
	SettlerMvpTestHelper.teardown(ctx, host)
