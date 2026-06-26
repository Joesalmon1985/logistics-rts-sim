extends TestCase

const Helper := preload("res://sim/tests/viewer/actual_human_launch_helper.gd")
const ReviewUtil := preload("res://sim/tools/actual_human_launch_review_util.gd")


func get_name() -> String:
	return "test_actual_human_build_palette_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_via_menu_route(host, ReviewUtil.LAUNCH_SIZE)
	var scene: Node = ctx.get("scene")
	var palette: BuildPalette = scene.find_child("BuildPalette", true, false) as BuildPalette
	assert_true(palette != null and palette.visible, "build palette visible")
	palette.ensure_built()
	var count: int = palette.find_children("*", "TextureButton", true, false).size()
	assert_true(count >= 5, "labelled build buttons %d" % count)
	Helper.teardown(ctx, host)
