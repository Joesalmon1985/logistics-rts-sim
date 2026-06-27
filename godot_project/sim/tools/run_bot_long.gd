extends SceneTree

## Long/quarantined bot suite — dual-bot expansion and experimental tests.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var failed: bool = false
	failed = failed or not _run_test("res://sim/tests/bot/test_econ_bot_002_dual_bot_expansion_metrics.gd")
	failed = failed or not _run_script("res://sim/tools/run_bot_smoke.gd", ["--smoke=004"], 300)
	failed = failed or not _run_script("res://sim/tools/run_tutorial_bot_smoke.gd", ["--seed=42"], 900)
	if failed:
		print("BOT LONG SUITE FAIL")
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
	else:
		print("BOT LONG SUITE PASS")
		print("EXIT: 0 (pass)")
		call_deferred("quit", 0)


func _run_test(script_path: String) -> bool:
	var script: Script = load(script_path)
	if script == null or not script.can_instantiate():
		print("FAIL load %s" % script_path)
		return false
	var inst: Object = script.new()
	inst.call("run")
	if not bool(inst.call("passed")):
		print("FAIL %s — %s" % [script_path, str(inst.call("failure_message"))])
		return false
	print("PASS %s" % script_path)
	return true


func _run_script(script_path: String, extra_args: PackedStringArray, timeout_s: int = 600) -> bool:
	var godot: String = OS.get_executable_path()
	var output: Array = []
	var args: PackedStringArray = ["--headless", "--path", ProjectSettings.globalize_path("res://"), "--script", script_path]
	args.append_array(extra_args)
	var wrapper: PackedStringArray = ["timeout", str(timeout_s) + "s", godot]
	wrapper.append_array(args)
	var exit_code: int = OS.execute("timeout", wrapper, output, true, false)
	if exit_code != 0:
		print("FAIL subprocess %s exit=%d" % [script_path, exit_code])
		return false
	print("PASS subprocess %s" % script_path)
	return true
