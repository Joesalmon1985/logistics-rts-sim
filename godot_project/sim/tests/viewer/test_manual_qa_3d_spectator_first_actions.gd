extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")


func get_name() -> String:
	return "test_manual_qa_3d_spectator_first_actions"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = Helper.mount_menu_route(host, "hybrid_3d")
	var scene: Node = ctx.get("scene")
	assert_not_null(scene, "3d scene mounted")
	if scene.has_method("ensure_match_started"):
		scene.call("ensure_match_started")
	var status: Label = scene.find_child("StatusLabel", true, false) as Label
	assert_not_null(status, "status")
	assert_true(status.text.length() > 3, "hud readable")
	var play: Button = scene.find_child("PlayPauseBtn", true, false) as Button
	if play == null:
		play = scene.find_child("PlayBtn", true, false) as Button
	assert_not_null(play, "play control")
	CaptureHelper.teardown(ctx, host)
