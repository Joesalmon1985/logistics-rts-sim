extends TestCase

const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_carrier_animation_state"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	var layer: SettlerSpriteLayer = scene.find_child("SettlerSpriteLayer", true, false) as SettlerSpriteLayer
	assert_true(layer != null, "sprite layer exists")
	assert_true(layer.animation_enabled, "animation enabled")
	layer._process(0.1)
	assert_true(layer._anim_phase > 0.0, "animation phase advances")
	SettlerHelper.teardown(ctx, host)
