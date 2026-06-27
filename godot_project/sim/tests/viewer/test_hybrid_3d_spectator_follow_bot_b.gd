extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_follow_bot_b"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount(host)
	var scene: Node = ctx.get("scene")
	scene.call("follow_bot_for_tests", "p2")
	var controller: Hybrid3dSpectatorController = scene.controller
	assert_eq(controller.follow_target, "p2")
	assert_eq(controller.camera_mode, "follow_bot_b")
	Helper.teardown(ctx, host)
