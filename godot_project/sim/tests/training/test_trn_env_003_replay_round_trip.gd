extends "res://sim/tests/test_case.gd"

const TrainingEnvServerScript := preload("res://sim/tools/training_env_server.gd")


func get_name() -> String:
	return "test_TRN_ENV_003_replay_round_trip"


func _run_body() -> void:
	SimBootstrap.load_all()
	var server := TrainingEnvServerScript.new()
	var rollout: Dictionary = server.handle_request({
		"op": "rollout_stub",
		"scenario_path": "scenarios/economy/tiny_success_map.json",
		"seed": 99,
		"policy_seed": 7,
		"max_ticks": 50,
		"actor_id": "p1",
	})
	assert_true(rollout.get("ok", false), "rollout ok")
	var live_hash: String = str(rollout.get("hash", ""))

	var replay_path: String = SimPaths.default_export_file("trn_env_003_replay.json")
	var save: Dictionary = server.handle_request({"op": "save_replay", "path": replay_path})
	assert_true(save.get("ok", false), "save replay")

	var verify: Dictionary = server.handle_request({"op": "verify_replay", "path": replay_path})
	assert_true(verify.get("ok", false), "verify replay")
	var verify_data: Dictionary = verify.get("verify", {})
	assert_eq(str(verify_data.get("final_hash", "")), live_hash, "hash round trip")
