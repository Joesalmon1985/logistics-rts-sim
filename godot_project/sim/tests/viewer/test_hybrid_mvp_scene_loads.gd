extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_mvp_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_mvp_scene_loads"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = ViewerTestHarness.mount(host, "hybrid_mvp")
	assert_true(bool(ctx.get("ok", false)), str(ctx.get("reason", "mount failed")))
	var scene: Node = ctx.get("scene")
	assert_true(scene != null, "scene instantiated")
	if scene.has_method("initialize_for_tests"):
		scene.call("initialize_for_tests")
	Helper.settle_frames(ctx)
	assert_true(scene.get_node_or_null("%MapViewport") != null, "map viewport present")
	Helper.teardown(ctx, host)
