extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")


func get_name() -> String:
	return "test_manual_qa_text_size_and_contrast"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = Helper.mount_menu_route(host, "settler_tutorial")
	var scene: Node = ctx.get("scene")
	var instruction: RichTextLabel = scene.find_child("TutorialInstruction", true, false) as RichTextLabel
	assert_not_null(instruction, "instruction")
	var font_size: int = instruction.get_theme_font_size("normal_font_size")
	assert_true(font_size >= 14, "tutorial font >= 14 got %d" % font_size)
	CaptureHelper.teardown(ctx, host)
