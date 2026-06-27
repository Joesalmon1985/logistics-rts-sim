extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_main_menu_entry"


func _run_body() -> void:
	var menu_host := Node.new()
	var menu_ctx: Dictionary = ViewerTestHarness.mount(menu_host, "main_menu")
	assert_true(bool(menu_ctx.get("ok", false)), "menu mount")
	var btn: Button = menu_ctx.get("scene").get_node_or_null("%Hybrid3dSpectatorBtn") as Button
	assert_true(btn != null, "3D Bot Spectator button missing")
	assert_true(btn.visible, "button not visible")
	assert_true("3D Bot Spectator" in btn.text, "button label")
	ViewerTestHarness.teardown(menu_ctx)
	menu_host.queue_free()
