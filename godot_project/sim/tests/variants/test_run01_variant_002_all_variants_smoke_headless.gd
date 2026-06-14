extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_VARIANT_002_all_variants_smoke_headless"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	for variant_id in ["economy_trial", "military_trial", "hybrid"]:
		var scenario: Dictionary = {
			"variant": variant_id,
			"seed": 1,
			"map_width": 4,
			"map_height": 4,
			"players": ["p1"],
		}
		var state: GameState = runner.create_game(scenario)
		assert_eq(state.variant_id, variant_id)
		runner.submit_command(state, NoopCommand.new("p1", 0))
		state = runner.run_ticks(state, 3)

		var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
		assert_eq(int(snapshot["schema_version"]), 1)
		var hash: String = ReplayHash.compute_state_hash(state)
		assert_true(hash.begins_with("sha256:"))

		var tick_events: int = 0
		for event in state.event_log.events:
			if event.type == "tick_advanced":
				tick_events += 1
		assert_eq(tick_events, 3, "%s should emit tick_advanced events" % variant_id)
