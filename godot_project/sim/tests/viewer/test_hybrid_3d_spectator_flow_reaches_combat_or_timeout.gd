extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_flow_reaches_combat_or_timeout"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount(host)
	var scene: Node = ctx.get("scene")
	var result: Dictionary = scene.call("advance_until_combat_for_tests", 600)
	assert_true(int(result.get("steps", 0)) > 0, "no steps taken")
	assert_true(
		bool(result.get("progress", false)) or bool(result.get("saw_units", false)) or bool(result.get("saw_combat", false)) or bool(result.get("finished", false)),
		str(result),
	)
	Helper.teardown(ctx, host)
