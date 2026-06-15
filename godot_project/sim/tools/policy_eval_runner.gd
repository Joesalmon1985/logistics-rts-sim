class_name PolicyEvalRunner
extends RefCounted

## Runs one evaluation episode with a named baseline policy (RUN_06b).


static func run_episode(
	server: TrainingEnvServer,
	scenario_path: String,
	seed: int,
	policy_kind: String,
	actor_id: String = "p1",
	fog_enabled: int = -1,
	max_ticks_override: int = -1,
) -> Dictionary:
	var reset_req: Dictionary = {
		"op": "reset",
		"scenario_path": scenario_path,
		"seed": seed,
		"actor_id": actor_id,
	}
	if fog_enabled >= 0:
		reset_req["fog_enabled"] = fog_enabled

	var reset: Dictionary = server.handle_request(reset_req)
	if not reset.get("ok", false):
		return {"ok": false, "error": reset.get("error", "reset_failed")}

	var scenario: Dictionary = server.scenario
	var max_ticks: int = int(scenario.get("eval_max_ticks", 500))
	if max_ticks_override >= 0:
		max_ticks = max_ticks_override
	var rng := RandomNumberGenerator.new()
	rng.seed = seed * 17 + 3

	var command_log: Array = []
	var script_index: int = 0
	var heuristic_bot_kind: String = ""
	if policy_kind == "heuristic":
		if server.state.variant_id == "economy_trial":
			heuristic_bot_kind = "economy"
		elif server.state.variant_id == "military_trial":
			heuristic_bot_kind = "military"

	for _tick_i in range(max_ticks):
		var legal: Array = CommandAvailabilityProvider.enumerate_legal_commands(server.state, actor_id)
		if legal.is_empty():
			return {"ok": false, "error": "empty_legal_commands"}

		var chosen: Dictionary = {}
		if heuristic_bot_kind != "":
			var bot_result: Dictionary = server.handle_request({"op": "bot_decide", "bot_kind": heuristic_bot_kind})
			var cmd_dict: Variant = bot_result.get("command")
			if cmd_dict == null:
				chosen = PolicyBaselineSelector.select_command(
					"pass_only", server.state, actor_id, scenario, legal, rng, script_index,
				)
			else:
				chosen = _match_legal_dict(cmd_dict as Dictionary, legal, actor_id, server.state.tick)
		else:
			chosen = PolicyBaselineSelector.select_command(
				policy_kind, server.state, actor_id, scenario, legal, rng, script_index,
			)

		if chosen.is_empty():
			return {"ok": false, "error": "no_command_selected"}

		if PolicyBaselineSelector.should_advance_script(policy_kind, chosen, scenario, script_index):
			script_index += 1

		var step: Dictionary = server.handle_request({
			"op": "step",
			"include_policy_observation": false,
			"command": entry_to_command(chosen, actor_id, server.state.tick),
		})
		if not step.get("ok", false):
			return {"ok": false, "error": step.get("error", "step_failed")}

		if step.get("accepted", false) and str(chosen.get("type", "")) != "NoopCommand":
			var entries: Array = server.get_recorder().get_command_entries()
			if not entries.is_empty():
				command_log.append(entries[-1])

		var terminal: Dictionary = step.get("terminal", {})
		if bool(terminal.get("done", false)):
			break

		if _scenario_victory_reached(server.state, actor_id, scenario):
			break

	var episode: Dictionary = PolicyEpisodeResult.compute_episode_result(server.state, actor_id, scenario)
	var hash_val: String = ReplayHash.compute_state_hash(server.state)
	return {
		"ok": true,
		"policy_kind": policy_kind,
		"scenario_path": scenario_path,
		"seed": seed,
		"episode_result": episode,
		"command_log": command_log,
		"hash": hash_val,
		"tick": server.state.tick,
	}


static func _scenario_victory_reached(state: GameState, actor_id: String, scenario: Dictionary) -> bool:
	var result: Dictionary = PolicyEpisodeResult.compute_episode_result(state, actor_id, scenario)
	return str(result.get("outcome", "")) == "win"


static func _match_legal_dict(cmd_dict: Dictionary, legal: Array, actor_id: String, tick: int) -> Dictionary:
	var cmd_type: String = str(cmd_dict.get("type", ""))
	var payload: Dictionary = cmd_dict.get("payload", {}).duplicate(true)
	payload.erase("tick")
	for entry in legal:
		if str(entry.get("type", "")) != cmd_type:
			continue
		var entry_payload: Dictionary = entry.get("payload", {}).duplicate(true)
		entry_payload.erase("tick")
		if JSON.stringify(entry_payload) == JSON.stringify(payload):
			return entry
	return PolicyBaselineSelector.select_command(
		"pass_only", null, actor_id, {}, legal, RandomNumberGenerator.new(), 0,
	)


static func _match_legal(cmd: SimCommand, legal: Array, actor_id: String, tick: int) -> Dictionary:
	return _match_legal_dict(cmd.to_dict(), legal, actor_id, tick)


static func entry_to_command(entry: Dictionary, actor_id: String, tick: int) -> Dictionary:
	var payload: Dictionary = entry.get("payload", {}).duplicate(true)
	payload["tick"] = tick
	return {
		"type": str(entry.get("type", "")),
		"player_id": str(entry.get("actor_id", actor_id)),
		"tick": tick,
		"payload": payload,
	}
