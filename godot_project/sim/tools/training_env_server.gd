class_name TrainingEnvServer
extends RefCounted

## Headless training env core (RUN_06 / DDR-033). Same command path as BotRunner.

const TrainingCommandMatchScript := preload("res://sim/tools/training_command_match.gd")
const RESPONSE_PREFIX: String = "@@RUN06_JSON@@"

var runner: SimulationTickRunner
var validator: CommandValidator
var state: GameState
var recorder: ReplayRecorder
var scenario: Dictionary = {}
var actor_id: String = "p1"
var _economy_bot: HeuristicEconomyBot
var _military_bot: HeuristicMilitaryBot
var _last_event_index: int = 0


func _init() -> void:
	runner = SimulationTickRunner.new()
	validator = CommandValidator.new(CommandRegistry.create_default())
	runner.command_validator = validator
	recorder = ReplayRecorder.new()
	_economy_bot = HeuristicEconomyBot.new()
	_military_bot = HeuristicMilitaryBot.new()


static func format_response(payload: Dictionary) -> String:
	return RESPONSE_PREFIX + JSON.stringify(payload)


func handle_request(req: Dictionary) -> Dictionary:
	var op: String = str(req.get("op", ""))
	match op:
		"reset":
			return _op_reset(req)
		"snapshot":
			return _op_snapshot(req)
		"submit":
			return _op_submit(req)
		"step":
			return _op_step(req)
		"tick":
			return _op_tick(req)
		"state_hash":
			return _op_state_hash()
		"save_replay":
			return _op_save_replay(req)
		"verify_replay":
			return _op_verify_replay(req)
		"bot_decide":
			return _op_bot_decide(req)
		"record_bc_episode":
			return _op_record_bc_episode(req)
		"rollout_stub":
			return _op_rollout_stub(req)
		"policy_observation":
			return _op_policy_observation(req)
		"legal_commands":
			return _op_legal_commands(req)
		"play_episode":
			return _op_play_episode(req)
		"episode_result":
			return _op_episode_result(req)
		"shutdown":
			return {"ok": true, "op": "shutdown"}
		_:
			return {"ok": false, "error": "unknown_op:%s" % op}


func _op_reset(req: Dictionary) -> Dictionary:
	var scenario_path: String = str(req.get("scenario_path", ""))
	if scenario_path == "":
		return {"ok": false, "error": "missing_scenario_path"}

	scenario = ConfigLoader.load_scenario(scenario_path)
	if scenario.is_empty():
		return {"ok": false, "error": "scenario_load_failed:%s" % scenario_path}

	if req.has("seed"):
		scenario["seed"] = int(req.get("seed", scenario.get("seed", 0)))
	if req.has("fog_enabled"):
		scenario["fog_enabled"] = int(req.get("fog_enabled", scenario.get("fog_enabled", 1)))

	actor_id = str(req.get("actor_id", "p1"))
	state = runner.create_game(scenario)
	if state.variant_id == "":
		return {"ok": false, "error": "create_game_failed"}

	recorder.clear()
	_last_event_index = 0
	_economy_bot.reset(actor_id)
	_military_bot.reset(actor_id)

	return {
		"ok": true,
		"op": "reset",
		"variant": state.variant_id,
		"tick": state.tick,
		"seed": state.seed,
		"snapshot": SnapshotExporter.export_training_snapshot(state, actor_id),
		"policy_observation": PolicyObservationExporter.export_policy_observation(state, actor_id),
		"legal_commands": CommandAvailabilityProvider.enumerate_legal_commands(state, actor_id),
	}


func _op_policy_observation(_req: Dictionary) -> Dictionary:
	if state == null:
		return {"ok": false, "error": "not_reset"}
	return {
		"ok": true,
		"op": "policy_observation",
		"observation": PolicyObservationExporter.export_policy_observation(state, actor_id),
		"tick": state.tick,
	}


func _op_legal_commands(_req: Dictionary) -> Dictionary:
	if state == null:
		return {"ok": false, "error": "not_reset"}
	return {
		"ok": true,
		"op": "legal_commands",
		"legal_commands": CommandAvailabilityProvider.enumerate_legal_commands(state, actor_id),
		"tick": state.tick,
	}


