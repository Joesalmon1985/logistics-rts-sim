extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## SETTLER-MVP-TIME-007 — pan/zoom/select/build while paused without ticking.


func get_name() -> String:
	return "test_SETTLER_MVP_TIME_007_input_works_while_paused"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	var map: MapViewport = ctx.get("map")
	var controller: SettlerMvpController = ctx.get("controller")
	SettlerMvpTestHelper.invoke_pause(scene)
	var tick0: int = SettlerMvpTestHelper.get_current_tick(scene)
	var pan0: Vector2 = map.get_camera_state().get("pan_offset", Vector2.ZERO)
	SettlerMvpTestHelper.simulate_key(scene, KEY_A)
	assert_true(map.get_camera_state().get("pan_offset", Vector2.ZERO) != pan0, "pan while paused")
	var zoom0: float = map.get_camera_state().get("zoom", 1.0)
	SettlerMvpTestHelper.simulate_key(scene, KEY_MINUS)
	assert_true(map.get_camera_state().get("zoom", 1.0) != zoom0, "zoom while paused")
	SettlerMvpTestHelper.simulate_map_click_at_hex(scene, 6, 6)
	assert_true(SettlerMvpTestHelper.get_selected_hex(scene).x >= 0, "select while paused")
	SettlerMvpTestHelper.simulate_key(scene, KEY_1)
	assert_eq(controller.placement_building_type, "woodcutter", "build mode while paused")
	assert_eq(SettlerMvpTestHelper.get_current_tick(scene), tick0, "no tick advance during paused input")
	SettlerMvpTestHelper.teardown(ctx, host)
