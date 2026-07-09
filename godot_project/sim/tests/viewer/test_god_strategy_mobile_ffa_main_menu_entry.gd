extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_god_strategy_mobile_ffa_main_menu_entry"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = ViewerTestHarness.mount(host, "main_menu")
	var scene: Node = ctx.get("scene")
	var btn: Button = scene.get_node_or_null("%GodStrategyFfaBtn") as Button
	assert_not_null(btn, "God-Strategy FFA main menu entry")
	ViewerTestHarness.teardown(ctx)
	host.queue_free()
