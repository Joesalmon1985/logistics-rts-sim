extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_DIAG_004_csv_exports_generated"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 204,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 2)

	var output_dir: String = OS.get_environment("GODOT_USER_DATA_DIR") + "/econ_hardening_export_test"
	DirAccess.make_dir_recursive_absolute(output_dir)
	var results: Dictionary = EconomyExporter.export_all(state, output_dir)
	assert_true(bool(results.get("metrics", false)), "metrics CSV export must succeed")
	assert_true(bool(results.get("production", false)), "production status CSV export must succeed")
	assert_true(bool(results.get("delivery", false)), "delivery jobs CSV export must succeed")
	assert_true(bool(results.get("resources", false)), "resource status CSV export must succeed")

	var metrics_text: String = FileAccess.get_file_as_string(output_dir + "/metrics.csv")
	assert_true(metrics_text.contains("stall_reason"), "metrics CSV must contain stall columns")
	var production_text: String = FileAccess.get_file_as_string(output_dir + "/production_status.csv")
	assert_true(production_text.contains("building_type"), "production CSV must contain building rows")
