extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_test_helper.gd")
const MilitaryHelper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_manual_qa_skirmish_first_actions"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = Helper.mount_menu_route(host, "skirmish_tutorial")
	var scene: Node = ctx.get("scene")
	scene.call("simulate_tutorial_next")
	MilitaryHelper.simulate_map_click_at_hex(scene, 3, 3)
	MilitaryHelper.invoke_play(scene)
	if scene.has_method("invoke_attack_at_hex"):
		scene.call("invoke_attack_at_hex", 5, 5)
	var prod: RichTextLabel = scene.find_child("ProductionQueuesPanel", true, false) as RichTextLabel
	assert_not_null(prod, "production panel")
	CaptureHelper.teardown(ctx, host)