func _op_play_episode(req: Dictionary) -> Dictionary:
	if req.has("scenario_path") or state == null:
		var reset_result: Dictionary = _op_reset(req)
		if not reset_result.get("ok", false):
			return reset_result

	var policy_kind: String = str(req.get("policy_kind", "pass_only"))
	var max_ticks: int = int(req.get("max_ticks", scenario.get("eval_max_ticks", 500)))
	var rng_seed: int = int(req.get("policy_seed", int(scenario.get("seed", 1))))

	var rng := RandomNumberGenerator.new()
	rng.seed = rng_seed
	var command_log: Array = []
	var script_index: int = 0
	var heuristic_bot_kind: String = ""
	if policy_kind == "heuristic":
		if state.variant_id == "economy_trial":
			heuristic_bot_kind = "economy"
		elif state.variant_id == "military_trial":
			heuristic_bot_kind = "military"

	for _i in range(max_ticks):
		var legal: Array = CommandAvailabilityProvider.enumerate_legal_commands(state, actor_id)
		if legal.is_empty():
			return {"ok": false, "error": "empty_legal_commands", "op": "play_episode"}

		var chosen: Dictionary = {}
		if heuristic_bot_kind != "":
			var bot_result: Dictionary = _op_bot_decide({"bot_kind": heuristic_bot_kind})
			var cmd_dict: Variant = bot_result.get("command")
			if cmd_dict == null:
				chosen = PolicyBaselineSelector.select_command(
					"pass_only", state, actor_id, scenario, legal, rng, script_index,
				)
			else:
				chosen = PolicyEvalRunner._match_legal_dict(cmd_dict as Dictionary, legal, actor_id, state.tick)
		else:
			chosen = PolicyBaselineSelector.select_command(
				policy_kind, state, actor_id, scenario, legal, rng, script_index,
			)

		if chosen.is_empty():
			return {"ok": false, "error": "no_command_selected", "op": "play_episode"}

		if PolicyBaselineSelector.should_advance_script(policy_kind, chosen, scenario, script_index):
			script_index += 1

		var step: Dictionary = _op_step({
			"include_policy_observation": false,
			"command": PolicyEvalRunner.entry_to_command(chosen, actor_id, state.tick),
		})
		if not step.get("ok", false):
			return step
		if step.get("accepted", false) and str(chosen.get("type", "")) != "NoopCommand":
			var entries: Array = recorder.get_command_entries()
			if not entries.is_empty():
				command_log.append(entries[-1])

		if bool(step.get("terminal", {}).get("done", false)):
			break
		if PolicyEvalRunner._scenario_victory_reached(state, actor_id, scenario):
			break

	var episode_result: Dictionary = PolicyEpisodeResult.compute_episode_result(state, actor_id, scenario)
	return {
		"ok": true,
		"op": "play_episode",
		"policy_kind": policy_kind,
		"episode_result": episode_result,
		"command_log": command_log,
		"hash": ReplayHash.compute_state_hash(state),
		"tick": state.tick,
		"legal_commands": CommandAvailabilityProvider.enumerate_legal_commands(state, actor_id),
		"policy_observation": PolicyObservationExporter.export_policy_observation(state, actor_id),
	}


func _op_snapshot(_req: Dictionary) -> Dictionary:
	if state == null:
		return {"ok": false, "error": "not_reset"}
	var snap: Dictionary = SnapshotExporter.export_debug_snapshot(state, actor_id)
	return {
		"ok": true,
		"op": "snapshot",
		"snapshot": snap,
		"tick": state.tick,
	}


func _op_submit(req: Dictionary) -> Dictionary:
	if state == null:
		return {"ok": false, "error": "not_reset"}

	var cmd_dict: Dictionary = req.get("command", {})
	if typeof(cmd_dict) != TYPE_DICTIONARY or cmd_dict.is_empty():
		return {"ok": false, "error": "missing_command"}

	var cmd_type: String = str(cmd_dict.get("type", ""))
	var payload: Dictionary = cmd_dict.get("payload", {}).duplicate(true)
	payload["player_id"] = str(cmd_dict.get("player_id", payload.get("player_id", actor_id)))
	payload["tick"] = int(cmd_dict.get("tick", state.tick))

	var command: SimCommand = validator.registry.create(cmd_type, payload)
	if command == null:
		return {"ok": false, "error": "unknown_command:%s" % cmd_type}

	var result: CommandResult = validator.validate_and_apply(state, command)
	recorder.record(state.tick, command, result)

	return {
		"ok": true,
		"op": "submit",
		"accepted": result.success,
		"error_code": "" if result.success else str(result.error_code),
		"result": result.to_dict(),
		"events": _drain_new_events(),
		"tick": state.tick,
	}


