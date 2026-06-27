extends SceneTree

## Full MRP stack verification orchestrator — fails fast on first failure.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")

const STEPS: Array[Dictionary] = [
	{"label": "tutorial_bot_smoke", "script": "res://sim/tools/run_tutorial_bot_smoke.gd", "args": ["--seed=42"], "timeout_s": 900},
	{"label": "bot_fast", "script": "res://sim/tools/run_bot_fast.gd", "args": [], "timeout_s": 600},
	{"label": "settler_human_screen", "script": "res://sim/tools/verify_human_understandable_screen.gd", "args": [], "timeout_s": 120},
	{"label": "military_spectator_screen", "script": "res://sim/tools/verify_military_spectator_screen.gd", "args": [], "timeout_s": 120},
	{"label": "military_mvp_screen", "script": "res://sim/tools/verify_military_mvp_screen.gd", "args": [], "timeout_s": 120},
	{"label": "military_mvp_flow", "script": "res://sim/tools/verify_military_mvp_flow.gd", "args": [], "timeout_s": 180},
	{"label": "military_tutorial_screen", "script": "res://sim/tools/verify_human_understandable_military_screen.gd", "args": [], "timeout_s": 120},
	{"label": "military_tutorial_flow", "script": "res://sim/tools/verify_human_understandable_military_flow.gd", "args": [], "timeout_s": 180},
	{"label": "tutorial_military_bot_smoke", "script": "res://sim/tools/run_tutorial_military_bot_smoke.gd", "args": [], "timeout_s": 120},
	{"label": "hybrid_observer_screen", "script": "res://sim/tools/verify_hybrid_observer_screen.gd", "args": [], "timeout_s": 120},
	{"label": "hybrid_mvp_screen", "script": "res://sim/tools/verify_hybrid_mvp_screen.gd", "args": [], "timeout_s": 120},
	{"label": "hybrid_tutorial_screen", "script": "res://sim/tools/verify_human_understandable_hybrid_screen.gd", "args": [], "timeout_s": 120},
	{"label": "hybrid_tutorial_flow", "script": "res://sim/tools/verify_human_understandable_hybrid_flow.gd", "args": [], "timeout_s": 180},
	{"label": "tutorial_hybrid_bot_smoke", "script": "res://sim/tools/run_tutorial_hybrid_bot_smoke.gd", "args": [], "timeout_s": 180},
	{"label": "hybrid_3d_screen", "script": "res://sim/tools/verify_hybrid_3d_spectator_screen.gd", "args": [], "timeout_s": 120},
	{"label": "hybrid_3d_flow", "script": "res://sim/tools/verify_hybrid_3d_spectator_flow.gd", "args": [], "timeout_s": 180},
	{"label": "hybrid_3d_bot_smoke", "script": "res://sim/tools/run_hybrid_bot_vs_bot_3d_smoke.gd", "args": [], "timeout_s": 300},
	{"label": "failure_modes", "script": "res://sim/tools/verify_failure_modes.gd", "args": [], "timeout_s": 60},
]


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var skip_tests: bool = false
	for arg in args:
		if arg == "--skip-full-tests":
			skip_tests = true

	if not skip_tests:
		var test_result: int = _run_godot_script(
			"res://sim/tools/run_tests.gd",
			[],
			1200,
			"full_test_suite",
		)
		if test_result != 0:
			_finish(1, "full_test_suite failed")

	for step in STEPS:
		var code: int = _run_godot_script(
			str(step["script"]),
			step.get("args", []),
			int(step.get("timeout_s", 120)),
			str(step["label"]),
		)
		if code != 0:
			_finish(1, "%s failed (exit %d)" % [str(step["label"]), code])

	print("MRP FULL VERIFICATION PASS")
	_finish(0, "")


func _run_godot_script(script_path: String, extra_args: Array, timeout_s: int, label: String) -> int:
	var godot: String = OS.get_executable_path()
	var project: String = ProjectSettings.globalize_path("res://")
	var output: Array = []
	var cmd_args: PackedStringArray = [
		"--headless", "--path", project, "--script", script_path,
	]
	for a in extra_args:
		cmd_args.append(str(a))
	print("MRP VERIFY: %s (timeout %ds)" % [label, timeout_s])
	var pid: int = OS.create_process(godot, cmd_args)
	if pid <= 0:
		print("MRP VERIFY FAIL: could not spawn %s" % label)
		return 1
	var elapsed: float = 0.0
	while elapsed < float(timeout_s):
		OS.delay_msec(200)
		elapsed += 0.2
		var exit_code: int = OS.get_process_exit_code(pid)
		if exit_code >= 0:
			if exit_code != 0:
				print("MRP VERIFY FAIL: %s exit=%d" % [label, exit_code])
			else:
				print("MRP VERIFY PASS: %s" % label)
			return exit_code
	OS.kill(pid)
	print("MRP VERIFY FAIL: %s timed out after %ds" % [label, timeout_s])
	return 124


func _finish(code: int, reason: String) -> void:
	if reason != "":
		print("MRP FULL VERIFICATION FAIL: %s" % reason)
	print("EXIT: %d (%s)" % [code, "pass" if code == 0 else "fail"])
	call_deferred("quit", code)
