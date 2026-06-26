extends TestCase

const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_building_status_animation_state"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerHelper.mount_settler(host)
	var layer: SettlerSpriteLayer = ctx.get("scene").find_child("SettlerSpriteLayer", true, false) as SettlerSpriteLayer
	assert_true(layer != null, "sprite layer")
	assert_true(layer.has_method("_building_is_working"), "status helpers exist")
	assert_true(layer._building_is_working({"state": "active"}), "active is working")
	assert_true(layer._building_is_waiting({"state": "paused_missing_input", "paused_reason": "input_missing"}), "paused is waiting")
	SettlerHelper.teardown(ctx, host)
