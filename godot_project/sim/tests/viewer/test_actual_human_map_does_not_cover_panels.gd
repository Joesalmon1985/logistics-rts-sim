extends TestCase

const Helper := preload("res://sim/tests/viewer/actual_human_launch_helper.gd")
const ReviewUtil := preload("res://sim/tools/actual_human_launch_review_util.gd")


func get_name() -> String:
	return "test_actual_human_map_does_not_cover_panels"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_via_menu_route(host, ReviewUtil.LAUNCH_SIZE)
	var scene: Node = ctx.get("scene")
	var hsplit: HSplitContainer = scene.get_node_or_null("RootVBox/ContentHBox") as HSplitContainer
	assert_true(hsplit != null, "split layout exists")
	assert_true(hsplit.split_offset >= 300, "map column width")
	assert_true(hsplit.split_offset <= 980, "right column reserved")
	var scroll: Control = scene.find_child("RightPanelScroll", true, false) as Control
	assert_true(scroll != null and scroll.visible, "right panel scroll visible")
	assert_true(scroll.custom_minimum_size.x >= 300, "right panel min width")
	Helper.teardown(ctx, host)
