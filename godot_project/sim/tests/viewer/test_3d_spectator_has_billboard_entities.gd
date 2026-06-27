extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_3d_spectator_has_billboard_entities"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = CaptureHelper.launch_via_menu_route(host, "hybrid_3d")
	Helper.force_layout(ctx)
	var scene: Node = ctx.get("scene")
	var counts: Dictionary = scene.get_world_entity_count()
	var snap: Dictionary = scene.controller.snapshot
	var entity_total: int = (
		int(counts.get("buildings", 0))
		+ int(counts.get("units", 0))
		+ int(counts.get("resources", 0))
		+ snap.get("buildings", []).size()
		+ snap.get("military_buildings", []).size()
		+ snap.get("units", []).size()
	)
	assert_true(entity_total > 0, "3d/snapshot entities=%s" % str(counts))
	CaptureHelper.teardown(ctx, host)
