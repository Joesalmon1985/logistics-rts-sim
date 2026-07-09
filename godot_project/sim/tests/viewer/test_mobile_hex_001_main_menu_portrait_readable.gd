extends TestCase


func get_name() -> String:
	return "test_mobile_hex_001_main_menu_portrait_readable"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	MobileLayoutProfiles.apply_to_control(host, "mobile_portrait_common")
	var ctx := ViewerTestHarness.mount(host, "main_menu")
	assert_true(bool(ctx.get("ok", false)), "menu portrait")
	ViewerTestHarness.teardown(ctx)
	host.queue_free()
