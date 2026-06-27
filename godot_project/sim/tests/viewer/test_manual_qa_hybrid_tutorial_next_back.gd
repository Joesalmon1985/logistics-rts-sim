extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")


func get_name() -> String:
	return "test_manual_qa_hybrid_tutorial_next_back"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = Helper.mount_menu_route(host, "hybrid_tutorial")
	var scene: Node = ctx.get("scene")
	assert_true(bool(ctx.get("main_menu_route_used", false)), "menu route")
	var idx0: int = scene.get_tutorial_step_index()
	scene.call("simulate_tutorial_next")
	assert_true(scene.get_tutorial_step_index() > idx0, "next")
	scene.call("simulate_tutorial_back")
	assert_eq(scene.get_tutorial_step_index(), idx0, "back")
	var buttons: Dictionary = Helper.assert_four_tutorial_buttons(scene)
	assert_true(bool(buttons.get("ok", false)), str(buttons.get("msg", "")))
	CaptureHelper.teardown(ctx, host)
