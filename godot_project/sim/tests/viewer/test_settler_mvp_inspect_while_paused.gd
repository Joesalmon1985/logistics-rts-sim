extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## SETTLER-MVP-INPUT-004 — inspection works while paused without ticking.


func get_name() -> String:
	return "test_SETTLER_MVP_INPUT_004_inspect_while_paused"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	var map: MapViewport = ctx.get("map")
	var controller: SettlerMvpController = ctx.get("controller")
	SettlerMvpTestHelper.invoke_pause(scene)
	var tick_before: int = SettlerMvpTestHelper.get_current_tick(scene)
	SettlerMvpTestHelper.simulate_map_click_at_hex(scene, 6, 6)
	assert_true(SettlerMvpTestHelper.get_selected_hex(scene).x >= 0, "select while paused")
	assert_false(SettlerMvpTestHelper.get_inspector_text(scene).is_empty(), "inspector while paused")
	map.pan_by_screen_delta(Vector2(30, 20))
	map.zoom_by_factor(1.1)
	SettlerMvpTestHelper.invoke_build_mode(scene, "woodcutter")
	assert_eq(controller.placement_building_type, "woodcutter", "build mode while paused")
	assert_eq(SettlerMvpTestHelper.get_current_tick(scene), tick_before, "tick advanced during paused inspection")
	SettlerMvpTestHelper.teardown(ctx, host)
