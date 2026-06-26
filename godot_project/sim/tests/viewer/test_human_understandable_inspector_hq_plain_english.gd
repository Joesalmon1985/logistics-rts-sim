extends TestCase

const Helper := preload("res://sim/tests/viewer/human_understandable_test_helper.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_inspector_hq_plain_english"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_tutorial_scene(host)
	var scene: Node = ctx.get("scene")
	var snapshot: Dictionary = scene.controller.get_snapshot()
	var hq: Dictionary = {}
	for building in snapshot.get("buildings", []):
		if str(building.get("building_type", "")) in ["headquarters", "command_hub"]:
			hq = building
			break
	assert_false(hq.is_empty(), "HQ missing from scenario")
	var selection: Dictionary = {"kind": "building", "data": hq}
	var text: String = SelectionFormatter.format(selection, snapshot, scene.controller.get_mode_context())
	assert_true(text.contains("Headquarters") or text.to_lower().contains("headquarters"), text)
	assert_true(text.contains("Stored goods") or text.contains("Road connection"), text)
	SettlerHelper.teardown(ctx, host)
