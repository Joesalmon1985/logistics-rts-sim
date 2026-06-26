extends TestCase

const Helper := preload("res://sim/tests/viewer/human_understandable_test_helper.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_tutorial_can_exit"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_scene(host)
	var scene: Node = ctx.get("scene")
	var skip: Button = scene.get_node_or_null("%TutorialSkipBtn") as Button
	var exit_btn: Button = scene.get_node_or_null("%TutorialExitBtn") as Button
	assert_true(skip != null and skip.visible, "skip tutorial button")
	assert_true(exit_btn != null and exit_btn.visible, "exit button")
	scene.call("_skip_tutorial")
	assert_false(scene.is_tutorial_enabled(), "tutorial disabled after skip")
	SettlerHelper.teardown(ctx, host)
