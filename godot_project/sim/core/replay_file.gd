class_name ReplayFile
extends RefCounted

const SCHEMA_VERSION: int = 1


static func save_from_run(
	state: GameState,
	recorder: ReplayRecorder,
	scenario: Dictionary,
	path: String,
) -> bool:
	var payload := {
		"schema_version": SCHEMA_VERSION,
		"variant": str(scenario.get("variant", state.variant_id)),
		"scenario": scenario.duplicate(true),
		"seed": int(scenario.get("seed", state.seed)),
		"tick_count": state.tick,
		"commands": recorder.get_command_entries(),
		"expected_hash": ReplayHash.compute_state_hash(state),
	}
	return _write_json(path, payload)


static func load_from_path(path: String) -> Dictionary:
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_error("ReplayFile: cannot open %s" % path)
		return {}
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	if typeof(parsed) != TYPE_DICTIONARY:
		push_error("ReplayFile: invalid JSON in %s" % path)
		return {}
	return parsed as Dictionary


static func load_and_run(
	replay_path: String,
	validator: CommandValidator = null,
) -> Dictionary:
	var data: Dictionary = load_from_path(replay_path)
	if data.is_empty():
		return {"success": false, "error": "load_failed"}

	var scenario: Dictionary = data.get("scenario", {})
	if scenario.is_empty():
		return {"success": false, "error": "missing_scenario"}

	var tick_count: int = int(data.get("tick_count", 0))
	var expected_hash: String = str(data.get("expected_hash", ""))
	var commands: Array = data.get("commands", [])

	var runner := SimulationTickRunner.new()
	var cmd_validator: CommandValidator = validator if validator != null else CommandValidator.new(CommandRegistry.create_default())
	runner.command_validator = cmd_validator

	var state: GameState = runner.create_game(scenario)
	var commands_by_tick: Dictionary = {}
	for entry in commands:
		var tick: int = int(entry.get("tick", 0))
		if not commands_by_tick.has(tick):
			commands_by_tick[tick] = []
		commands_by_tick[tick].append(entry)

	for tick in range(tick_count):
		if commands_by_tick.has(tick):
			for entry in commands_by_tick[tick]:
				var cmd_dict: Dictionary = entry.get("command", {})
				var cmd_type: String = str(cmd_dict.get("type", ""))
				var payload: Dictionary = cmd_dict.get("payload", {}).duplicate(true)
				payload["player_id"] = str(cmd_dict.get("player_id", payload.get("player_id", "p1")))
				payload["tick"] = int(cmd_dict.get("tick", tick))

				var command: SimCommand = cmd_validator.registry.create(cmd_type, payload)
				if command == null:
					return {"success": false, "error": "unknown_command:%s" % cmd_type}

				var before_hash: String = ReplayHash.compute_state_hash(state)
				var result: CommandResult = cmd_validator.validate_and_apply(state, command)
				var expected_accepted: bool = bool(entry.get("accepted", false))
				if result.success != expected_accepted:
					return {
						"success": false,
						"error": "validation_mismatch:tick=%d type=%s expected=%s actual=%s"
						% [tick, cmd_type, expected_accepted, result.success],
					}
				if not result.success:
					var after_hash: String = ReplayHash.compute_state_hash(state)
					if before_hash != after_hash:
						return {"success": false, "error": "rejected_command_mutated_state:tick=%d" % tick}
		state = runner.tick(state)

	var final_hash: String = ReplayHash.compute_state_hash(state)
	if expected_hash != "" and not ReplayHash.matches(expected_hash, final_hash):
		return {
			"success": false,
			"error": "hash_mismatch",
			"expected_hash": expected_hash,
			"actual_hash": final_hash,
		}

	return {
		"success": true,
		"final_hash": final_hash,
		"expected_hash": expected_hash,
		"tick_count": tick_count,
	}


static func verify(path: String) -> Dictionary:
	return load_and_run(path)


static func _write_json(path: String, payload: Dictionary) -> bool:
	if not SimPaths.ensure_parent_dir(path):
		push_error("ReplayFile: cannot create parent dir for %s" % path)
		return false
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_error("ReplayFile: cannot write %s" % path)
		return false
	file.store_string(JSON.stringify(payload, "\t"))
	file.close()
	return true
