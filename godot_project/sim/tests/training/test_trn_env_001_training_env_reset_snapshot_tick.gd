extends "res://sim/tests/test_case.gd"

const TrainingEnvServerScript := preload("res://sim/tools/training_env_server.gd")


func get_name() -> String:
	return "test_TRN_ENV_001_training_env_reset_snapshot_tick"


func _run_body() -> void:
	SimBootstrap.load_all()
	var server := TrainingEnvServerScript.new()
	var reset: Dictionary = server.handle_request({
		"op": "reset",
		"scenario_path": "scenarios/economy/tiny_success_map.json",
		"seed": 42,
		"actor_id": "p1",
	})
	assert_true(reset.get("ok", false), "reset ok")
	assert_eq(str(reset.get("variant", "")), "economy_trial", "economy variant")

	var snap: Dictionary = server.handle_request({"op": "snapshot"})
	assert_true(snap.get("ok", false), "snapshot ok")
	var snapshot: Dictionary = snap.get("snapshot", {})
	assert_eq(int(snapshot.get("schema_version", 0)), 1, "schema version")
	assert_true(snapshot.has("available_commands"), "availability present")

	var tick_result: Dictionary = server.handle_request({"op": "tick"})
	assert_true(tick_result.get("ok", false), "tick ok")
	assert_eq(int(tick_result.get("tick", -1)), 1, "tick advanced")
