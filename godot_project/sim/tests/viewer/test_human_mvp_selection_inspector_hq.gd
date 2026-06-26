extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_mvp_selection_inspector_hq"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	var snap: Dictionary = scene.controller.get_snapshot()
	var formatted: String = ""
	for b in snap.get("buildings", []):
		if str(b.get("building_type", "")) in ["headquarters", "command_hub"]:
			formatted = SelectionFormatter.format({"kind": "building", "data": b}, snap, {})
			break
	assert_true(formatted.find("Headquarters") >= 0, "expected Headquarters: %s" % formatted.substr(0, 80))
	assert_true(formatted.find("Stored goods") >= 0, "HQ stock context")
	SettlerMvpTestHelper.simulate_map_click_at_hex(scene, int(snap.get("buildings", [{}])[0].get("anchor_q", 6)), int(snap.get("buildings", [{}])[0].get("anchor_r", 6)))
	SettlerMvpTestHelper.teardown(ctx, host)
