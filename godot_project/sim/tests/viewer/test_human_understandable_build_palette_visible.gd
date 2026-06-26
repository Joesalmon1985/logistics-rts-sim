extends TestCase

const Helper := preload("res://sim/tests/viewer/human_understandable_test_helper.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_build_palette_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_scene(host)
	var scene: Node = ctx.get("scene")
	var palette: BuildPalette = scene.find_child("BuildPalette", true, false) as BuildPalette
	assert_true(palette != null and palette.visible, "palette visible")
	palette.ensure_built()
	var count: int = 0
	for btn in palette.find_children("*", "TextureButton", true, false):
		if (btn as Control).visible:
			count += 1
	assert_true(count >= 3, "need 3+ build buttons, got %d" % count)
	SettlerHelper.teardown(ctx, host)
