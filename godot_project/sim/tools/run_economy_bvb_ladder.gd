extends SceneTree

## Minimal ablation ladder runner for ECON_BOT_002 diagnostics (RUN_02.8F).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const LADDER_SCRIPT_PATH: String = "res://sim/economy/economy_bvb_ladder.gd"

const OUTPUT_SUBDIR: String = "training_runs/diagnostics/run_02_8f_economy_bvb"


func _initialize() -> void:
	BootstrapScript.load_all()
	var ladder_script: Script = load(LADDER_SCRIPT_PATH) as Script
	if ladder_script == null:
		print("LADDER FAIL: cannot load %s" % LADDER_SCRIPT_PATH)
		print("EXIT: 1 (fail)")
		quit(1)
		return

	var max_ticks: int = 800
	var wall_budget_ms: int = 0
	var output_dir: String = _resolve_output_dir()
	var single_profile: String = ""
	var args: PackedStringArray = OS.get_cmdline_user_args()
	for arg in args:
		if arg.begins_with("--max-ticks="):
			max_ticks = int(arg.substr("--max-ticks=".length()))
		elif arg.begins_with("--max-wall-ms="):
			wall_budget_ms = int(arg.substr("--max-wall-ms=".length()))
		elif arg.begins_with("--output-dir="):
			output_dir = arg.substr("--output-dir=".length())
		elif arg.begins_with("--profile="):
			single_profile = arg.substr("--profile=".length())

	DirAccess.make_dir_recursive_absolute(output_dir)
	var summary: Dictionary
	if single_profile != "":
		var row: Dictionary = ladder_script.call("run_profile", single_profile, max_ticks)
		var pass_row: bool = bool(row.get("pass", false))
		var classification: String = "all_passed"
		if not pass_row:
			classification = str(ladder_script.call("_classify_failure", single_profile, row, "", true))
		summary = {
			"profiles": [row],
			"first_failing_rung": null if pass_row else single_profile,
			"classification": classification,
		}
	else:
		summary = ladder_script.call("run_ladder", max_ticks, wall_budget_ms)
	summary["max_ticks"] = max_ticks
	summary["output_dir"] = output_dir

	var summary_path: String = output_dir.path_join("ladder_summary.json")
	if not bool(ladder_script.call("write_ladder_summary", summary, summary_path)):
		print("LADDER FAIL: cannot write %s" % summary_path)
		print("EXIT: 1 (fail)")
		quit(1)
		return

	for row in summary.get("profiles", []):
		if typeof(row) != TYPE_DICTIONARY:
			continue
		print(
			"%s pass=%s p1_b=%d p2_b=%d noop_p1=%s noop_p2=%s"
			% [
				str(row.get("profile", "?")),
				str(row.get("pass", false)),
				int(row.get("p1_buildings", 0)),
				int(row.get("p2_buildings", 0)),
				str(row.get("p1_last_noop_reason", "")),
				str(row.get("p2_last_noop_reason", "")),
			]
		)
	print(
		"first_failing_rung=%s classification=%s"
		% [str(summary.get("first_failing_rung", "null")), str(summary.get("classification", ""))]
	)
	print("LADDER SUMMARY: %s" % summary_path)
	if summary.get("first_failing_rung") == null:
		print("LADDER PASS")
		print("EXIT: 0 (pass)")
		quit(0)
		return
	print("LADDER DIAGNOSTIC: first failure at %s" % str(summary.get("first_failing_rung")))
	print("EXIT: 2 (diagnostic)")
	quit(2)


func _resolve_output_dir() -> String:
	return ProjectSettings.globalize_path("res://").path_join("..").path_join(OUTPUT_SUBDIR)
