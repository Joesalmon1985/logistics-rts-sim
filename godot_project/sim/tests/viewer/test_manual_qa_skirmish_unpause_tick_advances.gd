extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")
const MilitaryHelper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_manual_qa_skirmish_unpause_tick_advances"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = Helper.mount_menu_route(host, "skirmish_mvp")
	var scene: Node = ctx.get("scene")
	var tick0: int = MilitaryHelper.get_current_tick(scene)
	MilitaryHelper.invoke_play(scene)
	for _i in range(3):
		MilitaryHelper.run_tick(scene)
	assert_true(MilitaryHelper.get_current_tick(scene) > tick0, "tick advances")
	CaptureHelper.teardown(ctx, host)
