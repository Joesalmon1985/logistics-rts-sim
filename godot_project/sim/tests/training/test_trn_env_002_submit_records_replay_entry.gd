extends "res://sim/tests/test_case.gd"

const TrainingEnvServerScript := preload("res://sim/tools/training_env_server.gd")


func get_name() -> String:
	return "test_TRN_ENV_002_submit_records_replay_entry"


func _run_body() -> void:
	SimBootstrap.load_all()
	var server := TrainingEnvServerScript.new()
	var reset: Dictionary = server.handle_request({
		"op": "reset",
		"scenario_path": "scenarios/economy/tiny_success_map.json",
		"seed": 7,
		"actor_id": "p1",
	})
	assert_true(reset.get("ok", false), "reset ok")

	var snap: Dictionary = server.handle_request({"op": "snapshot"})
	var available: Array = snap.get("snapshot", {}).get("available_commands", [])
	assert_true(available.size() > 0, "has legal commands")

	var entry: Dictionary = available[0]
	var submit: Dictionary = server.handle_request({
		"op": "submit",
		"command": {
			"type": str(entry.get("type", "")),
			"player_id": str(entry.get("actor_id", "p1")),
			"tick": 0,
			"payload": entry.get("payload", {}),
		},
	})
	assert_true(submit.get("ok", false), "submit ok")

	var recorder: ReplayRecorder = server.get_recorder()
	assert_eq(recorder.get_command_entries().size(), 1, "one replay entry")
	var recorded: Dictionary = recorder.get_command_entries()[0]
	assert_eq(str(recorded.get("command", {}).get("type", "")), str(entry.get("type", "")), "command type recorded")
