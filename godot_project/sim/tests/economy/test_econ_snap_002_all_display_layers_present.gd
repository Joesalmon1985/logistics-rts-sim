extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_SNAP_002_all_display_layers_present"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 101,
		"map_width": 6,
		"map_height": 6,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_has_keys(snapshot, ["display_layers"], "snapshot must include display_layers")
	var layers: Dictionary = snapshot["display_layers"]
	assert_has_keys(
		layers,
		[
			"terrain",
			"height_buildability",
			"resources",
			"tree_density",
			"ownership",
			"hex_grid_visible",
		],
		"all display layers must be present",
	)
	assert_true(typeof(layers["terrain"]) == TYPE_ARRAY)
	assert_true(typeof(layers["height_buildability"]) == TYPE_ARRAY)
	assert_true(layers["terrain"].size() > 0, "terrain layer must be populated from map state")
