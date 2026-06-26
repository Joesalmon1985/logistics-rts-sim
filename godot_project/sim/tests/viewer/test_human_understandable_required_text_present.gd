extends TestCase

const Helper := preload("res://sim/tests/viewer/human_understandable_test_helper.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_required_text_present"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_scene(host)
	var scene: Node = ctx.get("scene")
	var title: String = scene.get_tutorial_title_text() if scene.has_method("get_tutorial_title_text") else ""
	var instruction: String = scene.get_tutorial_instruction_text() if scene.has_method("get_tutorial_instruction_text") else ""
	var objective: String = scene.get_objective_text() if scene.has_method("get_objective_text") else ""
	assert_true(title.length() > 5, "tutorial title too short")
	assert_true(instruction.length() > 40, "tutorial instruction too short")
	assert_true(objective.length() > 20, "objective too short")
	SettlerHelper.teardown(ctx, host)
