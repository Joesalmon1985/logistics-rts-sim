extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_spectator_013_trained_intent_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx := Helper.mount(host)
	var scene: Node = ctx.get("scene")
	if scene != null and scene.controller != null:
		scene.controller.reset_match_trained_neural()
		scene.controller.step_ticks(5)
	var lines: PackedStringArray = scene.controller.get_scoreboard_lines()
	var joined: String = "\n".join(lines)
	assert_true(joined.find("trained_neural") >= 0 or joined.find("neural") >= 0, "trained intent visible: %s" % joined)
	ViewerTestHarness.teardown(ctx)
	host.queue_free()
