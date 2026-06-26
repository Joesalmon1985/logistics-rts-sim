extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## SETTLER-MVP-INPUT-001 — keyboard fallback controls while paused.


func get_name() -> String:
	return "test_SETTLER_MVP_INPUT_001_keyboard_controls"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	var map: MapViewport = ctx.get("map")
	var controller: SettlerMvpController = ctx.get("controller")
	SettlerMvpTestHelper.invoke_pause(scene)
	assert_true(controller.paused, "should start paused for keyboard test")
	var pan0: Vector2 = map.get_camera_state().get("pan_offset", Vector2.ZERO)
	SettlerMvpTestHelper.simulate_key(scene, KEY_W)
	assert_true(map.get_camera_state().get("pan_offset", Vector2.ZERO) != pan0, "W pan failed")
	var zoom0: float = map.get_camera_state().get("zoom", 1.0)
	SettlerMvpTestHelper.simulate_key(scene, KEY_EQUAL)
	assert_true(map.get_camera_state().get("zoom", 1.0) != zoom0, "+ zoom failed")
	SettlerMvpTestHelper.simulate_key(scene, KEY_1)
	assert_eq(controller.placement_building_type, "woodcutter", "1 woodcutter")
	SettlerMvpTestHelper.simulate_key(scene, KEY_2)
	assert_eq(controller.placement_building_type, "forester", "2 forester")
	SettlerMvpTestHelper.simulate_key(scene, KEY_3)
	assert_eq(controller.placement_building_type, "sawmill", "3 sawmill")
	SettlerMvpTestHelper.simulate_key(scene, KEY_4)
	assert_eq(controller.placement_building_type, "quarry", "4 quarry")
	SettlerMvpTestHelper.simulate_key(scene, KEY_R)
	assert_true(controller.road_mode_active, "R road mode")
	SettlerMvpTestHelper.simulate_key(scene, KEY_ESCAPE)
	assert_false(controller.road_mode_active, "Esc cancel")
	assert_eq(controller.placement_building_type, "", "Esc cleared build")
	var help_before: bool = SettlerMvpTestHelper.is_help_visible(scene)
	SettlerMvpTestHelper.simulate_key(scene, KEY_H)
	assert_eq(SettlerMvpTestHelper.is_help_visible(scene), not help_before, "H toggles help")
	SettlerMvpTestHelper.teardown(ctx, host)
