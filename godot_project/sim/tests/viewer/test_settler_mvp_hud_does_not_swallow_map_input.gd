extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## SETTLER-MVP-INPUT-002 — HUD must not fully block map input.


func get_name() -> String:
	return "test_SETTLER_MVP_INPUT_002_hud_does_not_swallow_map_input"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	var map: MapViewport = ctx.get("map")
	for node in SettlerMvpTestHelper.decorative_labels(scene):
		if node is Control:
			assert_eq((node as Control).focus_mode, Control.FOCUS_NONE, "decorative focus: %s" % node.name)
			assert_eq((node as Control).mouse_filter, Control.MOUSE_FILTER_IGNORE, "decorative filter: %s" % node.name)
	assert_false(SettlerMvpTestHelper.map_fully_covered_by_stop(scene, map), "map fully covered by STOP control")
	SettlerMvpTestHelper.simulate_map_click_at_hex(scene, 6, 6)
	assert_true(SettlerMvpTestHelper.get_selected_hex(scene).x >= 0, "map click did not reach selection path")
	SettlerMvpTestHelper.teardown(ctx, host)
