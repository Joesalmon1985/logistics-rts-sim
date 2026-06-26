extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## SETTLER-MVP-TIME-006 — Space toggles play/pause.


func get_name() -> String:
	return "test_SETTLER_MVP_TIME_006_keyboard_space_toggles_play"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	SettlerMvpTestHelper.invoke_pause(scene)
	assert_true(SettlerMvpTestHelper.is_sim_paused(scene), "starts paused")
	SettlerMvpTestHelper.simulate_key(scene, KEY_SPACE)
	assert_false(SettlerMvpTestHelper.is_sim_paused(scene), "space should play")
	SettlerMvpTestHelper.simulate_key(scene, KEY_SPACE)
	assert_true(SettlerMvpTestHelper.is_sim_paused(scene), "space should pause again")
	SettlerMvpTestHelper.teardown(ctx, host)
