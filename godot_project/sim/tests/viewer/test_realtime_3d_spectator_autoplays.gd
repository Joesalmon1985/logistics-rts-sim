extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const RtHelper := preload("res://sim/tests/viewer/realtime_playability_test_helper.gd")


func get_name() -> String:
	return "test_realtime_3d_spectator_autoplays"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = RtHelper.mount_route(host, "hybrid_3d")
	var scene: Node = ctx.get("scene")
	_assert_check(RtHelper.check_starts_playing(scene, "hybrid_3d"))
	var tick0: int = RtHelper.current_tick(scene, "hybrid_3d")
	for _i in range(8):
		ViewerTestHarness.tick_frames(ctx)
		if scene.has_method("_on_autoplay_tick"):
			scene.call("_on_autoplay_tick")
	var tick1: int = RtHelper.current_tick(scene, "hybrid_3d")
	assert_true(tick1 > tick0, "3d spectator tick advances without Step")
	var play_btn: Button = scene.find_child("PlayPauseBtn", true, false) as Button
	if play_btn != null:
		assert_true("Pause" in play_btn.text, "PlayPauseBtn shows Pause when running")
	CaptureHelper.teardown(ctx, host)


func _assert_check(result: Dictionary) -> void:
	assert_true(bool(result.get("ok", false)), str(result.get("msg", "check failed")))
