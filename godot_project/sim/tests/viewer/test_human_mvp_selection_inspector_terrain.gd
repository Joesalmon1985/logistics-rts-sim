extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_mvp_selection_inspector_terrain"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	var snap: Dictionary = scene.controller.get_snapshot()
	var layers: Dictionary = snap.get("display_layers", {})
	var tile: Dictionary = {}
	for t in layers.get("terrain", []):
		if int(t.get("q", -1)) >= 0:
			tile = t
			break
	assert_false(tile.is_empty(), "no terrain tile in snapshot")
	var q: int = int(tile.get("q", 2))
	var r: int = int(tile.get("r", 2))
	SettlerMvpTestHelper.simulate_map_click_at_hex(scene, q, r)
	var text: String = SettlerMvpTestHelper.get_inspector_text(scene)
	assert_true(text.find("Terrain") >= 0 or text.find("Hex") >= 0, "terrain inspector: %s" % text.substr(0, 60))
	SettlerMvpTestHelper.teardown(ctx, host)
