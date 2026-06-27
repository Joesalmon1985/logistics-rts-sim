extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_two_bots_spawn"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount(host)
	var scene: Node = ctx.get("scene")
	var controller: Hybrid3dSpectatorController = scene.controller
	assert_true(controller.dual != null, "dual runner missing")
	assert_true(controller.dual.bot_p1 != null, "bot p1 missing")
	assert_true(controller.dual.bot_p2 != null, "bot p2 missing")
	scene.call("advance_for_tests", 3)
	Helper.force_layout(ctx)
	var summary_a: String = controller.build_bot_summary("p1")
	var summary_b: String = controller.build_bot_summary("p2")
	assert_true("Bot A" in summary_a, summary_a)
	assert_true("Bot B" in summary_b, summary_b)
	Helper.teardown(ctx, host)
