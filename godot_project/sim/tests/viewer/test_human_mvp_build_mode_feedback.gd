extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_mvp_build_mode_feedback"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	SettlerMvpTestHelper.scene_call(scene, "invoke_build_mode", ["woodcutter"])
	var banner: PanelContainer = scene.get_node_or_null("%ModeBanner") as PanelContainer
	assert_true(banner != null and banner.visible, "mode banner should be visible")
	var title: Label = scene.get_node_or_null("%ModeTitle") as Label
	assert_true(title != null and title.text.find("Woodcutter") >= 0, "banner title")
	var palette: BuildPalette = scene.get_node_or_null("%BuildPalette") as BuildPalette
	assert_true(palette != null, "palette")
	SettlerMvpTestHelper.teardown(ctx, host)
