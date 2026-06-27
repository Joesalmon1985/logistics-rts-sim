extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_scene_loads"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount(host)
	assert_true(bool(ctx.get("ok", false)), str(ctx.get("reason", "")))
	var scene: Node = ctx.get("scene")
	Helper.force_layout(ctx)
	var counts: Dictionary = scene.call("get_world_entity_count")
	var terrain_count: int = int(counts.get("terrain", 0))
	var building_count: int = int(counts.get("buildings", 0))
	var snap_terrain: int = Hybrid3dWorldRenderer._terrain_tiles(scene.controller.snapshot).size()
	assert_true(
		terrain_count > 0 or building_count > 0 or snap_terrain > 0,
		"terrain=%d buildings=%d snap_terrain=%d counts=%s" % [terrain_count, building_count, snap_terrain, str(counts)],
	)
	Helper.teardown(ctx, host)
