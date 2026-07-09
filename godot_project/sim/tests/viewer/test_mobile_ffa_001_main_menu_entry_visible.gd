extends TestCase


func get_name() -> String:
	return "test_mobile_ffa_001_main_menu_entry_visible"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx := ViewerTestHarness.mount(host, "main_menu")
	var scene: Node = ctx.get("scene")
	var btn: Button = scene.get_node_or_null("%HybridFfaBtn") as Button
	assert_not_null(btn, "Hybrid FFA entry")
	ViewerTestHarness.teardown(ctx)
	host.queue_free()
