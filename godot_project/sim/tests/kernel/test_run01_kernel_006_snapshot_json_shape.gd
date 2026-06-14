extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_KERNEL_006_snapshot_json_shape"


func run() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 11,
		"map_width": 4,
		"map_height": 4,
		"players": ["p1"],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	runner.tick(state)

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_has_keys(
		snapshot,
		[
			"schema_version",
			"variant",
			"tick",
			"seed",
			"rng_state",
			"map",
			"roads",
			"road_nodes",
			"buildings",
			"entities",
			"diagnostics",
			"players",
		],
	)
	assert_eq(int(snapshot["schema_version"]), 1)
	assert_eq(str(snapshot["variant"]), "military_trial")
	assert_true(typeof(snapshot["map"]) == TYPE_DICTIONARY)

	var json_text: String = JSON.stringify(snapshot)
	var parsed: Variant = JSON.parse_string(json_text)
	assert_true(typeof(parsed) == TYPE_DICTIONARY, "snapshot must be JSON-compatible")