func _op_step(req: Dictionary) -> Dictionary:
	if state == null:
		return {"ok": false, "error": "not_reset"}

	var accepted: bool = false
	if req.has("command") and typeof(req.get("command")) == TYPE_DICTIONARY:
		var submit_result: Dictionary = _op_submit(req)
		if not submit_result.get("ok", false):
			return submit_result
		accepted = bool(submit_result.get("accepted", false))

	var tick_result: Dictionary = _op_tick({})
	if not tick_result.get("ok", false):
		return tick_result

	var include_policy_obs: bool = bool(req.get("include_policy_observation", true))
	var snapshot: Dictionary = SnapshotExporter.export_training_snapshot(state, actor_id)
	var response: Dictionary = {
		"ok": true,
		"op": "step",
		"accepted": accepted,
		"snapshot": snapshot,
		"legal_commands": CommandAvailabilityProvider.enumerate_legal_commands(state, actor_id),
		"tick": state.tick,
		"events": tick_result.get("events", []),
		"terminal": tick_result.get("terminal", {}),
	}
	if include_policy_obs:
		response["policy_observation"] = PolicyObservationExporter.export_policy_observation(state, actor_id)
	return response


func _op_episode_result(_req: Dictionary) -> Dictionary:
	if state == null:
		return {"ok": false, "error": "not_reset"}
	return {
		"ok": true,
		"op": "episode_result",
		"episode_result": PolicyEpisodeResult.compute_episode_result(state, actor_id, scenario),
		"tick": state.tick,
	}


func _op_tick(_req: Dictionary) -> Dictionary:
	if state == null:
		return {"ok": false, "error": "not_reset"}

	state = runner.tick(state)
	return {
		"ok": true,
		"op": "tick",
		"tick": state.tick,
		"events": _drain_new_events(),
		"terminal": _terminal_flags(),
	}


func _op_state_hash() -> Dictionary:
	if state == null:
		return {"ok": false, "error": "not_reset"}
	return {
		"ok": true,
		"op": "state_hash",
		"hash": ReplayHash.compute_state_hash(state),
		"tick": state.tick,
	}


func _op_save_replay(req: Dictionary) -> Dictionary:
	if state == null:
		return {"ok": false, "error": "not_reset"}

	var path: String = str(req.get("path", ""))
	if path == "":
		path = SimPaths.default_export_file("training_replay.json")

	var ok: bool = ReplayFile.save_from_run(state, recorder, scenario, path)
	if not ok:
		return {"ok": false, "error": "save_failed:%s" % path}

	return {
		"ok": true,
		"op": "save_replay",
		"path": path,
		"hash": ReplayHash.compute_state_hash(state),
		"command_count": recorder.get_command_entries().size(),
	}


func _op_verify_replay(req: Dictionary) -> Dictionary:
	var path: String = str(req.get("path", ""))
	if path == "":
		return {"ok": false, "error": "missing_path"}

	var result: Dictionary = ReplayFile.verify(path)
	return {
		"ok": bool(result.get("success", false)),
		"op": "verify_replay",
		"verify": result,
	}


func _op_bot_decide(req: Dictionary) -> Dictionary:
	if state == null:
		return {"ok": false, "error": "not_reset"}

	var bot_kind: String = str(req.get("bot_kind", ""))
	var bot: Bot = null
	match bot_kind:
		"economy":
			bot = _economy_bot
		"military":
			bot = _military_bot
		_:
			return {"ok": false, "error": "unknown_bot_kind:%s" % bot_kind}

	var snapshot: Dictionary = SnapshotExporter.export_debug_snapshot(state, actor_id)
	bot.observe(snapshot, actor_id)
	var command: SimCommand = bot.decide(state.tick)
	var cmd_dict: Variant = null
	if command != null:
		cmd_dict = command.to_dict()

	return {
		"ok": true,
		"op": "bot_decide",
		"command": cmd_dict,
		"available_commands": snapshot.get("available_commands", []),
		"tick": state.tick,
	}


