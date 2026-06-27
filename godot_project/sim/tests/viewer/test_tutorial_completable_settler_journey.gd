extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const ManualHelper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")
const RtHelper := preload("res://sim/tests/viewer/realtime_playability_test_helper.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_tutorial_completable_settler_journey"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = ManualHelper.mount_menu_route(host, "settler_tutorial")
	var scene: Node = ctx.get("scene")
	var nav: Dictionary = ManualHelper.assert_four_tutorial_buttons(scene)
	assert_true(nav.get("ok", false), str(nav.get("msg", "")))
	assert_true(ManualHelper.assert_not_stranded(scene), "tutorial not stranded")
	var playing: Dictionary = RtHelper.check_starts_playing(scene, "settler_tutorial")
	assert_true(playing.get("ok", false), str(playing.get("msg", "")))
	var instruction: String = scene.get_tutorial_instruction_text() if scene.has_method("get_tutorial_instruction_text") else ""
	assert_false(instruction.is_empty(), "first instruction visible")
	assert_true(RtHelper.forest_or_resource_visible(scene, "settler_tutorial"), "forest/resource visible on map")
	var tick0: int = SettlerHelper.get_current_tick(scene)
	for _i in range(4):
		SettlerHelper.run_tick(scene)
	assert_true(SettlerHelper.get_current_tick(scene) > tick0, "sim running during tutorial")
	scene.call("simulate_tutorial_next")
	var title: String = scene.get_tutorial_title_text() if scene.has_method("get_tutorial_title_text") else ""
	assert_false(title.is_empty(), "tutorial advances via Next")
	CaptureHelper.teardown(ctx, host)
