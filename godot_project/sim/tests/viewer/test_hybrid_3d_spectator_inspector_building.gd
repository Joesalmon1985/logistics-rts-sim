extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")
const ReviewUtil := preload("res://sim/tools/hybrid_3d_spectator_review_util.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_inspector_building"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount(host)
	var scene: Node = ctx.get("scene")
	var hex: Vector2i = ReviewUtil.find_first_building_hex(scene, "headquarters", "p1")
	if hex.x < 0:
		hex = ReviewUtil.find_first_building_hex(scene, "command_hub", "p1")
	assert_true(hex.x >= 0, "no HQ hex")
	scene.call("select_hex_for_tests", hex.x, hex.y)
	Helper.force_layout(ctx)
	var text: String = Hybrid3dInspectorPanel.format_selection(scene.controller.get_selection(), scene.controller.snapshot)
	assert_true("Headquarters" in text or "Command Hub" in text, text)
	Helper.teardown(ctx, host)
