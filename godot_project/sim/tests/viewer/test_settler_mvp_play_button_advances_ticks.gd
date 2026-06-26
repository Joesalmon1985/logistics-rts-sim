extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## SETTLER-MVP-TIME-002 — invoke_play advances ticks.


func get_name() -> String:
	return "test_SETTLER_MVP_TIME_002_play_button_advances_ticks"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	var tick0: int = SettlerMvpTestHelper.get_current_tick(scene)
	SettlerMvpTestHelper.invoke_play(scene)
	assert_false(SettlerMvpTestHelper.is_sim_paused(scene), "should be playing")
	assert_true(SettlerMvpTestHelper.get_status_text(scene).contains("Playing"), "status should show Playing")
	for _i in range(3):
		SettlerMvpTestHelper.run_tick(scene)
	assert_true(SettlerMvpTestHelper.get_current_tick(scene) > tick0, "tick should advance after play")
	SettlerMvpTestHelper.teardown(ctx, host)
