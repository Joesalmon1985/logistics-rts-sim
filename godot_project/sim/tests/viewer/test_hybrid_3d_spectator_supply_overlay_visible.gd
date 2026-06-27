extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_supply_overlay_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount(host)
	var scene: Node = ctx.get("scene")
	var controller: Hybrid3dSpectatorController = scene.controller
	controller.set_overlay_visible("supply", true)
	scene.call("advance_for_tests", 20)
	Helper.force_layout(ctx)
	assert_true(controller.overlay_visibility.get("supply", false), "supply overlay off")
	var renderer: Hybrid3dWorldRenderer = scene.get_node_or_null("%WorldRenderer")
	assert_true(renderer != null, "renderer missing")
	Helper.teardown(ctx, host)
