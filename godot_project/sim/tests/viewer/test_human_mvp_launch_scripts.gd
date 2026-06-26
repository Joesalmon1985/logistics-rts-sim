extends TestCase

## HUMAN-MVP-LAUNCH-001 — run_game.sh exists and errors without Godot.


func get_name() -> String:
	return "test_human_mvp_launch_scripts"


func _run_body() -> void:
	var root: String = ProjectSettings.globalize_path("res://").path_join("..")
	var run_game: String = root.path_join("scripts/run_game.sh")
	var desktop: String = root.path_join("scripts/create_desktop_launcher.sh")
	assert_true(FileAccess.file_exists(run_game), "run_game.sh missing")
	assert_true(FileAccess.file_exists(desktop), "create_desktop_launcher.sh missing")
	var run_bytes: PackedByteArray = FileAccess.get_file_as_bytes(run_game)
	assert_true(run_bytes.size() > 20, "run_game.sh empty")
