extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")


func get_name() -> String:
	return "test_manual_qa_hybrid_unpause_tick_advances"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = Helper.mount_menu_route(host, "hybrid_mvp")
	var scene: Node = ctx.get("scene")
	var tick0: int = scene.get_current_tick()
	scene.call("invoke_play")
	if scene.has_method("_on_tick_timer"):
		for _i in range(3):
			scene.call("_on_tick_timer")
	assert_true(scene.get_current_tick() > tick0, "tick advances")
	CaptureHelper.teardown(ctx, host)
