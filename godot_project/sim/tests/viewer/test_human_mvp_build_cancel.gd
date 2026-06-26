extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_mvp_build_cancel"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	SettlerMvpTestHelper.scene_call(scene, "invoke_build_mode", ["woodcutter"])
	SettlerMvpTestHelper.scene_call(scene, "invoke_cancel_modes")
	var banner: PanelContainer = scene.get_node_or_null("%ModeBanner") as PanelContainer
	assert_true(banner != null and not banner.visible, "banner hidden after cancel")
	SettlerMvpTestHelper.teardown(ctx, host)
