extends TestCase

const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_military_mvp_move_command_map_driven"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_military(host)
	var scene: Node = ctx.get("scene")
	var controller: MilitaryMvpController = Helper.get_controller(scene)
	var hex: Vector2i = controller.find_first_unit_hex("rifle_squad", "p1")
	assert_true(hex.x >= 0, "no unit to move")
	Helper.simulate_map_click_at_hex(scene, hex.x, hex.y)
	scene.invoke_move_to_hex(hex.x + 1, hex.y)
	assert_true(controller.last_command_message.contains("Move") or controller.last_command_message.contains("move"), "move feedback")
	Helper.teardown(ctx, host)
