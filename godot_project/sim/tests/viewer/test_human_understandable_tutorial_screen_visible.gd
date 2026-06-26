extends TestCase

const Helper := preload("res://sim/tests/viewer/human_understandable_test_helper.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_tutorial_screen_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_scene(host)
	var scene: Node = ctx.get("scene")
	assert_true(scene.is_tutorial_enabled(), "tutorial enabled")
	assert_true(scene.get_tutorial_title_text().length() > 5, "tutorial title")
	assert_true(scene.get_tutorial_instruction_text().length() > 40, "tutorial instruction")
	assert_true(scene.get_objective_text().length() > 20, "objective")
	var panel: Control = scene.find_child("TutorialPanel", true, false) as Control
	assert_true(panel != null and panel.visible, "tutorial panel visible")
	SettlerHelper.teardown(ctx, host)
