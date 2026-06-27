extends SceneTree

## Unified manual playability QA gate.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const CaptureScript := preload("res://sim/tools/capture_actual_playability_screenshots.gd")
const ReviewUtil := preload("res://sim/tools/manual_playability_review_util.gd")

var _failures: Array = []


func _initialize() -> void:
	BootstrapScript.load_all()
	ReviewUtil.ensure_output_dir()
	print("=== MANUAL PLAYABILITY QA ===")
	print("CAPTURE METHOD: %s" % _capture_method())
	_run_capture_phase()
	_run_manual_qa_tests()
	_run_bot_smokes()
	_write_summary()
	if _failures.is_empty():
		print("MANUAL PLAYABILITY QA PASS")
		call_deferred("quit", 0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("MANUAL PLAYABILITY QA FAIL")
		call_deferred("quit", 1)


func _capture_method() -> String:
	if OS.has_environment("DISPLAY") and OS.get_environment("DISPLAY") != "":
		return "real_desktop_window"
	return "in_engine_menu_route_fallback"


func _run_capture_phase() -> void:
	var code: int = _run_godot_script("res://sim/tools/capture_actual_playability_screenshots.gd", 240)
	if code != 0:
		_failures.append("screenshot capture exit %d" % code)
	var review_path: String = ReviewUtil.output_dir().path_join("manual_playability_visual_review.json")
	if not FileAccess.file_exists(review_path):
		_failures.append("missing visual review json")


func _run_manual_qa_tests() -> void:
	var env: Dictionary = OS.get_environment()
	env["TEST_FILTER"] = "manual_qa"
	OS.set_environment("TEST_FILTER", "manual_qa")
	var code: int = _run_godot_script("res://sim/tools/run_tests.gd", 480)
	if code != 0:
		_failures.append("manual_qa tests exit %d" % code)


func _run_bot_smokes() -> void:
	var scripts: Array = [
		"res://sim/tools/run_tutorial_bot_smoke.gd",
		"res://sim/tools/run_tutorial_military_bot_smoke.gd",
		"res://sim/tools/run_tutorial_hybrid_bot_smoke.gd",
		"res://sim/tools/run_hybrid_bot_vs_bot_3d_smoke.gd",
	]
	for script_path in scripts:
		var code: int = _run_godot_script(script_path, 120)
		if code != 0:
			_failures.append("bot smoke failed %s exit %d" % [script_path, code])


func _run_godot_script(script_path: String, timeout_sec: int) -> int:
	var godot: String = OS.get_environment("GODOT") if OS.has_environment("GODOT") else "godot"
	var proc_args: PackedStringArray = PackedStringArray([
		"%ds" % timeout_sec, godot, "--headless", "--path", "godot_project", "--script", script_path,
	])
	var output: Array = []
	return OS.execute("timeout", proc_args, output, true)


func _write_summary() -> void:
	var out_dir: String = ProjectSettings.globalize_path("res://").path_join("../docs/audit")
	DirAccess.make_dir_recursive_absolute(out_dir)
	var summary: Dictionary = {
		"capture_method": _capture_method(),
		"failures": _failures,
		"display_available": OS.has_environment("DISPLAY") and OS.get_environment("DISPLAY") != "",
	}
	var path: String = out_dir.path_join("RUN_MANUAL_PLAYABILITY_QA_SUMMARY.json")
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(summary, "\t"))
