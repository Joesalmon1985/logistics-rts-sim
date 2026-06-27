extends SceneTree

## Fast bot suite — must complete quickly; excludes quarantined dual-bot tests.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var failed: bool = false
	failed = failed or not _run_script("res://sim/tools/run_bot_smoke.gd", ["--smoke=001"])
	failed = failed or not _run_script("res://sim/tools/run_bot_smoke.gd", ["--smoke=002"])
	failed = failed or not _run_script("res://sim/tools/run_tutorial_bot_smoke.gd", ["--seed=42"])
	if failed:
		print("BOT FAST SUITE FAIL")
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
	else:
		print("BOT FAST SUITE PASS")
		print("EXIT: 0 (pass)")
		call_deferred("quit", 0)


func _run_script(script_path: String, extra_args: PackedStringArray) -> bool:
	var godot: String = OS.get_executable_path()
	var output: Array = []
	var args: PackedStringArray = ["--headless", "--path", ProjectSettings.globalize_path("res://"), "--script", script_path]
	args.append_array(extra_args)
	var exit_code: int = OS.execute(godot, args, output, true, false)
	var text: String = "\n".join(output)
	if exit_code != 0:
		print("FAIL subprocess %s exit=%d\n%s" % [script_path, exit_code, text])
		return false
	print("PASS subprocess %s" % script_path)
	return true
