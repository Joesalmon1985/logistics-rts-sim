extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_3d_spectator_bot_activity_visible"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = CaptureHelper.launch_via_menu_route(host, "hybrid_3d")
	Helper.force_layout(ctx)
	var scene: Node = ctx.get("scene")
	assert_true(scene.controller.autoplay_enabled, "3d spectator autoplay on")
	var tick0: int = int(scene.controller.snapshot.get("tick", 0))
	scene.advance_for_tests(10)
	var tick1: int = int(scene.controller.snapshot.get("tick", 0))
	assert_true(tick1 > tick0, "bot activity advances tick within 10 steps")
	var snap: Dictionary = scene.controller.snapshot
	var visible: int = (
		snap.get("buildings", []).size()
		+ snap.get("military_buildings", []).size()
		+ snap.get("units", []).size()
	)
	assert_true(visible > 0, "buildings/units in snapshot")
	CaptureHelper.teardown(ctx, host)
