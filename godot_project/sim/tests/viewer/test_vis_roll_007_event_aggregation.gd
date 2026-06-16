extends TestCase

## VIS-ROLL-007 — aggregated events and delta summaries.


func get_name() -> String:
	return "test_VIS_ROLL_007_event_aggregation"


const FIXTURE := "res://sim/tests/fixtures/visual_rollouts/minimal_economy"


func _run_body() -> void:
	var controller := RolloutController.new()
	var loaded: Dictionary = controller.load_rollout(FIXTURE)
	assert_true(loaded.get("ok", false), str(loaded.get("error", "")))

	controller.events = [
		{"step_index": 0, "tick": 1, "event_type": "route_blocked", "payload": {}},
		{"step_index": 0, "tick": 1, "event_type": "route_blocked", "payload": {}},
		{"step_index": 0, "tick": 1, "event_type": "ware_delivered", "payload": {}},
	]
	controller.step_index = 0
	controller.set_interface_mode("economy")
	var aggregated: String = controller.build_event_text()
	assert_true(aggregated.contains("route_blocked"), aggregated)
	assert_true(aggregated.contains("× 2") or aggregated.contains("x 2"), "aggregated route_blocked count")
	assert_false(aggregated.split("route_blocked").size() > 3, "raw spam suppressed")

	controller.set_interface_mode("debug")
	var raw: String = controller.build_event_text()
	assert_true(raw.contains("Events (raw)"), raw)

	var delta: String = controller.build_delta_summary_text()
	assert_true(delta.contains("Changed since previous step"), delta)
