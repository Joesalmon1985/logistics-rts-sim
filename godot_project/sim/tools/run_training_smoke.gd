extends SceneTree

## RUN_06 training smoke runner — TRN-SMOKE-001/002.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const TrainingEnvServerScript := preload("res://sim/tools/training_env_server.gd")

const VALID_SMOKES: Array[String] = ["all", "001", "002", "TRN-SMOKE-001", "TRN-SMOKE-002"]


func _initialize() -> void:
	BootstrapScript.load_all()
	var smoke_id: String = "all"
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--smoke="):
			smoke_id = arg.substr("--smoke=".length())
	smoke_id = smoke_id.replace("TRN-SMOKE-", "")

	if smoke_id not in VALID_SMOKES:
		print("TRAINING SMOKE FAIL: unknown smoke id '%s'" % smoke_id)
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
		return

	var failed: bool = false
	if smoke_id == "all" or smoke_id == "001":
		failed = failed or not _run_smoke_001()
	if smoke_id == "all" or smoke_id == "002":
		failed = failed or not _run_smoke_002()

	if failed:
		print("TRAINING SMOKE FAIL")
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
	else:
		print("TRAINING SMOKE PASS")
		print("EXIT: 0 (pass)")
		call_deferred("quit", 0)


func _run_smoke_001() -> bool:
	var logs: Array = []
	for _i in range(10):
		var server := TrainingEnvServerScript.new()
		var rollout: Dictionary = server.handle_request({
			"op": "rollout_stub",
			"scenario_path": "scenarios/economy/tiny_success_map.json",
			"seed": 42,
			"policy_seed": 7,
			"max_ticks": 100,
			"actor_id": "p1",
		})
		if not rollout.get("ok", false):
			print("TRN-SMOKE-001 FAIL: rollout %d" % _i)
			return false
		logs.append(JSON.stringify(rollout.get("command_log", [])))

	for i in range(1, logs.size()):
		if logs[i] != logs[0]:
			print("TRN-SMOKE-001 FAIL: command logs differ at run %d" % i)
			return false

	print("TRN-SMOKE-001 PASS: 10 deterministic rollouts identical")
	return true


func _run_smoke_002() -> bool:
	var server := TrainingEnvServerScript.new()
	var rollout: Dictionary = server.handle_request({
		"op": "rollout_stub",
		"scenario_path": "scenarios/economy/tiny_success_map.json",
		"seed": 42,
		"policy_seed": 7,
		"max_ticks": 100,
		"actor_id": "p1",
	})
	if not rollout.get("ok", false):
		print("TRN-SMOKE-002 FAIL: rollout")
		return false

	var live_hash: String = str(rollout.get("hash", ""))
	var replay_path: String = SimPaths.default_export_file("trn_smoke_002_replay.json")
	var save: Dictionary = server.handle_request({"op": "save_replay", "path": replay_path})
	if not save.get("ok", false):
		print("TRN-SMOKE-002 FAIL: save replay")
		return false

	var verify: Dictionary = server.handle_request({"op": "verify_replay", "path": replay_path})
	if not verify.get("ok", false):
		print("TRN-SMOKE-002 FAIL: verify replay")
		return false
	var final_hash: String = str(verify.get("verify", {}).get("final_hash", ""))
	if final_hash != live_hash:
		print("TRN-SMOKE-002 FAIL: hash mismatch live=%s verify=%s" % [live_hash, final_hash])
		return false

	print("TRN-SMOKE-002 PASS: replay hash match")
	return true
