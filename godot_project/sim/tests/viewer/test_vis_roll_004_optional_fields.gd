extends TestCase

## VIS-ROLL-004 — missing optional fields tolerated.


func get_name() -> String:
	return "test_VIS_ROLL_004_optional_fields"


func _run_body() -> void:
	var controller := RolloutController.new()
	var loaded: Dictionary = controller.load_rollout(
		"res://sim/tests/fixtures/visual_rollouts/minimal_economy"
	)
	assert_true(loaded.get("ok", false), str(loaded.get("error", "")))
	controller.metrics.erase("total_score")
	controller.metrics.erase("comparison_baseline")
	var text: String = controller.build_metrics_text()
	assert_false(text.is_empty(), "metrics text still produced")
	for cmd in controller.commands:
		cmd.erase("policy_confidence")
		cmd.erase("reward_delta")
	var action: String = controller.build_action_text()
	assert_true(action.contains("Previous command"), "action panel renders")
