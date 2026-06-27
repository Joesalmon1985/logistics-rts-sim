extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")
const ReviewUtil := preload("res://sim/tools/hybrid_3d_spectator_review_util.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_screenshot_capture"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount(host)
	var scene: Node = ctx.get("scene")
	scene.call("advance_for_tests", 5)
	Helper.settle_frames(ctx, 8)
	var controller: Hybrid3dSpectatorController = scene.controller
	assert_false(controller.snapshot.is_empty(), "snapshot empty")
	assert_true("Bot A" in controller.build_bot_summary("p1"), controller.build_bot_summary("p1"))
	assert_true("Bot B" in controller.build_bot_summary("p2"), controller.build_bot_summary("p2"))
	var counts: Dictionary = scene.call("get_world_entity_count")
	var snap_terrain: int = Hybrid3dWorldRenderer._terrain_tiles(controller.snapshot).size()
	assert_true(int(counts.get("terrain", 0)) > 0 or snap_terrain > 0, str(counts))
	var inspector: String = Hybrid3dInspectorPanel.format_selection(controller.get_selection(), controller.snapshot)
	assert_true(inspector.length() > 8, inspector)
	Helper.teardown(ctx, host)
