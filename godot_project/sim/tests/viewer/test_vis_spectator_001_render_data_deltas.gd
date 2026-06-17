extends TestCase

## VIS-SPECTATOR-001 — production-chain spectator produces start→end activity deltas.


func get_name() -> String:
	return "test_VIS_SPECTATOR_001_render_data_deltas"


func _run_body() -> void:
	var controller := EconomySpectatorController.new()
	var activity: Dictionary = controller.measure_activity_after_steps(
		30,
		EconomySpectatorController.DEMO_PRODUCTION_CHAIN,
	)
	assert_true(
		EconomySpectatorMetrics.passes_smoke_gate(activity, 2),
		"expected meaningful_count>=2 with non-sprite delta (got meaningful=%d non_sprite=%d signals=%s)"
		% [
			int(activity.get("meaningful_count", 0)),
			int(activity.get("non_sprite_delta_count", 0)),
			str(activity.get("signals", {})),
		],
	)
