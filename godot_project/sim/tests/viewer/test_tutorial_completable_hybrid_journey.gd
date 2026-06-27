extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const ManualHelper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")
const RtHelper := preload("res://sim/tests/viewer/realtime_playability_test_helper.gd")


func get_name() -> String:
	return "test_tutorial_completable_hybrid_journey"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = ManualHelper.mount_menu_route(host, "hybrid_tutorial")
	var scene: Node = ctx.get("scene")
	var nav: Dictionary = ManualHelper.assert_four_tutorial_buttons(scene)
	assert_true(nav.get("ok", false), str(nav.get("msg", "")))
	var playing: Dictionary = RtHelper.check_starts_playing(scene, "hybrid_tutorial")
	assert_true(playing.get("ok", false), str(playing.get("msg", "")))
	assert_true(RtHelper.forest_or_resource_visible(scene, "hybrid_tutorial"), "hybrid tutorial resources visible")
	var instruction: String = scene.get_tutorial_instruction_text() if scene.has_method("get_tutorial_instruction_text") else ""
	assert_true("Hybrid" in instruction or "Headquarters" in instruction, "hybrid tutorial intro clear")
	var snap: Dictionary = scene.controller.get_snapshot() if "controller" in scene else {}
	assert_false(snap.is_empty(), "economy snapshot visible")
	var tick0: int = int(snap.get("tick", 0))
	for _i in range(6):
		if scene.has_method("_on_tick_timer"):
			scene.call("_on_tick_timer")
	var tick1: int = int(scene.controller.get_snapshot().get("tick", 0))
	assert_true(tick1 > tick0, "hybrid tutorial sim running")
	scene.call("simulate_tutorial_next")
	var steps: int = scene.get_tutorial_step_index() if scene.has_method("get_tutorial_step_index") else 0
	assert_true(steps >= 1, "tutorial progress milestone")
	CaptureHelper.teardown(ctx, host)
