extends TestCase

const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_military_mvp_invalid_command_feedback"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_military(host)
	var scene: Node = ctx.get("scene")
	scene.invoke_build_mode("power_generator")
	Helper.simulate_map_click_at_hex(scene, 12, 12)
	var controller: MilitaryMvpController = Helper.get_controller(scene)
	assert_false(controller.last_command_message.is_empty(), "should report invalid build")
	assert_true(
		controller.last_command_message.to_lower().contains("cannot") or controller.last_command_message.to_lower().contains("too far") or controller.last_command_message.to_lower().contains("build"),
		"plain-English rejection",
	)
	Helper.teardown(ctx, host)
