extends SceneTree

## Top-level QA gate: real-time tutorials and 3D hybrid spectator repair.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/realtime_tutorials_review_util.gd")

var _failures: Array = []


func _initialize() -> void:
	BootstrapScript.load_all()
	ReviewUtil.ensure_output_dir()
	print("=== REALTIME TUTORIALS AND 3D QA ===")
	_run_capture()
	_run_tests("realtime", 420)
	_run_tests("tutorial_completable", 420)
	_run_tests("3d_spectator", 420)
	_run_bot_smoke()
	_check_screenshots()
	_write_summary()
	if _failures.is_empty():
		print("REALTIME TUTORIALS AND 3D QA PASS")
		call_deferred("quit", 0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("REALTIME TUTORIALS AND 3D QA FAIL")
		call_deferred("quit", 1)


func _run_capture() -> void:
	var code: int = _run_godot_script("res://sim/tools/capture_realtime_tutorials_and_3d_screenshots.gd", 300)
	if code != 0:
		_failures.append("screenshot capture exit %d" % code)


func _run_tests(filter: String, timeout_sec: int) -> void:
	OS.set_environment("TEST_FILTER", filter)
	var code: int = _run_godot_script("res://sim/tools/run_tests.gd", timeout_sec)
	if code != 0:
		_failures.append("%s tests exit %d" % [filter, code])


func _run_bot_smoke() -> void:
	var code: int = _run_godot_script("res://sim/tools/run_hybrid_bot_vs_bot_3d_smoke.gd", 180)
	if code != 0:
		_failures.append("hybrid bot-vs-bot 3d smoke exit %d" % code)


func _check_screenshots() -> void:
	var review_path: String = ReviewUtil.review_json_path()
	if not FileAccess.file_exists(review_path):
		_failures.append("missing screenshot review json")
		return
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(review_path))
	if typeof(parsed) != TYPE_DICTIONARY:
		_failures.append("invalid screenshot review json")
		return
	for entry in parsed.get("entries", []):
		if not bool(entry.get("pass", false)):
			_failures.append("screenshot fail %s: %s" % [entry.get("milestone", ""), entry.get("failure_reasons", [])])


func _run_godot_script(script_path: String, timeout_sec: int) -> int:
	var godot: String = OS.get_environment("GODOT") if OS.has_environment("GODOT") else "godot"
	var user_data: String = OS.get_environment("GODOT_USER_DATA_DIR") if OS.has_environment("GODOT_USER_DATA_DIR") else ""
	var filter: String = OS.get_environment("TEST_FILTER") if OS.has_environment("TEST_FILTER") else ""
	var repo_root: String = ProjectSettings.globalize_path("res://").path_join("..")
	var shell_cmd: String = (
		"cd %s && export GODOT=%s && export GODOT_USER_DATA_DIR=%s && export TEST_FILTER=%s && "
		% [repo_root, godot, user_data, filter]
		+ "timeout %ds %s --headless --path godot_project --script %s"
		% [timeout_sec, godot, script_path]
	)
	var output: Array = []
	return OS.execute("bash", ["-c", shell_cmd], output, true, false)


func _write_summary() -> void:
	var out_dir: String = ProjectSettings.globalize_path("res://").path_join("../docs/audit")
	var summary: Dictionary = {
		"failures": _failures,
		"display_available": OS.has_environment("DISPLAY") and OS.get_environment("DISPLAY") != "",
	}
	var path: String = out_dir.path_join("RUN_REALTIME_TUTORIALS_AND_3D_QA_SUMMARY.json")
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(summary, "\t"))
