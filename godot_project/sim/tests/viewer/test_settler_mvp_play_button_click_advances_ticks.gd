extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## SETTLER-MVP-TIME-003 — Play button pressed signal advances ticks.


func get_name() -> String:
	return "test_SETTLER_MVP_TIME_003_play_button_click_advances_ticks"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	var play_btn: Button = scene.get_node_or_null("%PlayBtn") as Button
	assert_not_null(play_btn, "Play button missing")
	assert_true(play_btn.visible, "Play button not visible")
	var tick0: int = SettlerMvpTestHelper.get_current_tick(scene)
	play_btn.pressed.emit()
	assert_false(SettlerMvpTestHelper.is_sim_paused(scene), "playing after Play click")
	assert_true(SettlerMvpTestHelper.get_status_text(scene).contains("Playing"), "status Playing after click")
	for _i in range(3):
		SettlerMvpTestHelper.run_tick(scene)
	assert_true(SettlerMvpTestHelper.get_current_tick(scene) > tick0, "tick advanced after Play button click")
	SettlerMvpTestHelper.teardown(ctx, host)
