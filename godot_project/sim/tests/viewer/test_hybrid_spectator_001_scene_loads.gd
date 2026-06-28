extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_spectator_001_scene_loads"


func _run_body() -> void:
	var host := Node.new()
	var ctx := Helper.mount(host)
	assert_true(bool(ctx.get("ok", false)), "mount")
	assert_not_null(ctx.get("scene"), "scene loads")
	ViewerTestHarness.teardown(ctx)
	host.queue_free()
