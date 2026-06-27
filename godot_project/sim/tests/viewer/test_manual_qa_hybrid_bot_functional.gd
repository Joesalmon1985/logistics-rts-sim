extends TestCase


func get_name() -> String:
	return "test_manual_qa_hybrid_bot_functional"


func _run_body() -> void:
	var root: String = ProjectSettings.globalize_path("res://").path_join("..")
	var godot_bin: String = OS.get_environment("GODOT") if OS.has_environment("GODOT") else "godot"
	var output: Array = []
	var code: int = OS.execute(
		godot_bin,
		PackedStringArray([
			"--headless", "--path", root.path_join("godot_project"),
			"--script", "res://sim/tools/run_tutorial_hybrid_bot_smoke.gd",
		]),
		output,
		true,
		false
	)
	assert_true(code == 0, "hybrid bot smoke exit %d" % code)
	var summary_path: String = root.path_join("docs/audit/bot_runs/manual_playability/hybrid_bot_summary.json")
	assert_true(FileAccess.file_exists(summary_path), "hybrid bot summary missing")
