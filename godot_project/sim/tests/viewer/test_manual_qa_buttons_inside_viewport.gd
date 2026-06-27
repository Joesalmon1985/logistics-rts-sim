extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")


func get_name() -> String:
	return "test_manual_qa_buttons_inside_viewport"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = Helper.mount_menu_route(host, "settler_tutorial")
	var scene: Node = ctx.get("scene")
	var vp: SubViewport = ctx.get("viewport")
	var size: Vector2 = Vector2(vp.size) if vp != null else Vector2(1280, 720)
	for name in ["TutorialBackBtn", "TutorialNextBtn", "TutorialSkipBtn", "TutorialExitBtn", "PlayBtn"]:
		var btn: Button = scene.find_child(name, true, false) as Button
		assert_not_null(btn, name)
		var r: Rect2 = btn.get_global_rect()
		assert_true(r.position.x >= -1 and r.position.y >= -1, "%s offscreen" % name)
		assert_true(r.end.x <= size.x + 1 and r.end.y <= size.y + 1, "%s clipped" % name)
	CaptureHelper.teardown(ctx, host)
