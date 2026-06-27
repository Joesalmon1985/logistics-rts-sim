extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_mvp_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_mvp_main_menu_entry"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = ViewerTestHarness.mount(host, "main_menu")
	assert_true(bool(ctx.get("ok", false)), "main menu mount")
	var scene: Node = ctx.get("scene")
	var btn: Button = scene.get_node_or_null("%HybridMvpBtn") as Button
	assert_true(btn != null, "Hybrid MVP button exists")
	assert_true("Hybrid" in btn.text, "button label mentions Hybrid")
	Helper.teardown(ctx, host)
