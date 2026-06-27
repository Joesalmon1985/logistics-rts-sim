extends TestCase

const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_military_mvp_attack_command_map_driven"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_military(host)
	var scene: Node = ctx.get("scene")
	var controller: MilitaryMvpController = Helper.get_controller(scene)
	var friendly: Vector2i = controller.find_first_unit_hex("rifle_squad", "p1")
	var enemy: Vector2i = controller.find_first_unit_hex("rifle_squad", "p2")
	assert_true(friendly.x >= 0 and enemy.x >= 0, "need friendly and enemy units")
	Helper.simulate_map_click_at_hex(scene, friendly.x, friendly.y)
	scene.invoke_attack_at_hex(enemy.x, enemy.y)
	assert_true(controller.last_command_message.contains("Attack") or controller.last_command_message.contains("attack"), "attack feedback")
	Helper.teardown(ctx, host)
