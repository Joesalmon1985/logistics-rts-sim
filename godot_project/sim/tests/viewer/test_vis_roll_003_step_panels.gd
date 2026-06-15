extends TestCase

## VIS-ROLL-003 — step forward/back updates snapshot and action panels.


func get_name() -> String:
	return "test_VIS_ROLL_003_step_panels"


const FIXTURE := "res://sim/tests/fixtures/visual_rollouts/minimal_economy"


func _run_body() -> void:
	var controller := RolloutController.new()
	var loaded: Dictionary = controller.load_rollout(FIXTURE)
	assert_true(loaded.get("ok", false), str(loaded.get("error", "")))
	var tick0: int = int(controller.snapshot.get("tick", -1))
	assert_eq(controller.step_index, 0, "starts at 0")
	assert_true(controller.get_previous_command().is_empty(), "no previous at step 0")
	assert_false(controller.get_current_decision().is_empty(), "has decision at step 0")
	assert_true(controller.step_forward(), "step forward")
	assert_false(controller.get_previous_command().is_empty(), "previous after step")
	var tick1: int = int(controller.snapshot.get("tick", -1))
	assert_true(tick1 >= tick0, "tick advances or stays")
	assert_true(controller.step_back(), "step back")
	assert_eq(controller.step_index, 0, "back to 0")
	assert_true(controller.get_previous_command().is_empty(), "previous cleared at 0")
