extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_mvp_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_mvp_tutorial_screen_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = ViewerTestHarness.mount(host, "hybrid_mvp")
	var scene: Node = ctx.get("scene")
	scene.call("initialize_for_tests")
	scene.call("enable_tutorial_for_tests")
	Helper.settle_frames(ctx)
	assert_true(scene.is_tutorial_enabled(), "tutorial enabled")
	assert_true(scene.get_tutorial_instruction_text().length() > 30, "tutorial text visible")
	Helper.teardown(ctx, host)
