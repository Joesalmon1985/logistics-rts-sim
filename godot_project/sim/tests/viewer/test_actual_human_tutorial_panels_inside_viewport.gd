extends TestCase

const Helper := preload("res://sim/tests/viewer/actual_human_launch_helper.gd")
const ReviewUtil := preload("res://sim/tools/actual_human_launch_review_util.gd")


func get_name() -> String:
	return "test_actual_human_tutorial_panels_inside_viewport"


func _panel_ok(scene: Node, panel_name: String) -> bool:
	var panel: Control = scene.find_child(panel_name, true, false) as Control
	if panel == null or not panel.visible:
		return false
	return panel.custom_minimum_size.y > 0 or panel.size.y > 0 or panel.get_minimum_size().y > 0


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_via_menu_route(host, ReviewUtil.LAUNCH_SIZE)
	var scene: Node = ctx.get("scene")
	for panel_name in ["TutorialPanel", "ObjectivePanel", "InspectorPanel", "BuildPalette"]:
		assert_true(_panel_ok(scene, panel_name), "panel visible %s" % panel_name)
	Helper.teardown(ctx, host)