func _op_record_bc_episode(req: Dictionary) -> Dictionary:
	var reset_result: Dictionary = _op_reset(req)
	if not reset_result.get("ok", false):
		return reset_result

	var bot_kind: String = str(req.get("bot_kind", ""))
	var max_ticks: int = int(req.get("max_ticks", 1000))
	var bot: Bot = null
	match bot_kind:
		"economy":
			bot = _economy_bot
		"military":
			bot = _military_bot
		_:
			return {"ok": false, "error": "unknown_bot_kind:%s" % bot_kind}

	var steps: Array = []
	var matched: int = 0
	var unmatched: int = 0
	var noop: int = 0

	for _i in range(max_ticks):
		var snapshot: Dictionary = SnapshotExporter.export_debug_snapshot(state, actor_id)
		bot.observe(snapshot, actor_id)
		var command: SimCommand = bot.decide(state.tick)
		var available: Array = snapshot.get("available_commands", [])
		var label: int = TrainingCommandMatchScript.find_index(
			command, available, validator.registry, state.variant_id
		)
		if label < 0 and command != null:
			available.append(TrainingCommandMatchScript.availability_entry_for_command(command))
			label = available.size() - 1
			snapshot["available_commands"] = available

		if label < 0:
			unmatched += 1
		else:
			matched += 1
			if label == TrainingCommandMatchScript.noop_index(state.variant_id):
				noop += 1
			steps.append({
				"snapshot": snapshot,
				"label": label,
			})

		if command != null:
			var result: CommandResult = validator.validate_and_apply(state, command)
			recorder.record(state.tick, command, result)
			if bot.has_method("on_command_applied"):
				bot.on_command_applied(command, result)
			if not result.success and str(result.error_code) != "":
				pass

		var tick_result: Dictionary = _op_tick({})
		if not tick_result.get("ok", false):
			return tick_result
		if bool(tick_result.get("terminal", {}).get("done", false)):
			break

	var dataset_path: String = str(req.get("dataset_path", ""))
	if dataset_path != "":
		var dataset := {
			"variant": state.variant_id,
			"steps": steps,
		}
		if not SimPaths.write_text_file(dataset_path, JSON.stringify(dataset)):
			return {"ok": false, "error": "dataset_write_failed:%s" % dataset_path}

	return {
		"ok": true,
		"op": "record_bc_episode",
		"step_count": steps.size(),
		"dataset_path": dataset_path,
		"stats": {
			"matched": matched,
			"unmatched": unmatched,
			"noop": noop,
			"match_rate": float(matched) / float(max(1, matched + unmatched)),
		},
	}


func _op_rollout_stub(req: Dictionary) -> Dictionary:
	var reset_result: Dictionary = _op_reset(req)
	if not reset_result.get("ok", false):
		return reset_result

	var policy_seed: int = int(req.get("policy_seed", 7))
	var max_ticks: int = int(req.get("max_ticks", 100))
	var rng := RandomNumberGenerator.new()
	rng.seed = policy_seed

	var command_log: Array = []
	for _i in range(max_ticks):
		var snapshot: Dictionary = SnapshotExporter.export_debug_snapshot(state, actor_id)
		var available: Array = snapshot.get("available_commands", [])
		var noop: int = TrainingCommandMatchScript.noop_index(state.variant_id)
		var chosen_index: int = rng.randi_range(0, noop)
		if chosen_index < available.size():
			var entry: Dictionary = available[chosen_index]
			var payload: Dictionary = entry.get("payload", {}).duplicate(true)
			var submit_req := {
				"op": "submit",
				"command": {
					"type": str(entry.get("type", "")),
					"player_id": str(entry.get("actor_id", actor_id)),
					"tick": state.tick,
					"payload": payload,
				},
			}
			var submit_result: Dictionary = _op_submit(submit_req)
			if submit_result.get("accepted", false):
				command_log.append(recorder.get_command_entries()[-1])

		var tick_result: Dictionary = _op_tick({})
		var terminal: Dictionary = tick_result.get("terminal", {})
		if bool(terminal.get("done", false)):
			break

	return {
		"ok": true,
		"op": "rollout_stub",
		"command_log": command_log,
		"hash": ReplayHash.compute_state_hash(state),
		"tick": state.tick,
	}


func _drain_new_events() -> Array:
	var out: Array = []
	if state == null:
		return out
	while _last_event_index < state.event_log.events.size():
		out.append(state.event_log.events[_last_event_index].to_dict())
		_last_event_index += 1
	return out


func _terminal_flags() -> Dictionary:
	if state == null:
		return {"done": true, "reason": "no_state"}

	if state.variant_id == "military_trial":
		var decided: int = int(MilitaryState.get_meta_value(state, "victory_decided", 0))
		var episode: Dictionary = PolicyEpisodeResult.compute_episode_result(state, actor_id, scenario)
		return {
			"done": decided == 1,
			"reason": "victory" if decided == 1 else "",
			"victory_decided": decided,
			"episode_result": episode,
		}

	if state.variant_id == "economy_trial":
		var episode: Dictionary = PolicyEpisodeResult.compute_episode_result(state, actor_id, scenario)
		var done: bool = str(episode.get("outcome", "")) == "win"
		return {
			"done": done,
			"reason": str(episode.get("reason", "")),
			"episode_result": episode,
		}

	return {"done": false, "reason": ""}


func get_recorder() -> ReplayRecorder:
	return recorder


func get_state() -> GameState:
	return state
