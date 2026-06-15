extends SceneTree

## Verifies that test/smoke runners report failure correctly (exit non-zero, no false PASS).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const DELIBERATE_FAIL_SCRIPT := preload(
	"res://sim/tests/integrity/test_run01_integrity_deliberate_fail.gd"
)

const DEFAULT_GODOT := "/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64"


func _initialize() -> void:
	BootstrapScript.load_all()
	var failures: Array[String] = []
	print("RUN_01 failure-mode integrity checks")

	failures.append_array(_check_deliberate_test_fails_in_process())
	failures.append_array(_check_subprocess_invalid_variant())
	failures.append_array(_check_subprocess_hash_mismatch())
	failures.append_array(_check_subprocess_invalid_ticks())
	failures.append_array(_check_subprocess_economy_smoke_invalid())
	failures.append_array(_check_subprocess_military_smoke_invalid())
	failures.append_array(_check_subprocess_military_scenario_invalid())
	failures.append_array(_check_subprocess_bot_smoke_invalid())
	failures.append_array(_check_subprocess_viewer_smoke_invalid())
	failures.append_array(_check_subprocess_replay_verify_invalid())

	print("—")
	if failures.is_empty():
		print("INTEGRITY PASS: all failure-mode checks behaved correctly")
		call_deferred("quit", 0)
	else:
		for msg in failures:
			print("INTEGRITY FAIL: %s" % msg)
		print("INTEGRITY SUMMARY: %d failure-mode check(s) did not fail as expected" % failures.size())
		call_deferred("quit", 1)


func _check_deliberate_test_fails_in_process() -> Array[String]:
	var failures: Array[String] = []
	var test_instance: Object = DELIBERATE_FAIL_SCRIPT.new()
	test_instance.call("run")
	var completed: bool = bool(test_instance.call("run_completed"))
	var ok: bool = bool(test_instance.call("passed"))
	if not completed:
		failures.append("deliberate-fail test did not complete run()")
	elif ok:
		failures.append("deliberate-fail test reported PASS (expected FAIL)")
	else:
		print("INTEGRITY OK: in-process deliberate-fail test reported FAIL")
	return failures


func _check_subprocess_invalid_variant() -> Array[String]:
	var result: Dictionary = _run_godot_script(
		"res://sim/tools/run_smoke.gd",
		["--variant=not_a_variant", "--ticks=10", "--seed=1"],
	)
	return _expect_nonzero_exit(result, "invalid variant smoke")


func _check_subprocess_hash_mismatch() -> Array[String]:
	var result: Dictionary = _run_godot_script(
		"res://sim/tools/run_smoke.gd",
		[
			"--variant=economy_trial",
			"--ticks=10",
			"--seed=1",
			"--expected-hash=sha256:definitely_wrong_hash_value",
		],
	)
	return _expect_nonzero_exit(result, "hash mismatch smoke")


func _check_subprocess_invalid_ticks() -> Array[String]:
	var result: Dictionary = _run_godot_script(
		"res://sim/tools/run_smoke.gd",
		["--variant=economy_trial", "--ticks=0", "--seed=1", "--no-hash-check"],
	)
	return _expect_nonzero_exit(result, "invalid ticks smoke")


func _check_subprocess_economy_smoke_invalid() -> Array[String]:
	var result: Dictionary = _run_godot_script(
		"res://sim/tools/run_economy_smoke.gd",
		["--smoke=999"],
	)
	return _expect_nonzero_exit(result, "invalid economy smoke id")


func _check_subprocess_military_smoke_invalid() -> Array[String]:
	var result: Dictionary = _run_godot_script(
		"res://sim/tools/run_military_smoke.gd",
		["--smoke=999"],
	)
	return _expect_nonzero_exit(result, "invalid military smoke id")


func _check_subprocess_military_scenario_invalid() -> Array[String]:
	var result: Dictionary = _run_godot_script(
		"res://sim/tools/run_smoke.gd",
		[
			"--variant=military_trial",
			"--scenario=scenarios/military/does_not_exist.json",
			"--ticks=10",
			"--seed=1",
			"--no-hash-check",
		],
	)
	return _expect_nonzero_exit(result, "invalid military scenario")


func _check_subprocess_bot_smoke_invalid() -> Array[String]:
	var result: Dictionary = _run_godot_script(
		"res://sim/tools/run_bot_smoke.gd",
		["--smoke=999"],
	)
	return _expect_nonzero_exit(result, "invalid bot smoke id")


func _check_subprocess_viewer_smoke_invalid() -> Array[String]:
	var result: Dictionary = _run_godot_script(
		"res://sim/tools/run_viewer_smoke.gd",
		["--smoke=999"],
	)
	return _expect_nonzero_exit(result, "invalid viewer smoke id")


func _check_subprocess_replay_verify_invalid() -> Array[String]:
	var missing_replay: String = SimPaths.default_export_file("run04_nonexistent_replay.json")
	var result: Dictionary = _run_godot_script(
		"res://sim/tools/run_replay.gd",
		["--mode=verify", "--replay=%s" % missing_replay],
	)
	return _expect_nonzero_exit(result, "invalid replay verify")


func _resolve_godot_binary() -> String:
	var godot_bin: String = OS.get_environment("GODOT")
	if godot_bin.is_empty():
		godot_bin = OS.get_executable_path()
	if godot_bin.is_empty():
		godot_bin = DEFAULT_GODOT
	return godot_bin


func _run_godot_script(script_res: String, user_args: Array) -> Dictionary:
	var godot_bin: String = _resolve_godot_binary()
	if godot_bin.is_empty():
		return {"exit_code": 125, "output": "Godot binary path is empty"}
	if not FileAccess.file_exists(godot_bin):
		return {"exit_code": 125, "output": "Godot binary not found: %s" % godot_bin}

	var project_dir: String = ProjectSettings.globalize_path("res://")
	var args: PackedStringArray = PackedStringArray([
		"--headless",
		"--path", project_dir,
		"--script", script_res,
		"--",
	])
	for arg in user_args:
		args.append(str(arg))

	var output: Array = []
	var exit_code: int = OS.execute(godot_bin, args, output, true, false)
	return {"exit_code": exit_code, "output": "\n".join(output)}


func _expect_nonzero_exit(result: Dictionary, label: String) -> Array[String]:
	var exit_code: int = int(result.get("exit_code", -999))
	var output: String = str(result.get("output", ""))
	if exit_code == 0:
		return ["subprocess %s exited 0 (expected non-zero). output tail: %s" % [label, _tail(output)]]
	if exit_code == 124:
		return ["subprocess %s timed out (exit 124) — failure path not verified" % label]
	if exit_code == 125:
		return ["subprocess %s could not run command (exit 125). output tail: %s" % [label, _tail(output)]]
	if exit_code == 127:
		return ["subprocess %s could not run (exit 127) — executable not found. output tail: %s" % [label, _tail(output)]]
	print("INTEGRITY OK: subprocess %s exited %d" % [label, exit_code])
	return []


func _tail(text: String, max_lines: int = 8) -> String:
	var lines: PackedStringArray = text.split("\n", false)
	if lines.size() <= max_lines:
		return text
	return "\n".join(lines.slice(lines.size() - max_lines))
