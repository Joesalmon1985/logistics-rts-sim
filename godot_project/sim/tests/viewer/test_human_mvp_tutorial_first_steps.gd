extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

const HQ_Q: int = 6
const HQ_R: int = 6


func get_name() -> String:
	return "test_human_mvp_tutorial_first_steps"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	if scene.has_method("enable_tutorial_for_tests"):
		scene.call("enable_tutorial_for_tests")
	var tutorial: SettlerTutorialController = scene.tutorial
	tutorial.advance_manual()
	assert_eq(tutorial.step_index, 1, "advanced past welcome")
	SettlerMvpTestHelper.get_map(scene).pan_by_screen_delta(Vector2(40, 20))
	tutorial.update(scene.controller.get_snapshot(), SettlerMvpTestHelper.get_map(scene), {}, scene.controller)
	SettlerMvpTestHelper.simulate_map_click_at_hex(scene, HQ_Q, HQ_R)
	var selection: Dictionary = scene.controller.get_selection_at(HQ_Q, HQ_R)
	tutorial.update(scene.controller.get_snapshot(), SettlerMvpTestHelper.get_map(scene), selection, scene.controller)
	SettlerMvpTestHelper.scene_call(scene, "invoke_play")
	for _i in range(3):
		if scene.controller.paused:
			break
		scene.controller.step_tick()
	tutorial.update(scene.controller.get_snapshot(), SettlerMvpTestHelper.get_map(scene), selection, scene.controller)
	assert_true(tutorial.step_index >= 1, "tutorial advanced past welcome")
	SettlerMvpTestHelper.teardown(ctx, host)
