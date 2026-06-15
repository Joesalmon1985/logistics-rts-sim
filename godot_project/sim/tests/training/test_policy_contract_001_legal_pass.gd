extends "res://sim/tests/test_case.gd"

## POLICY-CONTRACT-001/002 — legal enumeration includes pass; observation export valid.


func get_name() -> String:
	return "test_POLICY_CONTRACT_001_legal_commands_include_pass"


func _run_body() -> void:
	SimBootstrap.load_all()
	var server := TrainingEnvServer.new()
	var reset: Dictionary = server.handle_request({
		"op": "reset",
		"scenario_path": "scenarios/economy/eval_build_obvious.json",
		"seed": 11,
		"actor_id": "p1",
	})
	assert_true(reset.get("ok", false), "reset ok")

	var legal: Array = reset.get("legal_commands", [])
	assert_true(legal.size() >= 1, "non-empty legal list")
	var has_pass: bool = false
	for entry in legal:
		assert_has_keys(entry, ["id", "type", "actor_id", "payload", "source"])
		if str(entry.get("type", "")) == "NoopCommand":
			has_pass = true
	assert_true(has_pass, "pass/noop in legal list")

	var obs: Dictionary = reset.get("policy_observation", {})
	assert_eq(int(obs.get("schema_version", 0)), 1, "observation schema version")
	assert_false(obs.has("error"), "observation export ok")
	assert_eq(int(obs.get("map_width", 0)), 16, "fixed map width")
	assert_true(obs.get("board_cells", []).size() > 0, "board cells present")
	assert_eq(obs.get("player_state", []).size(), 9, "player vector size")

	var legal_resp: Dictionary = server.handle_request({"op": "legal_commands"})
	assert_true(legal_resp.get("ok", false), "legal_commands op")
	assert_true(legal_resp.get("legal_commands", []).size() >= 1, "legal_commands non-empty")

	var pass_entry: Dictionary = legal[0]
	for entry in legal:
		if str(entry.get("type", "")) == "NoopCommand":
			pass_entry = entry
			break
	var submit: Dictionary = server.handle_request({
		"op": "submit",
		"command": {
			"type": "NoopCommand",
			"player_id": "p1",
			"tick": 0,
			"payload": pass_entry.get("payload", {}),
		},
	})
	assert_true(submit.get("ok", false), "submit noop ok")
	assert_true(submit.get("accepted", false), "noop accepted")
