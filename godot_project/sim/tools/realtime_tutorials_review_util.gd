class_name RealtimeTutorialsReviewUtil
extends RefCounted

## Screenshot review for real-time tutorials and 3D spectator QA.


static func output_dir() -> String:
	return ProjectSettings.globalize_path("res://").path_join("../docs/audit/screenshots/realtime_tutorials_and_3d")


static func review_json_path() -> String:
	return output_dir().path_join("realtime_tutorials_visual_review.json")


static func ensure_output_dir() -> void:
	DirAccess.make_dir_recursive_absolute(output_dir())


static func review_entry(
	mode: String,
	milestone: String,
	screenshot_path: String,
	tick: int,
	simulation_state: String,
	extra: Dictionary = {},
) -> Dictionary:
	var passed: bool = true
	var reasons: PackedStringArray = PackedStringArray()
	if tick <= 0 and milestone.contains("launch_running"):
		passed = false
		reasons.append("tick not advancing at launch")
	if simulation_state.to_lower().contains("paused") and milestone.contains("launch_running"):
		passed = false
		reasons.append("starts paused")
	if not FileAccess.file_exists(screenshot_path):
		passed = false
		reasons.append("missing screenshot")
	var entry: Dictionary = {
		"screenshot_path": screenshot_path,
		"mode": mode,
		"milestone": milestone,
		"tick": tick,
		"simulation_state": simulation_state,
		"pass": passed,
		"failure_reasons": reasons,
		"visible_resources": extra.get("visible_resources", ""),
		"visible_buildings": extra.get("visible_buildings", 0),
		"visible_units": extra.get("visible_units", 0),
		"visible_hud_text": extra.get("visible_hud_text", ""),
		"player_can_tell_what_to_do_next": extra.get("player_can_tell_what_to_do_next", false),
	}
	return entry
