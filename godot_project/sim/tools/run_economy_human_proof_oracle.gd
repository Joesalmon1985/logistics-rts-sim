extends SceneTree

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var plan_path: String = "res://sim/tests/fixtures/human_proof/oracle_one_building_p1.json"
	var export_path: String = ""
	var print_steps: bool = false
	for arg in args:
		if arg.begins_with("--plan="):
			plan_path = arg.substr("--plan=".length())
		elif arg.begins_with("--export="):
			export_path = arg.substr("--export=".length())
		elif arg == "--print-each-step":
			print_steps = true

	var oracle := EconomyHumanProofOracleRunner.new()
	oracle.print_each_step = print_steps
	oracle.reset()
	var loaded: Dictionary = oracle.load_plan_from_path(plan_path)
	if not bool(loaded.get("ok", false)):
		print("ORACLE FAIL: %s" % str(loaded.get("error", "load_failed")))
		print("EXIT: 1 (fail)")
		quit(1)
		return

	var summary: Dictionary = oracle.run_plan(loaded.get("plan", {}))
	var gates: Dictionary = summary.get("gates", {})
	var p1: Dictionary = gates.get("players", {}).get("p1", {})
	var buildings: int = int(p1.get("buildings", 0))
	print("tick=%d p1 b=%d r=%d d=%d" % [
		int(summary.get("tick", 0)),
		buildings,
		int(p1.get("roads", 0)),
		int(p1.get("deliveries", 0)),
	])
	print(EconomyHumanProofContract.format_gate_line(summary.get("state"), "p1"))
	print(EconomyHumanProofContract.format_gate_line(summary.get("state"), "p2"))

	if export_path != "":
		var replay_ok: bool = oracle.export_replay(export_path)
		var summary_path: String = export_path.replace(".json", "_summary.json")
		if not summary_path.ends_with("_summary.json"):
			summary_path = export_path + "_summary.json"
		var sidecar_ok: bool = oracle.export_summary_sidecar(summary_path)
		print("export replay=%s summary=%s" % [str(replay_ok), str(sidecar_ok)])

	if buildings < 1:
		print("ORACLE FAIL: p1 did not complete one building")
		print("EXIT: 1 (fail)")
		quit(1)
		return

	print("ORACLE PASS")
	print("EXIT: 0 (pass)")
	quit(0)
