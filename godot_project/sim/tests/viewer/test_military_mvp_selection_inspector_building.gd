extends TestCase

const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_military_mvp_selection_inspector_building"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_military(host)
	var scene: Node = ctx.get("scene")
	Helper.simulate_map_click_at_hex(scene, Helper.HUB_Q, Helper.HUB_R)
	var text: String = Helper.get_inspector_text(scene)
	assert_true("Command Hub" in text, "inspector should name hub")
	Helper.teardown(ctx, host)
