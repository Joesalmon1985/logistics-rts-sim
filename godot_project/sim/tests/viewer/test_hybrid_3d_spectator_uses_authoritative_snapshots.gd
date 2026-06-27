extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_uses_authoritative_snapshots"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount(host)
	var scene: Node = ctx.get("scene")
	var controller: Hybrid3dSpectatorController = scene.controller
	assert_true(controller.uses_authoritative_snapshots_only(), "must use snapshots only")
	Helper.force_layout(ctx)
	assert_false(controller.snapshot.is_empty(), "snapshot empty")
	assert_true(controller.snapshot.has("variant"), "snapshot missing variant")
	assert_eq(str(controller.snapshot.get("variant", "")), "hybrid")
	Helper.teardown(ctx, host)
