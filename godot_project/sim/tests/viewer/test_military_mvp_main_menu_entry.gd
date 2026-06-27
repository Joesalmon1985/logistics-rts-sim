extends TestCase


func get_name() -> String:
	return "test_military_mvp_main_menu_entry"


func _run_body() -> void:
	SimBootstrap.load_all()
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = ViewerTestHarness.mount(host, "main_menu")
	assert_true(bool(ctx.get("ok", false)), "menu mount failed")
	var scene: Node = ctx.get("scene")
	var skirmish: Button = scene.get_node_or_null("%SkirmishMvpBtn") as Button
	var tutorial: Button = scene.get_node_or_null("%SkirmishTutorialBtn") as Button
	assert_true(skirmish != null, "Skirmish MVP button missing")
	assert_true(tutorial != null, "Skirmish Tutorial button missing")
	ViewerTestHarness.teardown(ctx)
	host.free()
