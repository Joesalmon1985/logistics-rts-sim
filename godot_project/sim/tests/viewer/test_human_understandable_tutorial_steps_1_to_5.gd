extends TestCase

const Helper := preload("res://sim/tests/viewer/human_understandable_test_helper.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_tutorial_steps_1_to_5"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_scene(host)
	var scene: Node = ctx.get("scene")
	var tutorial: SettlerTutorialController = scene.tutorial
	assert_eq(tutorial.step_index, 0, "starts at welcome")
	tutorial.advance_manual()
	assert_eq(tutorial.step_index, 1, "step 2 camera")
	SettlerHelper.get_map(scene).pan_by_screen_delta(Vector2(50, 30))
	tutorial.update(scene.controller.get_snapshot(), SettlerHelper.get_map(scene), {}, scene.controller)
	SettlerHelper.simulate_map_click_at_hex(scene, Helper.HQ_Q, Helper.HQ_R)
	var selection: Dictionary = scene.controller.get_selection_at(Helper.HQ_Q, Helper.HQ_R)
	tutorial.update(scene.controller.get_snapshot(), SettlerHelper.get_map(scene), selection, scene.controller)
	SettlerHelper.invoke_play(scene)
	for _i in range(3):
		if not scene.controller.paused:
			scene.controller.step_tick()
	tutorial.update(scene.controller.get_snapshot(), SettlerHelper.get_map(scene), selection, scene.controller)
	assert_true(tutorial.step_index >= 2, "advanced through early steps")
	SettlerHelper.teardown(ctx, host)
