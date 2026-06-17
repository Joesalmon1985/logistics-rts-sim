class_name EconomyHumanProofReplay
extends RefCounted

## Replay human/oracle proof attempts with prepare_expansion_duel_map applied.


static func run_replay_file(replay_path: String, validator: CommandValidator = null) -> Dictionary:
	var data: Dictionary = ReplayFile.load_from_path(replay_path)
	if data.is_empty():
		return {"ok": false, "error": "load_failed"}
	var scenario: Dictionary = data.get("scenario", {})
	if scenario.is_empty():
		return {"ok": false, "error": "missing_scenario"}
	var commands: Array = data.get("commands", [])
	var runner := SimulationTickRunner.new()
	var cmd_validator: CommandValidator = validator if validator != null else CommandValidator.new(CommandRegistry.create_default())
	runner.command_validator = cmd_validator
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	var replay_result: Dictionary = _replay_commands(runner, cmd_validator, state, commands, int(data.get("tick_count", 0)))
	if not bool(replay_result.get("ok", false)):
		return replay_result
	state = replay_result.get("state")
	var gates: Dictionary = EconomyHumanProofContract.evaluate_match_gates(state)
	return {
		"ok": true,
		"state": state,
		"gates": gates,
		"integrity": replay_result,
	}


static func run_oracle_plan(plan: Dictionary) -> Dictionary:
	var oracle := EconomyHumanProofOracleRunner.new()
	oracle.reset()
	var summary: Dictionary = oracle.run_plan(plan)
	return {
		"ok": true,
		"summary": summary,
		"gates": summary.get("gates", {}),
		"recorder": oracle.recorder,
	}


static func export_oracle_run(oracle: EconomyHumanProofOracleRunner, replay_path: String) -> bool:
	return oracle.export_replay(replay_path)


static func _replay_commands(
	runner: SimulationTickRunner,
	validator: CommandValidator,
	state: GameState,
	commands: Array,
	target_tick: int,
) -> Dictionary:
	var by_tick: Dictionary = {}
	for entry in commands:
		var tick: int = int(entry.get("tick", 0))
		if not by_tick.has(tick):
			by_tick[tick] = []
		by_tick[tick].append(entry)
	var max_tick: int = target_tick
	if max_tick <= 0:
		for entry in commands:
			max_tick = maxi(max_tick, int(entry.get("tick", 0)))
		max_tick += 500
	max_tick = mini(max_tick, int(EconomyHumanProofContract.proof_scenario().get("max_ticks", 800)))
	for tick in range(max_tick + 1):
		if by_tick.has(tick):
			for entry in by_tick[tick]:
				var cmd_dict: Dictionary = entry.get("command", {})
				var cmd_type: String = str(cmd_dict.get("type", ""))
				var payload: Dictionary = cmd_dict.get("payload", {}).duplicate(true)
				payload["player_id"] = str(cmd_dict.get("player_id", payload.get("player_id", "p1")))
				payload["tick"] = int(cmd_dict.get("tick", tick))
				var command: SimCommand = validator.registry.create(cmd_type, payload)
				if command == null and cmd_type == "NoopCommand":
					command = NoopCommand.new(payload["player_id"], tick)
				if command == null:
					return {"ok": false, "error": "unknown_command:%s" % cmd_type}
				var before_hash: String = ReplayHash.compute_state_hash(state)
				var result: CommandResult = validator.validate_and_apply(state, command)
				var expected_accepted: bool = bool(entry.get("accepted", false))
				if result.success != expected_accepted:
					return {
						"ok": false,
						"error": "validation_mismatch:tick=%d type=%s" % [tick, cmd_type],
					}
				if not result.success:
					var after_hash: String = ReplayHash.compute_state_hash(state)
					if before_hash != after_hash:
						return {"ok": false, "error": "rejected_mutated_state:tick=%d" % tick}
		state = runner.tick(state)
	return {"ok": true, "state": state, "tick": state.tick}
