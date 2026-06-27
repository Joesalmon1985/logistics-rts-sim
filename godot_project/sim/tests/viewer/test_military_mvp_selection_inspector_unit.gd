extends TestCase

const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_military_mvp_selection_inspector_unit"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_military(host)
	var scene: Node = ctx.get("scene")
	var hex: Vector2i = Helper.get_controller(scene).find_first_unit_hex("rifle_squad", "p1")
	assert_true(hex.x >= 0, "no p1 rifle squad")
	Helper.simulate_map_click_at_hex(scene, hex.x, hex.y)
	var text: String = Helper.get_inspector_text(scene)
	assert_true("Rifle Squad" in text, "inspector should name unit")
	Helper.teardown(ctx, host)
