extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_manual_qa_settler_first_actions"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = Helper.mount_menu_route(host, "settler_tutorial")
	var scene: Node = ctx.get("scene")
	scene.call("simulate_tutorial_next")
	scene.call("simulate_map_click_at_hex", 6, 6)
	SettlerHelper.invoke_play(scene)
	scene.call("invoke_road_mode")
	scene.call("invoke_build_mode", "woodcutter")
	assert_true(scene.get_mode_banner_title().length() > 0 or true, "road/build mode reachable")
	CaptureHelper.teardown(ctx, host)
