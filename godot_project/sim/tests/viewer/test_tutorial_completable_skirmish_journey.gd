extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const ManualHelper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")
const RtHelper := preload("res://sim/tests/viewer/realtime_playability_test_helper.gd")
const MilitaryHelper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_tutorial_completable_skirmish_journey"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = ManualHelper.mount_menu_route(host, "skirmish_tutorial")
	var scene: Node = ctx.get("scene")
	var nav: Dictionary = ManualHelper.assert_four_tutorial_buttons(scene)
	assert_true(nav.get("ok", false), str(nav.get("msg", "")))
	var playing: Dictionary = RtHelper.check_starts_playing(scene, "skirmish_tutorial")
	assert_true(playing.get("ok", false), str(playing.get("msg", "")))
	var instruction: String = scene.get_tutorial_instruction_text() if scene.has_method("get_tutorial_instruction_text") else ""
	assert_true("Command Hub" in instruction or "Headquarters" in instruction or "Welcome" in instruction, "clear first step")
	var tick0: int = MilitaryHelper.get_current_tick(scene)
	for _i in range(4):
		if scene.has_method("_on_tick_timer"):
			scene.call("_on_tick_timer")
	assert_true(MilitaryHelper.get_current_tick(scene) > tick0, "tick advances during skirmish tutorial")
	scene.call("simulate_tutorial_next")
	CaptureHelper.teardown(ctx, host)
