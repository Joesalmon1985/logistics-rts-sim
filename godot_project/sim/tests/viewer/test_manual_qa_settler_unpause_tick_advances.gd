extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_manual_qa_settler_unpause_tick_advances"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = Helper.mount_menu_route(host, "settler_tutorial")
	var scene: Node = ctx.get("scene")
	var tick0: int = SettlerHelper.get_current_tick(scene)
	SettlerHelper.invoke_play(scene)
	for _i in range(3):
		SettlerHelper.run_tick(scene)
	assert_true(SettlerHelper.get_current_tick(scene) > tick0, "tick advances after play")
	var play: Button = scene.get_node_or_null("%PlayBtn") as Button
	assert_not_null(play, "play btn")
	assert_true(play.visible, "play visible")
	CaptureHelper.teardown(ctx, host)
