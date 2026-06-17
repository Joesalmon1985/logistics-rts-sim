extends SceneTree

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const FIXTURE_PLAN := "res://sim/tests/fixtures/human_proof/oracle_one_building_p1.json"


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var smoke_id: String = "all"
	for arg in args:
		if arg.begins_with("--smoke="):
			smoke_id = arg.substr("--smoke=".length())

	var valid: Array[String] = ["all", "001", "002", "003"]
	if smoke_id not in valid:
		print("HUMAN PROOF SMOKE FAIL: unknown smoke id '%s'" % smoke_id)
		print("EXIT: 1 (fail)")
		quit(1)
		return

	var failed: bool = false
	if smoke_id == "all" or smoke_id == "001":
		failed = failed or not _run_smoke_001()
	if smoke_id == "all" or smoke_id == "002":
		failed = failed or not _run_smoke_002()
	if smoke_id == "all" or smoke_id == "003":
		failed = failed or not _run_smoke_003()

	if failed:
		print("HUMAN PROOF SMOKE FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
	else:
		print("HUMAN PROOF SMOKE PASS")
		print("EXIT: 0 (pass)")
		quit(0)


func _run_smoke_001() -> bool:
	var state: GameState = EconomyHumanProofContract.create_proof_state()
	if state == null:
		print("HUMAN-PROOF-SMOKE-001 FAIL: no state")
		return false
	var gates: Dictionary = EconomyHumanProofContract.evaluate_match_gates(state)
	if bool(gates.get("pass", false)):
		print("HUMAN-PROOF-SMOKE-001 FAIL: gates pass at tick 0")
		return false
	if EconomyState.all_buildings(state).size() < 2:
		print("HUMAN-PROOF-SMOKE-001 FAIL: missing HQ buildings")
		return false
	print("HUMAN-PROOF-SMOKE-001 PASS: proof start non-empty, gates fail initially")
	return true


func _run_smoke_002() -> bool:
	var oracle := EconomyHumanProofOracleRunner.new()
	oracle.reset()
	var path: String = ProjectSettings.globalize_path(FIXTURE_PLAN)
	var loaded: Dictionary = oracle.load_plan_from_path(path)
	if not bool(loaded.get("ok", false)):
		print("HUMAN-PROOF-SMOKE-002 FAIL: plan load")
		return false
	var summary: Dictionary = oracle.run_plan(loaded.get("plan", {}))
	var p1: Dictionary = summary.get("gates", {}).get("players", {}).get("p1", {})
	if int(p1.get("buildings", 0)) < 1:
		print("HUMAN-PROOF-SMOKE-002 FAIL: one-building path")
		return false
	print("HUMAN-PROOF-SMOKE-002 PASS: oracle one-building fixture")
	return true


func _run_smoke_003() -> bool:
	var oracle := EconomyHumanProofOracleRunner.new()
	oracle.reset()
	var path: String = ProjectSettings.globalize_path(FIXTURE_PLAN)
	var loaded: Dictionary = oracle.load_plan_from_path(path)
	oracle.run_plan(loaded.get("plan", {}))
	var export_path: String = SimPaths.default_export_file("human_proof_smoke_replay.json")
	if not oracle.export_replay(export_path):
		print("HUMAN-PROOF-SMOKE-003 FAIL: export")
		return false
	var replay: Dictionary = EconomyHumanProofReplay.run_replay_file(export_path)
	if not bool(replay.get("ok", false)):
		print("HUMAN-PROOF-SMOKE-003 FAIL: replay %s" % str(replay.get("error", "")))
		return false
	print("HUMAN-PROOF-SMOKE-003 PASS: replay round trip")
	return true
