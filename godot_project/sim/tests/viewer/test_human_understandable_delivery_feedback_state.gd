extends TestCase

const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_delivery_feedback_state"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerHelper.mount_settler(host)
	var layer: SettlerSpriteLayer = ctx.get("scene").find_child("SettlerSpriteLayer", true, false) as SettlerSpriteLayer
	assert_true(layer != null, "sprite layer")
	var render: Dictionary = {
		"carriers": [{"q": 6.0, "r": 6.0, "state": "moving", "ware_type": "timber", "ware_amount": 1}],
	}
	layer.map_viewport = SettlerHelper.get_map(ctx.get("scene"))
	layer.map_viewport.set_render_data(render)
	layer._process(0.05)
	assert_true(layer._anim_phase > 0.0, "delivery feedback animates")
	SettlerHelper.teardown(ctx, host)
