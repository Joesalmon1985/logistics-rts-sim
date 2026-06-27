extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_mvp_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_mvp_build_palette_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = ViewerTestHarness.mount(host, "hybrid_mvp")
	var scene: Node = ctx.get("scene")
	scene.call("initialize_for_tests")
	Helper.settle_frames(ctx)
	var palette: Control = scene.get_node_or_null("%BuildPalette") as Control
	assert_true(palette != null and palette.visible, "build palette visible")
	var guard_btn: Button = palette.get_node_or_null("Build_guard_post") as Button
	assert_true(guard_btn != null, "guard post build button")
	Helper.teardown(ctx, host)
