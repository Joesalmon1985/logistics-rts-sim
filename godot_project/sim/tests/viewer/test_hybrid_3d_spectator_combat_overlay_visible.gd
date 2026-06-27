extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_combat_overlay_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount(host)
	var scene: Node = ctx.get("scene")
	var controller: Hybrid3dSpectatorController = scene.controller
	controller.set_overlay_visible("combat_events", true)
	var result: Dictionary = scene.call("advance_until_combat_for_tests", 500)
	Helper.force_layout(ctx)
	assert_true(
		bool(result.get("progress", false)) or bool(result.get("saw_units", false)) or bool(result.get("saw_combat", false)) or bool(result.get("finished", false)),
		str(result),
	)
	Helper.teardown(ctx, host)
