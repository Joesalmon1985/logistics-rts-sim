extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## SETTLER-MVP-TIME-004 — pause stops tick advancement.


func get_name() -> String:
	return "test_SETTLER_MVP_TIME_004_pause_button_stops_ticks"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	SettlerMvpTestHelper.invoke_play(scene)
	for _i in range(2):
		SettlerMvpTestHelper.run_tick(scene)
	var tick_playing: int = SettlerMvpTestHelper.get_current_tick(scene)
	SettlerMvpTestHelper.invoke_pause(scene)
	assert_true(SettlerMvpTestHelper.is_sim_paused(scene), "paused")
	assert_true(SettlerMvpTestHelper.get_status_text(scene).contains("Paused"), "status Paused")
	for _j in range(4):
		SettlerMvpTestHelper.run_tick(scene)
	assert_eq(SettlerMvpTestHelper.get_current_tick(scene), tick_playing, "tick should not advance while paused")
	SettlerMvpTestHelper.teardown(ctx, host)
