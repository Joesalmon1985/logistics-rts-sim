extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_spectator_003_click_building_inspector"


func _run_body() -> void:
	var host := Node.new()
	var ctx := Helper.mount(host)
	var scene: Node = ctx.get("scene")
	scene.controller.select_hex(3, 11)
	assert_true(scene.controller.get_inspector_text().length() > 5, "inspector")
	ViewerTestHarness.teardown(ctx)
	host.queue_free()
