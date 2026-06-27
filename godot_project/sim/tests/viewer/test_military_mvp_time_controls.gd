extends TestCase

const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_military_mvp_time_controls"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_military(host)
	var scene: Node = ctx.get("scene")
	var tick0: int = Helper.get_current_tick(scene)
	Helper.invoke_play(scene)
	assert_false(Helper.is_sim_paused(scene), "should be playing")
	for _i in range(3):
		Helper.run_tick(scene)
	assert_true(Helper.get_current_tick(scene) > tick0, "tick advanced")
	Helper.invoke_pause(scene)
	assert_true(Helper.is_sim_paused(scene), "should pause")
	Helper.teardown(ctx, host)
