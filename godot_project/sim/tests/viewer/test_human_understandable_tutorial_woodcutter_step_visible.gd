extends TestCase

const Helper := preload("res://sim/tests/viewer/human_understandable_test_helper.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_tutorial_woodcutter_step_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_scene(host)
	var scene: Node = ctx.get("scene")
	scene.invoke_build_mode("woodcutter")
	if scene.has_method("_refresh_mode_ui"):
		scene.call("_refresh_mode_ui")
	SettlerHelper.settle_frames(ctx)
	var title: String = scene.get_mode_banner_title() if scene.has_method("get_mode_banner_title") else ""
	assert_true(title.to_lower().contains("woodcutter"), title)
	SettlerHelper.teardown(ctx, host)
