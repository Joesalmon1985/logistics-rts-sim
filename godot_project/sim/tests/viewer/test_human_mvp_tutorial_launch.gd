extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_mvp_tutorial_launch"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	scene.call("enable_tutorial_for_tests")
	var panel: Control = scene.get_node_or_null("%TutorialPanel") as Control
	assert_true(panel != null and panel.visible, "tutorial panel visible")
	var title: Label = scene.get_node_or_null("%TutorialTitle") as Label
	assert_true(title != null and not title.text.is_empty(), "tutorial title")
	SettlerMvpTestHelper.teardown(ctx, host)
