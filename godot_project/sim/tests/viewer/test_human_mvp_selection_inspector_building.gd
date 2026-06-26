extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_mvp_selection_inspector_building"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	var snap: Dictionary = scene.controller.get_snapshot()
	var clicked: bool = false
	for b in snap.get("buildings", []):
		var btype: String = str(b.get("building_type", ""))
		if btype not in ["headquarters", "command_hub", "woodcutter", "sawmill"]:
			continue
		var aq: int = int(b.get("anchor_q", b.get("q", 6)))
		var ar: int = int(b.get("anchor_r", b.get("r", 6)))
		for off in BuildingCatalog.footprint_offsets(btype):
			var q: int = aq + int(off[0])
			var r: int = ar + int(off[1])
			var sel: Dictionary = scene.controller.get_selection_at(q, r)
			if str(sel.get("kind", "")) != "building":
				continue
			SettlerMvpTestHelper.simulate_map_click_at_hex(scene, q, r)
			clicked = true
			break
		if clicked:
			break
	assert_true(clicked, "no building hex without carrier overlap")
	var text: String = SettlerMvpTestHelper.get_inspector_text(scene)
	assert_true(text.find("Headquarters") >= 0 or text.find("Sawmill") >= 0 or text.find("Woodcutter") >= 0, "building name")
	SettlerMvpTestHelper.teardown(ctx, host)
