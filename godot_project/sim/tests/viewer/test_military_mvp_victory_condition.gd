extends TestCase

const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_military_mvp_victory_condition"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_military(host)
	var scene: Node = ctx.get("scene")
	var controller: MilitaryMvpController = Helper.get_controller(scene)
	assert_false(controller.is_victory(), "should not start in victory")
	var objective: String = controller.build_objective_text()
	assert_true("Command Hub" in objective, "objective mentions enemy hub")
	Helper.teardown(ctx, host)
