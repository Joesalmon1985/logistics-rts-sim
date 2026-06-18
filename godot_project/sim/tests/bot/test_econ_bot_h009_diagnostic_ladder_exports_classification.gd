extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_BOT_H009_diagnostic_ladder_exports_classification"


func _run_body() -> void:
	var output_dir: String = SimPaths.default_export_dir().path_join("run_02_8f_h009")
	DirAccess.make_dir_recursive_absolute(output_dir)
	var summary_path: String = output_dir.path_join("ladder_summary.json")
	var summary: Dictionary = EconomyBvbLadder.run_ladder(120, 0)
	summary["output_dir"] = output_dir
	assert_true(EconomyBvbLadder.write_ladder_summary(summary, summary_path), "write ladder summary")
	assert_true(FileAccess.file_exists(summary_path), "ladder_summary.json exists")

	var expected_profiles: Array = EconomyBvbSetup.ladder_profile_order()
	var rows: Array = summary.get("profiles", [])
	assert_eq(rows.size(), expected_profiles.size(), "all ladder profiles ran")

	for i in range(expected_profiles.size()):
		var row: Dictionary = rows[i]
		assert_eq(str(row.get("profile", "")), expected_profiles[i], "profile order")
		assert_true(row.has("pass"), "profile pass flag")
		assert_true(row.has("p1_buildings"), "p1_buildings")
		assert_true(row.has("p2_buildings"), "p2_buildings")
		assert_true(row.has("p1_last_noop_reason"), "p1_last_noop_reason")
		assert_true(row.has("p2_last_noop_reason"), "p2_last_noop_reason")

	var first_failing: Variant = summary.get("first_failing_rung", null)
	if first_failing != null:
		assert_true(str(first_failing) in expected_profiles, "first_failing_rung valid profile")
	assert_true(summary.has("classification"), "classification present")
	assert_true(str(summary.get("classification", "")) != "", "classification non-empty")
