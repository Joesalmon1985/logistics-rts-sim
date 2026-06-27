extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")


func get_name() -> String:
	return "test_manual_qa_hybrid_first_actions"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = Helper.mount_menu_route(host, "hybrid_tutorial")
	var scene: Node = ctx.get("scene")
	scene.call("simulate_tutorial_next")
	scene.call("_on_map_click", 2, 2)
	scene.call("invoke_play")
	scene.call("invoke_attack_mode")
	var supply: Label = scene.find_child("SupplyLabel", true, false) as Label
	assert_not_null(supply, "supply label")
	CaptureHelper.teardown(ctx, host)
