extends TestCase

const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_understandable_inspector_building_plain_english"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	var snapshot: Dictionary = scene.controller.get_snapshot()
	for building in snapshot.get("buildings", []):
		if str(building.get("building_type", "")) == "woodcutter":
			var q: int = int(building.get("anchor_q", -1))
			var r: int = int(building.get("anchor_r", -1))
			if q >= 0:
				SettlerHelper.simulate_map_click_at_hex(scene, q, r)
				var text: String = SettlerHelper.get_inspector_text(scene)
				assert_true(text.to_lower().contains("woodcutter") or text.contains("Lodge"), text)
				SettlerHelper.teardown(ctx, host)
				return
	var selection: Dictionary = {
		"kind": "building",
		"data": {"building_type": "woodcutter", "anchor_q": 4, "anchor_r": 4, "state": "active", "id": 1},
	}
	var text2: String = SelectionFormatter.format(selection, snapshot, {})
	assert_true(text2.to_lower().contains("woodcutter") or text2.contains("timber"), text2)
	SettlerHelper.teardown(ctx, host)
