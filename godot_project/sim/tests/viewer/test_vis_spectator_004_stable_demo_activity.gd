extends TestCase

## VIS-SPECTATOR-004 — stable production-chain demo produces visible start→end activity.


func get_name() -> String:
	return "test_VIS_SPECTATOR_004_stable_demo_activity"


func _run_body() -> void:
	var controller := EconomySpectatorController.new()
	var activity: Dictionary = controller.measure_activity_after_steps(
		30,
		EconomySpectatorController.DEMO_PRODUCTION_CHAIN,
	)
	assert_true(
		int(activity.get("meaningful_count", 0)) >= 2,
		"meaningful_count should be >= 2 (got %d)" % int(activity.get("meaningful_count", 0)),
	)
	assert_true(
		int(activity.get("non_sprite_delta_count", 0)) >= 1,
		"expected at least one non-sprite delta signal",
	)
	var signals: Dictionary = activity.get("signals", {})
	var has_chain_or_event: bool = bool(signals.get("chain_fingerprint_changed", false)) or bool(signals.get("events_grew", false))
	assert_true(has_chain_or_event, "production chain should change chain fingerprint or grow events")
