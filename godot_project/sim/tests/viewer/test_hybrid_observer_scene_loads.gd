extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_mvp_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_observer_scene_loads"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = ViewerTestHarness.mount(host, "hybrid_observer")
	assert_true(bool(ctx.get("ok", false)), str(ctx.get("reason", "")))
	var scene: Node = ctx.get("scene")
	assert_true(scene.get_node_or_null("%ChainPanel") != null, "chain panel")
	Helper.teardown(ctx, host)
