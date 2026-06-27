extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")


func get_name() -> String:
	return "test_manual_qa_no_map_only_screens"


func _run_body() -> void:
	for route in ["settler_tutorial", "skirmish_tutorial", "hybrid_tutorial"]:
		var host := Node.new()
		Engine.get_main_loop().root.add_child(host)
		var ctx: Dictionary = Helper.mount_menu_route(host, route)
		var scene: Node = ctx.get("scene")
		var title: String = scene.get_tutorial_title_text() if scene.has_method("get_tutorial_title_text") else ""
		assert_true(title.length() > 3, "%s not map-only" % route)
		var buttons: Dictionary = Helper.assert_four_tutorial_buttons(scene)
		assert_true(bool(buttons.get("ok", false)), route)
		CaptureHelper.teardown(ctx, host)
