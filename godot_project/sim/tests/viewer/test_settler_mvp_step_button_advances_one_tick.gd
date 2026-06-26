extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## SETTLER-MVP-TIME-005 — step advances exactly one tick while paused.


func get_name() -> String:
	return "test_SETTLER_MVP_TIME_005_step_button_advances_one_tick"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	SettlerMvpTestHelper.invoke_pause(scene)
	var tick0: int = SettlerMvpTestHelper.get_current_tick(scene)
	SettlerMvpTestHelper.invoke_step(scene)
	assert_eq(SettlerMvpTestHelper.get_current_tick(scene), tick0 + 1, "step should advance one tick")
	assert_true(SettlerMvpTestHelper.is_sim_paused(scene), "still paused after step")
	SettlerMvpTestHelper.teardown(ctx, host)
