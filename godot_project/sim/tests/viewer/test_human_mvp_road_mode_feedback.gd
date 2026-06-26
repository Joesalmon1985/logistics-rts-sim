extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_mvp_road_mode_feedback"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	SettlerMvpTestHelper.scene_call(scene, "invoke_road_mode")
	var banner: PanelContainer = scene.get_node_or_null("%ModeBanner") as PanelContainer
	assert_true(banner != null and banner.visible, "road mode banner visible")
	var title: Label = scene.get_node_or_null("%ModeTitle") as Label
	assert_true(title != null and title.text.find("Road") >= 0, "road banner title")
	SettlerMvpTestHelper.teardown(ctx, host)
