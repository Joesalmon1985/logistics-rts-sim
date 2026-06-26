extends TestCase

const Helper := preload("res://sim/tests/viewer/human_understandable_test_helper.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_text_inside_viewport"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_scene(host)
	var scene: Node = ctx.get("scene")
	for name in ["TutorialTitle", "TutorialInstruction", "ObjectivePanel", "Inspector"]:
		var node: Control = scene.find_child(name, true, false) as Control
		assert_true(node != null and node.visible, "%s visible" % name)
		if node is RichTextLabel:
			assert_true((node as RichTextLabel).get_parsed_text().length() > 5 or (node as RichTextLabel).text.length() > 5, "%s has text" % name)
		elif node is Label:
			assert_true((node as Label).text.length() > 0, "%s has text" % name)
	SettlerHelper.teardown(ctx, host)
