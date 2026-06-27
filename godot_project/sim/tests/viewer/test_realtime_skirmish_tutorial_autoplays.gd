extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const RtHelper := preload("res://sim/tests/viewer/realtime_playability_test_helper.gd")


func get_name() -> String:
	return "test_realtime_skirmish_tutorial_autoplays"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = RtHelper.mount_route(host, "skirmish_tutorial")
	_assert_check(RtHelper.check_starts_playing(ctx.get("scene"), "skirmish_tutorial"))
	_assert_check(RtHelper.check_tick_advances_without_play_or_step(ctx, "skirmish_tutorial"))
	_assert_check(RtHelper.check_pause_stops_tick(ctx, "skirmish_tutorial"))
	_assert_check(RtHelper.check_space_resumes_tick(ctx, "skirmish_tutorial"))
	CaptureHelper.teardown(ctx, host)


func _assert_check(result: Dictionary) -> void:
	assert_true(bool(result.get("ok", false)), str(result.get("msg", "check failed")))
