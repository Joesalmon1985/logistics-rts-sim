extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_3d_spectator_has_hex_board"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = CaptureHelper.launch_via_menu_route(host, "hybrid_3d")
	Helper.force_layout(ctx)
	var scene: Node = ctx.get("scene")
	var counts: Dictionary = scene.get_world_entity_count()
	var snap_terrain: int = Hybrid3dWorldRenderer._terrain_tiles(scene.controller.snapshot).size()
	assert_true(
		int(counts.get("terrain", 0)) > 0 or snap_terrain > 0,
		"hex board terrain=%d snap=%d" % [int(counts.get("terrain", 0)), snap_terrain],
	)
	CaptureHelper.teardown(ctx, host)
