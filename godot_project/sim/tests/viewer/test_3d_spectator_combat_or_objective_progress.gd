extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_3d_spectator_combat_or_objective_progress"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = CaptureHelper.launch_via_menu_route(host, "hybrid_3d")
	Helper.force_layout(ctx)
	var scene: Node = ctx.get("scene")
	var result: Dictionary = scene.advance_until_combat_for_tests(400)
	assert_true(bool(result.get("progress", false)), "combat/units/military progress observed")
	CaptureHelper.teardown(ctx, host)
