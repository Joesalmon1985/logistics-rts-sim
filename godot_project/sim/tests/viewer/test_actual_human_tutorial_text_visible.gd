extends TestCase

const Helper := preload("res://sim/tests/viewer/actual_human_launch_helper.gd")
const ReviewUtil := preload("res://sim/tools/actual_human_launch_review_util.gd")


func get_name() -> String:
	return "test_actual_human_tutorial_text_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_via_menu_route(host, ReviewUtil.LAUNCH_SIZE)
	var scene: Node = ctx.get("scene")
	assert_true(scene.get_tutorial_instruction_text().length() > 80, "instruction length")
	assert_true(scene.get_tutorial_title_text().length() > 5, "title length")
	assert_true(scene.get_objective_text().length() > 20, "objective text")
	assert_true(str(scene.get_mode_status_text()).length() > 5, "mode status")
	var panel: Control = scene.find_child("TutorialPanel", true, false) as Control
	assert_true(panel != null and panel.visible, "tutorial panel visible")
	Helper.teardown(ctx, host)
